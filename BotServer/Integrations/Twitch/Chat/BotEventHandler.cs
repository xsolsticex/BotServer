using BotServer.Features.Chat;
using BotServer.Integrations.Twitch.Chat;
using TwitchLib.Client;
using TwitchLib.Client.Events;

namespace BotServer.Integrations.Twitch.Chat;

/// <summary>Adapts Twitch callbacks to scoped handlers; contains no command or persistence logic.</summary>
public class BotEventHandler(IServiceScopeFactory scopes)
{
    private TwitchClient? _client;

    public void Initialize(TwitchClient client) => _client = client;

    public Task OnConnected(object? sender, OnConnectedEventArgs e) =>
        DispatchAsync<ChannelEventHandler>(handler => handler.HandleConnectedAsync());

    public Task OnChannelJoined(object? sender, OnJoinedChannelArgs e) =>
        DispatchAsync<ChannelEventHandler>(handler => handler.HandleJoinedAsync(e.Channel));

    public Task onMessageReceived(object? sender, OnMessageReceivedArgs e) =>
        DispatchAsync<ChatMessageHandler>(handler => handler.HandleAsync(e, _client?.TwitchUsername));

    internal Task onCommandReceived(object? sender, OnChatCommandReceivedArgs e) =>
        DispatchAsync<BotCommandHandler>(handler => handler.HandleAsync(e, _client));

    internal Task onUserJoined(object? sender, OnUserJoinedArgs e) =>
        DispatchAsync<ChatProfileService>(handler => handler.EnsureUserAsync(e.Username));

    internal Task newSub(object? sender, OnNewSubscriberArgs e) =>
        DispatchAsync<ChannelEventHandler>(handler => handler.HandleSubscriptionAsync(e.Channel));

    private async Task DispatchAsync<THandler>(Func<THandler, Task> handle) where THandler : notnull
    {
        using var scope = scopes.CreateScope();
        await handle(scope.ServiceProvider.GetRequiredService<THandler>());
    }
}
