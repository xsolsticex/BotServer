using System.Net;
using System.Text.Json;
using BotServer.Features.Releases;
using BotServer.Integrations.Igdb;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

int passed = 0;
void Check(bool condition, string name) { if (!condition) throw new Exception(name); Console.WriteLine("PASS: " + name); passed++; }
var zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
var clock = new ManualClock(DateTimeOffset.Parse("2026-11-03T12:00:00Z"));
var period = ReleasePeriod.Current(clock.GetUtcNow(), zone);
Check(period.Start.ToString("yyyy-MM-dd") == "2026-11-03" && period.End.ToString("yyyy-MM-dd") == "2026-11-04", "Today and tomorrow in Madrid");
Check(ReleasePeriod.Current(DateTimeOffset.Parse("2026-11-03T23:30:00Z"), zone).Start.ToString("yyyy-MM-dd") == "2026-11-04",
    "Today follows the Madrid date, not the UTC date");
Check(ReleasePeriod.Current(DateTimeOffset.Parse("2026-03-28T12:00:00Z"), zone) is var spring && (spring.Until - spring.From).TotalHours == 47
    && ReleasePeriod.Current(DateTimeOffset.Parse("2026-10-24T12:00:00Z"), zone) is var autumn && (autumn.Until - autumn.From).TotalHours == 49,
    "Calendar day boundaries handle both daylight saving transitions");

IgdbReleaseDate Row(int id, int platform, long timestamp, int category = 0) => new(timestamp, category,
    new(id, "Juego " + id, new string('a', 400), new("//images.igdb.com/igdb/image/upload/t_thumb/test.jpg")),
    new(platform, "Plataforma " + platform, "P" + platform));
var dates = new[] {
    Row(1, 1, period.From.ToUnixTimeSeconds()), Row(1, 1, period.From.ToUnixTimeSeconds()),
    Row(1, 2, period.From.AddDays(1).ToUnixTimeSeconds()), Row(1, 3, period.Until.ToUnixTimeSeconds()),
    Row(2, 1, period.From.AddSeconds(-1).ToUnixTimeSeconds()), Row(3, 1, period.From.AddDays(1).ToUnixTimeSeconds(), 1)
};
var games = ReleaseService.BuildGames(dates, period);
Check(games.Count == 1 && games[0].Platforms.Count == 2, "One card per game; only platforms released this period");
Check(games[0].Platforms[0].Dates.Count == 1 && games[0].Platforms[1].Dates.Single() == "2026-11-04", "Duplicate release records collapse; platform dates are preserved");
Check(games[0].Description.Length <= 260 && !JsonSerializer.Serialize(games[0]).Contains("Price", StringComparison.OrdinalIgnoreCase), "Short summary and no price in the API response");
Check(games[0].ImageUrl == "https://images.igdb.com/igdb/image/upload/t_cover_big/test.jpg", "Cover URL uses HTTPS and the large IGDB image");
var noCover = dates[0] with { Game = dates[0].Game! with { Cover = new("https://untrusted.test/image.jpg") } };
Check(ReleaseService.BuildGames([noCover], period)[0].ImageUrl == null, "Untrusted cover URLs use the fallback");

foreach (var slug in new[] { "free-to-play", "f2p", "free2play" })
{
    var free = dates[0] with { Game = dates[0].Game! with { Keywords = [new(slug)] } };
    Check(ReleaseService.BuildGames([free], period).Count == 0, "Exclude IGDB free-to-play keyword: " + slug);
}
var taggedPlatform = dates[1] with { Game = dates[1].Game! with { Keywords = [new("free-to-play")] } };
Check(ReleaseService.BuildGames([dates[0], taggedPlatform], period).Count == 0,
    "A free-to-play tag on any platform removes the whole game");
var untagged = dates[0] with { Game = dates[0].Game! with { Keywords = [new("action")] } };
Check(ReleaseService.BuildGames([untagged], period).Count == 1,
    "Unrelated keywords do not exclude a game");

var modernJson = """
    [{"date":TIMESTAMP,"date_format":{"id":1,"format":"YYYYMMMMDD"},
      "game":{"id":11,"name":"Modern IGDB game"},"platform":{"id":6,"name":"PC"}}]
    """.Replace("TIMESTAMP", period.From.ToUnixTimeSeconds().ToString());
var modern = JsonSerializer.Deserialize<List<IgdbReleaseDate>>(modernJson,
    new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
Check(ReleaseService.BuildGames(modern, period).Count == 1 && modern[0].Category == null,
    "Current IGDB date_format responses work without the deprecated category field");
Check(ReleaseService.BuildGames([modern[0] with { DateFormat = new("YYYY-MM-DD") }], period).Count == 1,
    "Exact dates with separators are supported");
Check(ReleaseService.BuildGames([modern[0] with { Category = 0, DateFormat = new("YYYYMMMM") }], period).Count == 0
    && ReleaseService.BuildGames([modern[0] with { DateFormat = null }], period).Count == 0,
    "Monthly or unknown precision is never mistaken for an exact release day");

var source = new FakeIgdb { Rows = dates };
var service = new ReleaseService(source, clock, NullLogger<ReleaseService>.Instance);
var concurrent = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => service.GetAsync()));
Check(source.Calls == 1 && concurrent.All(value => value.Games.Count == 1), "Concurrent requests share one cached IGDB response");
clock.Advance(TimeSpan.FromHours(3));
await service.GetAsync();
Check(source.Calls == 2, "Cache refreshes after three hours");
clock.Advance(TimeSpan.FromHours(3)); source.Fail = true;
Check((await service.GetAsync()).IsStale, "A refresh failure preserves this period's data and marks it stale");
clock.Advance(TimeSpan.FromDays(1));
try { await service.GetAsync(); throw new Exception("Previous period was reused"); }
catch (HttpRequestException) { Check(true, "Previous period's cards are never reused on error"); }
var calls = source.Calls;
try { await service.GetAsync(); } catch (HttpRequestException) { }
Check(source.Calls == calls, "Failure backoff prevents repeated upstream requests");
source.Fail = false; source.Rows = [];
clock.Advance(TimeSpan.FromMinutes(1));
Check((await service.GetAsync()).Games.Count == 0, "Empty period is a valid result, not an API error");
calls = source.Calls;
clock.Advance(TimeSpan.FromMinutes(10));
await service.GetAsync();
Check(source.Calls == calls + 1, "Empty periods refresh after ten minutes instead of three hours");

var midnightClock = new ManualClock(DateTimeOffset.Parse("2026-11-03T22:50:00Z"));
var midnightSource = new FakeIgdb { Rows = dates };
var midnightService = new ReleaseService(midnightSource, midnightClock, NullLogger<ReleaseService>.Instance);
await midnightService.GetAsync();
midnightClock.Advance(TimeSpan.FromMinutes(20));
var afterMidnight = await midnightService.GetAsync();
Check(midnightSource.Calls == 2 && afterMidnight.PeriodStart == "2026-11-04"
    && afterMidnight.Games.All(game => game.FirstReleaseDate == "2026-11-04"),
    "Midnight refreshes the calendar even when the three-hour cache is still fresh");

var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
    ["CLIENT_ID"] = "test-client", ["CLIENT_SECRET"] = "test-secret"
}).Build();
var transport = new Transport { Rows = [dates[0]] };
var client = new IgdbReleaseClient(transport, configuration, TimeProvider.System);
var response = await client.GetReleaseDatesAsync(period, CancellationToken.None);
await client.GetReleaseDatesAsync(period, CancellationToken.None);
Check(response.Count == 1 && transport.TokenRequests == 1 && transport.ApiRequests == 2,
    "OAuth token is reused across IGDB requests");
Check(transport.Queries.All(query => query.Contains("date_format.format") && query.Contains("game.keywords.slug") && !query.Contains("category") && query.Contains("limit 500")
    && query.Contains($"date >= {period.From.ToUnixTimeSeconds()}") && query.Contains($"date < {period.Until.ToUnixTimeSeconds()}")),
    "IGDB query uses the current date_format relation without deprecated category filters");
transport.Unauthorized = true;
await client.GetReleaseDatesAsync(period, CancellationToken.None);
Check(transport.TokenRequests == 2, "An expired token is renewed once on HTTP 401");
transport.Rows = Enumerable.Repeat(dates[0], 500).ToArray();
await client.GetReleaseDatesAsync(period, CancellationToken.None);
Check(transport.Queries.Last().Contains("offset 500"), "IGDB results are paginated beyond 500 rows");
var missing = new IgdbReleaseClient(transport, new ConfigurationBuilder().Build(), TimeProvider.System);
try { await missing.GetReleaseDatesAsync(period, CancellationToken.None); throw new Exception("Missing credentials accepted"); }
catch (IgdbConfigurationException) { Check(true, "Missing credentials fail without sending a request"); }
Console.WriteLine($"{passed} checks passed.");

sealed class ManualClock(DateTimeOffset now) : TimeProvider
{
    public override DateTimeOffset GetUtcNow() => now;
    public void Advance(TimeSpan value) => now += value;
}
sealed class FakeIgdb : IIgdbReleaseClient
{
    public IReadOnlyList<IgdbReleaseDate> Rows = [];
    public bool Fail;
    public int Calls;
    public Task<IReadOnlyList<IgdbReleaseDate>> GetReleaseDatesAsync(ReleasePeriod period, CancellationToken token)
    {
        Calls++;
        if (Fail) throw new HttpRequestException("simulated outage");
        return Task.FromResult(Rows);
    }
}
sealed class Transport : HttpMessageHandler, IHttpClientFactory
{
    public IReadOnlyList<IgdbReleaseDate> Rows = [];
    public bool Unauthorized;
    public int TokenRequests;
    public int ApiRequests;
    public List<string> Queries = [];
    public HttpClient CreateClient(string name) => new(this, disposeHandler: false) {
        BaseAddress = new Uri(name == "Igdb" ? "https://api.igdb.com/v4/" : "https://id.twitch.tv/")
    };
    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        if (request.RequestUri!.Host == "id.twitch.tv")
        {
            if (request.Method != HttpMethod.Post || !string.IsNullOrEmpty(request.RequestUri.Query)) throw new Exception("Credentials must not appear in URLs");
            TokenRequests++;
            return new(HttpStatusCode.OK) { Content = new StringContent("{\"access_token\":\"test-token\",\"expires_in\":3600}") };
        }
        if (request.Headers.GetValues("Client-ID").Single() != "test-client" || request.Headers.Authorization?.Scheme != "Bearer")
            throw new Exception("Missing IGDB headers");
        ApiRequests++;
        var query = await request.Content!.ReadAsStringAsync(cancellationToken);
        Queries.Add(query);
        if (Unauthorized) { Unauthorized = false; return new(HttpStatusCode.Unauthorized); }
        return new(HttpStatusCode.OK) { Content = new StringContent(JsonSerializer.Serialize(query.Contains("offset 500") ? [] : Rows)) };
    }
}
