namespace BotServer.Integrations.Twitch.Auth.Models
{
    public class UserToken
    {
        public string AccessToken { get; set; }
        public string RefreshToken { get; set; }

        public string Username { get; set; }

        public string UserId { get; set; }

        public string Profile { get; set; }
    }
}
