using System;
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
using MobileManager.Utils;
using Newtonsoft.Json;

#pragma warning disable 1998

namespace MobileManager.Controllers
{
    /// <inheritdoc cref="IReservationQueueController" />
    /// <summary>
    /// Reservations queue controller.
    /// </summary>
    [Route("api/v1/reservation")]
    [EnableCors("AllowAllHeaders")]
    public class ReservationsQueueController : ControllerExtensions, IReservationQueueController
    {
        private readonly IRepository<Reservation> _reservationsQueueRepository;
        private readonly IRestClient _restClient;
        private readonly IManagerLogger _logger;
        private readonly DeviceUtils _deviceUtils;
        private readonly IExternalProcesses _externalProcesses;


        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:MobileManager.Controllers.ReservationsQueueController" /> class.
        /// </summary>
        /// <param name="reservationsQueueRepository">Reservations queue repository.</param>
        /// <param name="restClient">Rest client.</param>
        /// <param name="logger">Logger.</param>
        /// <param name="externalProcesses"></param>
        public ReservationsQueueController(IRepository<Reservation> reservationsQueueRepository,
            IRestClient restClient, IManagerLogger logger, IExternalProcesses externalProcesses) : base(logger)
        {
            _reservationsQueueRepository = reservationsQueueRepository;
            _restClient = restClient;
            _logger = logger;
            _externalProcesses = externalProcesses;
            _deviceUtils = new DeviceUtils(_logger, _externalProcesses);
        }

        /// <summary>
        /// Gets all Reservations.
        /// </summary>
        /// <returns>Reservations.</returns>
        [HttpGet]
        [ProducesResponseType(typeof(IEnumerable<Reservation>), StatusCodes.Status200OK)]
        public IEnumerable<Reservation> GetAll()
        {
            LogRequest();
            var reservations = _reservationsQueueRepository.GetAll();
            _logger.Debug(string.Format("GetAll reservations queued: [{0}]",
                JsonConvert.SerializeObject(reservations)));
            return reservations;
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets the Reservation by identifier.
        /// </summary>
        /// <returns>The reservation identifier.</returns>
        /// <param name="id">Reservation.</param>
        [HttpGet("{id}", Name = "getQueueReservation")]
        [ProducesResponseType(typeof(Reservation), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        public IActionResult GetById(string id)
        {
            LogRequest();
            var reservationFromQueue = _reservationsQueueRepository.Find(id);

            if (reservationFromQueue == null)
            {
                return NotFoundExtension("Reservation not found in database.");
            }

            return JsonExtension(reservationFromQueue);
        }

        /// <inheritdoc />
        /// <summary>
        /// Creates the reservation.
        /// </summary>
        /// <returns>The reservation.</returns>
        /// <param name="reservation">Reservation.</param>
        [HttpPost]
        [ProducesResponseType(typeof(Reservation), StatusCodes.Status201Created)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public async Task<IActionResult> CreateAsync([FromBody] Reservation reservation)
        {
            LogRequest();
            if (reservation == null)
            {
                return BadRequestExtension("Reservation is empty.");
            }

            if (reservation.RequestedDevices == null || !reservation.RequestedDevices.Any())
            {
                return BadRequestExtension("RequestedDevices property is empty.");
            }

            if (reservation.RequestedDevices.Count >= 1)
            {
                var deviceIds = reservation.RequestedDevices.Where(arg => arg.DeviceId != null)
                    .Select(arg => arg.DeviceId);
                if (deviceIds.GroupBy(n => n).Any(c => c.Count() > 1))
                {
                    return BadRequestExtension("RequestedDevices property contains duplicate DeviceId.");
                }

                if (!ValidateRequestedDevices(reservation, out var actionResult))
                {
                    return actionResult;
                }
            }

            try
            {
                _reservationsQueueRepository.Add(reservation);
            }
            catch (Exception ex)
            {
                return StatusCodeExtension(500, "Failed to Add reservation in database. " + ex.Message);
            }

            _logger.Debug(string.Format("Created new queued reservation: [{0}]",
                JsonConvert.SerializeObject(reservation)));

            return CreatedAtRoute("getQueueReservation", new {id = reservation.Id}, reservation);
        }

        private bool ValidateRequestedDevices(Reservation reservation, out IActionResult actionResult)
        {
            var actionResults = new List<ActionResult>();
            foreach (var requestedDevice in reservation.RequestedDevices)
            {
                var res = _deviceUtils.FindMatchingDevice(requestedDevice, _restClient).Result;

                if (res != null)
                {
                    actionResults.Add(Ok());
                }
                else
                {
                    actionResults.Add(BadRequestExtension(JsonConvert.SerializeObject(requestedDevice,
                        new JsonSerializerSettings
                        {
                            NullValueHandling = NullValueHandling.Ignore
                        })));
                }
            }

            if (actionResults.OfType<BadRequestObjectResult>().Any())
            {
                var badRequests = actionResults.OfType<BadRequestObjectResult>().Select(req => req.Value);
                actionResult = BadRequestExtension("Requested device(s) not found in the device pool: " +
                                                   JsonConvert.SerializeObject(badRequests));
                return false;
            }

            actionResult = Ok();
            return true;
        }


        /// <inheritdoc />
        /// <summary>
        /// Delete the specified reservation by id.
        /// </summary>
        /// <returns>null.</returns>
        /// <param name="id">Reservation Identifier.</param>
        [HttpDelete("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult Delete(string id)
        {
            LogRequest();
            var reservationFromQueue = _reservationsQueueRepository.Find(id);

            if (string.IsNullOrWhiteSpace(id))
            {
                return BadRequestExtension("Empty id.");
            }

            if (reservationFromQueue == null)
            {
                return NotFoundExtension("Reservation not found in the database.");
            }

            try
            {
                _reservationsQueueRepository.Remove(id);
            }
            catch (Exception ex)
            {
                return StatusCodeExtension(500, "Failed to Remove reservation from database. " + ex.Message);
            }

            return OkExtension();
        }


        /// <summary>
        /// Update the specified device.
        /// </summary>
        /// <returns>The update.</returns>
        /// <param name="id">Identifier.</param>
        /// <param name="reservationUpdated">Device updated.</param>
        [HttpPut("{id}")]
        [ProducesResponseType(typeof(Reservation), StatusCodes.Status200OK)]
        [ProducesResponseType(typeof(string), StatusCodes.Status400BadRequest)]
        [ProducesResponseType(typeof(string), StatusCodes.Status404NotFound)]
        [ProducesResponseType(typeof(string), StatusCodes.Status500InternalServerError)]
        public IActionResult Update(string id, [FromBody] Reservation reservationUpdated)
        {
            LogRequest();
            if (reservationUpdated == null || reservationUpdated.Id != id)
            {
                return BadRequestExtension("Empty reservation in request");
            }

            var reservation = _reservationsQueueRepository.Find(id);
            if (reservation == null)
            {
                return NotFoundExtension("Reservation not found in database.");
            }

            try
            {
                _reservationsQueueRepository.Update(reservationUpdated);
            }
            catch (Exception ex)
            {
                return StatusCodeExtension(500, "Failed to Update reservation in database. " + ex.Message);
            }

            _logger.Debug(string.Format("Updated reservation: [{0}] to [{1}]", reservation,
                JsonConvert.SerializeObject(reservationUpdated)));


            return JsonExtension(reservationUpdated);
        }
    }
}
