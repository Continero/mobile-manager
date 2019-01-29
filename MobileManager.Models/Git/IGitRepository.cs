namespace MobileManager.Models.Git
{
    public interface IGitRepository
    {
        string Name { get; set; }
        string Remote { get; set; }
    }
}
