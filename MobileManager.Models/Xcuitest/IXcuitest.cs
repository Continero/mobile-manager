using MobileManager.Models.Git;

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
    }
}
