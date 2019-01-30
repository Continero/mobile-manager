using System;
using MobileManager.Models.Git;
using MobileManager.Models.Xcuitest.enums;

namespace MobileManager.Models.Xcuitest
{
    public interface IXcuitest
    {
        GitRepository GitRepository { get; set; }
        string Project { get; set; }
        string Scheme { get; set; }
        string Sdk { get; set; }
        string Destination { get; set; }
        string Action { get; set; }
        Guid Id { get; set; }
        string Results { get; set; }
        XcuitestOutputFormat OutputFormat { get; set; }
    }
}
