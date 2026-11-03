namespace BotServer.Integrations.Twitch.EventSub.Conditions
{
    public interface IEventSubCondition
    {
        public Dictionary<string, string> ToDictionary();
    }
}
