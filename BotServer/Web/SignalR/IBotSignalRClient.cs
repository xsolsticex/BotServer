namespace BotServer.Web.SignalR
{
    public interface IBotSignalRClient
    {
        Task StartClient();
        Task Send(string channel, Dictionary<string, object> message);
        Task SendFollow(string channel, Dictionary<string, string> message);
        Task UpdateFollowers(string channel, Dictionary<string, int> message);
        Task UpdateCounter(string channel, string counter_state);
        Task ChangeColor(string channel, string color);
    }
}
