using BotServer.TwitchBotClient.SignalRClient;
using TwitchLib.Client;

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

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await _signalR.StartClient();
            var main = Environment.GetEnvironmentVariable("MAIN_CHANNEL");
            using var fscope = _scope.CreateScope();

            var fac = fscope.ServiceProvider.GetRequiredService<TwitchClientFactory>();

            _client = await fac.Create(main,true);
      
            RegisterEvents();
            _events.Initialize(_client);

            await _client.ConnectAsync();
            await _client.JoinChannelAsync(main);

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
