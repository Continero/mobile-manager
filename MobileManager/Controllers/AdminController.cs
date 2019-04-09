using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MobileManager.Appium;
using MobileManager.Configuration.Interfaces;
using MobileManager.Controllers.Interfaces;
using MobileManager.Http.Clients.Interfaces;
using MobileManager.Logging.Logger;
using MobileManager.Models.Devices;
using MobileManager.Models.Reservations;

namespace MobileManager.Controllers
{
    /// <inheritdoc cref="IAdminController" />
    /// <summary>
    /// Admin controller
    /// </summary>
    [Route("api/v1/admin")]
    [EnableCors("AllowAllHeaders")]
    public class AdminController : ControllerExtensions, IAdminController
    {
        private readonly IRestClient _restClient;
        private readonly IManagerLogger _logger;

        /// <inheritdoc />
        /// <summary>
        /// Initializes a new instance of the <see cref="T:MobileManager.Controllers.AdminController" /> class.
        /// </summary>
        /// <param name="restClient">Rest client.</param>
        /// <param name="logger">Logger</param>
        public AdminController(IRestClient restClient, IManagerLogger logger) : base(logger)
        {
            _restClient = restClient;
            _logger = logger;
        }

        public class AllRepositoriesData
        {
            public IEnumerable<Device> Devices { get; }

            public IEnumerable<Reservation> Reservations { get; }

            public IEnumerable<ReservationApplied> ReservationsApplied { get; }

            public IManagerConfiguration Configuration { get; }

            public IEnumerable<AppiumProcess> AppiumProcesses { get; }

            public AllRepositoriesData(IEnumerable<Device> devices, IEnumerable<Reservation> reservations, IEnumerable<ReservationApplied> reservationsApplied, IManagerConfiguration configuration, IEnumerable<AppiumProcess> appiumProcesses)
            {
                Devices = devices;
                Reservations = reservations;
                ReservationsApplied = reservationsApplied;
                Configuration = configuration;
                AppiumProcesses = appiumProcesses;
            }
        }

        /// <inheritdoc />
        /// <summary>
        /// Gets all repositories.
        /// </summary>
        /// <returns>The all repositories.</returns>
        [HttpGet("repositories", Name = "getAllRepositories")]
        [ProducesResponseType(typeof(AllRepositoriesData), StatusCodes.Status200OK)]
        public async Task<IActionResult> GetAllRepositoriesAsync()
        {
            var resultDevices = await _restClient.GetDevices();
            var resultReservationQueue = await _restClient.GetReservations();
            var resultReservationApplied = await _restClient.GetAppliedReservations();
            var resultConfiguration = await _restClient.GetManagerConfiguration();
            var resultAppiumProcesses = await _restClient.GetAppiumProcesses();

            var result = new AllRepositoriesData(resultDevices, resultReservationQueue, resultReservationApplied, resultConfiguration, resultAppiumProcesses);

            return JsonExtension(result);
        }
    }
}
