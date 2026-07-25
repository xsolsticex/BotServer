using Microsoft.AspNetCore.SignalR;

namespace BotServer.Hubs
{
    public class ChatHub : Hub
    {
        public async Task Join(string channel)
        {
            Console.WriteLine($" Se une al canal : {channel}");
            await Groups.AddToGroupAsync(Context.ConnectionId, channel);
        }


        public async Task SendToClient(string channel,string message)
        {
            Console.WriteLine("Enviando mensaje");
            await Clients.Group(channel).SendAsync("RespuestaServer", message);
        }

        public async Task SendFromBot(string channel, Dictionary<string,string> message)
        {
            Console.WriteLine("Mensaje recibido del bot");
            await Clients.Group(channel).SendAsync("botMessage", message);
            //await Clients.Group(channel).SendAsync("RespuestaServer", message);
        }
    }
}
