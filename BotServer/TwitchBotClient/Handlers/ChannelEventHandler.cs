using BotServer.API;
using BotServer.Database.Models;
using BotServer.Database.Services;
using BotServer.Interfaces;
using BotServer.TwitchBotClient.SignalRClient;

namespace BotServer.TwitchBotClient.Handlers
{
    public class ChannelEventHandler
    {
        private readonly TwitchBotApi _api;
        private readonly UsersService _users;
        private readonly ChannelsService _channels;
        private readonly IBotSignalRClient _signalR;

        public ChannelEventHandler(TwitchBotApi api, UsersService users, ChannelsService channels, IBotSignalRClient signalR)
        {
            _api = api;
            _users = users;
            _channels = channels;
            _signalR = signalR;
        }

        public async Task HandleConnectedAsync()
        {
            Console.WriteLine("Connected");
            await _api.GetChannelBadges();
        }

        private async Task RegisterNewEvent(string eventType, Users broadcaster, IEventSubCondition condition, string level, string? bot = null)
        {
            bool exists = string.IsNullOrEmpty(bot)
                ? await _api.CheckEventSubs(eventType, broadcaster.TwitchId)
                : await _api.CheckEventSubs(eventType, broadcaster.TwitchId, bot);

            if (!exists)
            {
                await _api.RegisterEvent(eventType, level, condition);
                var message = $"[EventSub] Evento {eventType} ya existe en {broadcaster.Username}";
                ShowMessage(ConsoleColor.Yellow, message);
            }
            else
            {
                var message = $"[EventSub] Evento {eventType} con exito en {broadcaster.Username}";
                ShowMessage(ConsoleColor.Green, message);
            }
        }

        private static void ShowMessage(ConsoleColor color, string message)
        {
            Console.ForegroundColor = color;
            Console.WriteLine(message);
            Console.ResetColor();
        }

        public async Task HandleJoinedAsync(string channel)
        {
            try
            {
                // Parallelize fetching broadcaster and bot user data
                var userTask = _api.GetUserData(channel);
                var botTask = _api.GetUserData("xhipibotx");
                await Task.WhenAll(userTask, botTask);

                var user = userTask.Result;
                var bot = botTask.Result;

                var followCondition = new FollowCondition { BroadCasterUserId = user.TwitchId, ModeratorUserId = user.TwitchId };
                var messagesCondition = new ChatMessageCondition { BroadCasterUserId = user.TwitchId, UserId = bot.TwitchId };

                await Task.WhenAll(
                    RegisterNewEvent("channel.follow", user, followCondition, "2"),
                    RegisterNewEvent("channel.chat.message", user, messagesCondition, "1", bot.TwitchId),
                    _api.GetCustomBadges(user.TwitchId, channel)
                );

                Console.WriteLine($"Joined to {channel} channel");
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        public async Task HandleSubscriptionAsync(string channel)
        {
            var usersDb = _users;
            var db = _channels;

            var user = await usersDb.GetUser(channel);
            var channelData = await db.FindChannel(channel);

            if (channelData != null && user != null)
            {
                var followersTask = _api.GetFollowers(user.Username, user.UserId);
                var subsTask = _api.GetSubs(user.Username, user.UserId);

                await Task.WhenAll(followersTask, subsTask);

                var followers = followersTask.Result;
                var subs = subsTask.Result;

                await db.UpdateChannel(channel, followers, subs);
                await _signalR.UpdateFollowers(channel, new Dictionary<string, int> { { "followers", followers }, { "subs", subs } });
            }
        }
    }
}
