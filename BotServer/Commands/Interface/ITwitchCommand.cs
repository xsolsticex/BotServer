namespace BotServer.Commands.Interface
{
    public interface ITwitchCommand
    {
        string Name { get; }

        Task ExecuteAsync(string channel, string? username = null, string[]? args = null);
    }
}
