using BotServer.Data.Services;

namespace BotServer.Features.Badges
{
    public class ChatBadgeService
    {
        private readonly GlobalBadgesService _globalBadges;
        private readonly CustomBadgesServices _customBadges;

        public ChatBadgeService(GlobalBadgesService globalBadges, CustomBadgesServices customBadges)
        {
            _globalBadges = globalBadges;
            _customBadges = customBadges;
        }
        private static List<string> FormatBadges(IEnumerable<string> badges)
        {
            var result = new List<string>();
            foreach (var element in badges)
            {
                var formatted = element switch
                {
                    "bot-badge" => "Chat Bot",
                    "premium" => "Prime Gaming",
                    _ => element
                };

                if (formatted.Contains('-') || formatted.Contains('_'))
                {
                    var newName = formatted.Split(new[] { '-', '_' }, StringSplitOptions.RemoveEmptyEntries);
                    formatted = string.Join(" ", newName.Select(w => char.ToUpper(w[0]) + (w.Length > 1 ? w.Substring(1) : string.Empty)));
                }

                result.Add(formatted);
            }
            return result;
        }

        public async Task<List<string>> GetBadgesAsync(List<string> rawBadges, string channel)
        {
            var badges = FormatBadges(rawBadges);
            var badgesdb = _globalBadges;
            var custombadgeDb = _customBadges;

            // Both services share the scoped DbContext: do not run EF queries concurrently.
            var customBadges = await custombadgeDb.GetUrlCustomBadges(rawBadges, channel);
            var badgesurl = await badgesdb.GetBadgesUrlbadges(badges);

            var res = new List<string>();
            for (var i = 0; i < badges.Count; i++)
            {
                if (customBadges.TryGetValue(rawBadges[i], out var customUrl))
                {
                    res.Add(customUrl);
                }
                else if (badgesurl.TryGetValue(badges[i].ToLowerInvariant(), out var url))
                {
                    res.Add(url);
                }
            }

            return res;
        }
    }
}
