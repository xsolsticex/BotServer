using System.ComponentModel.DataAnnotations;

namespace BotServer.Data.Models
{
    public class JoinedChannels
    {
        [Key]
        public int Id { get; set; }

        public string ChannelName { get; set; }

        public int Followers { get; set; } = 0;

        public int Subs { get; set; } = 0;

        public ICollection<ChannelMessages> Messages { get; set; }

        public ICollection<CustomBadges> Badges { get; set; } = new List<CustomBadges>();
    }
}
