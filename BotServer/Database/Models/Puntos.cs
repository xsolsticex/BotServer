using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BotServer.Database.Models
{
    public class Puntos
    {
        [Key]
        public int Id { get; set; }

        [NotNull]
        public string ChannelName { get; set; }

        [NotNull]
        public int UserPoints { get; set; }

        public int UsersId { get; set; }

        public Users User { get; set; }
    }
}
