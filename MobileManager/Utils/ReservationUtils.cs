using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using MobileManager.Http.Clients;
using MobileManager.Http.Clients.Interfaces;
using MobileManager.Logging.Logger;
using MobileManager.Models.Devices;
using MobileManager.Models.Devices.Interfaces;
using MobileManager.Models.Reservations;
using MobileManager.Models.Reservations.enums;
using MobileManager.Services;
using Newtonsoft.Json;

namespace MobileManager.Utils
{
    /// <summary>
    /// Reservation utils.
    /// </summary>
    public class ReservationUtils
    {
        private readonly IManagerLogger _logger;
        private readonly DeviceUtils _deviceUtils;
        private readonly AppiumService _appiumService;
        private readonly IRestClient _restClient;

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="deviceUtils"></param>
        /// <param name="appiumService"></param>
        /// <param name="restClient"></param>
        public ReservationUtils(IManagerLogger logger, DeviceUtils deviceUtils, AppiumService appiumService, IRestClient restClient)
        {
            _logger = logger;
            _deviceUtils = deviceUtils;
            _appiumService = appiumService;
            _restClient = restClient;
        }

        /// <summary>
        /// Unlocks all reserved devices in failed reservation.
        /// </summary>
        /// <param name="reservation"></param>
        /// <param name="reservedDevices"></param>
        /// <returns></returns>
        public async Task UnlockAllReservedDevicesForFailedReservation(Reservation reservation, List<ReservedDevice> reservedDevices)
        {
            _logger.Error(
                $"Applying reservation - failed to lock all requested devices: {JsonConvert.SerializeObject(reservedDevices)}");
            foreach (var reservedDevice in reservedDevices)
            {
                _logger.Debug($"Applying reservation - unlock device: {JsonConvert.SerializeObject(reservedDevice)}");

                await _deviceUtils.UnlockDeviceByReservationType(reservation, reservedDevice, _restClient, _appiumService);
            }
        }

        public async Task<ICollection<ReservationApplied>> AddAppliedReservation(
            ICollection<ReservationApplied> appliedReservations, Reservation reservation,
            List<ReservedDevice> reservedDevices)
        {
            _logger.Debug(
                $"Applying reservation - all devices are locked: {JsonConvert.SerializeObject(reservedDevices)}");
            var reservationToBeApplied = new ReservationApplied(reservation.Id)
            {
                ReservedDevices = reservedDevices,
                DateCreated = reservation.DateCreated,
                ReservationType = reservation.ReservationType
            };

            _logger.Debug(
                $"Applying reservation - adding reservation applied: {JsonConvert.SerializeObject(reservationToBeApplied)}");
            var appliedReservation = await _restClient.ApplyReservation(reservationToBeApplied);
            appliedReservations.Add(appliedReservation);

            return appliedReservations;
        }

        public async Task ReserveAllRequestedDevices(Reservation reservation, List<ReservedDevice> reservedDevices)
        {
            foreach (var requestedDevice in reservation.RequestedDevices)
            {
                //var device = await RestClient.GetDevice(requestedDevice.DeviceId);

                var device = await _deviceUtils.FindMatchingDevice(requestedDevice, _restClient);

                if (device == null)
                {
                    _logger.Error("Device " + JsonConvert.SerializeObject(requestedDevice) + " not found.");
                    continue;
                }

                _logger.Debug($"Applying reservation - device is found: {JsonConvert.SerializeObject(device)}");

                if (await IsDeviceAvailable(device))
                {
                    _logger.Debug($"Applying reservation - locking device: {JsonConvert.SerializeObject(device)}");

                    await LockDeviceByReservationType(reservation, reservedDevices, device);
                }
            }
        }

        private async Task LockDeviceByReservationType(Reservation reservation, List<ReservedDevice> reservedDevices,
            Device device)
        {
            switch (reservation.ReservationType)
            {
                case ReservationType.Appium:
                    _logger.Debug(
                        $"{nameof(ReserveAllRequestedDevices)}: reservation [{reservation.Id}] [{ReservationType.Appium}].");
                    await LockDeviceWithAppiumEndpoint(reservation, reservedDevices, device);
                    break;
                case ReservationType.XcuiTest:
                    _logger.Debug(
                        $"{nameof(ReserveAllRequestedDevices)}: reservation [{reservation.Id}] [{ReservationType.XcuiTest}].");
                    await LockDeviceWithXcuiTest(reservation, reservedDevices, device);
                    break;
                case ReservationType.Manual:
                    _logger.Debug(
                        $"{nameof(ReserveAllRequestedDevices)}: reservation [{reservation.Id}] [{ReservationType.Manual}].");
                    await LockDeviceWithManual(reservation, reservedDevices, device);
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private async Task LockDeviceWithAppiumEndpoint(Reservation reservation, List<ReservedDevice> reservedDevices,
            Device device)
        {
            try
            {
                var updatedDevice = await _deviceUtils.LockDeviceAppium(device.Id, _restClient, _appiumService);
                if (!updatedDevice.Available)
                {
                    _logger.Debug(
                        $"{nameof(LockDeviceWithAppiumEndpoint)}: Applying reservation - failed to lock device: {JsonConvert.SerializeObject(updatedDevice)}");
                    var reservedDevice = new ReservedDevice(updatedDevice);
                    reservedDevices.Add(reservedDevice);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to apply reservation {reservation.Id}", e);
                reservation.FailedToApply++;
                await _restClient.UpdateReservation(reservation);
            }
        }

        private async Task LockDeviceWithXcuiTest(Reservation reservation, List<ReservedDevice> reservedDevices,
            Device device)
        {
            try
            {
                var updatedDevice = await _deviceUtils.LockDeviceXcuiTest(device.Id, _restClient);
                if (!updatedDevice.Available)
                {
                    _logger.Debug(
                        $"{nameof(LockDeviceWithAppiumEndpoint)}: Applying reservation - failed to lock device: {JsonConvert.SerializeObject(updatedDevice)}");
                    var reservedDevice = new ReservedDevice(updatedDevice);
                    reservedDevices.Add(reservedDevice);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to apply reservation {reservation.Id}", e);
                reservation.FailedToApply++;
                await _restClient.UpdateReservation(reservation);
            }
        }

        private async Task LockDeviceWithManual(Reservation reservation, List<ReservedDevice> reservedDevices,
            Device device)
        {
            try
            {
                var updatedDevice = await _deviceUtils.LockDeviceManual(device.Id, _restClient);
                if (!updatedDevice.Available)
                {
                    _logger.Debug(
                        $"{nameof(LockDeviceWithAppiumEndpoint)}: Applying reservation - failed to lock device: {JsonConvert.SerializeObject(updatedDevice)}");
                    var reservedDevice = new ReservedDevice(updatedDevice);
                    reservedDevices.Add(reservedDevice);
                }
            }
            catch (Exception e)
            {
                _logger.Error($"Failed to apply reservation {reservation.Id}", e);
                reservation.FailedToApply++;
                await _restClient.UpdateReservation(reservation);
            }
        }

        public async Task<bool> IsReservationEligible(Reservation reservation)
        {
            var reservationAligable = true;
            _logger.Debug(
                $"Applying reservation - contains multiple devices: {JsonConvert.SerializeObject(reservation.RequestedDevices)}");

            var listOfLockedDevices = new List<Device>();

            foreach (var requestedDevice in reservation.RequestedDevices)
            {
                var device = await _deviceUtils.FindMatchingDevice(requestedDevice, _restClient);

                if (device == null)
                {
                    _logger.Error("Device " + JsonConvert.SerializeObject(requestedDevice) + " not found.");
                    reservationAligable = false;
                    continue;
                }

                if (!device.Available)
                {
                    _logger.Debug(
                        $"Applying reservation - device is not available: {device.Id}");
                    reservationAligable = false;
                }
                else
                {
                    device.Available = false;
                    listOfLockedDevices.Add(await _restClient.UpdateDevice(device));
                    _logger.Debug($"Applying reservation - device is available: {device.Id}");
                }
            }

            if (!reservationAligable)
            {
                var tasks = new List<Task>();

                foreach (var lockedDevice in listOfLockedDevices)
                {
                    lockedDevice.Available = true;
                    tasks.Add(_restClient.UpdateDevice(lockedDevice));
                }

                await Task.WhenAll(tasks);
            }

            return reservationAligable;
        }

        private async Task<bool> IsDeviceAvailable(IDevice device)
        {
            var dev = await _restClient.GetDevice(device.Id);
            return dev.Available;
        }

    }
}
