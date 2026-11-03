using BotServer.Integrations.Spotify;
using BotServer.Data.Services;

namespace BotServer.Web.Endpoints
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
