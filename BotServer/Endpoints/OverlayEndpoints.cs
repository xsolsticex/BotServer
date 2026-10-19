using BotServer.Database.Services;
using System.Runtime.CompilerServices;

namespace BotServer.Endpoints
{
    public static class OverlayEndpoints
    {
        public static IEndpointRouteBuilder MapOverlayEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/notifier/{username}", (string username) => { return Results.File("logro.html", "text/html"); });


            app.MapGet("/status/{username}", (string username) => { return Results.File("followers.html", "text/html"); });

            app.MapGet("/chat/{username}", (string username) => { return Results.File("index.html", "text/html"); });

            app.MapGet("/counter/{username}", (string username) => { return Results.File("counter.html", "text/html"); });


            app.MapGet("/data/{username}", async (string username, [FromKeyedServices] ChannelsService channelDb) =>
            {

                var ch = await channelDb.FindChannel(username);
                return new Dictionary<string, int>() { { "followers", ch.Followers }, { "subs", ch.Subs } };

            });


            app.MapGet("/profile/{username}", async (string username, [FromKeyedServices] UsersService users) =>
            {

                var ch = await users.GetUser(username);
                return new Dictionary<string, string>() { { "profile", ch.Profile } };

            });

            return app;

        }
    }
}
