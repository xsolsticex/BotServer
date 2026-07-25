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
            //Pendiende de añadir perfil de usuario

            var scope = _scope.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<UsersService>();

            var usu = await db.GetUser(user);


            var profile = usu.Profile;
            //var profile = await _api.GetUserProfile(user);

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
                    var time = DateTime.Now.ToString();
                    await _client.SendMessageAsync(channel, time);
                    break;

                case "join":
                    await _client.JoinChannelAsync(username);

                    await _client.SendMessageAsync(channel, $"Añade a tu OBS la fuente como navegador: https://botserver-qccm.onrender.com/chat/{username}");

                    await _client.SendMessageAsync(channel, "Para dar permisos usa el siguiente enlace: https://botserver-qccm.onrender.com/connect");
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
                var userid = await _api.GetUserId(usu);

                var profile = await _api.GetUserProfile(usu);


                Users user = new Users { Profile = profile, TwitchId = userid,Username=usu };
                await db.CreateUser(user);

            }






        }
    }
}
