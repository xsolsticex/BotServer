using BotServer.Integrations.Twitch.EventSub.Conditions;
using System.Text.Json.Serialization;

namespace BotServer.Integrations.Twitch.EventSub.Conditions
{
    public class ChatMessageCondition : IEventSubCondition
    {
        [JsonPropertyName("broadcaster_user_id")]
        public required string BroadCasterUserId { get; set; }

        [JsonPropertyName("user_id")]
        public required string UserId { get; set; }

        public Dictionary<string, string> ToDictionary() => new()
        {
            ["broadcaster_user_id"] = BroadCasterUserId,
            ["user_id"] = UserId,
        };
    }
}
