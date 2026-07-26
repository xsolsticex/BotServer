using BotServer.API.Models;
using BotServer.Database.Models;
using BotServer.Database.Services;
using System.Diagnostics;
using System.Security.AccessControl;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Helix.Models.Users.GetUsers;

namespace BotServer.API
{
    public class TwitchBotApi
    {
        private TwitchAPI _api;
        private IServiceScopeFactory _service;

        private string redirectUri = "https://botserver-qccm.onrender.com/confirm";

        public TwitchBotApi(TwitchAPI api,IServiceScopeFactory scope)
        {
            _api = api;
            _service = scope;
            _api.Settings.ClientId = Environment.GetEnvironmentVariable("CLIENT_ID"); 
            _api.Settings.Secret = Environment.GetEnvironmentVariable("CLIENT_SECRET");
  
        }


        public async Task<UserToken> GetTokenWithCode(string code)
        {
            AuthCodeResponse token = await _api.Auth.GetAccessTokenFromCodeAsync(code, clientId: _api.Settings.ClientId, clientSecret: _api.Settings.Secret, redirectUri: "https://botserver-qccm.onrender.com/confirm");
            if (token is not null)
            {
                return new UserToken { AccessToken = token.AccessToken, RefreshToken = token.RefreshToken };
            }
            else
            {
                return null;
            }

        }

        public async Task<ValidateAccessTokenResponse> ValidateToken(string token)
        {
            var isValid = await _api.Auth.ValidateAccessTokenAsync(accessToken: token);
            
            if(isValid is not null)
            {
                return isValid;
            }
            return null;

            
        }


        public async Task<string> GetUserProfile(string username)
        {
            try
            {
          
                var token = await GetValidToken("xhipibotx");
                var user = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { username });

                if (user is not null)
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

        public async Task<Users> GetUserData(string username)
        {
            try
            {

                var token = await GetValidToken("xhipibotx");
                var user = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { username });

                if (user is not null)
                {
                    var u = user.Users.FirstOrDefault();
                    return new Users { Profile = u.ProfileImageUrl, TwitchId = u.Id, Username = u.Login };
                }

            }
            catch (Exception e)
            {

                Console.WriteLine(e);
            }

            return null;

        }





        public async Task<UserToken> RefreshToken(string refreshtoken)
        {

            RefreshResponse token = await _api.Auth.RefreshAuthTokenAsync(refreshtoken,  _api.Settings.Secret, _api.Settings.ClientId );

            return new UserToken { AccessToken = token.AccessToken, RefreshToken = token.RefreshToken };
        }


        public async Task<User> GetUser(string twitch_id)
        {
            User? user = _api.Helix.Users.GetUsersAsync().Result.Users.Where(p => p.Id == twitch_id).FirstOrDefault();

            return user;
        }

        public async Task<UserToken> GetValidToken(UserToken token)
        {
            var isValid = await ValidateToken(token.AccessToken);

            if (isValid == null)
            {
                token = await RefreshToken(token.RefreshToken);
                isValid = await ValidateToken(token.AccessToken);

            }


            _api.Settings.AccessToken = token.AccessToken;

            token.Username = isValid.Login;
            token.UserId = isValid.UserId;

            return token;
        }

        public async Task<UserToken> GetValidToken(string username)
        {
            var service = _service.CreateScope();
            var tokenService = service.ServiceProvider.GetRequiredService<TokensService>();    
            var token = await tokenService.GetAccessToken(username);

            if (token == null)
                throw new Exception($"No existe un token para el usuario '{username}'.");

            var isValid = await ValidateToken(token.AccessToken);
            
            if (isValid == null)
            {

                token = await RefreshToken(token.RefreshToken);
                isValid = await ValidateToken(token.AccessToken);

            }


            _api.Settings.AccessToken = token.AccessToken;

            token.Username = isValid.Login;
            token.UserId = isValid.UserId;

            return token;
        }


        public async Task GetAutorizationUrl(bool local = false)
        {

            var connection = new List<string> { "local", "remote" };
            var con = connection[1];
            if (con == "local")
            {
                redirectUri = "http://localhost:8000/confirm";
            }

            Console.WriteLine($"Auth URL : https://id.twitch.tv/oauth2/authorize?client_id={_api.Settings.ClientId}&redirect_uri={redirectUri}&scope=chat:edit%20moderator:manage:banned_users%20chat:read%20channel:manage:vips%20channel:manage:moderators%20channel:manage:polls%20moderator:manage:shoutouts%20user:manage:whispers%20clips:edit%20channel:manage:broadcast%20moderator:manage:chat_messages&response_type=code&force_verify=true");

            await Task.Delay(8000);
        }

    }
}
