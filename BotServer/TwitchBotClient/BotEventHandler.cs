using BotServer.API;
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

        public async Task OnChannelJoined(object? sender, OnJoinedChannelArgs e)
        {
            try
            {
                var service = _scope.CreateScope();
                var db = service.ServiceProvider.GetRequiredService<ChannelsService>();
                var channel = e.Channel;
                var user = await _api.GetUserData(channel);

                await _api.GetCustomBadges(user.TwitchId, channel);

                Console.WriteLine($"Joined to {channel} channel");
            }
            catch (Exception a)
            {

                Console.WriteLine(a);
            }

        }

        public async Task onMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            var message = e.ChatMessage.Message;
            var channel = e.ChatMessage.Channel;
            var color = e.ChatMessage.HexColor;
            var user = e.ChatMessage.Username;
            var badges = e.ChatMessage.Badges.Select(b => b.Key).ToList();

            if (string.IsNullOrEmpty(color))
            {
                color = "#ffc107";
            }


            badges = badges.Select(element =>
            {

                if (element == "bot-badge")
                {
                    element = "Chat Bot";
                }else if(element == "premium")
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
            //badges.Add("subscriber");
            //badges.Add("Final Fantasy Xiv Fan Festival 2026 Eu Content Unlock Quest Chat");
            //badges.Add("Lego Batman Legacy Of The Dark Knight");

            try
            {
                if (message.StartsWith("!")) return;
                //if (message.StartsWith("!join") || message.StartsWith("!win") || message.StartsWith("!lose") || message.StartsWith("!nowin") || message.StartsWith("!counter") || message.StartsWith("!chat") || message.StartsWith("!nolose") || message.StartsWith("!reset")) return;
                //Pendiende de añadir perfil de usuario

                var scope = _scope.CreateScope();
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
               
                    res.Add(badgesurl[setId.ToLower()]);
                }

                var usu = await db.GetUser(user);
                string profile = string.Empty;
                if (usu == null)
                {
                    profile = await _api.GetUserProfile(user);
                }
                else
                {
                    profile = usu.Profile;
                }

                var data = new Dictionary<string, object>();
                data.Add("username", user);
                data.Add("content", message);
                data.Add("color", color);
                data.Add("profile", profile);
                data.Add("badges", res);


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

            switch (command)
            {
                case "hora":
                    TimeZoneInfo zona = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
                    var time = TimeZoneInfo.ConvertTime(DateTime.UtcNow, zona);
                    var timeToString = time.ToString();
                    await _client.SendMessageAsync(channel, timeToString);
                    break;

                 case "join":

                    
                    var service = _scope.CreateScope();
                    var db = service.ServiceProvider.GetRequiredService<ChannelsService>();
                    var exists = await db.FindChannel(username);
                
                    if (exists == null)
                    {

                        var cnd = new List<string> { "local", "remote" };
                        var con = cnd[0];
                        var urlOBS = $"http://localhost:8000/chat/{username}";
                        var urlAuth = $"http://localhost:8000/connect";


                        try
                        {
                            await _api.GetCustomBadges(user, channel);

                            await _client.JoinChannelAsync(username);
                            await db.AddChannel(username);
              

                            if (con == "remote")
                            {
                                urlOBS = $"https://botserver-qccm.onrender.com/chat/{username}";
                                urlAuth = $"https://botserver-qccm.onrender.com/connect";
                            }
                        }
                        catch (Exception a)
                        {

                            Console.WriteLine(a);
                        }
 

                        //await _client.SendReplyAsync(channel, e.ChatMessage.Id.ToString(), $"Añade a tu OBS la fuente como navegador: {urlOBS}");

                        await _client.SendReplyAsync(channel, messageID, $"Para dar permisos usa el siguiente enlace: {urlAuth}");


                    }
                    else
                    {
                        await _client.SendReplyAsync(channel, messageID, "Ya estoy unido a tu canal");
                    }

                    break;

                case "counter":
                    await _client.SendReplyAsync(channel, messageID, $"https://botserver-qccm.onrender.com/counter/{username.ToLower()}");
                    break;

                case "status":
                    await _client.SendReplyAsync(channel, messageID, $"https://botserver-qccm.onrender.com/status/{username.ToLower()}");
                    break;

                case "chat":
                    await _client.SendReplyAsync(channel, messageID, $"https://botserver-qccm.onrender.com/chat/{username.ToLower()}");
                    break;

                case "win":
                    await _signalR.UpdateCounter(channel, "win");
                    break;

                case "help":
                    await _client.SendReplyAsync(channel, messageID, $"Comandos: Test: !hora, Unir a canal: !join, Url contador: !counter, Url estado: !status, Url chat: !chat, Comandos contador: !win, !lose, !reset");
                    break;

                //case "sub":
                //    await _signalR.UpdateFollowers(channel, new Dictionary<string, int> { {"followers",50 },{"subs",20 } });
                //    break;

                case "lose":
                    await _signalR.UpdateCounter(channel, "lose");
                    break;

                case "reset":
                    await _signalR.UpdateCounter(channel, "reset");
                    break;
            }

            Console.WriteLine(command);
        }

        internal async Task onUserJoined(object? sender, OnUserJoinedArgs e)
        {
            var scope = _scope.CreateScope();
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

        internal async Task SubsHandler(object? sender, EventArgs e)
        {
            
        }

        internal async Task newSub(object? sender, OnNewSubscriberArgs e)
        {

            var scope = _scope.CreateScope();
            var usersDb = scope.ServiceProvider.GetRequiredService<UsersService>();
            var db = scope.ServiceProvider.GetRequiredService<ChannelsService>();


            var channel = e.Channel;

            var user = await usersDb.GetUser(channel);
            var channelData = await db.FindChannel(channel);

            if(channelData != null && user != null)
            {
                var followers = await _api.GetFollowers(user.Username, user.UserId);
                var subs = await _api.GetFollowers(user.Username, user.UserId);

                await db.UpdateChannel(channel, followers, subs);
                await _signalR.UpdateFollowers(channel, new Dictionary<string, int> { { "followers", followers }, { "subs", subs } });
               
            }
    
            
        }
    }
}
