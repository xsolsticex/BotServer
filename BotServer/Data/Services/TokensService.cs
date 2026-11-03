using BotServer.Integrations.Twitch.Auth.Models;
using BotServer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BotServer.Data.Services
{
    public class TokensService
    {
        private TwitchDbContext _dbContext;

        public TokensService(TwitchDbContext dbContext)
        {

            _dbContext = dbContext;
        }


        public async Task<UserToken?> GetAccessToken(string username)
        {
            var user = await _dbContext.Users
                .FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());

            if (user == null)
                return null;

            var token = await _dbContext.Tokens
                .FirstOrDefaultAsync(t => t.UsersId == user.Id);

            if (token == null)
                return null;

            return new UserToken
            {
                AccessToken = token.AccessToken,
                RefreshToken = token.RefreshToken,
                UserId = user.TwitchId,
                Username = username
            };
        }


        public async Task CreateToken(UserToken user)
        {
            await _dbContext.Tokens.AddAsync(new UserTokens {Id = int.Parse(user.UserId),AccessToken=user.AccessToken,RefreshToken=user.RefreshToken });
            await _dbContext.SaveChangesAsync();
        }

        public async Task SaveData()
        {
            await _dbContext.SaveChangesAsync();
        }
    }
}
