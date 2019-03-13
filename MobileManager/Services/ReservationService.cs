using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Hosting;
using MobileManager.Configuration;
using MobileManager.Configuration.ConfigurationProvider;
using MobileManager.Configuration.Interfaces;
using MobileManager.Http.Clients;
using MobileManager.Logging.Logger;
using MobileManager.Models.Devices;
using MobileManager.Models.Devices.Interfaces;
using MobileManager.Models.Reservations;
using MobileManager.Models.Reservations.enums;
using MobileManager.Models.Reservations.Interfaces;
using MobileManager.Services.Interfaces;
using MobileManager.Utils;
using Newtonsoft.Json;

namespace MobileManager.Services
{
    /// <inheritdoc cref="IReservationService" />
    /// <summary>
    /// Reservation service.
    /// </summary>
    public class ReservationService : IReservationService, IHostedService, IDisposable
    {
        private readonly IManagerLogger _logger;

        private readonly RestClient _restClient;
        private readonly AppiumService _appiumService;
        private Task _reservationService;
        private readonly DeviceUtils _deviceUtils;
        private readonly ReservationUtils _reservationUtils;


        /// <summary>
        /// Initializes a new instance of the <see cref="T:MobileManager.Services.ReservationService"/> class.
        /// </summary>
        public ReservationService(IManagerConfiguration configuration, IManagerLogger logger,
            IExternalProcesses externalProcesses)
        {
            _logger = logger;
            var externalProcesses1 = externalProcesses;
            _deviceUtils = new DeviceUtils(_logger, externalProcesses1);
            _restClient = new RestClient(configuration, _logger);
            _appiumService = new AppiumService(configuration, logger, externalProcesses1);
            _reservationUtils = new ReservationUtils(_logger,_deviceUtils,_appiumService,_restClient);
        }

        /// <inheritdoc />
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _reservationService =
                Task.Factory.StartNew(async () => { await RunAsyncApplyReservationTaskAsync(cancellationToken); },
                    cancellationToken);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public Task StopAsync(CancellationToken cancellationToken)
        {
            _reservationService.Wait(cancellationToken);
            return Task.CompletedTask;
        }

        /// <inheritdoc />
        public void Dispose()
        {
            _reservationService?.Dispose();
        }

        /// <inheritdoc />
        /// <summary>
        /// Runs the async apply reservation task async.
        /// </summary>
        public async Task RunAsyncApplyReservationTaskAsync(CancellationToken cancellationToken)
        {
            _logger.Info("RunAsyncApplyReservationTaskAsync Thread Started.");
            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var applied = false;
                    if (await _restClient.TryToConnect())
                    {
                        _logger.Info("ApplyAvailableReservations [START]");
                        try
                        {
                            applied = await ApplyAvailableReservations();
                        }
                        catch (Exception e)
                        {
                            _logger.Info("ApplyAvailableReservations: " + e.Message + " [ERROR]");
                        }

                        _logger.Info("ApplyAvailableReservations: " + applied + " [STOP]");

                        Thread.Sleep((await _restClient.GetManagerConfiguration()).ReservationServiceRefreshTime);
                    }
                    else
                    {
                        _logger.Error("ApplyAvailableReservations: Failed connecting to " + _restClient.Endpoint +
                                      " [STOP]");
                        var sleep = AppConfigurationProvider.Get<ManagerConfiguration>().GlobalReconnectTimeout;
                        _logger.Info("ApplyAvailableReservations Sleep for [ms]: " + sleep);
                        Thread.Sleep(sleep);
                        _logger.Info("ApplyAvailableReservations Sleep finished");
                    }
                }
                catch (Exception e)
                {
                    _logger.Error("Exception during RunAsyncApplyReservationTaskAsync.", e);
                }
            }

            _logger.Info($"{nameof(RunAsyncApplyReservationTaskAsync)} STOP.");
        }

        /// <inheritdoc />
        /// <summary>
        /// Applies the available reservations.
        /// </summary>
        /// <returns>The available reservations.</returns>

        //todo refactor - split into multiple methods
        public async Task<bool> ApplyAvailableReservations()
        {
            var reservationQueue = await LoadReservationQueue();
            var appliedReservations = new List<ReservationApplied>();
            foreach (var reservation in reservationQueue)
            {
                _logger.Info($"Applying reservation - item in queue: {JsonConvert.SerializeObject(reservation)}");
                var reservedDevices = new List<ReservedDevice>();

                if (reservation.RequestedDevices == null)
                {
                    throw new NullReferenceException("RequestedDevices property on reservation is empty.");
                }

                var reservationEligible = true;
                if (reservation.RequestedDevices.Count > 1)
                {
                    reservationEligible = await _reservationUtils.IsReservationEligible(reservation);
                }

                if (reservationEligible)
                {
                    await _reservationUtils.ReserveAllRequestedDevices(reservation, reservedDevices);
                }
                else
                {
                    _logger.Debug($"Reservation is not eligible. {JsonConvert.SerializeObject(reservation)}");
                    continue;
                }

                if (reservedDevices.Count == reservation.RequestedDevices.Count)
                {
                    try
                    {
                        var reservationApplied = await _reservationUtils.AddAppliedReservation(appliedReservations, reservation,
                            reservedDevices);
                        appliedReservations.AddRange(reservationApplied);
                    }
                    catch (Exception e)
                    {
                        _logger.Error($"Failed to AddAppliedReservation.", e);
                        await _reservationUtils.UnlockAllReservedDevicesForFailedReservation(reservation, reservedDevices);
                        continue;
                    }

                    _logger.Info(
                        $"Applying reservation - removing reservation from queue: {JsonConvert.SerializeObject(reservation)}");
                    await _restClient.DeleteReservation(reservation.Id);
                }
                else if (reservedDevices.Any())
                {
                    await _reservationUtils.UnlockAllReservedDevicesForFailedReservation(reservation, reservedDevices);
                }
            }

            _logger.Info(
                $"Applying reservation - applied reservations: {JsonConvert.SerializeObject(appliedReservations)}");
            return appliedReservations.Any();
        }

        /// <inheritdoc />
        /// <summary>
        /// Loads the reservation queue.
        /// </summary>
        /// <returns>The reservation queue.</returns>
        public async Task<IEnumerable<Reservation>> LoadReservationQueue()
        {
            return await _restClient.GetReservations();
        }

        #region privateMethods



        #endregion
    }
}
