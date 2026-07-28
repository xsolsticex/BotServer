using BotServer.API;
using BotServer.Database;
using BotServer.Database.Services;
using BotServer.Hubs;
using BotServer.TwitchBotClient;
using BotServer.TwitchBotClient.SignalRClient;
using Microsoft.EntityFrameworkCore;
using TwitchLib.Api;

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
            if(con == "local")
            {
                dbPath = Path.Combine(path,"twitch.db");
            }
            else
            {
                dbPath = "/data/twitch.db";
            }

          
           
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
            builder.Services.AddScoped<ChannelsService>();
            builder.Services.AddScoped<GlobalBadgesService>();



            var app = builder.Build();

            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<TwitchDbContext>();
                db.Database.Migrate();
            }


            
            app.UseStaticFiles();
            app.UseWebSockets();
            
            app.MapGet("/chat/{username}", (string username) => { return Results.File("index.html", "text/html"); });

            app.MapGet("/counter/{username}", (string username) => { return Results.File("counter.html", "text/html"); });


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
       
               
                
                return Results.Redirect($"https://id.twitch.tv/oauth2/authorize?client_id={client_id}&redirect_uri={redirect}&scope=chat:edit%20moderator:manage:banned_users%20chat:read%20channel:manage:vips%20channel:manage:moderators%20channel:manage:polls%20moderator:manage:shoutouts%20user:manage:whispers%20clips:edit%20channel:manage:broadcast%20moderator:manage:chat_messages&response_type=code&force_verify=true");

            });

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
