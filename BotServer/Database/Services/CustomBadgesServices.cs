using BotServer.Database.DTO;
using Microsoft.EntityFrameworkCore;

namespace BotServer.Database.Services;

public class CustomBadgesServices(TwitchDbContext context)
{
    public Task AddBadge(CustomBadge badge) => AddAllBadges([badge]);

    public async Task<Dictionary<string, string>> GetUrlCustomBadges(List<string> badges, string channel)
    {
        var requested = badges.Select(b => b.Trim().ToLowerInvariant()).ToHashSet(StringComparer.OrdinalIgnoreCase);
        var channelName = channel.ToLowerInvariant();
        var rows = await context.CustomBadges.AsNoTracking()
            .Where(b => b.Channel.ChannelName.ToLower() == channelName)
            .OrderByDescending(b => b.Id)
            .Select(b => new { b.SetId, b.BadgeUrl })
            .ToListAsync();

        // Also tolerate duplicate keys in databases that have not yet been cleaned up.
        return rows.Where(b => requested.Contains(b.SetId))
            .GroupBy(b => b.SetId, StringComparer.OrdinalIgnoreCase)
            .ToDictionary(group => group.Key, group => group.First().BadgeUrl, StringComparer.OrdinalIgnoreCase);
    }

    public Task<List<string>> GetUrlBadge(List<string> badges) =>
        context.GlobalBadges.Where(b => badges.Contains(b.name)).Select(b => b.url).ToListAsync();

    public async Task AddAllBadges(List<CustomBadge> badges)
    {
        if (badges == null || badges.Count == 0) return;

        // Group each channel independently; a mixed batch must never use the first channel for every row.
        foreach (var group in badges.Where(b => !string.IsNullOrWhiteSpace(b.Channel)
                && !string.IsNullOrWhiteSpace(b.SetId) && !string.IsNullOrWhiteSpace(b.BadgeUrl))
            .GroupBy(b => b.Channel.Trim(), StringComparer.OrdinalIgnoreCase))
        {
            var name = group.Key.ToLowerInvariant();
            var channelId = await context.JoinedChannels.Where(c => c.ChannelName.ToLower() == name)
                .Select(c => (int?)c.Id).FirstOrDefaultAsync();
            if (channelId == null) continue;

            await using var transaction = await context.Database.BeginTransactionAsync();
            foreach (var badge in group.GroupBy(b => b.SetId.Trim(), StringComparer.OrdinalIgnoreCase)
                .Select(items => items.Last()))
            {
                var setId = badge.SetId.Trim().ToLowerInvariant();
                // SQLite upsert is atomic, including simultaneous loads from different bot instances.
                await context.Database.ExecuteSqlInterpolatedAsync($"""
                    INSERT INTO "CustomBadges" ("ChannelId", "SetId", "BadgeUrl")
                    VALUES ({channelId.Value}, {setId}, {badge.BadgeUrl})
                    ON CONFLICT ("ChannelId", "SetId") DO UPDATE SET "BadgeUrl" = excluded."BadgeUrl";
                    """);
            }
            await transaction.CommitAsync();
        }
    }
}
