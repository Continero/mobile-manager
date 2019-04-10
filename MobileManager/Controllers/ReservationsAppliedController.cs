﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MobileManager.Controllers.Interfaces;
using MobileManager.Database.Repositories.Interfaces;
using MobileManager.Http.Clients.Interfaces;
using MobileManager.Logging.Logger;
using MobileManager.Models.Reservations;
using MobileManager.Services;
using MobileManager.Services.Interfaces;
using MobileManager.Utils;
using Newtonsoft.Json;

namespace MobileManager.Controllers
{
    /// <inheritdoc cref="IReservationsAppliedController" />
    /// <summary>
    /// Reservations applied controller.
    /// </summary>
    [Route("api/v1/reservation/applied")]
    [EnableCors("AllowAllHeaders")]
    public class ReservationsAppliedController : ControllerExtensions, IReservationsAppliedController
    {
        private readonly IRepository<ReservationApplied> _reservationsAppliedRepository;
        private readonly IRestClient _restClient;
        private readonly IAppiumService _appiumService;
        private readonly IManagerLogger _logger;
        private readonly IDeviceUtils _deviceUtils;
        private readonly IExternalProcesses _externalProcesses;

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:MobileManager.Controllers.ReservationsAppliedController" /> class.
        /// </summary>
        /// <param name="reservationsAppliedRepository">Reservations applied repository.</param>
        /// <param name="restClient">Rest client.</param>
        /// <param name="appiumService">Appium service.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="externalProcesses"></param>
        /// <param name="deviceUtils"></param>
        public ReservationsAppliedController(IRepository<ReservationApplied> reservationsAppliedRepository,
            IRestClient restClient, IAppiumService appiumService, IManagerLogger logger,
            IExternalProcesses externalProcesses, IDeviceUtils deviceUtils) : base(logger)
        {
            _reservationsAppliedRepository = reservationsAppliedRepository;
            _restClient = restClient;
            _appiumService = appiumService;
            _logger = logger;
            _externalProcesses = externalProcesses;
            _deviceUtils = deviceUtils;
        }

        /// <summary>
        /// Gets all applied reservations.
        /// </summary>
        /// <returns>The all applied reservations.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<ReservationApplied>), StatusCodes.Status200OK)]
        public IEnumerable<ReservationApplied> GetAllAppliedReservations()
        {
            LogRequest();
            var reservations = _reservationsAppliedRepository.GetAll();
            _logger.Debug(
                string.Format("GetAll reservations applied: [{0}]", JsonConvert.SerializeObject(reservations)));
            return reservations;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the reservationApplied by identifier.
        /// </summary>
        /// <returns>ReservationApplied.</returns>
        /// <param name="id">ReservationApplied Identifier.</param>
        [HttpGet("{id}", Name = "getAppliedReservation")]
        [ProducesResponseType(typeof(ReservationApplied), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public IActionResult GetById(string id)
        {
            LogRequest();
            var reservationFromApplied = _reservationsAppliedRepository.Find(id);

            if (reservationFromApplied == null)
            {
                return NotFoundExtension("Reservation not found in the database.");
            }

            return JsonExtension(reservationFromApplied);
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates the specified reservation. [INTERNAL-ONLY]
        /// </summary>
        /// <returns>Created reservationApplied.</returns>
        /// <param name="reservation">ReservationApplied.</param>
        [HttpPost]
        [ProducesResponseType(typeof(ReservationApplied), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult Create([FromBody] ReservationApplied reservation)
        {
            LogRequest();
            if (reservation == null)
            {
                return BadRequestExtension("Reservation is empty.");
            }

            if (reservation.ReservedDevices == null || !reservation.ReservedDevices.Any())
            {
                return BadRequestExtension("RequestedDevices property is empty.");
            }

            try
            {
                _reservationsAppliedRepository.Add(reservation);
            }
            catch (Exception ex)
            {
                return StatusCodeExtension(500, "Failed to Add reservation in database. " + ex.Message);
            }

            _logger.Debug(string.Format("Created new applied reservation: [{0}]",
                JsonConvert.SerializeObject(reservation)));

            return CreatedAtRoute("getAppliedReservation", new {id = reservation.Id}, reservation);
        }

        /// <inheritdoc />
        /// <summary>
        /// Deletes the ReservationApplied.
        /// </summary>
        /// <returns>null.</returns>
        /// <param name="id">ReservationApplied Identifier.</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> DeleteAsync(string id)
        {
            LogRequest();
            var reservationFromApplied = _reservationsAppliedRepository.Find(id);

            if (reservationFromApplied == null)
            {
                return NotFoundExtension("Reservation not found in database.");
            }

            foreach (var reservedDevice in reservationFromApplied.ReservedDevices)
            {
                try
                {
                    await _deviceUtils.UnlockDeviceByReservationType(reservationFromApplied, reservedDevice, _restClient, _appiumService);
                }
                catch (Exception ex)
                {
                    _logger.Error($"Failed to remove reserved devices due to: {ex.Message}.", ex);
                    return StatusCodeExtension(500,
                        "Failed to unlock device id: " + reservedDevice.DeviceId + " from reservation.");
                }
            }


            try
            {
                _reservationsAppliedRepository.Remove(id);
            }
            catch (Exception ex)
            {
                return StatusCodeExtension(500, "Failed to Remove reservation from database. " + ex.Message);
            }

            return OkExtension();
        }
    }
}
