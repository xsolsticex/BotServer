using BotServer.Integrations.Twitch;
using BotServer.Features.Chat.Markov;
using BotServer.Configuration;
using BotServer.Data.Services;
using BotServer.Web.SignalR;
using TwitchLib.Client;
using TwitchLib.Client.Events;

namespace BotServer.Integrations.Twitch.Chat
{
    public class BotCommandHandler
    {
        private readonly MarkovChatService _markov;
        private readonly TwitchBotApi _api;
        private readonly ChannelsService _channels;
        private readonly IBotSignalRClient _signalR;

        public BotCommandHandler(TwitchBotApi api, ChannelsService channels, IBotSignalRClient signalR, MarkovChatService markov)
        {
            _markov = markov;
            _api = api;
            _channels = channels;
            _signalR = signalR;
        }

        public async Task HandleAsync(OnChatCommandReceivedArgs e, TwitchClient? client)
        {
            var command = e.Command.Name;
            var channel = e.ChatMessage.Channel;
            var username = e.ChatMessage.Username;
            var user = e.ChatMessage.UserId;
            var messageID = e.ChatMessage.Id.ToString();

            switch (command.ToLowerInvariant())
            {
                case "new":
                    if (!username.Equals(channel, StringComparison.OrdinalIgnoreCase))
                    {
                        if (client is not null)
                        {
                            await client.SendReplyAsync(channel, messageID, "Solo el propietario del canal puede limpiar Markov.");
                        }
                        break;
                    }
                    try
                    {
                        _markov.ClearChannel(channel);
                    }
                    catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
                    {
                        if (client is not null)
                        {
                            await client.SendReplyAsync(channel, messageID, "No se pudo guardar la limpieza de Markov. Revisa el registro del bot.");
                        }
                        break;
                    }
                    if (client is not null)
                    {
                        await client.SendReplyAsync(channel, messageID, "Markov de este canal se ha limpiado. Aprenderé de los nuevos mensajes.");
                    }
                    break;
                case "hora":
                    await HandleTimeCommand(channel);
                    break;
                case "color":
                    await HandleColorCommand(channel, messageID, e.Command.ArgumentsAsList);
                    break;
                case "join":
                    await HandleJoinCommand(username, channel, user, messageID, client);
                    break;
                case "counter":
                case "status":
                case "notifier":
                case "chat":
                    if (client != null)
                    {
                        await client.SendReplyAsync(channel, messageID, $"https://botserver-qccm.onrender.com/{command}/{username.ToLower()}");
                    }
                    break;
                case "win":
                case "lose":
                case "reset":
                    await HandleCounterCommand(channel, username, command);
                    break;
                case "help":
                    if (client != null)
                    {
                        await client.SendReplyAsync(channel, messageID, "Comandos: Test: !hora, Unir a canal: !join, Url contador: !counter, Url estado: !status, Url chat: !chat, Comandos contador: !win, !lose, !reset; Limpiar Markov del canal (propietario): !new");
                    }
                    break;
            }
        }

        private async Task HandleJoinCommand(string username, string channel, string userId, string messageID, TwitchClient? client)
        {
            var db = _channels;
            var exists = await db.FindChannel(username);
            if (exists == null)
            {
                try
                {
                    await _api.GetCustomBadges(userId, channel);

                    if (client != null)
                    {
                        await client.JoinChannelAsync(username);
                    }
                    await db.AddChannel(username);

                    var urlAuth = AppEnvironment.IsRemote
                        ? "https://botserver-qccm.onrender.com/connect"
                        : "http://localhost:8000/connect";

                    if (client != null)
                    {
                        await client.SendReplyAsync(channel, messageID, $"Para dar permisos usa el siguiente enlace: {urlAuth}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex);
                }
            }
            else
            {
                if (client != null)
                {
                    await client.SendReplyAsync(channel, messageID, "Ya estoy unido a tu canal");
                }
            }
        }

        private async Task HandleCounterCommand(string channel, string username, string counterStatus)
        {
            if (username.Equals(channel, StringComparison.OrdinalIgnoreCase))
            {
                await _signalR.UpdateCounter(channel, counterStatus);
            }
        }

        private async Task HandleTimeCommand(string channel)
        {
            var broadcasterTask = _api.GetUserData(channel);
            var botTask = _api.GetUserData("xhipibotx");
            var tokenTask = _api.GetAppToken();

            await Task.WhenAll(broadcasterTask, botTask, tokenTask);

            var broadcaster = broadcasterTask.Result;
            var bot = botTask.Result;
            var token = tokenTask.Result;

            TimeZoneInfo zona = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            var time = TimeZoneInfo.ConvertTime(DateTime.UtcNow, zona);
            var timeToString = time.ToString();

            await _api.SendChatMessage(broadcaster.TwitchId, bot.TwitchId, timeToString, token);
        }

        private async Task HandleColorCommand(string channel, string messageId, List<string> argumentsAsList)
        {
            try
            {
                if (argumentsAsList.Count > 0 && !string.IsNullOrWhiteSpace(argumentsAsList[0]))
                {
                    var color = argumentsAsList[0];
                    if (!color.StartsWith('#'))
                    {
                        color = "#" + color;
                    }

                    await _signalR.ChangeColor(channel, color);
                    return;
                }

                var broadcasterTask = _api.GetUserData(channel);
                var botTask = _api.GetUserData("xhipibotx");
                var tokenTask = _api.GetAppToken();

                await Task.WhenAll(broadcasterTask, botTask, tokenTask);

                await _api.ReplyChatMessage(broadcasterTask.Result.TwitchId, botTask.Result.TwitchId, "Debes pasar un color en formato Hexadecimal como parametro", tokenTask.Result, messageId);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }


    }
}
