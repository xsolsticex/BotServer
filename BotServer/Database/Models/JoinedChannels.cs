using System.ComponentModel.DataAnnotations;

namespace BotServer.Database.Models
{
    public class JoinedChannels
    {
        [Key]
        public int Id { get; set; }

        public string ChannelName { get; set; }

        public ICollection<ChannelMessages> Messages { get; set; }
    }
}
