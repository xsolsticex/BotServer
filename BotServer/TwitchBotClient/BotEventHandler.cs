using BotServer.API;
using BotServer.Database.Models;
using BotServer.Database.Services;
using BotServer.TwitchBotClient.SignalRClient;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using static System.Reflection.Metadata.BlobBuilder;

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
        }

        public async Task OnChannelJoined(object? sender, OnJoinedChannelArgs e)
        {
            var channel = e.Channel;



            await _client.SendMessageAsync(channel, "Connected to chat");

            Console.WriteLine($"Joined to {channel} channel");
        }

        public async Task onMessageReceived(object? sender, OnMessageReceivedArgs e)
        {
            var message = e.ChatMessage.Message;
            var channel = e.ChatMessage.Channel;
            var color = e.ChatMessage.HexColor;
            var user = e.ChatMessage.Username;

            if (message.StartsWith("!join") || message.StartsWith("!win") || message.StartsWith("!lose") || message.StartsWith("!nowin") || message.StartsWith("!nolose") || message.StartsWith("!reset")) return;
            //Pendiende de añadir perfil de usuario

            var scope = _scope.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UsersService>();

            var usu = await db.GetUser(user);

            var profile = usu.Profile;

            if (profile == null)
            {
                profile = await _api.GetUserProfile(user);
            }

            var data = new Dictionary<string, string>();
            data.Add("username", user);
            data.Add("content", message);
            data.Add("color", color);
            data.Add("profile", profile);

            try
            {
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

            switch (command)
            {
                case "hora":
                    TimeZoneInfo zona = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
                    var time = TimeZoneInfo.ConvertTime(DateTime.UtcNow,zona);
                    await _client.SendMessageAsync(channel, time.ToString());
                    break;

                case "join":

                    var service = _scope.CreateScope();

                    var db = service.ServiceProvider.GetRequiredService<ChannelsService>();

                    var exists = await db.FindChannel(username);

                    if(exists == null)
                    {
                        await _client.JoinChannelAsync(username);



                        await db.AddChannel(username);

                        var connection = new List<string> { "local", "remote" };
                        var con = connection[1];
                        var urlOBS = $"http://localhost:8000/chat/{username}";
                        var urlAuth = $"http://localhost:8000/connect";

                        if (con == "remote")
                        {
                            urlOBS = $"https://botserver-qccm.onrender.com/chat/{username}";
                            urlAuth = $"https://botserver-qccm.onrender.com/connect";
                        }

                        await _client.SendMessageAsync(channel, $"Añade a tu OBS la fuente como navegador: {urlOBS}");

                        await _client.SendMessageAsync(channel, $"Para dar permisos usa el siguiente enlace: {urlAuth}");

                    }
                    else
                    {
                        await _client.SendReplyAsync(channel,e.ChatMessage.Id.ToString(), "Ya estoy unido a tu canal");
                    }


                    break;

                case "win":
                    await _signalR.UpdateCounter(channel,"win");
                    break;

                case "lose":
                    await _signalR.UpdateCounter(channel,"lose");
                    break;
                case "reset":
                    await _signalR.UpdateCounter(channel,"reset");
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
    }
}
