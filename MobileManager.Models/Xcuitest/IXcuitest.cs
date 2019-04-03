using System;
using MobileManager.Models.Git;
using MobileManager.Models.Xcuitest.enums;

namespace MobileManager.Models.Xcuitest
{
    public interface IXcuitest
    {
        string ReservationId { get; set; }

        GitRepository GitRepository { get; set; }

        string Project { get; set; }

        string Scheme { get; set; }

        string Sdk { get; set; }

        string Destination { get; set; }

        string Action { get; set; }

        string Workspace { get; set; }

        string OnlyTestingOption { get; set; }

        Guid Id { get; set; }

        string Results { get; set; }

        XcuitestOutputFormat OutputFormat { get; set; }

        CustomBashScript CustomPreBuildScript { get; set; }
    }
}
