using BotServer.Database.Models;
using Microsoft.EntityFrameworkCore;
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
            var channel = await _context.JoinedChannels.FirstOrDefaultAsync(ch => ch.ChannelName.ToLower() == name.ToLower());

            if (channel == null) {

                await _context.JoinedChannels.AddAsync(new JoinedChannels {ChannelName = name,Followers=followers,Subs=subs });
                await _context.SaveChangesAsync();
            }

        }

        public async Task UpdateChannel(string name, int followers = 0, int subs = 0)
        {
            var channel = await _context.JoinedChannels.FirstOrDefaultAsync(ch => ch.ChannelName.ToLower() == name.ToLower());

            if (channel != null)
            {
                channel.Followers = followers;
                channel.Subs = subs;
                await _context.SaveChangesAsync();
            }

        }

        public async Task<List<string>> GetChannels()
        {
            var channels = await _context.JoinedChannels.Select(ch => ch.ChannelName).ToListAsync();
            return channels;

        }

        public async Task<JoinedChannels?> FindChannel(string name)
        {
            var channel = await _context.JoinedChannels.FirstOrDefaultAsync(ch => ch.ChannelName.ToLower() == name.ToLower());
            return channel;

        }
    }
}
