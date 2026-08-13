using BotServer.Database.DTO;
using BotServer.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BotServer.Database.Services
{
    public class CustomBadgesServices
    {
        private TwitchDbContext _context;

        public CustomBadgesServices(TwitchDbContext context)
        {

            _context = context;

        }


        public async Task AddBadge(CustomBadge badge)
        {
            //Find Channel
            var channel = _context.JoinedChannels.Where(c => c.ChannelName == badge.Channel).FirstOrDefault();

            if (channel != null)
            {
                //Find badge
                var custom_exists = _context.CustomBadges.Where(c => c.ChannelId == channel.Id).Where(b => b.SetId == badge.SetId).FirstOrDefault();
                if (custom_exists == null)
                {
                    var newBadge = new CustomBadges { ChannelId = channel.Id, BadgeUrl = badge.BadgeUrl, SetId = badge.SetId };
                    await _context.CustomBadges.AddAsync(newBadge);
                    await _context.SaveChangesAsync();

                }
            }
        }

        public async Task<Dictionary<string,string>> GetUrlCustomBadges(List<string> badges, string channel)
        {
            var badgesLower = badges.Select(badge => badge.ToLower()).ToList();

            //Buscar canal
            int ch = _context.JoinedChannels.Where(chan => chan.ChannelName == channel).Select(c => c.Id).FirstOrDefault();

            //Buscar badges del canal
            var csm = await _context.CustomBadges.Where(canal => canal.ChannelId == ch).ToDictionaryAsync(c => c.SetId,c=>c.BadgeUrl);


           // List<string> customBadgesUrl = _context.CustomBadges.Where(canal => canal.ChannelId == ch).Select(c => c.BadgeUrl).ToList();


            if (csm.Count() != 0)
            {
                return csm;
            }
            return null;

        }

        public async Task<List<string>> GetUrlBadge(List<string> badges)
        {
            return _context.GlobalBadges.Where(b => badges.Contains(b.name)).Select(b => b.url).ToList();





            //Find badge

            //    if (custom_exists == null)
            //    {
            //        var newBadge = new CustomBadges { ChannelId = channel.Id, BadgeUrl = badge.BadgeUrl, SetId = badge.SetId };
            //        await _context.CustomBadges.AddAsync(newBadge);
            //        await _context.SaveChangesAsync();

            //    }
            //}


        }

        public async Task AddAllBadges(List<CustomBadge> badges)
        {
            if (badges == null || badges.Count == 0)
                return;

            var channel = _context.JoinedChannels.FirstOrDefault(c => c.ChannelName == badges[0].Channel);

            if (channel == null)
                return;

            var existingSetIds = _context.CustomBadges.Where(b => b.ChannelId == channel.Id).Select(b => b.SetId).ToList();

            var newBadges = badges
                .Where(b => !existingSetIds.Contains(b.SetId))
                .Select(b => new CustomBadges
                {
                    ChannelId = channel.Id,
                    SetId = b.SetId,
                    BadgeUrl = b.BadgeUrl
                })
                .ToList();

            if (newBadges.Any())
            {
                await _context.CustomBadges.AddRangeAsync(newBadges);
                await _context.SaveChangesAsync();
            }
        }
    }
}
