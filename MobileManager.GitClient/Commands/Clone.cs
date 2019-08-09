using System;
using System.Linq;
using System.Text.RegularExpressions;
using MobileManager.GitClient.Commands.Interfaces;
using MobileManager.GitClient.Utils;
using MobileManager.Logging.Logger;

namespace MobileManager.GitClient.Commands
{
    public class Clone : IGitCommand
    {
        private readonly ExternalProcesses _externalProcesses;

        public Clone(IManagerLogger logger)
        {
            _externalProcesses = new ExternalProcesses(logger);
        }

        public string Execute(string param)
        {
            return _externalProcesses.RunProcessAndReadOutput("git", $"clone {param}");
        }

        public string Execute(string param, string[] arguments)
        {
            if (arguments.Any(x => !Regex.IsMatch(x, @"((--|-)+\S+)\s\S+")))
            {
                throw new ArgumentException($"Invalid argument format: [{string.Join(',', arguments)}]");
            }

            return _externalProcesses.RunProcessAndReadOutput("git", $"clone {param} {string.Join(" ", arguments)}");
        }
    }
}
