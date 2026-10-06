namespace BotServer.Database.Models
{
    public class SpotifyDB
    {

        public int Id { get; set; }
        public string Username { get; set; }
        public string AccesToken { get; set; }

        public string RefreshToken { get; set; }
    }
}
