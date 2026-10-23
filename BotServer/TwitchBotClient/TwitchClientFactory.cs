using BotServer.API;
using BotServer.Database.Services;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace BotServer.TwitchBotClient
{
    public class TwitchClientFactory
    {
        private readonly UsersService _service;
        private readonly TwitchBotApi _api;

        public TwitchClientFactory(UsersService service, TwitchBotApi api)
        {

            _service = service;
            _api = api;

        }
        public async Task<TwitchClient> Create(string username, bool local = false, CancellationToken cancelToken = default)
        {


            var loggerFactory = LoggerFactory.Create(c => c.AddConsole());
            
            var user = await _service.GetUser(username);
            if (user == null)
            {
                await _api.GetAutorizationUrl(local);
                do
                {
                    await Task.Delay(1000,cancelToken);
                    user = await _service.GetUser(username);
                }
                while (user == null);
            }

            cancelToken.ThrowIfCancellationRequested();

            var token = await _api.GetValidToken(user);

            //Update User Profile
            var profile = await _api.GetUserProfile(token.Username);
            
            token.Profile = profile;
            token = await _service.UpdateUser(token);

            var credentials = new ConnectionCredentials(token.Username, token.AccessToken);
            var client = new TwitchClient(loggerFactory: loggerFactory);
            client.Initialize(credentials);

            return client;

        }
    }
}
