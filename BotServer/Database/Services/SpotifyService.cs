using BotServer.Database.Models;

namespace BotServer.Database.Services
{
    public class SpotifyService
    {
        private TwitchDbContext _context;

        public SpotifyService(TwitchDbContext context)
        {

            _context = context;
        
        
        }



        public async Task SaveToken(string access_token,string refresh_token, string username)
        {
            SpotifyDB user = new SpotifyDB {AccesToken = access_token,RefreshToken=refresh_token,Username=username };

            await _context.AddAsync(user);

            await _context.SaveChangesAsync();
        }



    }
}
