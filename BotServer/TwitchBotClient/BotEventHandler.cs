using BotServer.API;
using BotServer.Configuration;
using BotServer.Database.Models;
using BotServer.Database.Services;
using BotServer.Interfaces;
using BotServer.TwitchBotClient.SignalRClient;
using TwitchLib.Client;
using TwitchLib.Client.Events;

namespace BotServer.TwitchBotClient
{
    public class BotEventHandler
    {
        private TwitchClient? _client;
        private readonly TwitchBotApi _api;
        private readonly IServiceScopeFactory _scope;
        private readonly IBotSignalRClient _signalR;

        public bool eventsInitialized { get; set; } = false;

        public BotEventHandler(IBotSignalRClient signalR, TwitchBotApi api, IServiceScopeFactory scope)
        {
            _signalR = signalR;
            _api = api;
            _scope = scope;
        }

        public void Initialize(TwitchClient client)
        {
            _client = client;
        }

        public async Task OnConnected(object? sender, OnConnectedEventArgs e)
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

        public async Task OnChannelJoined(object? sender, OnJoinedChannelArgs e)
        {
            try
            {
                var channel = e.Channel;

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

        private static List<string> FormatBadges(IEnumerable<string> badges)
        {
            var result = new List<string>();
            foreach (var element in badges)
            {
                var formatted = element switch
                {
                    "bot-badge" => "Chat Bot",
                    "premium" => "Prime Gaming",
                    _ => element
                };

                if (formatted.Contains('-') || formatted.Contains('_'))
                {
                    var newName = formatted.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                    formatted = string.Join(" ", newName.Select(w => char.ToUpper(w[0]) + (w.Length > 1 ? w.Substring(1) : string.Empty)));
                }

                result.Add(formatted);
            }
            return result;
        }

        private async Task<string> GetProfile(string username, IServiceScope scope)
        {
            var db = scope.ServiceProvider.GetRequiredService<UsersService>();
            var usu = await db.GetUser(username);
            if (usu != null && !string.IsNullOrEmpty(usu.Profile))
            {
                return usu.Profile;
            }
            return await _api.GetUserProfile(username);
        }

        private async Task<List<string>> GetBadgesAsync(List<string> badges, string channel, IServiceScope scope)
        {
            var badgesdb = scope.ServiceProvider.GetRequiredService<GlobalBadgesService>();
            var custombadgeDb = scope.ServiceProvider.GetRequiredService<CustomBadgesServices>();

            var customBadgesTask = custombadgeDb.GetUrlCustomBadges(badges, channel);
            var badgesurlTask = badgesdb.GetBadgesUrlbadges(badges);

            await Task.WhenAll(customBadgesTask, badgesurlTask);

            var customBadges = customBadgesTask.Result;
            var badgesurl = badgesurlTask.Result;

            if (customBadges != null)
            {
                foreach (var (setId, url) in customBadges)
                {
                    badgesurl[setId.ToLower()] = url;
                }
            }

            var res = new List<string>();
            foreach (var setId in badges)
            {
                if (badgesurl.TryGetValue(setId.ToLowerInvariant(), out var url))
                {
                    res.Add(url);
                }
            }

            return res;
        }

        public async Task onMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            var message = e.ChatMessage.Message;
            if (string.IsNullOrEmpty(message) || message.StartsWith('!')) return;

            var channel = e.ChatMessage.Channel;
            var color = string.IsNullOrEmpty(e.ChatMessage.HexColor) ? "#ffc107" : e.ChatMessage.HexColor;
            var user = e.ChatMessage.Username;
            var rawBadges = e.ChatMessage.Badges.Select(b => b.Key).ToList();

            using var scope = _scope.CreateScope();

            try
            {
                var badges = FormatBadges(rawBadges);

                // Parallelize fetching custom/global badges and user profile
                var badgesTask = GetBadgesAsync(badges, channel, scope);
                var profileTask = GetProfile(user, scope);

                await Task.WhenAll(badgesTask, profileTask);

                var data = new Dictionary<string, object>
                {
                    ["username"] = user,
                    ["content"] = message,
                    ["color"] = color,
                    ["profile"] = profileTask.Result,
                    ["badges"] = badgesTask.Result
                };

                await _signalR.Send(channel, data);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        internal async Task onCommandReceived(object? sender, OnChatCommandReceivedArgs e)
        {
            var command = e.Command.Name;
            var channel = e.ChatMessage.Channel;
            var username = e.ChatMessage.Username;
            var user = e.ChatMessage.UserId;
            var messageID = e.ChatMessage.Id.ToString();

            using var scope = _scope.CreateScope();

            switch (command.ToLowerInvariant())
            {
                case "hora":
                    await HandleTimeCommand(channel);
                    break;
                case "color":
                    await HandleColorCommand(channel, messageID, e.Command.ArgumentsAsList);
                    break;
                case "join":
                    await HandleJoinCommand(username, channel, user, messageID, scope);
                    break;
                case "counter":
                case "status":
                case "notifier":
                case "chat":
                    if (_client != null)
                    {
                        await _client.SendReplyAsync(channel, messageID, $"https://botserver-qccm.onrender.com/{command}/{username.ToLower()}");
                    }
                    break;
                case "win":
                case "lose":
                case "reset":
                    await HandleCounterCommand(channel, username, command);
                    break;
                case "help":
                    if (_client != null)
                    {
                        await _client.SendReplyAsync(channel, messageID, "Comandos: Test: !hora, Unir a canal: !join, Url contador: !counter, Url estado: !status, Url chat: !chat, Comandos contador: !win, !lose, !reset");
                    }
                    break;
            }
        }

        private async Task HandleJoinCommand(string username, string channel, string userId, string messageID, IServiceScope scope)
        {
            var db = scope.ServiceProvider.GetRequiredService<ChannelsService>();
            var exists = await db.FindChannel(username);
            if (exists == null)
            {
                try
                {
                    await _api.GetCustomBadges(userId, channel);

                    if (_client != null)
                    {
                        await _client.JoinChannelAsync(username);
                    }
                    await db.AddChannel(username);

                    var urlAuth = AppEnvironment.IsRemote
                        ? "https://botserver-qccm.onrender.com/connect"
                        : "http://localhost:8000/connect";

                    if (_client != null)
                    {
                        await _client.SendReplyAsync(channel, messageID, $"Para dar permisos usa el siguiente enlace: {urlAuth}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            else
            {
                if (_client != null)
                {
                    await _client.SendReplyAsync(channel, messageID, "Ya estoy unido a tu canal");
                }
            }
        }

        private async Task HandleCounterCommand(string channel, string username, string counterStatus)
        {
            if (username.Equals(channel, StringComparison.OrdinalIgnoreCase))
            {
                await _signalR.UpdateCounter(channel, counterStatus);
            }
        }

        private async Task HandleTimeCommand(string channel)
        {
            var broadcasterTask = _api.GetUserData(channel);
            var botTask = _api.GetUserData("xhipibotx");
            var tokenTask = _api.GetAppToken();

            await Task.WhenAll(broadcasterTask, botTask, tokenTask);

            var broadcaster = broadcasterTask.Result;
            var bot = botTask.Result;
            var token = tokenTask.Result;

            TimeZoneInfo zona = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            var time = TimeZoneInfo.ConvertTime(DateTime.UtcNow, zona);
            var timeToString = time.ToString();

            await _api.SendChatMessage(broadcaster.TwitchId, bot.TwitchId, timeToString, token);
        }

        private async Task HandleColorCommand(string channel, string messageId, List<string> argumentsAsList)
        {
            try
            {
                if (argumentsAsList.Count > 0 && !string.IsNullOrWhiteSpace(argumentsAsList[0]))
                {
                    var color = argumentsAsList[0];
                    if (!color.StartsWith('#'))
                    {
                        color = "#" + color;
                    }

                    await _signalR.ChangeColor(channel, color);
                    return;
                }

                var broadcasterTask = _api.GetUserData(channel);
                var botTask = _api.GetUserData("xhipibotx");
                var tokenTask = _api.GetAppToken();

                await Task.WhenAll(broadcasterTask, botTask, tokenTask);

                await _api.ReplyChatMessage(broadcasterTask.Result.TwitchId, botTask.Result.TwitchId, "Debes pasar un color en formato Hexadecimal como parametro", tokenTask.Result, messageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        internal async Task onUserJoined(object? sender, OnUserJoinedArgs e)
        {
            using var scope = _scope.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UsersService>();
            var usu = e.Username;
            var exists = await db.GetUser(usu);

            if (exists == null)
            {
                var user = await _api.GetUserData(usu);
                await db.CreateUser(user);
            }
        }

        internal async Task newSub(object? sender, OnNewSubscriberArgs e)
        {
            using var scope = _scope.CreateScope();
            var usersDb = scope.ServiceProvider.GetRequiredService<UsersService>();
            var db = scope.ServiceProvider.GetRequiredService<ChannelsService>();

            var channel = e.Channel;

            var userTask = usersDb.GetUser(channel);
            var channelDataTask = db.FindChannel(channel);

            await Task.WhenAll(userTask, channelDataTask);

            var user = userTask.Result;
            var channelData = channelDataTask.Result;

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
