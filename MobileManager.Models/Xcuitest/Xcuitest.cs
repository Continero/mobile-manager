using MobileManager.Models.Git;

namespace MobileManager.Models.Xcuitest
{
    /// <inheritdoc />
    public class Xcuitest : IXcuitest
    {
        public GitRepository GitRepository { get; set; }
        public string Project { get; set; }
        public string Scheme { get; set; }
        public string Sdk { get; set; }
        public string Destination { get; set; }
        public string Action { get; set; }
    }
}
