using System.ComponentModel.DataAnnotations;

namespace BotServer.Data.Models
{
    public class ChannelMessages
    {
        [Key]
        public int Id { get; set; }

        public int ChannelId { get; set; }

        public string Message { get; set; }

        public JoinedChannels Channel { get; set; }
    }
}
