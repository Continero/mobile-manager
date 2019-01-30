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
        [HttpGet]
        IEnumerable<string> GetAvailableDevices();

        [HttpGet]
        IEnumerable<IGitRepository> GetAvailableRepositories();
        
        [HttpPost]
        IActionResult CloneTestRepository([FromBody] GitRepository gitRepository);

        [HttpPost]
        IActionResult RunXcuitest([FromBody] Xcuitest xcuitest);

        [HttpGet]
        IActionResult GetXcuitestResultArtifact(string id);

    }
}
