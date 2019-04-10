namespace MobileManager.Services
{
    /// <summary>
    /// External processes interface
    /// </summary>
    public interface IExternalProcesses
    {
        /// <summary>
        /// Runs the process and read output.
        /// </summary>
        /// <returns>The process and read output.</returns>
        /// <param name="processName">Process name.</param>
        /// <param name="processArgs">Process arguments.</param>
        /// <param name="timeout">Timeout.</param>
        string RunProcessAndReadOutput(string processName, string processArgs, int timeout = 5000);

        /// <summary>
        /// Runs the process and read output.
        /// </summary>
        /// <returns>The process and read output.</returns>
        /// <param name="processName">Process name.</param>
        /// <param name="processArgs">Process arguments.</param>
        /// <param name="workingDirectory">Directory where to execute process.</param>
        /// <param name="timeout">Timeout.</param>
        string RunProcessAndReadOutput(string processName, string processArgs, string workingDirectory,
            int timeout = 5000);

        /// <summary>
        /// Runs the process and read output.
        /// </summary>
        /// <returns>The process and read output.</returns>
        /// <param name="processName">Process name.</param>
        /// <param name="processArgs">Process arguments.</param>
        /// <param name="workingDirectory">Working directory</param>
        /// <param name="outputFilePath">Pipe output to another binary.</param>
        /// <param name="timeout">Timeout.</param>
        int RunProcessWithBashAndReadOutput(string processName, string processArgs, string workingDirectory = "",
            string outputFilePath = "",
            int timeout = 5000);

        string RunScriptWithBashAndReadOutput(string scriptLine, string workingDirectory = "", string pipe = "",
            int timeout = 5000);

        /// <summary>
        /// Runs the shell process.
        /// </summary>
        /// <returns>The shell process.</returns>
        /// <param name="processName">Process name.</param>
        /// <param name="processArgs">Process arguments.</param>
        /// <param name="timeout">Timeout.</param>
        string RunShellProcess(string processName, string processArgs, int timeout = 5000);

        /// <summary>
        /// Runs the process in background.
        /// </summary>
        /// <returns>The process in background.</returns>
        /// <param name="processName">Process name.</param>
        /// <param name="processArgs">Process arguments.</param>
        int RunProcessInBackground(string processName, string processArgs);

        /// <summary>
        /// Starts the process in background running.
        /// </summary>
        /// <returns><c>true</c>, if process in background running was started, <c>false</c> otherwise.</returns>
        /// <param name="processId">Process identifier.</param>
        bool IsProcessInBackgroundRunning(int processId);

        /// <summary>
        /// Stops the process running in background.
        /// </summary>
        /// <param name="containsStringInName">Contains string in name.</param>
        void StopProcessRunningInBackground(string containsStringInName);
    }
}
