using BotServer.API;
using BotServer.API.Spotify;
using BotServer.Database.Services;
using BotServer.TwitchBotClient.SignalRClient;
using TwitchLib.Client;

namespace BotServer.TwitchBotClient
{
    /// <summary>
    /// Servicio en segundo plano encargado de gestionar el bot de Twitch.
    /// </summary>
    /// <remarks>
    /// Administra la conexión con Twitch, el registro de eventos,
    /// la incorporación a canales y la comunicación con SignalR.
    /// </remarks>
    public class TwitchBot : BackgroundService
    {
        private TwitchClient? _client;
        private readonly BotEventHandler _events;
        private readonly IServiceScopeFactory _scope;
        private readonly IBotSignalRClient _signalR;
        private readonly SpotyClient _spoty;
        private readonly ILogger<TwitchBot> _logger;
        private string? _mainChannel;

        public TwitchBot(
            BotEventHandler events,
            IServiceScopeFactory scope,
            IBotSignalRClient signalR,
            SpotyClient spoty,
            ILogger<TwitchBot> logger)
        {
            _events = events;
            _scope = scope;
            _signalR = signalR;
            _spoty = spoty;
            _logger = logger;
        }

        public async Task Stop()
        {
            if (_client != null)
            {
                await _client.DisconnectAsync();
            }
        }

        public async Task SendMessage(string channel, string message)
        {
            if (_client != null)
            {
                await _client.SendMessageAsync(channel, message);
            }
        }

        public void RegisterEvents()
        {
            if (_client == null) return;

            _client.OnConnected += _events.OnConnected;
            _client.OnJoinedChannel += _events.OnChannelJoined;
            _client.OnMessageReceived += _events.onMessageReceived;
            _client.OnChatCommandReceived += _events.onCommandReceived;
            _client.OnUserJoined += _events.onUserJoined;
            _client.OnNewSubscriber += _events.newSub;
        }

        public async Task JoinToChannels(List<string> channels)
        {
            if (_client == null) return;

            var tasks = channels.Select(channel => _client.JoinChannelAsync(channel));
            await Task.WhenAll(tasks);
        }

        public async Task InitializeTwitch()
        {
            _mainChannel = Environment.GetEnvironmentVariable("MAIN_CHANNEL") ?? "xhipibotx";

            using var fscope = _scope.CreateScope();
            var fac = fscope.ServiceProvider.GetRequiredService<TwitchClientFactory>();

            _client = await fac.Create(_mainChannel, false);

            RegisterEvents();
            _events.Initialize(_client);

            await _client.ConnectAsync();
        }

        public async Task InitializeChannels()
        {
            using var service = _scope.CreateScope();
            var db = service.ServiceProvider.GetRequiredService<ChannelsService>();
            var badges = service.ServiceProvider.GetRequiredService<GlobalBadgesService>();

            await badges.DropData();

            var ch = await db.GetChannels();

            if (ch.Count == 0 && !string.IsNullOrEmpty(_mainChannel))
            {
                ch.Add(_mainChannel);
                await db.AddChannel(_mainChannel);
            }

            await JoinToChannels(ch);
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            try
            {
                await _signalR.StartClient();
                await InitializeTwitch();
                await InitializeChannels();

                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException)
            {
                _logger.LogInformation("TwitchBot service is stopping.");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "An error occurred while running TwitchBot service.");
            }
            finally
            {
                if (_client != null)
                {
                    await _client.DisconnectAsync();
                }
            }
        }
    }
}
