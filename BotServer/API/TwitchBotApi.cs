using BotServer.API.Models;
using BotServer.Database.DTO;
using BotServer.Database.Models;
using BotServer.Database.Services;
using System.Diagnostics;
using System.Security.AccessControl;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Helix.Models.Users.GetUsers;
using static System.Runtime.InteropServices.JavaScript.JSType;

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


        public async Task<UserToken?> GetTokenWithCode(string code)
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

        public async Task<ValidateAccessTokenResponse?> ValidateToken(string token)
        {
            var isValid = await _api.Auth.ValidateAccessTokenAsync(accessToken: token);
            
            if(isValid is not null)
            {
                return isValid;
            }
            return null;

            
        }

        public async Task<int> GetFollowers(string username,string userid)
        {

            try
            {
                var token = await GetValidToken(username);
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
            var token = await GetValidToken(user.Username);
            var followers = await _api.Helix.Channels.GetChannelFollowersAsync(user.TwitchId);
            return followers.Total;


        }

        public async Task<int> GetSubs(string username, string userid)
        {
            var token = await GetValidToken(username);
            var subs = await _api.Helix.Subscriptions.GetBroadcasterSubscriptionsAsync(userid);
            return subs.Total;

        }

        public async Task<int> GetSubs(Users user)
        {
            var token = await GetValidToken(user.Username);
            var subs = await _api.Helix.Subscriptions.GetBroadcasterSubscriptionsAsync(user.TwitchId);
            return subs.Total;

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
                
                var user = await _api.Helix.Users.GetUsersAsync(logins: new List<string> { username },accessToken:  token.AccessToken);

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
                //refreshed token
                var refreshedToken = await RefreshToken(token.RefreshToken);

                token.AccessToken = refreshedToken.AccessToken;
                token.RefreshToken = refreshedToken.RefreshToken;

                await tokenService.SaveData();

                isValid = await ValidateToken(token.AccessToken);
                        
            }

            _api.Settings.AccessToken = token.AccessToken;

            token.Username = isValid.Login;
            token.UserId = isValid.UserId;

            return token;
        }


        public async Task GetChannelBadges()
        {
            var badges  = await _api.Helix.Chat.GetGlobalChatBadgesAsync();
            var emoteSet = badges.EmoteSet;
            List<GlobalBadges> globalBadges = new();

            foreach (var item in emoteSet)
            {
                var name = item.Versions.First().Title;
                if (name.Contains('_') || name.Contains('-'))
                {
                    var name_splited = name.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                    name = string.Join(" ", name_splited);
                }

                if(name.Contains("   "))
                {
                    name = name.Replace("   "," ");
                }
        
                globalBadges.Add(new GlobalBadges { BadgeId= item.Versions.First().Id, name=name,url=item.Versions.First().ImageUrl1x });
     
            }

            var scope = _service.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<GlobalBadgesService>();

            await db.AddBadges(globalBadges);
            

        }

        public async Task GetCustomBadges(string broadcaster_id,string channel)
        {
            List<CustomBadge> globalBadges = new();

            //Get Services
            var scope = _service.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<CustomBadgesServices>();
            var channelsDb = scope.ServiceProvider.GetRequiredService<JoinedChannelsServices>();

            //Get channel info
            var channelid = await channelsDb.GetChannelId(channel);
            var badges = await _api.Helix.Chat.GetChannelChatBadgesAsync(broadcaster_id=broadcaster_id);
            var emoteSet = badges.EmoteSet;


            if (emoteSet.Length == 0) return;

            foreach (var item in emoteSet)
            {
                globalBadges.Add(new CustomBadge { SetId = item.Versions.First().Title, Channel = channel, BadgeUrl = item.Versions.First().ImageUrl1x });
            }

     
            await db.AddAllBadges(globalBadges);


        }

        public async Task GetAutorizationUrl(bool local = false)
        {

            var cnd = new List<string> { "local", "remote" };
            var con = cnd[1];
            if (con == "local")
            {
                redirectUri = "http://localhost:8000/confirm";
            }

            Console.WriteLine($"Auth URL : https://id.twitch.tv/oauth2/authorize?client_id={_api.Settings.ClientId}&redirect_uri={redirectUri}&scope=chat:edit%20moderator:manage:banned_users%20chat:read%20channel:manage:vips%20channel:manage:moderators%20channel:manage:polls%20moderator:manage:shoutouts%20user:manage:whispers%20clips:edit%20channel:manage:broadcast%20moderator:manage:chat_messages%20channel:read:subscriptions%20moderator:read:followers&response_type=code&force_verify=true");
            await Task.Delay(8000);
        }

    }
}
