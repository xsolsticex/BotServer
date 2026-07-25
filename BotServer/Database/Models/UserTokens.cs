using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BotServer.Database.Models
{
    public class UserTokens
    {
        [Key]
        public int Id { get; set; }

        [NotNull]
        public string AccessToken { get; set; }

        [NotNull]
        public string RefreshToken { get; set; }

        public int UsersId { get; set; }

        public Users User { get; set; }




    }
}
