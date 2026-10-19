using BotServer.API;
using BotServer.Database.Models;
using BotServer.Database.Services;
using BotServer.TwitchBotClient.SignalRClient;
using TwitchLib.Client;
using TwitchLib.Client.Events;

namespace BotServer.TwitchBotClient
{
    public class BotEventHandler
    {

        private TwitchClient _client;
        private TwitchBotApi _api;
        private IServiceScopeFactory _scope;
        private BotSignalRClient _signalR;

        public bool eventsInitialized { get; set; } = false;

        public BotEventHandler(BotSignalRClient signalR, TwitchBotApi api, IServiceScopeFactory scope)
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


        private async Task RegisterNewEvent(string eventType, Users broadcaster, Dictionary<string, string> condition, string level, string bot = null)
        {
            var exists = false;

            if (string.IsNullOrEmpty(bot))
            {
                exists = await _api.CheckEventSubs(eventType, broadcaster.TwitchId);

            }
            else
            {
                exists = await _api.CheckEventSubs(eventType, broadcaster.TwitchId,bot);
            }

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

        private void ShowMessage(ConsoleColor color,string message)
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

                using var service = _scope.CreateScope();
                var db = service.ServiceProvider.GetRequiredService<ChannelsService>();
                var tokensdb = service.ServiceProvider.GetRequiredService<TokensService>();


                //Get broadcaster and bot ID
                var user = await _api.GetUserData(channel);
                var bot = await _api.GetUserData("xhipibotx");


                //Register follows and messages EventSubs
                var condition = new Dictionary<string, string> { { "broadcaster_user_id", user.TwitchId }, { "moderator_user_id", user.TwitchId } };

                var messagesCondition = new Dictionary<string, string> { { "broadcaster_user_id", user.TwitchId }, { "user_id", bot.TwitchId } };

                await RegisterNewEvent("channel.follow",user,condition,"2");

                await RegisterNewEvent("channel.chat.message", user, messagesCondition, "1",bot.TwitchId);

                await _api.GetCustomBadges(user.TwitchId, channel);

                Console.WriteLine($"Joined to {channel} channel");
            }
            catch (Exception a)
            {

                Console.WriteLine(a);
            }

        }

        private static List<string> FormatBadges(IEnumerable<string> badges)
        {
            return badges.Select(element =>
            {

                if (element == "bot-badge")
                {
                    element = "Chat Bot";
                }
                else if (element == "premium")
                {
                    element = "Prime Gaming";
                }

                if (element.Contains("-") || element.Contains("_"))
                {
                    string name = string.Empty;

                    var newName = element.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);


                    return string.Join(" ", newName.Select(w => char.ToUpper(w[0]) + w.Substring(1)));

                }
                else
                {
                    return element;
                }
            }).ToList();



        }


        private async Task<string> GetProfile(string username, IServiceScope scope)
        {
            var db = scope.ServiceProvider.GetRequiredService<UsersService>();
            var usu = await db.GetUser(username);
            string profile = string.Empty;

            profile = usu == null ? await _api.GetUserProfile(username) : profile = usu.Profile;

            return profile;
        }

        private async Task<List<string>> GetBadgesAsync(List<string> badges, string channel, IServiceScope scope)
        {
            var db = scope.ServiceProvider.GetRequiredService<UsersService>();
            var badgesdb = scope.ServiceProvider.GetRequiredService<GlobalBadgesService>();
            var custombadgeDb = scope.ServiceProvider.GetRequiredService<CustomBadgesServices>();

            //Obtener las customs
            var customBadges = await custombadgeDb.GetUrlCustomBadges(badges, channel);


            //Obtiene las globales
            var badgesurl = await badgesdb.GetBadgesUrlbadges(badges);


            if (customBadges != null)
            {
                foreach (var (setId, url) in customBadges)
                {
                    badgesurl[setId.ToLower()] = url;
                }

            }


            List<string> res = new();

            foreach (var setId in badges)
            {
                if (badgesurl.TryGetValue(setId.ToLowerInvariant(), out var url))
                {
                    res.Add(badgesurl[url]);
                }

            }

            return res;
        }

        public async Task onMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            var message = e.ChatMessage.Message;
            var channel = e.ChatMessage.Channel;
            var color = e.ChatMessage.HexColor;
            var user = e.ChatMessage.Username;
            var badges = e.ChatMessage.Badges.Select(b => b.Key).ToList();
            using var scope = _scope.CreateScope();

            try
            {
                if (message.StartsWith("!")) return;

                color = string.IsNullOrEmpty(color) ? "#ffc107" : color;

                badges = FormatBadges(badges);


                List<string> res = await GetBadgesAsync(badges, channel, scope);

                string profile = await GetProfile(user, scope);

                Dictionary<string, object> data = new Dictionary<string, object>
                {
                    ["username"] = user,
                    ["content"] = message,
                    ["color"] = color,
                    ["profile"] = profile,
                    ["badges"] = res
                };

                await _signalR.Send(channel, data);
            }
            catch (Exception c)
            {

                Console.WriteLine(c);
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



            switch (command)
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
                    await _client.SendReplyAsync(channel, messageID, $"https://botserver-qccm.onrender.com/counter/{username.ToLower()}");
                    break;

                case "status":
                    await _client.SendReplyAsync(channel, messageID, $"https://botserver-qccm.onrender.com/status/{username.ToLower()}");
                    break;

                case "notifier":
                    await _client.SendReplyAsync(channel, messageID, $"https://botserver-qccm.onrender.com/notifier/{username.ToLower()}");
                    break;

                case "chat":
                    await _client.SendReplyAsync(channel, messageID, $"https://botserver-qccm.onrender.com/chat/{username.ToLower()}");
                    break;

                case "win":

                    await HandleCounterCommand(channel, username, "win");

                    break;

                case "help":
                    await _client.SendReplyAsync(channel, messageID, $"Comandos: Test: !hora, Unir a canal: !join, Url contador: !counter, Url estado: !status, Url chat: !chat, Comandos contador: !win, !lose, !reset");
                    break;

                case "lose":
                    await HandleCounterCommand(channel, username, "lose");
                    break;

                case "reset":
                    await HandleCounterCommand(channel, username, "reset");
                    break;
            }


        }

        private async Task HandleCounterCommand(string channel, string username, string counterStatus)
        {
            if (username.ToLower().Equals(channel.ToLower()))
            {
                await _signalR.UpdateCounter(channel, counterStatus);
            }
        }

        private async Task HandleTimeCommand(string channel)
        {
            var broadcaster = await _api.GetUserData(channel);
            var bot = await _api.GetUserData("xhipibotx");
            var token = await _api.GetAppToken();

            TimeZoneInfo zona = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            var time = TimeZoneInfo.ConvertTime(DateTime.UtcNow, zona);
            var timeToString = time.ToString();

            await _api.SendChatMessage(broadcaster.TwitchId, bot.TwitchId, timeToString, token);
        }

        private async Task HandleColorCommand(string channel, string messageId, List<string> argumentsAsList)
        {

            try
            {
                if ((argumentsAsList.Count > 0) && (string.IsNullOrWhiteSpace(argumentsAsList[0])))
                {
                    var color = argumentsAsList[0];

                    if (!color.StartsWith('#'))
                    {
                        color = "#" + color;
                    }

                    await _signalR.ChangeColor(channel, color);
                    return;
                }

                var broadcaster = await _api.GetUserData(channel);
                var bot = await _api.GetUserData("xhipibotx");
                var token = await _api.GetAppToken();
                await _api.ReplyChatMessage(broadcaster.TwitchId, bot.TwitchId, "Debes pasar un color en formato Hexadecimal como parametro", token, messageId);



            }
            catch (Exception a)
            {

                Console.WriteLine(a);
            }


        }


        //Commands Handlers
        private async Task HandleJoinCommand(string username, string channel, string userId, string messageID, IServiceScope scope)
        {
            var db = scope.ServiceProvider.GetRequiredService<ChannelsService>();
            var exists = await db.FindChannel(username);
            if (exists == null)
            {

                var cnd = new List<string> { "local", "remote" };
                var con = cnd[0];
                var urlOBS = $"http://localhost:8000/chat/{username}";
                var urlAuth = $"http://localhost:8000/connect";


                try
                {
                    await _api.GetCustomBadges(userId, channel);

                    await _client.JoinChannelAsync(username);
                    await db.AddChannel(username);


                    if (con == "remote")
                    {
                        urlOBS = $"https://botserver-qccm.onrender.com/chat/{username}";
                        urlAuth = $"https://botserver-qccm.onrender.com/connect";
                    }

                    await _client.SendReplyAsync(channel, messageID, $"Para dar permisos usa el siguiente enlace: {urlAuth}");
                }
                catch (Exception a)
                {

                    Console.WriteLine(a);
                }

            }
            else
            {
                await _client.SendReplyAsync(channel, messageID, "Ya estoy unido a tu canal");
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
                var profile = await _api.GetUserProfile(usu);

                await db.CreateUser(user);

            }

        }

        internal async Task newSub(object? sender, OnNewSubscriberArgs e)
        {

            using var scope = _scope.CreateScope();
            var usersDb = scope.ServiceProvider.GetRequiredService<UsersService>();
            var db = scope.ServiceProvider.GetRequiredService<ChannelsService>();


            var channel = e.Channel;

            var user = await usersDb.GetUser(channel);
            var channelData = await db.FindChannel(channel);

            if (channelData != null && user != null)
            {
                var followers = await _api.GetFollowers(user.Username, user.UserId);
                var subs = await _api.GetSubs(user.Username, user.UserId);

                await db.UpdateChannel(channel, followers, subs);
                await _signalR.UpdateFollowers(channel, new Dictionary<string, int> { { "followers", followers }, { "subs", subs } });

            }


        }
    }
}
