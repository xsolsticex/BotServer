using BotServer.Data;
using BotServer.Extensions;
using BotServer.Features.Chat.Markov;
using BotServer.Integrations.Twitch.Chat;
using BotServer.Features.Badges;
using BotServer.Features.Chat;
using BotServer.Web.SignalR;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

var directory = Path.Combine(Path.GetTempPath(), "botserver-handlers-" + Guid.NewGuid());
Directory.CreateDirectory(directory);
var signalR = new RecordingSignalR();
var services = new ServiceCollection();
services.AddLogging();
services.AddSingleton<IConfiguration>(new ConfigurationBuilder().Build());
services.AddBotServices(Path.Combine(directory, "test.db"));
services.RemoveAll<IBotSignalRClient>();
services.AddSingleton<IBotSignalRClient>(signalR);
int passed = 0;
void Check(bool condition, string name)
{
    if (!condition) throw new Exception(name);
    Console.WriteLine($"PASS: {name}");
    passed++;
}
ChatMessage Message(string text, string username = "person", string channel = "one") => new(
    botUsername: "testbot", userId: "1", userName: username, displayName: username, hexColor: "",
    emoteSet: null!, message: text, userType: default, channel: channel, id: "message-id",
    subscribedMonthCount: 0, roomId: "1", isMe: false, isBroadcaster: username == channel,
    noisy: default, rawIrcMessage: "", emoteReplacedMessage: text,
    badges: [new("subscriber", "0"), new("premium", "1")],
    cheerBadge: null!, bits: 0, bitsInDollars: 0, userDetail: default);

try
{
    using var provider = services.BuildServiceProvider(new ServiceProviderOptions { ValidateScopes = true });
    using (var scope = provider.CreateScope())
    {
        var db = scope.ServiceProvider.GetRequiredService<TwitchDbContext>();
        await db.Database.MigrateAsync();
        await db.Database.ExecuteSqlRawAsync("""
            INSERT INTO JoinedChannels (ChannelName, Followers, Subs) VALUES ('one', 0, 0);
            INSERT INTO Users (TwitchId, Username, Profile) VALUES ('1', 'person', 'https://profiles.test/person');
            INSERT INTO Tokens (AccessToken, RefreshToken, UsersId) VALUES ('unused', 'unused', 1);
            INSERT INTO GlobalBadges (name, url) VALUES ('Prime Gaming', 'https://badges.test/prime');
            INSERT INTO CustomBadges (ChannelId, SetId, BadgeUrl) VALUES (1, 'subscriber', 'https://badges.test/subscriber');
            """);
        foreach (var type in new[] { typeof(ChatMessageHandler), typeof(ChatBadgeService), typeof(ChatProfileService), typeof(ChannelEventHandler), typeof(BotCommandHandler) })
            scope.ServiceProvider.GetRequiredService(type);
        Check(provider.GetRequiredService<TwitchBot>() != null, "Bot and extracted handlers resolve without scoped services in singletons");

        using var other = provider.CreateScope();
        Check(!ReferenceEquals(scope.ServiceProvider.GetRequiredService<ChatBadgeService>(),
            other.ServiceProvider.GetRequiredService<ChatBadgeService>()), "Handlers are isolated by event scope");
        var badges = await scope.ServiceProvider.GetRequiredService<ChatBadgeService>()
            .GetBadgesAsync(["subscriber", "premium"], "one");
        Check(badges.SequenceEqual(new[] { "https://badges.test/subscriber", "https://badges.test/prime" }),
            "Custom IDs and formatted global badges retain their original order");
        var profiles = scope.ServiceProvider.GetRequiredService<ChatProfileService>();
        Check(await profiles.GetProfile("person") == "https://profiles.test/person", "Profile lookup uses cached data");
        await profiles.EnsureUserAsync("person");
        Check(await db.Users.CountAsync() == 1, "Joining cached users does not duplicate records");

        var handler = scope.ServiceProvider.GetRequiredService<ChatMessageHandler>();
        await handler.HandleAsync(new OnMessageReceivedArgs(Message("!help")), "testbot");
        Check(signalR.Messages.Count == 0, "Commands do not enter the chat message pipeline");
        await handler.HandleAsync(new OnMessageReceivedArgs(Message("hola mundo")), "person");
        Check(provider.GetRequiredService<MarkovChatService>().Generate("one") == null,
            "Bot messages are forwarded but excluded from Markov training");

        var commands = scope.ServiceProvider.GetRequiredService<BotCommandHandler>();
        await commands.HandleAsync(new OnChatCommandReceivedArgs(Message("!win", "person"), new CommandInfo("!", "win", "", [])), null);
        Check(signalR.Counters.Count == 0, "Counter command still rejects non-channel owners");
        await commands.HandleAsync(new OnChatCommandReceivedArgs(Message("!win", "one"), new CommandInfo("!", "win", "", [])), null);
        Check(signalR.Counters.Single() == ("one", "win"), "Counter command still accepts channel owners");
        await commands.HandleAsync(new OnChatCommandReceivedArgs(Message("!color abcdef"), new CommandInfo("!", "color", "abcdef", ["abcdef"])), null);
        Check(signalR.Colors.Single() == ("one", "#abcdef"), "Color command retains parameter normalization");
    }

    var router = provider.GetRequiredService<BotEventHandler>();
    var initial = signalR.Messages.Count;
    await Task.WhenAll(Enumerable.Range(0, 8).Select(_ =>
        router.onMessageReceived(null, new OnMessageReceivedArgs(Message("hola mundo")))));
    Check(signalR.Messages.Count == initial + 8, "Concurrent Twitch callbacks use independent DbContexts");
    var data = signalR.Messages.Last();
    Check(data.Channel == "one" && (string)data.Payload["content"] == "hola mundo"
        && (string)data.Payload["color"] == "#ffc107"
        && (string)data.Payload["profile"] == "https://profiles.test/person"
        && ((List<string>)data.Payload["badges"]).Count == 2,
        "Router preserves the overlay payload, profile, badges and default color");
    Check(provider.GetRequiredService<MarkovChatService>().Generate("one") == "hola mundo",
        "User messages still train the channel's Markov model");
    Console.WriteLine($"{passed} checks passed.");
}
finally
{
    Microsoft.Data.Sqlite.SqliteConnection.ClearAllPools();
    Directory.Delete(directory, recursive: true);
}

sealed class RecordingSignalR : IBotSignalRClient
{
    public System.Collections.Concurrent.ConcurrentQueue<(string Channel, Dictionary<string, object> Payload)> Messages { get; } = new();
    public List<(string, string)> Counters { get; } = [];
    public List<(string, string)> Colors { get; } = [];
    public Task StartClient() => Task.CompletedTask;
    public Task Send(string channel, Dictionary<string, object> message) { Messages.Enqueue((channel, message)); return Task.CompletedTask; }
    public Task UpdateCounter(string channel, string status) { Counters.Add((channel, status)); return Task.CompletedTask; }
    public Task ChangeColor(string channel, string color) { Colors.Add((channel, color)); return Task.CompletedTask; }
    public Task SendFollow(string channel, Dictionary<string, string> message) => Task.CompletedTask;
    public Task UpdateFollowers(string channel, Dictionary<string, int> message) => Task.CompletedTask;
}
