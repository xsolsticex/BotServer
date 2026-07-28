using Microsoft.AspNetCore.SignalR;
using TwitchLib.Api.Helix.Models.Moderation.CheckAutoModStatus;

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

        public async Task SendFromBot(string channel, Dictionary<string,object> message)
        {

            try
            {
                Console.WriteLine("Mensaje recibido del bot");
                await Clients.Group(channel).SendAsync("botMessage", message);
                //await Clients.Group(channel).SendAsync("RespuestaServer", message);
            }
            catch (Exception e)
            {

                Console.WriteLine(e);
            }

        }

        public async Task UpdateCounter(string channel,string counter_state)
        {
            Console.WriteLine("Enviando mensaje al contador");
            await Clients.Group(channel).SendAsync(counter_state);


        }
    }
}
