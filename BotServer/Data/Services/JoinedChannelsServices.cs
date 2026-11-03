using Microsoft.EntityFrameworkCore;
using SQLitePCL;
using TwitchLib.Client.Models;

namespace BotServer.Data.Services
{
    public class JoinedChannelsServices
    {
        private TwitchDbContext _context;

        public JoinedChannelsServices(TwitchDbContext context) {

            _context = context;
        }

        public async Task<int> GetChannelId(string name)
        {
            return await _context.JoinedChannels.Where(c=> c.ChannelName == name).Select(c=>c.Id).FirstOrDefaultAsync();
        }
    }
}
