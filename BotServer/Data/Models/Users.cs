using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace BotServer.Data.Models
{
    public class Users
    {
        [Key]
        public int Id { get; set; }

        public string TwitchId { get; set; }

        public string Username { get; set; }

        public string? Profile { get; set; }

        public UserTokens Tokens { get; set; }

        public Puntos Puntos { get; set; }
    }
}
