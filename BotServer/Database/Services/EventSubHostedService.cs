using BotServer.TwitchBotClient.SignalRClient;
using TwitchLib.EventSub.Core.EventArgs.Channel;
using TwitchLib.EventSub.Webhooks.Core;
using TwitchLib.EventSub.Webhooks.Core.EventArgs;
using TwitchLib.EventSub.Webhooks.Core.Models;

namespace BotServer.Database.Services
{
    public class EventSubHostedService : IHostedService
    {

        private readonly ILogger<EventSubHostedService> _logger;
        private readonly IEventSubWebhooks _eventSubWebhooks;
        private BotSignalRClient _signalR;

        public EventSubHostedService(ILogger<EventSubHostedService> logger, IEventSubWebhooks eventSubWebhooks, BotSignalRClient signalR)
        {
            _logger = logger;
            _eventSubWebhooks = eventSubWebhooks;
            _signalR = signalR;
        }
        public Task StartAsync(CancellationToken cancellationToken)
        {
            _eventSubWebhooks.ChannelFollow += OnFollow;
            _eventSubWebhooks.UnknownEventSubNotification += OnUnknownEventSubNotification;
            _eventSubWebhooks.ChannelChatMessage += OnChannelChatMessage;

            return Task.CompletedTask;
        }

        private async Task OnFollow(object? sender, ChannelFollowArgs e)
        {
            var user = e.Payload.Event.UserName;
            var channel = e.Payload.Event.BroadcasterUserLogin;

            await _signalR.SendFollow(channel, new Dictionary<string, string> { { "follow", user } });

            Console.WriteLine(user);

            //return Task.CompletedTask;
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _eventSubWebhooks.ChannelFollow -= OnFollow;
            _eventSubWebhooks.UnknownEventSubNotification -= OnUnknownEventSubNotification;
            _eventSubWebhooks.ChannelChatMessage -= OnChannelChatMessage;

            return Task.CompletedTask;
        }

        private async Task OnChannelChatMessage(object? sender, ChannelChatMessageArgs e)
        {
            _logger.LogInformation($"@{e.Payload.Event.ChatterUserName} #{e.Payload.Event.BroadcasterUserName}: {e.Payload.Event.Message.Text}");
        }

        private async Task OnError(object? sender, OnErrorArgs e)
        {
            _logger.LogError($"Reason: {e.Reason} - Message: {e.Message}");
        }

        // Handling notifications that are not (yet) implemented
        private async Task OnUnknownEventSubNotification(object? sender, UnknownEventSubNotificationArgs e)
        {
            var metadata = (WebhookEventSubMetadata)e.Metadata;
            _logger.LogInformation("Received event that has not yet been implemented: type:{type}, version:{version}", metadata.SubscriptionType, metadata.SubscriptionVersion);

            switch ((metadata.SubscriptionType, metadata.SubscriptionVersion))
            {
                case ("channel.chat.message", "1"): /*code to handle the event*/ break;
                default: break;
            }
        }
    }
}
