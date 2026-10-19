using BotServer.API;
using BotServer.Database.Services;

namespace BotServer.Endpoints
{
    public static class OAuthEndpoints
    {
        public static IEndpointRouteBuilder MapOAuthEndpoints(this IEndpointRouteBuilder app)
        {
            app.Map("/connect", () =>
            {

                var cnd = new List<string> { "local", "remote" };
                var client_id = Environment.GetEnvironmentVariable("CLIENT_ID");
                var cnt = cnd[0];

                var redirect = "http://localhost:8000/confirm";

                if (cnt == "remote")
                {
                    redirect = Environment.GetEnvironmentVariable("REDIRECT_URL");
                }

                return Results.Redirect($"https://id.twitch.tv/oauth2/authorize?client_id={client_id}&redirect_uri={redirect}&scope=chat:edit%20moderator:manage:banned_users%20chat:read%20channel:manage:vips%20channel:manage:moderators%20channel:manage:polls%20moderator:manage:shoutouts%20user:manage:whispers%20clips:edit%20channel:manage:broadcast%20moderator:manage:chat_messages%20channel:read:subscriptions%20moderator:read:followers%20channel:bot&response_type=code&force_verify=true");

            });



            app.MapGet("/confirm", async (HttpContext context, [FromKeyedServices] TwitchBotApi api, [FromKeyedServices] UsersService userService, [FromKeyedServices] ChannelsService channelService, [FromKeyedServices] OAuthService oauthService) =>
            {
                var code = context.Request.Query["code"];
                await oauthService.ConfirmUser(code, api, userService, channelService);
                //await ConfirmUser(code, api, userService, channelService);

            });

            return app;
        }
    }
}
