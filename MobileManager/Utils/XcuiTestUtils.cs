using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using MobileManager.Database.Repositories;
using MobileManager.Database.Repositories.Interfaces;
using MobileManager.Http.Clients.Interfaces;
using MobileManager.Logging.Logger;
using MobileManager.Models.Git;
using MobileManager.Models.Xcuitest;
using MobileManager.Models.Xcuitest.enums;
using MobileManager.Services;

namespace MobileManager.Utils
{
    /// <summary>
    /// XcuiTest utils.
    /// </summary>
    public class XcuiTestUtils
    {
        private readonly IManagerLogger _logger;
        private readonly IExternalProcesses _externalProcesses;
        private readonly IRepository<Xcuitest> _xcuitestRepository;
        private readonly IRestClient _restClient;

        /// <summary>
        /// Test reports.
        /// </summary>
        public const string TestReports = "TestReports";

        /// <summary>
        /// Path to git xcuitest git repositories.
        /// </summary>
        public static readonly string GitRepositoryPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "mobile-manager-git-repos");

        /// <summary>
        /// Constructor.
        /// </summary>
        /// <param name="logger"></param>
        /// <param name="externalProcesses"></param>
        /// <param name="xcuitestRepository"></param>
        /// <param name="restClient"></param>
        public XcuiTestUtils(IManagerLogger logger, IExternalProcesses externalProcesses,
            IRepository<Xcuitest> xcuitestRepository, IRestClient restClient)
        {
            _logger = logger;
            _externalProcesses = externalProcesses;
            _xcuitestRepository = xcuitestRepository;
            _restClient = restClient;
        }


//        /// <summary>
//        ///
//        /// </summary>
//        /// <param name="xcuitest"></param>
//        /// <param name="outputFile"></param>
//        /// <returns></returns>
//        /// <exception cref="ArgumentOutOfRangeException"></exception>
//        public IActionResult GetContentInValidFormat(IXcuitest xcuitest, string outputFile)
//        {
//            switch (xcuitest.OutputFormat)
//            {
//                case XcuitestOutputFormat.PlainText:
//                    return new ContentResult()
//                    {
//                        Content = System.IO.File.ReadAllText(outputFile + ".txt"),
//                        ContentType = "text/plain",
//                    };
//                case XcuitestOutputFormat.Html:
//                    return new ContentResult()
//                    {
//                        Content = System.IO.File.ReadAllText(outputFile + ".html"),
//                        ContentType = "text/html",
//                    };
//                case XcuitestOutputFormat.JunitXml:
//                    return new ContentResult()
//                    {
//                        Content = System.IO.File.ReadAllText(outputFile + ".xml"),
//                        ContentType = "text/xml",
//                    };
//                default:
//                    throw new ArgumentOutOfRangeException();
//            }
//        }

//        /// <summary>
//        ///
//        /// </summary>
//        /// <param name="xcuitest"></param>
//        /// <param name="outputFormat"></param>
//        /// <returns></returns>
//        /// <exception cref="ArgumentOutOfRangeException"></exception>
//        public string GetOutputFormatFile(IXcuitest xcuitest, out string outputFormat)
//        {
//            var outputFile = Path.Combine(GitRepositoryPath, TestReports, xcuitest.Id.ToString());
//            switch (xcuitest.OutputFormat)
//            {
//                case XcuitestOutputFormat.PlainText:
//                    outputFormat = $"--output {outputFile}.txt";
//                    break;
//                case XcuitestOutputFormat.Html:
//                    outputFormat = $"--report html --output {outputFile}.html";
//                    break;
//                case XcuitestOutputFormat.JunitXml:
//                    outputFormat = $"--report junit --output {outputFile}.xml";
//                    break;
//                default:
//                    throw new ArgumentOutOfRangeException();
//            }
//
//            return outputFile;
//        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public List<InstrumentsDevice> GetInstrumentsDevices()
        {
            var result = _externalProcesses.RunProcessAndReadOutput("instruments", "-s devices");
            var regex = new Regex(@"(.+)\s(\(\S+\))\s(\[\S+\])(\s\(Simulator\))?");

            var instrumentsDevices = (from line in result.Split(Environment.NewLine)
                select regex.Match(line)
                into res
                where res.Success
                select new InstrumentsDevice
                {
                    Name = res.Groups[1].Value,
                    Version = res.Groups[2].Value,
                    Id = res.Groups[3].Value,
                    IsSimulator = !string.IsNullOrEmpty(res.Groups[4].Value),
                }).ToList();
            return instrumentsDevices;
        }


        /// <summary>
        /// Clone repository to gitPath or pull new changes if already exists.
        /// </summary>
        /// <param name="gitRepository"></param>
        /// <param name="gitPath"></param>
        /// <returns></returns>
        public string CloneOrPullGitRepository(GitRepository gitRepository, string gitPath)
        {
            string result = string.Empty;
            if (Directory.Exists(gitPath))
            {
                _logger.Debug($"{nameof(CloneOrPullGitRepository)}: Clean git repository and pull new changes.");

                if (gitRepository.CleanRepository)
                {
                    result = _externalProcesses.RunProcessWithBashAndReadOutput("git", "clean -xdf", gitPath);
                }

                result += _externalProcesses.RunProcessWithBashAndReadOutput("git", "pull", gitPath);

                _logger.Debug($"{nameof(CloneOrPullGitRepository)}: CLEAN + PULL: result=[{result}]");
            }
            else
            {
                result = _externalProcesses.RunProcessAndReadOutput("git",
                    $"clone {gitRepository.Remote} {gitPath}");

                _logger.Debug($"{nameof(CloneOrPullGitRepository)}: CLONE: result=[{result}]");
            }

            return result;
        }

        public string RunXcuiTest(Xcuitest xcuitest, out string result)
        {
            //var outputFile = GetOutputFormatFile(xcuitest, out outputFormat);
            var outputFile = Path.Combine(GitRepositoryPath, TestReports, xcuitest.Id.ToString()) + ".txt";

            var onlyTesting = string.Empty;
            if (!string.IsNullOrEmpty(xcuitest.OnlyTestingOption))
            {
                onlyTesting = $"-only-testing:{xcuitest.OnlyTestingOption}";
            }

            var workspace = string.Empty;
            if (!string.IsNullOrEmpty(xcuitest.Workspace))
            {
                workspace = $"-workspace {xcuitest.Workspace}";
            }

            var reservation = _restClient.GetAppliedReservation(xcuitest.ReservationId).Result;

            var destination = "";
            foreach (var reservedDevice in reservation.ReservedDevices)
            {
                destination += $"-destination \\\"id={reservedDevice.Id}\\\" ";
            }

            result = "";
            try
            {
                result = _externalProcesses.RunProcessWithBashAndReadOutput(
                    "xcodebuild",
                    $"{workspace} -scheme {xcuitest.Scheme} -sdk {xcuitest.Sdk} {destination} {onlyTesting} {xcuitest.Action}",
                    Path.Combine(GitRepositoryPath, xcuitest.Project),
                    $"{outputFile}");
            }
            catch (Exception e)
            {
                _logger.Error($"{nameof(RunXcuiTest)} failed with error.", e);
                result = File.ReadAllText(outputFile);
                throw new Exception(result);
            }
            finally
            {
                xcuitest.Results = result;
                _xcuitestRepository.Add(xcuitest);
            }

            return outputFile;
        }

        /// <summary>
        /// Runs cocoa pod installation in directory.
        /// </summary>
        /// <param name="directory"></param>
        public void InstallCocoaPodBundles(string directory)
        {
            var output = _externalProcesses.RunProcessAndReadOutput("bundle", "exec pod install", directory, 600000);
            _logger.Debug($"{nameof(InstallCocoaPodBundles)}: [{output}]");
        }

        /// <summary>
        /// Runs custom prebuilt script for each script line in working directory.
        /// </summary>
        /// <param name="xcuitest"></param>
        /// <exception cref="NotImplementedException"></exception>
        public void RunCustomPreBuildScript(Xcuitest xcuitest)
        {
            var outputFile = Path.Combine(GitRepositoryPath, TestReports, xcuitest.Id.ToString()) +
                             "_CustomPreBuildScript.txt";

            var workingDir = Path.Combine(GitRepositoryPath, xcuitest.Project);
            if (xcuitest.Project != xcuitest.CustomPreBuildScript.WorkingDirectory)
            {
                workingDir = xcuitest.CustomPreBuildScript.WorkingDirectory;
            }

            foreach (var scriptLine in xcuitest.CustomPreBuildScript.ScriptLine)
            {
                var output = _externalProcesses.RunScriptWithBashAndReadOutput(scriptLine,
                    workingDir, $"tee -a {outputFile}");

                _logger.Debug($"{nameof(RunCustomPreBuildScript)}: [{output}]");
            }
        }
    }
}
