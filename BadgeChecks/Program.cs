using BotServer.Database;
using BotServer.Database.DTO;
using BotServer.Database.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

var directory = Path.Combine(Path.GetTempPath(), "botserver-badges-" + Guid.NewGuid());
Directory.CreateDirectory(directory);
var file = Path.Combine(directory, "test.db");
var options = new DbContextOptionsBuilder<TwitchDbContext>().UseSqlite($"Data Source={file};Pooling=False").Options;
int passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    passed++;
    Console.WriteLine($"PASS: {name}");
}
CustomBadge Badge(string channel, string set, string url) => new() { Channel = channel, SetId = set, BadgeUrl = url };

try
{
    await using (var db = new TwitchDbContext(options))
    {
        // Reproduce production's old schema, including its global unique URL index.
        await db.GetService<IMigrator>().MigrateAsync("20261006134727_AddNewRowss");
        await db.Database.ExecuteSqlRawAsync("INSERT INTO JoinedChannels (ChannelName, Followers, Subs) VALUES ('one', 0, 0), ('two', 0, 0);");
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO CustomBadges (ChannelId, SetId, BadgeUrl) VALUES
                (1, 'Subscriber', 'https://badges.test/old'),
                (1, 'subscriber', 'https://badges.test/new');
            """);
        await db.Database.MigrateAsync();
        Check(await db.CustomBadges.CountAsync() == 2, "Migration preserves existing rows and URLs");
        Check(await db.CustomBadges.CountAsync(b => b.SetId.StartsWith("legacy:")) == 1,
            "Legacy duplicate keys are retained under distinct keys");
        Check(!db.Database.HasPendingModelChanges(), "Model, migration and snapshot agree");

        var service = new CustomBadgesServices(db);
        await service.AddBadge(Badge("two", "subscriber", "https://badges.test/new"));
        Check(await db.CustomBadges.CountAsync(b => b.BadgeUrl == "https://badges.test/new") == 2,
            "Same URL can belong to different channels without SQLite error 19");

        await service.AddAllBadges([
            Badge("one", "VIP", "https://badges.test/first"),
            Badge("ONE", "vip", "https://badges.test/latest"),
            Badge("two", "vip", "https://badges.test/shared"),
            Badge("missing", "vip", "https://badges.test/ignored")
        ]);
        Check(await db.CustomBadges.CountAsync(b => b.SetId == "vip") == 2,
            "Deduplicates incoming keys and routes mixed batches to their own channels");
        var read = await service.GetUrlCustomBadges(["vip", "subscriber"], "ONE");
        Check(read["VIP"] == "https://badges.test/latest" && read["subscriber"] == "https://badges.test/new",
            "Lookup uses real set IDs, is case insensitive and tolerates legacy data");

        await service.AddBadge(Badge("one", "vip", "https://badges.test/updated"));
        await service.AddBadge(Badge("one", "vip", "https://badges.test/updated"));
        Check(await db.CustomBadges.CountAsync(b => b.ChannelId == 1 && b.SetId == "vip") == 1
            && (await service.GetUrlCustomBadges(["vip"], "one"))["vip"] == "https://badges.test/updated",
            "Repeated loads update URLs without inserting duplicates");
        Check((await service.GetUrlCustomBadges(["unknown"], "one")).Count == 0,
            "Returns an empty dictionary when no requested badges exist");
        await service.AddAllBadges([Badge("one", "", "bad"), Badge("one", "invalid", "")]);
        Check(await db.CustomBadges.CountAsync(b => b.SetId == "invalid") == 0, "Invalid badge records are ignored");
    }

    await Task.WhenAll(Enumerable.Range(0, 4).Select(i => Task.Run(async () =>
    {
        await using var db = new TwitchDbContext(options);
        await new CustomBadgesServices(db).AddBadge(Badge("one", "concurrent", $"https://badges.test/{i}"));
    })));
    await using (var db = new TwitchDbContext(options))
    {
        Check(await db.CustomBadges.CountAsync(b => b.ChannelId == 1 && b.SetId == "concurrent") == 1,
            "Concurrent loads through independent SQLite connections are atomic");
        await db.Database.MigrateAsync();
        Check(await db.CustomBadges.CountAsync() == 6, "Restarting migrations preserves all badge records");
    }
    Console.WriteLine($"{passed} checks passed.");
}
finally { Directory.Delete(directory, recursive: true); }
