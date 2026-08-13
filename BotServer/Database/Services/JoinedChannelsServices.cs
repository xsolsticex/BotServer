using SQLitePCL;
using TwitchLib.Client.Models;

namespace BotServer.Database.Services
{
    public class JoinedChannelsServices
    {
        private TwitchDbContext _context;

        public JoinedChannelsServices(TwitchDbContext context) {

            _context = context;
        }

        public async Task<int> GetChannelId(string name)
        {
            return _context.JoinedChannels.Where(c=> c.ChannelName == name).Select(c=>c.Id).FirstOrDefault();
        }
    }
}
