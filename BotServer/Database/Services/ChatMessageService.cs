using TwitchLib.Client.Models;


namespace BotServer.Database.Services
{
    public class ChatMessageService
    {
        public ChatMessageService() { 
        
        
        }

        public void ProcessMessage(ChatMessage msg)
        {
            var message = msg.Message;
            var channel = msg.Channel;
            var color = msg.HexColor;
            var user = msg.Username;
            var badges = msg.Badges.Select(b => b.Key).ToList();

            color = string.IsNullOrEmpty(color) ? "#ffc107" : color;
        }
    }
}
