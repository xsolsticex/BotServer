using BotServer.Database.Models;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel;

namespace BotServer.Database.Services
{
    public class GlobalBadgesService
    {
        private TwitchDbContext _context;

        public GlobalBadgesService(TwitchDbContext context) {

            _context = context;

        }


        public async Task DropData()
        {
            var count = await _context.GlobalBadges.CountAsync();

            if (count > 0)
            {
                await _context.GlobalBadges.ExecuteDeleteAsync();
            }
        }


        public async Task<List<string>> GetBadgesUrlbadges(List<string> badges)
        {

            var badgesLower = badges.Select(b => b.ToLower()).ToList();

            return await _context.GlobalBadges
                .Where(b => badgesLower.Contains(b.name.ToLower()))
                .Select(b => b.url)
                .ToListAsync();
        }
        public async Task AddBadges(List<GlobalBadges> badges)
        {

            var existingNames = await _context.GlobalBadges
       .Select(b => b.name)
       .ToHashSetAsync();

            var newBadges = badges
                .Where(b => !existingNames.Contains(b.name))
                .ToList();

            if (newBadges.Count > 0)
            {
                await _context.GlobalBadges.AddRangeAsync(newBadges);
                await _context.SaveChangesAsync();
            }

        }
    }
}
