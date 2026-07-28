using System.ComponentModel.DataAnnotations;

namespace BotServer.Database.Models
{
    public class GlobalBadges
    {
        [Key]
        public int Id { get; set; }

        public string BadgeId { get; set; }
        public string name { get; set; }

        public string url { get; set; }

    }
}
