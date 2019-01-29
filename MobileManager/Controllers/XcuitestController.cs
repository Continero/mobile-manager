using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Castle.Core.Internal;
using Microsoft.AspNetCore.Mvc;
using MobileManager.Controllers.Interfaces;
using MobileManager.Logging.Logger;
using MobileManager.Models.Git;
using MobileManager.Models.Xcuitest;
using MobileManager.Services;

namespace MobileManager.Controllers
{
    /// <inheritdoc />
    public class XcuitestController : IXcuitestController
    {
        private readonly IExternalProcesses _externalProcesses;
        private readonly IManagerLogger _logger;

        private static readonly string GitRepositoryPath =
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile) + "/mobile-manager-git-repos";

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="externalProcesses"></param>
        /// <param name="logger"></param>
        public XcuitestController(IExternalProcesses externalProcesses, IManagerLogger logger)
        {
            _externalProcesses = externalProcesses;
            _logger = logger;

            if (!Directory.Exists(GitRepositoryPath))
            {
                Directory.CreateDirectory(GitRepositoryPath);
            }
        }

        /// <inheritdoc />
        [HttpGet("availableDevices", Name = "getAvailableDevices")]
        public IEnumerable<string> GetAvailableDevices()
        {
            var result = _externalProcesses.RunProcessAndReadOutput("instruments", "-s devices");

            return result.Split(Environment.NewLine).FindAll(x => x.Contains("(Simulator)"));
        }

        /// <inheritdoc />
        [HttpGet("availableRepositories", Name = "getAvailableRepositories")]
        public IEnumerable<IGitRepository> GetAvailableRepositories()
        {
            var directories = Directory.GetDirectories(GitRepositoryPath);

            return directories.Select(directory => new GitRepository {Name = directory}).ToList();
        }

        /// <inheritdoc />
        [HttpPost("cloneTestRepository", Name = "cloneTestRepository")]
        public IActionResult CloneTestRepository([FromBody] GitRepository gitRepository)
        {
            var result = _externalProcesses.RunProcessAndReadOutput("git",
                $"clone {gitRepository.Remote} {GitRepositoryPath}/{gitRepository.Name}");

            return new ObjectResult(result);
        }

        /// <inheritdoc />
        [HttpPost("runXcuitest", Name = "RunXcuitest")]
        public HttpResponseMessage RunXcuitest([FromBody] Xcuitest xcuitest)
        {
            CloneTestRepository(xcuitest.GitRepository);

            var gitPath = Path.Combine(GitRepositoryPath, xcuitest.GitRepository.Name, xcuitest.Project);

            var result = _externalProcesses.RunProcessAndReadOutput("xcodebuild",
                $" -verbose -project \"{gitPath}\" -scheme {xcuitest.Scheme} -sdk {xcuitest.Sdk} -destination \"{xcuitest.Destination}\" {xcuitest.Action}");

            var resp = new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(result, System.Text.Encoding.UTF8, "text/plain")
            };
            return resp;
        }

        /// <inheritdoc />
        public IActionResult GetXcuitestResultArtifact(string id)
        {
            throw new System.NotImplementedException();
        }
    }
}
