using BotServer.Integrations.Twitch.Auth.Models;
using BotServer.Data.Models;
using Microsoft.EntityFrameworkCore;

namespace BotServer.Data.Services
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

            if(user!= null && user.Tokens != null)
            {

                return new UserToken { AccessToken = user.Tokens.AccessToken, RefreshToken = user.Tokens.RefreshToken, Username = name, UserId = user.TwitchId, Profile = user.Profile };
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
            var exists = _dbContext.Users.Where(u => u.Username.ToLower() == user.Username.ToLower()).FirstOrDefault();

            if (exists == null)
            {
                Users userEntity = new Users { TwitchId = user.UserId, Username = user.Username, Profile = user.Profile };
                await _dbContext.Users.AddAsync(userEntity);
                await _dbContext.SaveChangesAsync();

                var tokenEntity = new UserTokens { UsersId = userEntity.Id, AccessToken = user.AccessToken, RefreshToken = user.RefreshToken };
                await _dbContext.Tokens.AddAsync(tokenEntity);
                await _dbContext.SaveChangesAsync();

            }
            else
            {

                await UpdateUser(user);
            }




        }


        public async Task AddTokens(UserToken user)
        {
            var exists = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == user.Username.ToLower());
            if (exists == null)
            {
                return;
            }
            exists.Profile = user.Profile;

            var f = await _dbContext.Tokens.FirstOrDefaultAsync(p => p.UsersId == exists.Id);

            if (f == null)
            {
                f = new UserTokens { AccessToken = user.AccessToken, RefreshToken = user.RefreshToken, UsersId = exists.Id };
                await _dbContext.Tokens.AddAsync(f);
            }
            else
            {
                f.AccessToken = user.AccessToken;
                f.RefreshToken = user.RefreshToken;
            }

            await _dbContext.SaveChangesAsync();

        }


        public async Task<UserToken?> UpdateUser(UserToken user)
        {
            var exists = await _dbContext.Users.FirstOrDefaultAsync(u => u.Username.ToLower() == user.Username.ToLower());
            if (exists == null)
            {
                return null;
            }
            exists.Profile = user.Profile;

            var f = await _dbContext.Tokens.FirstOrDefaultAsync(p => p.UsersId == exists.Id);

            if (f == null)
            {
               f = new UserTokens { AccessToken = user.AccessToken, RefreshToken = user.RefreshToken, UsersId = exists.Id };
                await _dbContext.Tokens.AddAsync(f);
            }
            else
            {
                f.AccessToken = user.AccessToken;
                f.RefreshToken = user.RefreshToken;
            }

            await _dbContext.SaveChangesAsync();


            return new UserToken {AccessToken= f.AccessToken,RefreshToken=f.RefreshToken,UserId=user.UserId,Username=user.Username,Profile=user.Profile };


        }
    }
}
