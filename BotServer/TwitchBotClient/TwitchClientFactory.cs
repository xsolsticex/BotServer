using BotServer.API;
using BotServer.Database.Models;
using BotServer.Database.Services;
using BotServer.TwitchBotClient.SignalRClient;
using System.Diagnostics;
using TwitchLib.Client;
using TwitchLib.Client.Models;

namespace BotServer.TwitchBotClient
{
    public class TwitchClientFactory
    {
        private UsersService _service;
        private TwitchBotApi _api;
        private BotSignalRClient _signalR;

        public TwitchClientFactory(UsersService service,TwitchBotApi api) {

            _service = service;
            _api = api;

        
        }
        public async Task<TwitchClient> Create(string username,bool local = false)
        {

            var loggerFactory = LoggerFactory.Create(c => c.AddConsole());
            var user = await _service.GetUser(username);

            if(user == null)
            {
                await _api.GetAutorizationUrl(local);
                do
                {
                    await Task.Delay(1000);
                    user = await _service.GetUser(username);
                }
                while (user == null);


            }

            var token = await _api.GetValidToken(user);

            var profile = await _api.GetUserProfile(token.Username);

            token.Profile = profile;

            token = await _service.UpdateUser(token);
   
            var credentials = new ConnectionCredentials(token.Username,token.AccessToken );
            var client = new TwitchClient(loggerFactory: loggerFactory);

            

            client.Initialize(credentials);

            return client;
        }
    }
}
