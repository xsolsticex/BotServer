using BotServer.Database.Services;
using BotServer.TwitchBotClient.SignalRClient;
using TwitchLib.Client;
using TwitchLib.Communication.Interfaces;

namespace BotServer.TwitchBotClient
{
    public class TwitchBot : BackgroundService
    {

        private TwitchClient _client;

        private BotEventHandler _events;
        private IServiceScopeFactory _scope;
        private BotSignalRClient _signalR;

        public TwitchBot(BotEventHandler events,IServiceScopeFactory scope, BotSignalRClient signalR)
        {
            
            _events = events;
            _scope = scope;
            _signalR = signalR;
        }

        public async Task Stop()
        {
            await _client.DisconnectAsync();
        }


        public async Task SendMessage(string channel, string message)
        {
            await _client.SendMessageAsync(channel, message);
        }



        public void RegisterEvents()
        {
            _client.OnConnected += _events.OnConnected;
            _client.OnJoinedChannel += _events.OnChannelJoined;
            _client.OnMessageReceived += _events.onMessageReceived;
            _client.OnChatCommandReceived += _events.onCommandReceived;
            _client.OnUserJoined += _events.onUserJoined;
            
        }

        public async Task JoinToChannels(List<string> channels)
        {
            var tasks = channels.Select(channel => _client.JoinChannelAsync(channel));

            await Task.WhenAll(tasks);

        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _signalR.StartClient();
            var main = Environment.GetEnvironmentVariable("MAIN_CHANNEL");
            using var fscope = _scope.CreateScope();

            var fac = fscope.ServiceProvider.GetRequiredService<TwitchClientFactory>();

            _client = await fac.Create(main,false);
      
            RegisterEvents();
            _events.Initialize(_client);

            await _client.ConnectAsync();

            var service = _scope.CreateScope();

            var db = service.ServiceProvider.GetRequiredService<ChannelsService>();

            var ch = await db.GetChannels();

            if(ch.Count == 0)
            {
                ch.Add(main);
                await db.AddChannel(main);
            }

            await JoinToChannels(ch);
            

            
            

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                // El host está cerrando
            }

            await _client.DisconnectAsync();
        }
    }
}
