using BotServer.API.Models;
using BotServer.Database.Models;
using Microsoft.EntityFrameworkCore;

namespace BotServer.Database.Services
{
    public class UsersService
    {
        private TwitchDbContext _dbContext;

        public UsersService(TwitchDbContext dbContext)
        {

            _dbContext = dbContext;
        }

        public async Task<UserToken> GetUser(string name)
        {
            var user = await _dbContext.Users.Include(u => u.Tokens).FirstOrDefaultAsync(u => u.Username.ToLower() == name.ToLower());

            if(user!= null)
            {
                return new UserToken { AccessToken = user.Tokens.AccessToken, RefreshToken = user.Tokens.RefreshToken };
            }
            return null;
          

        }


        public async Task CreateUser(Users user)
        {
            var exists = _dbContext.Users.Where(u => u.Username == user.Username).FirstOrDefault();

            if (exists == null)
            {
               // Users userEntity = new Users { TwitchId = user.UserId, Username = user.Username,Profile=user.Profile };
                await _dbContext.Users.AddAsync(user);
                await _dbContext.SaveChangesAsync();

                //var tokenEntity = new UserTokens { UsersId = userEntity.Id, AccessToken = user.AccessToken, RefreshToken = user.RefreshToken };
                //await _dbContext.Tokens.AddAsync(tokenEntity);
                //await _dbContext.SaveChangesAsync();

            }




        }

        public async Task CreateUser(UserToken user)
        {
            var exists = _dbContext.Users.Where(u => u.Username == user.Username).FirstOrDefault();

            if (exists == null)
            {
                Users userEntity = new Users { TwitchId = user.UserId, Username = user.Username, Profile = user.Profile };
                await _dbContext.Users.AddAsync(userEntity);
                await _dbContext.SaveChangesAsync();

                var tokenEntity = new UserTokens { UsersId = userEntity.Id, AccessToken = user.AccessToken, RefreshToken = user.RefreshToken };
                await _dbContext.Tokens.AddAsync(tokenEntity);
                await _dbContext.SaveChangesAsync();

            }




        }

        public async Task<UserToken> UpdateUser(UserToken user)
        {
            var exists = await _dbContext.Users.Where(u => u.Username == user.Username).FirstOrDefaultAsync();
            exists.Profile = user.Profile;
            UserTokens f;

            f = await _dbContext.Tokens.Where(p => p.UsersId == exists.Id).FirstOrDefaultAsync();
            f.AccessToken = user.AccessToken;
            f.RefreshToken = user.RefreshToken;
            

            await _dbContext.SaveChangesAsync();


            return new UserToken {AccessToken= f.AccessToken,RefreshToken=f.RefreshToken,UserId=user.UserId,Username=user.Username,Profile=user.Profile };


        }
    }
}
