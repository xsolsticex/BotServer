using BotServer.API;
using BotServer.API.Models;
using BotServer.Database;
using BotServer.Database.Services;
using BotServer.Hubs;
using BotServer.SpotifyClientApi;
using BotServer.TwitchBotClient;
using BotServer.TwitchBotClient.SignalRClient;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using TwitchLib.Api;
using TwitchLib.EventSub.Webhooks.Extensions;

namespace BotServer
{
    public class Program
    {


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

            //Temporal
            //dbPath = Path.Combine(path, "twitch.db");


            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSignalR(options =>
            {
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });
            builder.Services.AddSingleton<TwitchBot>();
            builder.Services.AddHostedService<TwitchBot>(sp => sp.GetRequiredService<TwitchBot>());
            builder.Services.AddSingleton<BotEventHandler>();
            builder.Services.AddSingleton<MessageSender>();
            builder.Services.AddDbContext<TwitchDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
            builder.Services.AddScoped<TwitchClientFactory>();
            builder.Services.AddSingleton<TwitchAPI>(new TwitchAPI());
            builder.Services.AddSingleton<TwitchBotApi>();
            builder.Services.AddScoped<UsersService>();
            builder.Services.AddSingleton<BotSignalRClient>();
            builder.Services.AddScoped<TokensService>();
            builder.Services.AddScoped<ChannelsService>();
            builder.Services.AddScoped<GlobalBadgesService>();
            builder.Services.AddScoped<CustomBadgesServices>();
            builder.Services.AddScoped<JoinedChannelsServices>();
            builder.Services.AddScoped<SpotifyService>();
            builder.Services.AddSingleton<SpotyTokenManager>(c =>
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("https://accounts.spotify.com/api/token");

                var spotify_id = Environment.GetEnvironmentVariable("SPOTIFY_ID");
                var spotify_secret = Environment.GetEnvironmentVariable("SPOTIFY_SECRET");
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{spotify_id}:{spotify_secret}"));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);



                return new SpotyTokenManager(client);


            });
            builder.Services.AddSingleton<SpotyClient>();


            builder.Services.AddTwitchLibEventSubWebhooks(config => {
                config.CallbackPath = "/eventSub";
                config.Secret = Environment.GetEnvironmentVariable("HOOK_SECRET");

            });

            builder.Services.AddHostedService<EventSubHostedService>();
            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TwitchDbContext>();
                db.Database.Migrate();
            }



            app.UseStaticFiles();
            app.UseWebSockets();
            app.UseTwitchLibEventSubWebhooks();

            app.MapGet("/auth/spotify/{username}", async (HttpContext context, string username, [FromKeyedServices] SpotifyService spoty, [FromKeyedServices] SpotyTokenManager spotyTM) =>
            {

                Console.WriteLine(username);

                var code = context.Request.Query["code"];

                var token = await spotyTM.GenerateUserToken(code);

                Console.WriteLine(token);

            });

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

            app.Map("/connect", () =>
            {

                var cnd = new List<string> { "local", "remote" };
                var client_id = Environment.GetEnvironmentVariable("CLIENT_ID");
                var cnt = cnd[1];

                var redirect = "http://localhost:8000/confirm";

                if (cnt == "remote")
                {
                    redirect = Environment.GetEnvironmentVariable("REDIRECT_URL");
                }

                return Results.Redirect($"https://id.twitch.tv/oauth2/authorize?client_id={client_id}&redirect_uri={redirect}&scope=chat:edit%20moderator:manage:banned_users%20chat:read%20channel:manage:vips%20channel:manage:moderators%20channel:manage:polls%20moderator:manage:shoutouts%20user:manage:whispers%20clips:edit%20channel:manage:broadcast%20moderator:manage:chat_messages%20channel:read:subscriptions%20moderator:read:followers&response_type=code&force_verify=true");

            });



            app.MapGet("/confirm", async (HttpContext context,[FromKeyedServices] TwitchBotApi api,[FromKeyedServices] UsersService userService,[FromKeyedServices] ChannelsService channelService) =>
            {
                var code = context.Request.Query["code"];
                await ConfirmUser(code, api, userService, channelService);

            });

            app.MapGet("/ws", async (context) => { });

            app.MapHub<ChatHub>("/chatHub");

            app.Run();
        }

        public static async Task ConfirmUser(string code, TwitchBotApi api, UsersService userService, ChannelsService channelService)
        {

            //Add User to database
            UserToken? token = await api.GetTokenWithCode(code);

            token = await api.GetValidToken(token);

            await userService.CreateUser(token);


            //Update channel followers
            var followers = await api.GetFollowers(token.Username, token.UserId);

            var subs = await api.GetSubs(token.Username, token.UserId);

            await channelService.UpdateChannel(token.Username, followers, subs);

            try
            {
                //Add User profile image
                var profile = await api.GetUserProfile(token.Username);

                token.Profile = profile;

                token = await userService.UpdateUser(token);

            }
            catch (Exception a)
            {

                Console.WriteLine(a);
            }
        }
    }
}
