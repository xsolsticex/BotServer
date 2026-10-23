
using BotServer.API;
using BotServer.Commands;
using BotServer.Commands.Interface;
using BotServer.Database.Services;
using BotServer.SpotifyClientApi;
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
        /// <summary>
        /// Cliente utilizado para comunicarse con el chat de Twitch.
        /// </summary>
        private TwitchClient _client;

        /// <summary>
        /// Gestor de eventos del cliente de Twitch.
        /// </summary>
        private BotEventHandler _events;

        private List<ITwitchCommand> botCommands;

        /// <summary>
        /// Factoría para crear ámbitos de servicios
        /// y resolver dependencias mediante inyección.
        /// </summary>
        private IServiceScopeFactory _scope;

        /// <summary>
        /// Cliente SignalR utilizado para establecer
        /// comunicación en tiempo real.
        /// </summary>
        private BotSignalRClient _signalR;

        /// <summary>
        /// Cliente de Spotify para acceder a sus funcionalidades.
        /// </summary>
        private SpotyClient _spoty;
        private TwitchBotApi _api;

        /// <summary>
        /// Nombre del canal principal de Twitch.
        /// Se obtiene de la variable de entorno MAIN_CHANNEL.
        /// </summary>
        private string? main;

        /// <summary>
        /// Inicializa una nueva instancia del servicio TwitchBot.
        /// </summary>
        /// <param name="events">
        /// Gestor responsable de procesar los eventos de Twitch.
        /// </param>
        /// <param name="scope">
        /// Factoría utilizada para crear ámbitos de servicios.
        /// </param>
        /// <param name="signalR">
        /// Cliente SignalR para la comunicación en tiempo real.
        /// </param>
        /// <param name="spoty">
        /// Cliente utilizado para interactuar con Spotify.
        /// </param>
        public TwitchBot(
            BotEventHandler events,
            IServiceScopeFactory scope,
            BotSignalRClient signalR,
            SpotyClient spoty)
        {
            _events = events;
            _scope = scope;
            _signalR = signalR;
            _spoty = spoty;

        }

        /// <summary>
        /// Desconecta el cliente del servidor de Twitch.
        /// </summary>
        /// <returns>
        /// Tarea que representa la operación asíncrona
        /// de desconexión.
        /// </returns>
        public async Task Stop()
        {
            await _client.DisconnectAsync();
        }

        /// <summary>
        /// Envía un mensaje a un canal de Twitch.
        /// </summary>
        /// <param name="channel">
        /// Nombre del canal al que se enviará el mensaje.
        /// </param>
        /// <param name="message">
        /// Contenido del mensaje que se desea enviar.
        /// </param>
        /// <returns>
        /// Tarea que representa la operación asíncrona de envío.
        /// </returns>
        public async Task SendMessage(string channel, string message)
        {
            await _client.SendMessageAsync(channel, message);
        }



        /// <summary>
        /// Registra los controladores de eventos del cliente de Twitch.
        /// </summary>
        /// <remarks>
        /// Asocia los eventos de conexión, entrada a canales,
        /// recepción de mensajes, comandos, nuevos usuarios
        /// y suscripciones con sus respectivos manejadores.
        /// </remarks>
        public void RegisterEvents()
        {
            _client.OnConnected += _events.OnConnected;
            _client.OnJoinedChannel += _events.OnChannelJoined;
            _client.OnMessageReceived += _events.onMessageReceived;
            _client.OnChatCommandReceived += _events.onCommandReceived;
            _client.OnUserJoined += _events.onUserJoined;
            _client.OnNewSubscriber += _events.newSub;
        }

        /// <summary>
        /// Incorpora el bot a una lista de canales de Twitch.
        /// </summary>
        /// <param name="channels">
        /// Lista de nombres de canales a los que debe unirse el bot.
        /// </param>
        /// <returns>
        /// Tarea que finaliza cuando se completan todas
        /// las operaciones de unión a los canales.
        /// </returns>
        /// <remarks>
        /// Las solicitudes de unión se ejecutan de forma concurrente
        /// mediante Task.WhenAll.
        /// </remarks>
        public async Task JoinToChannels(List<string> channels)
        {
            var tasks = channels.Select(
                channel => _client.JoinChannelAsync(channel)
            );

            await Task.WhenAll(tasks);
        }

        /// <summary>
        /// Inicializa el cliente de Twitch y establece su conexión.
        /// </summary>
        /// <returns>
        /// Tarea que representa el proceso de inicialización
        /// y conexión del cliente.
        /// </returns>
        /// <remarks>
        /// Obtiene el canal principal desde las variables de entorno,
        /// crea el cliente mediante TwitchClientFactory,
        /// registra los eventos y establece la conexión.
        /// </remarks>
        public async Task InitializeTwitch()
        {
            main = Environment.GetEnvironmentVariable("MAIN_CHANNEL");

            using var fscope = _scope.CreateScope();

            var fac = fscope.ServiceProvider
                .GetRequiredService<TwitchClientFactory>();

            _client = await fac.Create(main!, false);

            RegisterEvents();

            _events.Initialize(_client);

            await _client.ConnectAsync();
        }

        /// <summary>
        /// Inicializa los canales a los que debe conectarse el bot.
        /// </summary>
        /// <returns>
        /// Tarea que representa la carga y unión a los canales.
        /// </returns>
        /// <remarks>
        /// Elimina los datos existentes de insignias globales,
        /// obtiene los canales almacenados en la base de datos
        /// y añade el canal principal si no existen canales.
        /// Finalmente, incorpora el bot a los canales obtenidos.
        /// </remarks>
        public async Task InitializeChannels()
        {
            using var service = _scope.CreateScope();

            var db = service.ServiceProvider
                .GetRequiredService<ChannelsService>();

            var badges = service.ServiceProvider
                .GetRequiredService<GlobalBadgesService>();

            await badges.DropData();

            var ch = await db.GetChannels();

            if (ch.Count == 0)
            {
                ch.Add(main);
                await db.AddChannel(main);
            }

            await JoinToChannels(ch);
        }

        /// <summary>
        /// Ejecuta el ciclo de vida principal del bot
        /// como servicio en segundo plano.
        /// </summary>
        /// <param name="stoppingToken">
        /// Token que indica cuándo se solicita la detención
        /// del servicio.
        /// </param>
        /// <returns>
        /// Tarea que representa la ejecución del servicio.
        /// </returns>
        /// <remarks>
        /// Inicia SignalR, inicializa el cliente de Twitch
        /// y carga los canales configurados.
        ///
        /// Mantiene el servicio activo hasta recibir una
        /// solicitud de cancelación. Al finalizar,
        /// desconecta el cliente de Twitch.
        /// </remarks>
        protected override async Task ExecuteAsync(
            CancellationToken stoppingToken)
        {
            //await _spoty.GetSong();

            await _signalR.StartClient();

            await InitializeTwitch();

            await InitializeChannels();

            try
            {
                await Task.Delay(Timeout.Infinite, stoppingToken);
            }
            catch (OperationCanceledException a)
            {
                Console.WriteLine(a.Message);
            }

            await _client.DisconnectAsync();
        }
    }
}
