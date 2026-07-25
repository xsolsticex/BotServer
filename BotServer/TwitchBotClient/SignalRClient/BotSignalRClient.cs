using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;

namespace BotServer.TwitchBotClient.SignalRClient
{
    public class BotSignalRClient
    {
        HubConnection connection;
        public BotSignalRClient()
        {
            connection = new HubConnectionBuilder().WithUrl("https://botserver-qccm.onrender.com/chatHub").WithAutomaticReconnect().Build();

            connection.On<string>("botMessage", (message) =>
            {

                Console.WriteLine($"Bot message: {message}");

            });

            connection.Reconnecting += (error) =>
            {
                Console.WriteLine($"SignalR perdiendo conexión. Intentando reconectar... Motivo: {error?.Message}");
                return Task.CompletedTask;

            };

            connection.Reconnected += (connectionId) =>
            {
                Console.WriteLine($"SignalR reconectado con éxito. Nueva ID: {connectionId}");
                return Task.CompletedTask;
            };

            connection.Closed += async (error) =>
            {
                Console.WriteLine($"SignalR cerrado de forma definitiva: {error?.Message}. Reiniciando de forma manual...");
                await IntentarConectarConBucle(); // Fuerza el reinicio si falla la automática
            };




        }

        private async Task IntentarConectarConBucle()
        {
            while (connection.State == HubConnectionState.Disconnected)
            {
                try
                {
                    await connection.StartAsync();
                    Console.WriteLine("SignalR conectado correctamente.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Error al conectar: {ex.Message}. Reintentando en 5 segundos...");
                    await Task.Delay(5000);
                }
            }
        }


        public async Task StartClient()
        {
            try
            {
                await Task.Delay(5000); // Espera a que el servidor termine de arrancar
                await connection.StartAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


        public async Task Send(string channel, Dictionary<string, string> message)
        {
                        // 3. Validación de estado antes de enviar para evitar crashes
            if (connection.State != HubConnectionState.Connected)
            {
                Console.WriteLine($"No se pudo enviar el mensaje. El cliente está en estado: {connection.State}");
                return;
            }

            try
            {
                var sw = Stopwatch.StartNew();
                await connection.SendAsync("SendFromBot", channel, message);
                Console.WriteLine($"SignalR: {sw.ElapsedMilliseconds} ms");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error al enviar mensaje: {ex.Message}");
            }
        }
    }
}
