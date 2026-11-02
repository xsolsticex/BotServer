using BotServer.Configuration;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.Logging;
using System.Diagnostics;

namespace BotServer.TwitchBotClient.SignalRClient
{
    public class BotSignalRClient : IBotSignalRClient
    {
        HubConnection connection;
        private readonly ILogger<BotSignalRClient> _logger;

        public BotSignalRClient(ILogger<BotSignalRClient> logger)
        {
            _logger = logger;
            if (AppEnvironment.IsLocal)
            {
                connection = new HubConnectionBuilder()
                    .WithUrl("http://localhost:8000/chatHub")
                    .WithAutomaticReconnect()
                    .Build();
            }
            else
            {
                connection = new HubConnectionBuilder()
                    .WithUrl("https://botserver-qccm.onrender.com/chatHub")
                    .WithAutomaticReconnect()
                    .Build();
            }

            connection.On<string>("botMessage", (message) =>
            {
                _logger.LogInformation("Bot message: {Message}", message);
            });

            connection.Reconnecting += (error) =>
            {
                _logger.LogWarning(error, "SignalR perdiendo conexión. Intentando reconectar...");
                return Task.CompletedTask;
            };

            connection.Reconnected += (connectionId) =>
            {
                _logger.LogInformation("SignalR reconectado con éxito. Nueva ID: {ConnectionId}", connectionId);
                return Task.CompletedTask;
            };

            connection.Closed += async (error) =>
            {
                _logger.LogError(error, "SignalR cerrado de forma definitiva. Reiniciando de forma manual...");
                await EnsureConnectedAsync();
            };
        }

        private async Task EnsureConnectedAsync()
        {
            if (connection.State == HubConnectionState.Connected)
                return;

            while (connection.State != HubConnectionState.Connected)
            {
                try
                {
                    if (connection.State == HubConnectionState.Disconnected)
                    {
                        await connection.StartAsync();
                        _logger.LogInformation("SignalR conectado correctamente.");
                    }
                    else
                    {
                        await Task.Delay(2000);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error al conectar SignalR. Reintentando en 5 segundos...");
                    await Task.Delay(5000);
                }
            }
        }

        public async Task StartClient()
        {
            await EnsureConnectedAsync();
        }

        public async Task Send(string channel, Dictionary<string, object> message)
        {
            await EnsureConnectedAsync();

            try
            {
                var sw = Stopwatch.StartNew();
                await connection.SendAsync("SendFromBot", channel, message);
                _logger.LogDebug("SignalR Send: {Elapsed} ms", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar mensaje SignalR: {Message}", ex.Message);
            }
        }

        public async Task SendFollow(string channel, Dictionary<string, string> message)
        {
            await EnsureConnectedAsync();

            try
            {
                var sw = Stopwatch.StartNew();
                await connection.SendAsync("SendFollower", channel, message);
                _logger.LogDebug("SignalR SendFollow: {Elapsed} ms", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al enviar mensaje SendFollow: {Message}", ex.Message);
            }
        }

        public async Task UpdateFollowers(string channel, Dictionary<string, int> message)
        {
            await EnsureConnectedAsync();

            try
            {
                var sw = Stopwatch.StartNew();
                await connection.SendAsync("UpdateFollowers", channel, message);
                _logger.LogDebug("SignalR UpdateFollowers: {Elapsed} ms", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar seguidores: {Message}", ex.Message);
            }
        }

        public async Task UpdateCounter(string channel, string counter_state)
        {
            await EnsureConnectedAsync();

            try
            {
                var sw = Stopwatch.StartNew();
                await connection.SendAsync("UpdateCounter", channel, counter_state);
                _logger.LogDebug("SignalR UpdateCounter: {Elapsed} ms", sw.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al actualizar contador: {Message}", ex.Message);
            }
        }

        public async Task ChangeColor(string channel, string color)
        {
            await EnsureConnectedAsync();

            try
            {
                await connection.SendAsync("ChangeColor", channel, color);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al cambiar color: {Message}", ex.Message);
            }
        }
    }
}
