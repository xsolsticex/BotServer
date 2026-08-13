using BotServer.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BotServer.Database.Services
{
    public class GlobalBadgesService
    {
        private TwitchDbContext _context;

        public GlobalBadgesService(TwitchDbContext context)
        {

            _context = context;

        }


        public async Task DropData()
        {
            var count = await _context.GlobalBadges.CountAsync();

            if (count > 0)
            {
                await _context.GlobalBadges.ExecuteDeleteAsync();
                await _context.Database.ExecuteSqlRawAsync("DELETE FROM sqlite_sequence WHERE name = 'GlobalBadges';");
            }
        }


        /// <summary>
        /// Gets Badges Url
        /// </summary>
        /// <param name="badges"></param>
        /// <returns>Dictionary<string,string> GlobalBadgesUrl</returns>
        public async Task<Dictionary<string, string>> GetBadgesUrlbadges(List<string> badges)
        {


            var badgesLower = badges.Select(b => b.ToLower().Replace("®", "").Replace("™", "").Replace(":", "").Trim()).ToList();

            var results = await _context.GlobalBadges
    .Where(b => badgesLower.Contains(
        b.name
            .ToLower()
            .Replace("®", "")
            .Replace("™", "")
            .Replace(":", "")
            .Trim()))
    .ToListAsync();

            return results.ToDictionary(
    b => b.name
        .ToLower()
        .Replace("®", "")
        .Replace("™", "")
        .Replace(":", "")
        .Trim(),
    b => b.url);
        }
        public async Task AddBadges(List<GlobalBadges> badges)
        {

            var existingNames = await _context.GlobalBadges
       .Select(b => b.url)
       .ToHashSetAsync();

            var newBadges = badges
                .Where(b => !existingNames.Contains(b.url))
                .ToList();

            if (newBadges.Count > 0)
            {
                await _context.GlobalBadges.AddRangeAsync(newBadges);
                await _context.SaveChangesAsync();
            }

        }
    }
}
