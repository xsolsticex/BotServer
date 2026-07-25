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

            connection.On<string>("botMessage", (message) => {

                Console.WriteLine($"Bot message: {message}");
            
            });

            

            
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


        public async Task Send(string channel,Dictionary<string,string> message)
        {
            var sw = Stopwatch.StartNew();

            await connection.SendAsync("SendFromBot", channel,message);

            Console.WriteLine($"SignalR: {sw.ElapsedMilliseconds} ms");
        }
    }
}
