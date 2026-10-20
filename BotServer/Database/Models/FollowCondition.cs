using BotServer.Interfaces;
using System.Text.Json.Serialization;

namespace BotServer.Database.Models
{
    public class FollowCondition : IEventSubCondition
    {
        [JsonPropertyName("broadcaster_user_id")]
        public required string BroadCasterUserId { get; set; }
        [JsonPropertyName("moderator_user_id")]
        public required string ModeratorUserId { get; set; }

        public Dictionary<string, string> ToDictionary() => new()
        {
            ["broadcaster_user_id"] = BroadCasterUserId,
            ["moderator_user_id"] = ModeratorUserId,
        };
     

    }
}
