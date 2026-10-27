namespace BotServer.Database.Models
{
    public class TwitchOptions
    {
        public string BotUsername { get; set; } = "xhipibot";
        public string BotOAuthToken { get; set; } = string.Empty;

        public string MainChannel { get; set; } = "xhipibotx";

        public string Profile { get; set; }
    }
}
