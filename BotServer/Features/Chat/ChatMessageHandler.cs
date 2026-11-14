using BotServer.Features.Badges;
using BotServer.Features.Chat.Markov;
using BotServer.Web.SignalR;
using TwitchLib.Client.Events;

namespace BotServer.Features.Chat
{
    public class ChatMessageHandler
    {
        private readonly ChatBadgeService _badges;
        private readonly ChatProfileService _profiles;
        private readonly MarkovChatService _markov;
        private readonly IBotSignalRClient _signalR;
        private readonly ILogger<ChatMessageHandler> _logger;

        public ChatMessageHandler(ChatBadgeService badges, ChatProfileService profiles, MarkovChatService markov, IBotSignalRClient signalR, ILogger<ChatMessageHandler> logger)
        {
            _badges = badges;
            _profiles = profiles;
            _markov = markov;
            _signalR = signalR;
            _logger = logger;
        }

        public async Task HandleAsync(OnMessageReceivedArgs e, string? botUsername)
        {
            var message = e.ChatMessage.Message;
            _logger.LogInformation("Chat message received in {Channel} from {Username}; connected bot: {BotUsername}",
                e.ChatMessage.Channel, e.ChatMessage.Username, botUsername);
            if (string.IsNullOrEmpty(message) || message.StartsWith('!')) return;

            var channel = e.ChatMessage.Channel;
            var color = string.IsNullOrEmpty(e.ChatMessage.HexColor) ? "#ffc107" : e.ChatMessage.HexColor;
            var user = e.ChatMessage.Username;
            if (!string.Equals(user, botUsername, StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    _markov.Learn(channel, message);
                }
                catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                {
                    _logger.LogError(ex, "Markov training could not be persisted for {Channel}", channel);
                }
            }
            else
            {
                _logger.LogInformation("Markov skipped message in {Channel}: sender {Username} is the connected bot account",
                    channel, user);
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
