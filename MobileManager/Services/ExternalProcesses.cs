using System;
using System.Diagnostics;
using System.Threading;
using MobileManager.Logging.Logger;
using ToolBox.Bridge;
using ToolBox.Notification;
using ToolBox.Platform;

namespace MobileManager.Services
{
    /// <summary>
    /// External processes.
    /// </summary>
    public class ExternalProcesses : IExternalProcesses
    {
        private static INotificationSystem NotificationSystem { get; set; }

        private static IBridgeSystem BridgeSystem { get; set; }

        private static ShellConfigurator Shell { get; set; }

        private readonly IManagerLogger _logger;

        /// <param name="logger"></param>
        /// <inheritdoc />
        public ExternalProcesses(IManagerLogger logger)
        {
            _logger = logger;
            NotificationSystem = ToolBox.Notification.NotificationSystem.Default;
            switch (OS.GetCurrent())
            {
                case "win":
                    BridgeSystem = ToolBox.Bridge.BridgeSystem.Bat;
                    break;
                case "mac":
                case "gnu":
                    BridgeSystem = ToolBox.Bridge.BridgeSystem.Bash;
                    break;
                default:
                    throw new NotImplementedException();
            }

            Shell = new ShellConfigurator(BridgeSystem, NotificationSystem);
        }

        /// <inheritdoc />
        public string RunProcessAndReadOutput(string processName, string processArgs, int timeout = 5000)
        {
            _logger.Debug(string.Format("RunProcessAndReadOutput processName: [{0}] args: [{1}]", processName,
                processArgs));

            var psi = new ProcessStartInfo()
            {
                FileName = processName,
                Arguments = processArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true
            };
            var proc = new Process
            {
                StartInfo = psi
            };

            proc.Start();

            proc.WaitForExit(timeout);
            _logger.Debug($"{nameof(RunProcessAndReadOutput)} [{processName}] FINISHED.");

            var output = proc.StandardOutput.ReadToEnd();
            _logger.Debug($"{nameof(RunProcessAndReadOutput)} [{processName}] output: [{string.Join("\n", output)}]");

            var errorOutput = proc.StandardError.ReadToEnd();
            _logger.Debug($"{nameof(RunProcessAndReadOutput)} [{processName}] errorOutput: [{string.Join("\n", errorOutput)}]");


            return output + errorOutput;
        }

        /// <inheritdoc />
        public string RunProcessAndReadOutput(string processName, string processArgs, string workingDirectory,
            int timeout = 5000)
        {
            _logger.Debug(string.Format("RunProcessAndReadOutput processName: [{0}] args: [{1}]", processName,
                processArgs));

            var psi = new ProcessStartInfo()
            {
                FileName = processName,
                Arguments = processArgs,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };
            var proc = new Process
            {
                StartInfo = psi
            };

            proc.Start();

            proc.WaitForExit(timeout);
            _logger.Debug($"{nameof(RunProcessAndReadOutput)} [{processName}] FINISHED.");

            var output = proc.StandardOutput.ReadToEnd();
            _logger.Debug($"{nameof(RunProcessAndReadOutput)} [{processName}] output: [{string.Join("\n", output)}]");

            var errorOutput = proc.StandardError.ReadToEnd();
            _logger.Debug($"{nameof(RunProcessAndReadOutput)} [{processName}] errorOutput: [{string.Join("\n", errorOutput)}]");


            return output + errorOutput;
        }


        /// <inheritdoc />
        public string RunProcessWithBashAndReadOutput(string processName, string processArgs,
            string workingDirectory = "", string pipe = "",
            int timeout = 5000)
        {
            _logger.Debug(
                $"{nameof(RunProcessWithBashAndReadOutput)} processName: [{processName}], args: [{processArgs}], workingDir [{workingDirectory}], pipe [{pipe}]");

            var psi = new ProcessStartInfo()
            {
                FileName = "/bin/bash",
                Arguments =
                    $"-c \"{processName} {processArgs}{(string.IsNullOrEmpty(pipe) ? string.Empty : $" >> {pipe} 2>&1")}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };
            var proc = new Process
            {
                StartInfo = psi
            };

            proc.Start();

            proc.WaitForExit(timeout);
            _logger.Debug($"{nameof(RunProcessWithBashAndReadOutput)} [{processName}] FINISHED.");

            var output = proc.StandardOutput.ReadToEnd();
            _logger.Debug($"{nameof(RunProcessWithBashAndReadOutput)} [{processName}] output: [{string.Join("\n", output)}]");

            var errorOutput = proc.StandardError.ReadToEnd();
            _logger.Debug($"{nameof(RunProcessWithBashAndReadOutput)} [{processName}] errorOutput: [{string.Join("\n", errorOutput)}]");

            Thread.Sleep(500);
            if (proc.ExitCode != 0)
            {
                throw new Exception(errorOutput);
            }

            return output + errorOutput;
        }

        /// <inheritdoc />
        public string RunScriptWithBashAndReadOutput(string scriptLine, string workingDirectory = "", string pipe = "",
            int timeout = 5000)
        {
            _logger.Debug(
                $"{nameof(RunScriptWithBashAndReadOutput)} scriptLine [{scriptLine}], workingDir [{workingDirectory}], pipe [{pipe}]");

            var psi = new ProcessStartInfo()
            {
                FileName = "/bin/bash",
                Arguments =
                    $"-c \"{scriptLine}{(string.IsNullOrEmpty(pipe) ? string.Empty : " 2>&1 | " + pipe)}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                WorkingDirectory = workingDirectory
            };
            var proc = new Process
            {
                StartInfo = psi
            };

            proc.Start();

            proc.WaitForExit(timeout);
            _logger.Debug($"{nameof(RunScriptWithBashAndReadOutput)} [{scriptLine}] FINISHED.");

            var output = proc.StandardOutput.ReadToEnd();
            _logger.Debug($"{nameof(RunScriptWithBashAndReadOutput)} [{scriptLine}] output: [{string.Join("\n", output)}]");

            var errorOutput = proc.StandardError.ReadToEnd();
            _logger.Debug($"{nameof(RunScriptWithBashAndReadOutput)} [{scriptLine}] errorOutput: [{string.Join("\n", errorOutput)}]");

            Thread.Sleep(500);
            if (proc.ExitCode != 0)
            {
                throw new Exception(errorOutput);
            }

            return output + errorOutput;
        }

        /// <summary>
        /// Runs the shell process.
        /// </summary>
        /// <returns>The shell process.</returns>
        /// <param name="processName">Process name.</param>
        /// <param name="processArgs">Process arguments.</param>
        /// <param name="timeout">Timeout.</param>
        public string RunShellProcess(string processName, string processArgs, int timeout = 5000)
        {
            _logger.Debug(string.Format("RunProcessAndReadOutput processName: [{0}] args: [{1}]", processName,
                processArgs));

            var psi = new ProcessStartInfo()
            {
                FileName = processName,
                Arguments = processArgs,
                UseShellExecute = true,
                RedirectStandardOutput = false,
                RedirectStandardError = false
            };
            var proc = new Process
            {
                StartInfo = psi
            };

            proc.Start();

            proc.WaitForExit(timeout);
            return string.Empty;
        }

        /// <summary>
        /// Runs the process in background.
        /// </summary>
        /// <returns>The process in background.</returns>
        /// <param name="processName">Process name.</param>
        /// <param name="processArgs">Process arguments.</param>
        public int RunProcessInBackground(string processName, string processArgs)
        {
            var proc = $"{processName} {processArgs}";
            var response = Shell.Term(proc, Output.External);

            return response.code;
        }

        /// <summary>
        /// Starts the process in background running.
        /// </summary>
        /// <returns><c>true</c>, if process in background running was started, <c>false</c> otherwise.</returns>
        /// <param name="processId">Process identifier.</param>
        public bool IsProcessInBackgroundRunning(int processId)
        {
            return !Process.GetProcessById(processId).HasExited;
        }

        /// <summary>
        /// Stops the process running in background.
        /// </summary>
        /// <param name="containsStringInName">Contains string in name.</param>
        public void StopProcessRunningInBackground(string containsStringInName)
        {
            var result = RunProcessAndReadOutput("/bin/bash", $"close.sh {containsStringInName}");
            _logger.Debug($"StopProcessRunningInBackground output: [{result}]");
        }
    }
}
