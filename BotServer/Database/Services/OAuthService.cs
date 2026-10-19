using BotServer.API;
using BotServer.API.Models;

namespace BotServer.Database.Services
{
    public class OAuthService
    {
        

        public async Task ConfirmUser(string code, TwitchBotApi api, UsersService userService, ChannelsService channelService)
        {

            //Add User to database
            UserToken? token = await api.GetTokenWithCode(code);

            token = await api.GetValidToken(token);

            await userService.CreateUser(token);


            //Update channel followers
            var followers = await api.GetFollowers(token.Username, token.UserId);

            var subs = await api.GetSubs(token.Username, token.UserId);

            await channelService.UpdateChannel(token.Username, followers, subs);

            try
            {
                //Add User profile image
                var profile = await api.GetUserProfile(token.Username);

                token.Profile = profile;

                token = await userService.UpdateUser(token);

            }
            catch (Exception a)
            {

                Console.WriteLine(a);
            }
        }
    }
}
