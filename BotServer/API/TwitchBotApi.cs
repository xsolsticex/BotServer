using BotServer.API.Models;
using BotServer.Database.DTO;
using BotServer.Database.Models;
using BotServer.Database.Services;
using BotServer.Interfaces;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Core.Enums;
using TwitchLib.Api.Helix.Models.Channels.SendChatMessage;
using TwitchLib.Api.Helix.Models.EventSub;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace BotServer.API
{
    public class TwitchBotApi
    {
        private readonly TwitchAPI _api;
        private readonly IServiceScopeFactory _service;
        private readonly TwitchAuthService _authService;

        public TwitchBotApi(TwitchAPI api, IServiceScopeFactory scope, TwitchAuthService authService)
        {
            _api = api;
            _service = scope;
            _authService = authService;
            _api.Settings.ClientId = Environment.GetEnvironmentVariable("CLIENT_ID");
            _api.Settings.Secret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
            GenerateScopesUrl();
        }

        public string GenerateScopesUrl() => _authService.GenerateScopesUrl();

        public async Task<bool> CheckEventSubs(string eventType, string broadcasterId, string? botId = null)
        {
            var request = new GetEventSubSubscriptionsRequest();
            var events = await _api.Helix.EventSub.GetEventSubSubscriptionsAsync(request);

            const string callback = "https://botserver-qccm.onrender.com/eventSub";

            return events.Subscriptions.Any(sub =>
            {
                Console.WriteLine(
                    $"ID: {sub.Id} | " +
                    $"Type: {sub.Type} | " +
                    $"Status: {sub.Status} | " +
                    $"Callback: {sub.Transport.Callback}"
                );

                if (sub.Type != eventType ||
                    sub.Status != "enabled" ||
                    sub.Transport.Callback != callback ||
                    !sub.Condition.TryGetValue("broadcaster_user_id", out var id) ||
                    id != broadcasterId)
                {
                    return false;
                }

                if (eventType == "channel.chat.message")
                {
                    if (botId == null ||
                        !sub.Condition.TryGetValue("user_id", out var userId) ||
                        userId != botId)
                    {
                        return false;
                    }
                }

                return true;
            });
        }

        public async Task SendChatMessage(string broadcasterId, string botid, string message, string token)
        {
            var request = new SendChatMessageRequest { BroadcasterId = broadcasterId, SenderId = botid, Message = message };
            await _api.Helix.Chat.SendChatMessage(request, token);
        }

        public async Task ReplyChatMessage(string broadcasterId, string botid, string message, string token, string messageId)
        {
            var request = new SendChatMessageRequest { BroadcasterId = broadcasterId, SenderId = botid, Message = message, ReplyParentMessageId = messageId };
            await _api.Helix.Chat.SendChatMessage(request, token);
        }

        public async Task ClearAllEventSub()
        {
            var cl = new TwitchAPI
            {
                Settings =
                {
                    ClientId = _api.Settings.ClientId,
                    Secret = _api.Settings.Secret
                }
            };

            var token = await cl.Auth.GetAccessTokenAsync();

            while (true)
            {
                var request = new GetEventSubSubscriptionsRequest();
                var subs = await _api.Helix.EventSub.GetEventSubSubscriptionsAsync(request, accessToken: token, clientId: cl.Settings.ClientId);

                if (subs.Subscriptions.Length == 0) break;

                var deleteTasks = subs.Subscriptions.Select(item =>
                    _api.Helix.EventSub.DeleteEventSubSubscriptionAsync(item.Id, accessToken: token)
                );
                await Task.WhenAll(deleteTasks);
            }

            Console.WriteLine("Todas las suscripciones eliminadas");
        }

        public async Task RegisterEvent(string type, string version, IEventSubCondition condition)
        {
            try
            {
                var cl = new TwitchAPI
                {
                    Settings =
                    {
                        ClientId = _api.Settings.ClientId,
                        Secret = _api.Settings.Secret
                    }
                };

                var token = await cl.Auth.GetAccessTokenAsync();
                var validation = await _api.Auth.ValidateAccessTokenAsync(token);

                if (validation == null)
                    throw new Exception("Token inválido");

                Console.WriteLine($"ClientId: {validation.ClientId}");
                Console.WriteLine($"UserId: {validation.UserId}");

                if (!string.IsNullOrEmpty(validation.UserId))
                    throw new Exception("El token es de usuario, no de aplicación");

                bool isLocal = false;
                var testurl = isLocal ? "https://cancel-heave-tackiness.ngrok-free.dev/eventSub" : "https://botserver-qccm.onrender.com/eventSub";

                await _api.Helix.EventSub.CreateEventSubSubscriptionAsync(
                    type,
                    version,
                    condition.ToDictionary(),
                    method: EventSubTransportMethod.Webhook,
                    webhookCallback: testurl,
                    webhookSecret: Environment.GetEnvironmentVariable("HOOK_SECRET"),
                    accessToken: token
                );
            }
            catch (Exception a)
            {
                Console.ForegroundColor = ConsoleColor.DarkRed;
                Console.WriteLine($"[EventSub exception] : {a.Message}");
                Console.ResetColor();
            }
        }

        public async Task<UserToken?> GetTokenWithCode(string code) => await _authService.GetTokenWithCode(code);

        public async Task<ValidateAccessTokenResponse?> ValidateToken(string token) => await _authService.ValidateToken(token);

        public async Task<int> GetFollowers(string username, string userid)
        {
            try
            {
                _ = await GetValidToken(username);
                var followers = await _api.Helix.Channels.GetChannelFollowersAsync(userid);
                return followers.Total;
            }
            catch (Exception a)
            {
                Console.WriteLine(a);
                return 0;
            }
        }

        public async Task<int> GetFollowers(Users user)
        {
            _ = await GetValidToken(user.Username);
            var followers = await _api.Helix.Channels.GetChannelFollowersAsync(user.TwitchId);
            return followers.Total;
        }

        public async Task<int> GetSubs(string username, string userid)
        {
            _ = await GetValidToken(username);
            var subs = await _api.Helix.Subscriptions.GetBroadcasterSubscriptionsAsync(userid);
            return subs.Total;
        }

        public async Task<int> GetSubs(Users user)
        {
            _ = await GetValidToken(user.Username);
            var subs = await _api.Helix.Subscriptions.GetBroadcasterSubscriptionsAsync(user.TwitchId);
            return subs.Total;
        }

        public async Task<string?> GetUserProfile(string username)
        {
            try
            {
                _ = await GetValidToken("xhipibotx");
                var user = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { username });

                if (user?.Users != null && user.Users.Length > 0)
                {
                    return user.Users.First().ProfileImageUrl;
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        public async Task<string> GetAppToken() => await _authService.GetAppToken();

        public async Task<Users?> GetUserData(string username)
        {
            try
            {
                var token = await GetValidToken("xhipibotx");
                var user = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { username }, accessToken: token.AccessToken);

                if (user?.Users != null && user.Users.Length > 0)
                {
                    var u = user.Users.First();
                    return new Users { Profile = u.ProfileImageUrl, TwitchId = u.Id, Username = u.Login };
                }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }

            return null;
        }

        public async Task<UserToken> RefreshToken(string refreshtoken) => await _authService.RefreshToken(refreshtoken);

        public async Task<User?> GetUser(string twitch_id)
        {
            var response = await _api.Helix.Users.GetUsersAsync();
            return response?.Users?.FirstOrDefault(p => p.Id == twitch_id);
        }

        public async Task<UserToken> GetValidToken(UserToken token) => await _authService.GetValidToken(token);

        public async Task<UserToken> GetValidToken(string username) => await _authService.GetValidToken(username);

        public async Task GetChannelBadges()
        {
            var badges = await _api.Helix.Chat.GetGlobalChatBadgesAsync();
            var emoteSet = badges.EmoteSet;
            var globalBadges = new List<GlobalBadges>();

            foreach (var item in emoteSet)
            {
                var version = item.Versions.FirstOrDefault();
                if (version == null) continue;

                var name = version.Title;
                if (!string.IsNullOrEmpty(name))
                {
                    if (name.Contains('_') || name.Contains('-'))
                    {
                        var nameSplited = name.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                        name = string.Join(" ", nameSplited);
                    }

                    if (name.Contains("   "))
                    {
                        name = name.Replace("   ", " ");
                    }
                }

                globalBadges.Add(new GlobalBadges
                {
                    BadgeId = version.Id,
                    name = name ?? string.Empty,
                    url = version.ImageUrl1x
                });
            }

            using var scope = _service.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GlobalBadgesService>();
            await db.AddBadges(globalBadges);
        }

        public async Task GetCustomBadges(string broadcaster_id, string channel)
        {
            var customBadges = new List<CustomBadge>();

            using var scope = _service.CreateScope();
            var channelsDb = scope.ServiceProvider.GetRequiredService<JoinedChannelsServices>();
            var db = scope.ServiceProvider.GetRequiredService<CustomBadgesServices>();

            _ = await channelsDb.GetChannelId(channel);
            var badges = await _api.Helix.Chat.GetChannelChatBadgesAsync(broadcaster_id);
            var emoteSet = badges.EmoteSet;

            if (emoteSet.Length == 0) return;

            foreach (var item in emoteSet)
            {
                var version = item.Versions.FirstOrDefault();
                if (version == null) continue;

                customBadges.Add(new CustomBadge
                {
                    SetId = version.Title,
                    Channel = channel,
                    BadgeUrl = version.ImageUrl1x
                });
            }

            await db.AddAllBadges(customBadges);
        }

        public async Task GetAuthorizationUrl(bool local = false) => await _authService.GetAuthorizationUrl(local);

        public async Task GetAutorizationUrl(bool local = false) => await _authService.GetAuthorizationUrl(local);
    }
}
