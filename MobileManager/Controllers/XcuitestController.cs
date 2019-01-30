using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using Castle.Core.Internal;
using Microsoft.AspNetCore.Mvc;
using MobileManager.Controllers.Interfaces;
using MobileManager.Database.Repositories.Interfaces;
using MobileManager.Logging.Logger;
using MobileManager.Models.Git;
using MobileManager.Models.Xcuitest;
using MobileManager.Models.Xcuitest.enums;
using MobileManager.Services;

namespace MobileManager.Controllers
{
    /// <inheritdoc />
    public class XcuitestController : ControllerExtensions, IXcuitestController
    {
        private const string TestReports = "TestReports";
        private readonly IRepository<Xcuitest> _xcuitestRepository;
        private readonly IExternalProcesses _externalProcesses;
        private readonly IManagerLogger _logger;

        private static readonly string GitRepositoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "mobile-manager-git-repos");

        /// <summary>
        /// Constructor
        /// </summary>
        /// <param name="xcuitestRepository"></param>
        /// <param name="externalProcesses"></param>
        /// <param name="logger"></param>
        public XcuitestController(IRepository<Xcuitest> xcuitestRepository, IExternalProcesses externalProcesses,
            IManagerLogger logger) : base(logger)
        {
            _xcuitestRepository = xcuitestRepository;
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
            var gitPath = Path.Combine(GitRepositoryPath, gitRepository.Name);

            string result;
            if (Directory.Exists(gitPath))
            {
                result = _externalProcesses.RunProcessWithBashAndReadOutput("git", "pull", gitPath);
            }
            else
            {
                result = _externalProcesses.RunProcessAndReadOutput("git",
                    $"clone {gitRepository.Remote} {gitPath}");
            }

            return new ObjectResult(result);
        }

        /// <inheritdoc />
        [HttpPost("runXcuitest", Name = "RunXcuitest")]
        public IActionResult RunXcuitest([FromBody] Xcuitest xcuitest)
        {
            CloneTestRepository(xcuitest.GitRepository);

            xcuitest.GitRepository.DirectoryPath = Path.Combine(GitRepositoryPath, xcuitest.GitRepository.Name);

            var outputFile = GetOutputFormatFile(xcuitest, out var outputFormat);

            var result = _externalProcesses.RunProcessWithBashAndReadOutput(
                "xcodebuild",
                $"-scheme {xcuitest.Scheme} -sdk {xcuitest.Sdk} -destination \\\"{xcuitest.Destination}\\\" {xcuitest.Action}",
                Path.Combine(GitRepositoryPath, xcuitest.GitRepository.Name),
                $"xcpretty {outputFormat}");

            xcuitest.Results = result;
            _xcuitestRepository.Add(xcuitest);

            if (!Directory.EnumerateFiles(Path.Combine(GitRepositoryPath, TestReports), xcuitest.Id + ".*").Any())
            {
                return StatusCodeExtension(500, result);
            }

            return GetContentInValidFormat(xcuitest, outputFile);
        }

        private static IActionResult GetContentInValidFormat(IXcuitest xcuitest, string outputFile)
        {
            switch (xcuitest.OutputFormat)
            {
                case XcuitestOutputFormat.PlainText:
                    return new ContentResult()
                    {
                        Content = System.IO.File.ReadAllText(outputFile + ".txt"),
                        ContentType = "text/plain",
                    };
                case XcuitestOutputFormat.Html:
                    return new ContentResult()
                    {
                        Content = System.IO.File.ReadAllText(outputFile + ".html"),
                        ContentType = "text/html",
                    };
                case XcuitestOutputFormat.JunitXml:
                    return new ContentResult()
                    {
                        Content = System.IO.File.ReadAllText(outputFile + ".xml"),
                        ContentType = "text/xml",
                    };
                default:
                    throw new ArgumentOutOfRangeException();
            }
        }

        private static string GetOutputFormatFile(IXcuitest xcuitest, out string outputFormat)
        {
            var outputFile = Path.Combine(GitRepositoryPath, TestReports, xcuitest.Id.ToString());
            switch (xcuitest.OutputFormat)
            {
                case XcuitestOutputFormat.PlainText:
                    outputFormat = $"--output {outputFile}.txt";
                    break;
                case XcuitestOutputFormat.Html:
                    outputFormat = $"--report html --output {outputFile}.html";
                    break;
                case XcuitestOutputFormat.JunitXml:
                    outputFormat = $"--report junit --output {outputFile}.xml";
                    break;
                default:
                    throw new ArgumentOutOfRangeException();
            }

            return outputFile;
        }

        /// <inheritdoc />
        public IActionResult GetXcuitestResultArtifact(string id)
        {
            throw new System.NotImplementedException();
        }
    }
}
