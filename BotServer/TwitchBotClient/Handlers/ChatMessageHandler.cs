using BotServer.TwitchBotClient.SignalRClient;
using TwitchLib.Client.Events;

namespace BotServer.TwitchBotClient.Handlers
{
    public class ChatMessageHandler
    {
        private readonly ChatBadgeService _badges;
        private readonly ChatProfileService _profiles;
        private readonly MarkovChatService _markov;
        private readonly IBotSignalRClient _signalR;

        public ChatMessageHandler(ChatBadgeService badges, ChatProfileService profiles, MarkovChatService markov, IBotSignalRClient signalR)
        {
            _badges = badges;
            _profiles = profiles;
            _markov = markov;
            _signalR = signalR;
        }

        public async Task HandleAsync(OnMessageReceivedArgs e, string? botUsername)
        {
            var message = e.ChatMessage.Message;
            if (string.IsNullOrEmpty(message) || message.StartsWith('!')) return;

            var channel = e.ChatMessage.Channel;
            var color = string.IsNullOrEmpty(e.ChatMessage.HexColor) ? "#ffc107" : e.ChatMessage.HexColor;
            var user = e.ChatMessage.Username;
            if (!string.Equals(user, botUsername, StringComparison.OrdinalIgnoreCase))
            {
                _markov.Learn(channel, message);
            }
            var rawBadges = e.ChatMessage.Badges.Select(b => b.Key).ToList();

            try
            {
                // Keep queries sequential because these services share the scoped DbContext.
                var badgeUrls = await _badges.GetBadgesAsync(rawBadges, channel);
                var profile = await _profiles.GetProfile(user);

                var data = new Dictionary<string, object>
                {
                    ["username"] = user,
                    ["content"] = message,
                    ["color"] = color,
                    ["profile"] = profile,
                    ["badges"] = badgeUrls
                };

                await _signalR.Send(channel, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}
