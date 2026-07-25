using BotServer.API;
using BotServer.Database;
using BotServer.Database.Services;
using BotServer.Hubs;
using BotServer.TwitchBotClient;
using BotServer.TwitchBotClient.SignalRClient;
using Microsoft.EntityFrameworkCore;
using System.Reflection.Metadata.Ecma335;
using TwitchLib.Api;
using TwitchLib.Client;

namespace BotServer
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var path = AppContext.BaseDirectory;
            var dbPath = Path.Combine("data", "twitch.db");
            var builder = WebApplication.CreateBuilder(args);
            builder.Services.AddSignalR(options =>
            {
                options.ClientTimeoutInterval = TimeSpan.FromSeconds(60);
                options.KeepAliveInterval = TimeSpan.FromSeconds(15);
            });
            builder.Services.AddSingleton<TwitchBot>();
            builder.Services.AddHostedService<TwitchBot>(sp=> sp.GetRequiredService<TwitchBot>());
            builder.Services.AddSingleton<BotEventHandler>();
            builder.Services.AddSingleton<MessageSender>();
            builder.Services.AddDbContext<TwitchDbContext>(options => options.UseSqlite($"Data Source={dbPath}"));
            builder.Services.AddScoped<TwitchClientFactory>();  
            builder.Services.AddSingleton<TwitchAPI>(new TwitchAPI());
            builder.Services.AddSingleton<TwitchBotApi>();
            builder.Services.AddScoped<UsersService>();
            builder.Services.AddSingleton<BotSignalRClient>();
            builder.Services.AddScoped<TokensService>();

            var app = builder.Build();

            app.UseStaticFiles();
            app.UseWebSockets();
            
            app.MapGet("/chat/{username}", (string username) => { return Results.File("index.html", "text/html"); });

            app.MapGet("/confirm", async (HttpContext context,[FromKeyedServices] TwitchBotApi api, [FromKeyedServices] UsersService userService) => {
                
                var code = context.Request.Query["code"];

                var token = await api.GetTokenWithCode(code);


                token = await api.GetValidToken(token);


                
                await userService.CreateUser(token);

                try
                {
                    var profile = await api.GetUserProfile(token.Username);

                    token.Profile = profile;

                    token = await userService.UpdateUser(token);


                    Console.WriteLine(token);
                }
                catch (Exception a)
                {

                    Console.WriteLine(a);
                }



       
                
               
            
            
            });

            app.MapGet("/ws", async (context) => { });

            app.MapHub<ChatHub>("/chatHub");

         

            app.Run();
        }
    }
}
