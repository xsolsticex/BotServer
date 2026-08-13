namespace BotServer.Database.Models
{
    public class CustomBadges
    {
        public int Id { get; set; }

        public int ChannelId { get; set; }

        public string SetId { get; set; }

        public string BadgeUrl { get; set; }

        public JoinedChannels Channel { get; set; }
    }
}
