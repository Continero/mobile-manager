using System.Collections.Generic;
using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using MobileManager.Models.Git;
using MobileManager.Models.Xcuitest;

namespace MobileManager.Controllers.Interfaces
{
    /// <summary>
    /// XCUI Test Controller
    /// </summary>
    public interface IXcuitestController
    {
        /// <summary>
        /// Get all XCUITEST available devices.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        IEnumerable<InstrumentsDevice> GetAvailableDevices();

        /// <summary>
        /// Get all XCUITEST downloaded repositories.
        /// </summary>
        /// <returns></returns>
        [HttpGet]
        IEnumerable<IGitRepository> GetAvailableRepositories();

        /// <summary>
        /// Clone new XCUITEST repository.
        /// </summary>
        /// <param name="gitRepository"></param>
        /// <returns></returns>
        [HttpPost]
        IActionResult CloneTestRepository([FromBody] GitRepository gitRepository);

        /// <summary>
        /// Run XCUITEST on specified GIT repository.
        /// </summary>
        /// <param name="xcuitest"><see cref="IXcuitest"/>></param>
        /// <returns></returns>
        [HttpPost]
        IActionResult RunXcuitest([FromBody] Xcuitest xcuitest);

        /// <summary>
        /// Gets XCUITEST result from completed test run.
        /// </summary>
        /// <param name="id"><see cref="IXcuitest.Id"/></param>
        /// <returns></returns>
        [HttpGet]
        IActionResult GetXcuitestResultArtifact(string id);
    }
}
