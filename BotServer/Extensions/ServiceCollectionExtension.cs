using BotServer.API;
using BotServer.Database;
using BotServer.Database.Services;
using BotServer.SpotifyClientApi;
using BotServer.TwitchBotClient;
using BotServer.TwitchBotClient.SignalRClient;
using Microsoft.EntityFrameworkCore;
using System.Net.Http.Headers;
using System.Text;
using TwitchLib.Api;
using TwitchLib.EventSub.Webhooks.Extensions;

namespace BotServer.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection AddBotServices(this IServiceCollection services, string dbPath)
        {

            services.AddSignalR(options =>
            {
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });

            services.AddSingleton<OAuthService>();
            services.AddSingleton<TwitchBot>();
            services.AddHostedService<TwitchBot>(sp => sp.GetRequiredService<TwitchBot>());
            services.AddSingleton<BotEventHandler>();
            services.AddDbContext<TwitchDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
            services.AddScoped<TwitchClientFactory>();
            services.AddSingleton<TwitchAPI>(new TwitchAPI());
            services.AddSingleton<TwitchBotApi>();
            services.AddScoped<UsersService>();
            services.AddSingleton<BotSignalRClient>();
            services.AddScoped<TokensService>();
            services.AddScoped<ChannelsService>();
            services.AddScoped<GlobalBadgesService>();
            services.AddScoped<CustomBadgesServices>();
            services.AddScoped<JoinedChannelsServices>();

            services.AddScoped<SpotifyService>();
            services.AddSingleton<SpotyTokenManager>(c =>
            {
                HttpClient client = new HttpClient();
                client.BaseAddress = new Uri("https://accounts.spotify.com/api/token");

                var spotify_id = Environment.GetEnvironmentVariable("SPOTIFY_ID");
                var spotify_secret = Environment.GetEnvironmentVariable("SPOTIFY_SECRET");
                var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{spotify_id}:{spotify_secret}"));

                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

                return new SpotyTokenManager(client);


            });
            services.AddSingleton<SpotyClient>();


            services.AddTwitchLibEventSubWebhooks(config =>
            {
                config.CallbackPath = "/eventSub";
                config.Secret = Environment.GetEnvironmentVariable("HOOK_SECRET");

            });

            services.AddHostedService<EventSubHostedService>();


            return services;
        }
    }
}
