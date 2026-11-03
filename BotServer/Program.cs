using BotServer.Configuration;
using BotServer.Web.Endpoints;
using BotServer.Data;

using BotServer.Extensions;
using BotServer.Web.Hubs;
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
            var dbPath = string.Empty;

            if (AppEnvironment.IsLocal)
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
                // Creates SQLite only when absent; otherwise applies pending schema migrations in place.
                db.Database.Migrate();
            }

            app.UseStaticFiles();
            app.UseWebSockets();
            app.UseTwitchLibEventSubWebhooks();

            app.MapOAuthEndpoints();
            app.MapOverlayEndpoints();
            app.MapReleaseEndpoints();
            app.MapSpotifyEndpoints();

            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }

    }
}
