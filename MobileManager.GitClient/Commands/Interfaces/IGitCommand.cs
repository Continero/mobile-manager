namespace MobileManager.GitClient.Commands.Interfaces
{
    public interface IGitCommand
    {
        string Execute(string param);
        string Execute(string param, string[] arguments);
    }
}
