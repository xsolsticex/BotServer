using BotServer.API;
using BotServer.TwitchBotClient.SignalRClient;
using TwitchLib.Client;
using TwitchLib.Client.Events;

namespace BotServer.TwitchBotClient
{
    public class BotEventHandler
    {

        private TwitchClient _client;
        private TwitchBotApi _api;
        private BotSignalRClient _signalR;

        public BotEventHandler(BotSignalRClient signalR, TwitchBotApi api)
        {
            _signalR = signalR;
            _api = api;
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

            var profile = await _api.GetUserProfile(user);

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
            if (command == "hora")
            {
                var time = DateTime.Now.ToString();
                await _client.SendMessageAsync(channel, time);
            }
            Console.WriteLine(command);
        }
    }
}
