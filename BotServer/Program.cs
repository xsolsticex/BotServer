using BotServer.Database;
using BotServer.Endpoints;
using BotServer.Extensions;
using BotServer.Hubs;
using Microsoft.EntityFrameworkCore;
using TwitchLib.EventSub.Webhooks.Extensions;

namespace BotServer
{
    public class Program
    {

        //Crear rama carlos
        public static void Main(string[] args)
        {
            var path = AppContext.BaseDirectory;
            var cnd = new List<string> { "local", "remote" };
            var con = cnd[1];
            var dbPath = string.Empty;

            if (con == "local")
            {
                dbPath = Path.Combine(path, "twitch.db");
            }
            else
            {
                dbPath = "/data/twitch.db";
            }

            //TODO : Quitar antes de despliegue a produccion
            //dbPath = Path.Combine(path, "twitch.db");

            var builder = WebApplication.CreateBuilder(args);

            builder.Services.AddBotServices(dbPath);

            WebApplication app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TwitchDbContext>();
                db.Database.Migrate();
            }

            app.UseStaticFiles();
            app.UseWebSockets();
            app.UseTwitchLibEventSubWebhooks();

            app.MapOAuthEndpoints();
            app.MapOverlayEndpoints();
            app.MapSpotifyEndpoints();
  
            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }

    }
}
