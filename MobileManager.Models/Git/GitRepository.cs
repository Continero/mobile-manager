namespace MobileManager.Models.Git
{
    public class GitRepository : IGitRepository
    {
        public string Name { get; set; }
        public string Remote { get; set; }
    }
}
