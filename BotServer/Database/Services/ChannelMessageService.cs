using BotServer.Database.Models;

namespace BotServer.Database.Services
{
    public class ChannelMessageService
    {
        private TwitchDbContext _context;

        public ChannelMessageService(TwitchDbContext context)
        {

            _context = context;
        }

        public async Task AddMessage(string message,string channel)
        {
            var channel_exists = _context.JoinedChannels.Where(ch => ch.ChannelName == channel).FirstOrDefault();

            if (channel_exists != null) { 
            
                var msg = _context.ChannelMessages.Where(m => m.Message == message).FirstOrDefault();

                if (msg == null) {

                    await _context.ChannelMessages.AddAsync(new ChannelMessages {ChannelId=channel_exists.Id,Message=message });
                    await _context.SaveChangesAsync();
                
                }
            
            }
        }
    }
}
