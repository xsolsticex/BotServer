using BotServer.Database.Models;
using SQLitePCL;

namespace BotServer.Database.Services
{
    public class ChannelsService
    {
        private TwitchDbContext _context;

        public ChannelsService(TwitchDbContext context) {

            _context = context;
        }


        public async Task AddChannel(string name,int followers = 0,int subs=0)
        {
            var channel = _context.JoinedChannels.Where(ch => ch.ChannelName.ToLower() == name.ToLower()).FirstOrDefault();

            if (channel == null) {

                await _context.JoinedChannels.AddAsync(new JoinedChannels {ChannelName = name,Followers=followers,Subs=subs });
                await _context.SaveChangesAsync();
            }

        }

        public async Task UpdateChannel(string name, int followers = 0, int subs = 0)
        {
            var channel = _context.JoinedChannels.Where(ch => ch.ChannelName.ToLower() == name.ToLower()).FirstOrDefault();

            if (channel != null)
            {
                channel.Followers = followers;
                channel.Subs = subs;
                await _context.SaveChangesAsync();
            }

        }

        public async Task<List<string>> GetChannels()
        {
            var channels = _context.JoinedChannels.Select(ch => ch.ChannelName).ToList();
            return channels;

        }

        public async Task<JoinedChannels> FindChannel(string name)
        {
            var channel = _context.JoinedChannels.Where(ch => ch.ChannelName.ToLower() == name).FirstOrDefault();
            return channel;

        }
    }
}
