using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Castle.Core.Internal;
using Microsoft.AspNetCore.Cors;
using Microsoft.AspNetCore.Mvc;
using MobileManager.Controllers.Interfaces;
using MobileManager.Database.Repositories.Interfaces;
using MobileManager.Http.Clients.Interfaces;
using MobileManager.Logging.Logger;
using MobileManager.Models.Git;
using MobileManager.Models.Reservations.enums;
using MobileManager.Models.Xcuitest;
using MobileManager.Models.Xcuitest.enums;
using MobileManager.Services;
using MobileManager.Utils;
using Newtonsoft.Json;

namespace MobileManager.Controllers
{
    /// <inheritdoc />
    [Route("api/v1/xcuitest")]
    [EnableCors("AllowAllHeaders")]
    public class XcuitestController : ControllerExtensions, IXcuitestController
    {
        private readonly IManagerLogger _logger;
        private readonly XcuiTestUtils _xcuiTestUtils;
        private readonly IRestClient _restClient;

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xcuitestRepository"></param>
        /// <param name="externalProcesses"></param>
        /// <param name="logger"></param>
        /// <param name="restClient"></param>
        public XcuitestController(IRepository<Xcuitest> xcuitestRepository, IExternalProcesses externalProcesses,
            IManagerLogger logger, IRestClient restClient) : base(logger)
        {
            _logger = logger;
            _restClient = restClient;
            _xcuiTestUtils = new XcuiTestUtils(_logger, externalProcesses, xcuitestRepository);

            if (!Directory.Exists(XcuiTestUtils.GitRepositoryPath))
            {
                Directory.CreateDirectory(XcuiTestUtils.GitRepositoryPath);
            }
        }

        /// <inheritdoc />
        [HttpGet("availableDevices", Name = "getAvailableDevices")]
        public IEnumerable<InstrumentsDevice> GetAvailableDevices()
        {
            var instrumentsDevices = _xcuiTestUtils.GetInstrumentsDevices();

            _logger.Debug($"{nameof(GetAvailableDevices)}: {JsonConvert.SerializeObject(instrumentsDevices)}");

            return instrumentsDevices;
        }

        /// <inheritdoc />
        [HttpGet("availableRepositories", Name = "getAvailableRepositories")]
        public IEnumerable<IGitRepository> GetAvailableRepositories()
        {
            var directories = Directory.GetDirectories(XcuiTestUtils.GitRepositoryPath);

            return directories.Select(directory => new GitRepository {Name = directory}).ToList();
        }

        /// <inheritdoc />
        [HttpPost("cloneTestRepository", Name = "cloneTestRepository")]
        public IActionResult CloneTestRepository([FromBody] GitRepository gitRepository)
        {
            var gitPath = Path.Combine(XcuiTestUtils.GitRepositoryPath, gitRepository.Name);

            var result = _xcuiTestUtils.CloneOrPullGitRepository(gitRepository, gitPath);

            return new ObjectResult(result);
        }

        /// <inheritdoc />
        [HttpPost("runXcuitest", Name = "RunXcuitest")]
        public async Task<IActionResult> RunXcuitest([FromBody] Xcuitest xcuitest)
        {
            if (string.IsNullOrEmpty(xcuitest.ReservationId))
            {
                return BadRequestExtension("ReservationId is empty. Can only run XcuiTests on reserved devices.");
            }

            var reservation = await _restClient.GetAppliedReservation(xcuitest.ReservationId);

            if (reservation == null)
            {
                return BadRequestExtension("Invalid ReservationId does not exist in ReservationApplied.");
            }

            if (reservation.ReservationType != ReservationType.XcuiTest)
            {
                return BadRequestExtension(
                    $"Invalid {nameof(reservation.ReservationType)}. Only {ReservationType.XcuiTest} is available to run XcuiTests.");
            }

            if (xcuitest.GitRepository != null)
            {
                CloneTestRepository(xcuitest.GitRepository);
            }

            var outputFile = "";
            var result = "";
            var outputFormat = "";

            if (xcuitest.CustomPreBuildScript.ScriptLine.Any())
            {
                try
                {
                    _xcuiTestUtils.RunCustomPreBuildScript(xcuitest);

                    outputFile = _xcuiTestUtils.RunXcuiTest(xcuitest, out outputFormat, out result);
                }
                catch (Exception e)
                {
                    return StatusCodeExtension(500, e.Message);
                }
            }

            if (!Directory.EnumerateFiles(Path.Combine(XcuiTestUtils.GitRepositoryPath, XcuiTestUtils.TestReports),
                xcuitest.Id + ".*").Any())
            {
                return StatusCodeExtension(500, result);
            }

            return new ContentResult()
            {
                Content = System.IO.File.ReadAllText(outputFile),
                ContentType = "text/plain",
            };

            //return _xcuiTestUtils.GetContentInValidFormat(xcuitest, outputFile);
        }


        /// <inheritdoc />
        [HttpGet("resultArtifact", Name = "resultArtifact")]
        public IActionResult GetXcuitestResultArtifact(string id)
        {
            throw new System.NotImplementedException();
        }
    }
}
