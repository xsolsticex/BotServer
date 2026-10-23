using BotServer.Database.Services;
using BotServer.SpotifyClientApi;

namespace BotServer.Endpoints
{
    public static class SpotifyEndpoints
    {
        public static IEndpointRouteBuilder MapSpotifyEndpoints(this IEndpointRouteBuilder app)
        {
            app.MapGet("/auth/spotify/{username}", async (HttpContext context, string username, [FromKeyedServices] SpotifyService spoty, [FromKeyedServices] SpotyTokenManager spotyTM) =>
            {

                Console.WriteLine(username);

                var code = context.Request.Query["code"];

                var token = await spotyTM.GenerateUserToken(code);

                Console.WriteLine(token);

            });

            return app;
        }
    }
}
