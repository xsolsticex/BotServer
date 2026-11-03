using BotServer.Integrations.Twitch;
using BotServer.Data.Services;

namespace BotServer.Features.Chat
{
    public class ChatProfileService
    {
        private readonly UsersService _users;
        private readonly TwitchBotApi _api;

        public ChatProfileService(UsersService users, TwitchBotApi api)
        {
            _users = users;
            _api = api;
        }

        public async Task<string> GetProfile(string username)
        {
            var db = _users;
            var usu = await db.GetUser(username);
            if (usu != null && !string.IsNullOrEmpty(usu.Profile))
            {
                return usu.Profile;
            }
            return await _api.GetUserProfile(username);
        }

        public async Task EnsureUserAsync(string username)
        {
            var db = _users;
            var usu = username;
            var exists = await db.GetUser(usu);

            if (exists == null)
            {
                var user = await _api.GetUserData(usu);
                await db.CreateUser(user);
            }
        }
    }
}
