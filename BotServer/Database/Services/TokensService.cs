using BotServer.API.Models;
using BotServer.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BotServer.Database.Services
{
    public class TokensService
    {
        private TwitchDbContext _dbContext;

        public TokensService(TwitchDbContext dbContext)
        {

            _dbContext = dbContext;
        }


        public async Task<UserToken> GetAccessToken(string username)
        {
            var user = await _dbContext.Users.Where(p => p.Username.ToLower() == username.ToLower()).FirstOrDefaultAsync() ;
            var tokens = _dbContext.Tokens.Where(x => x.UsersId == user.Id).FirstOrDefault();

            return new UserToken {AccessToken= tokens.AccessToken,RefreshToken=tokens.RefreshToken,UserId=user.TwitchId,Username=username };
        }


        public async Task CreateToken(UserToken user)
        {
            await _dbContext.Tokens.AddAsync(new UserTokens {Id = int.Parse(user.UserId),AccessToken=user.AccessToken,RefreshToken=user.RefreshToken });
        }
    }
}
