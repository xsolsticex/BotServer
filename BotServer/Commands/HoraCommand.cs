using BotServer.API;
using BotServer.Commands.Interface;
using TwitchLib.Api;
using TwitchLib.Client;

namespace BotServer.Commands
{
    public class HoraCommand : ITwitchCommand
    {
        private TwitchClient _client;
        private TwitchBotApi _api;

        public string Name => "hora";

        public HoraCommand(TwitchClient client,TwitchBotApi api)
        {
            _client = client;
            _api  = api;
        }

        public async Task ExecuteAsync(string channel,string username, string[] args)
        {
            var broadcaster = await _api.GetUserData(channel);
            var bot = await _api.GetUserData("xhipibotx");
            var token = await _api.GetAppToken();

            TimeZoneInfo zona = TimeZoneInfo.FindSystemTimeZoneById("Romance Standard Time");
            var time = TimeZoneInfo.ConvertTime(DateTime.UtcNow, zona);
            var timeToString = time.ToString();

            await _api.SendChatMessage(broadcaster.TwitchId, bot.TwitchId, timeToString, token);

        }
    }
}
