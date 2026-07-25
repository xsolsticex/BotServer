using Microsoft.AspNetCore.SignalR.Client;
using System.Diagnostics;

namespace BotServer.TwitchBotClient.SignalRClient
{
    public class BotSignalRClient
    {
        HubConnection connection;
        public BotSignalRClient()
        {
            connection = new HubConnectionBuilder().WithUrl("http://localhost:8000/chatHub").WithAutomaticReconnect().Build();

            connection.On<string>("botMessage", (message) => {

                Console.WriteLine($"Bot message: {message}");
            
            });

            

            
        }


        public async Task StartClient()
        {
            await connection.StartAsync();
        }


        public async Task Send(string channel,Dictionary<string,string> message)
        {
            var sw = Stopwatch.StartNew();

            await connection.SendAsync("SendFromBot", channel,message);

            Console.WriteLine($"SignalR: {sw.ElapsedMilliseconds} ms");
        }
    }
}
