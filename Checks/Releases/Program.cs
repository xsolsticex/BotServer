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
var week = ReleaseWeek.Current(clock.GetUtcNow(), zone);
Check(week.Start.ToString("yyyy-MM-dd") == "2026-11-02" && week.End.ToString("yyyy-MM-dd") == "2026-11-08", "Monday-to-Sunday week in Madrid");
Check(ReleaseWeek.Current(DateTimeOffset.Parse("2026-03-25T12:00:00Z"), zone) is var spring && (spring.Until - spring.From).TotalHours == 167
    && ReleaseWeek.Current(DateTimeOffset.Parse("2026-10-22T12:00:00Z"), zone) is var autumn && (autumn.Until - autumn.From).TotalHours == 169,
    "Week boundaries handle both daylight saving transitions");

IgdbReleaseDate Row(int id, int platform, long timestamp, int category = 0) => new(timestamp, category,
    new(id, "Juego " + id, new string('a', 400), new("//images.igdb.com/igdb/image/upload/t_thumb/test.jpg")),
    new(platform, "Plataforma " + platform, "P" + platform));
var dates = new[] {
    Row(1, 1, week.From.ToUnixTimeSeconds()), Row(1, 1, week.From.ToUnixTimeSeconds()),
    Row(1, 2, week.From.AddDays(2).ToUnixTimeSeconds()), Row(1, 3, week.Until.ToUnixTimeSeconds()),
    Row(2, 1, week.From.AddSeconds(-1).ToUnixTimeSeconds()), Row(3, 1, week.From.AddDays(1).ToUnixTimeSeconds(), 1)
};
var games = WeeklyReleaseService.BuildGames(dates, week);
Check(games.Count == 1 && games[0].Platforms.Count == 2, "One card per game; only platforms released this week");
Check(games[0].Platforms[0].Dates.Count == 1 && games[0].Platforms[1].Dates.Single() == "2026-11-04", "Duplicate release records collapse; platform dates are preserved");
Check(games[0].Description.Length <= 260 && games[0].PriceLabel == "Por confirmar", "Short summary and no invented prices");
Check(games[0].ImageUrl == "https://images.igdb.com/igdb/image/upload/t_cover_big/test.jpg", "Cover URL uses HTTPS and the large IGDB image");
var noCover = dates[0] with { Game = dates[0].Game! with { Cover = new("https://untrusted.test/image.jpg") } };
Check(WeeklyReleaseService.BuildGames([noCover], week)[0].ImageUrl == null, "Untrusted cover URLs use the fallback");

var source = new FakeIgdb { Rows = dates };
var service = new WeeklyReleaseService(source, clock, NullLogger<WeeklyReleaseService>.Instance);
var concurrent = await Task.WhenAll(Enumerable.Range(0, 8).Select(_ => service.GetAsync()));
Check(source.Calls == 1 && concurrent.All(value => value.Games.Count == 1), "Concurrent requests share one cached IGDB response");
clock.Advance(TimeSpan.FromHours(3));
await service.GetAsync();
Check(source.Calls == 2, "Cache refreshes after three hours");
clock.Advance(TimeSpan.FromHours(3)); source.Fail = true;
Check((await service.GetAsync()).IsStale, "A refresh failure preserves this week's data and marks it stale");
clock.Advance(TimeSpan.FromDays(7));
try { await service.GetAsync(); throw new Exception("Previous week was reused"); }
catch (HttpRequestException) { Check(true, "Previous week's cards are never reused on error"); }
var calls = source.Calls;
try { await service.GetAsync(); } catch (HttpRequestException) { }
Check(source.Calls == calls, "Failure backoff prevents repeated upstream requests");
source.Fail = false; source.Rows = [];
clock.Advance(TimeSpan.FromMinutes(1));
Check((await service.GetAsync()).Games.Count == 0, "Empty week is a valid result, not an API error");

var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?> {
    ["CLIENT_ID"] = "test-client", ["CLIENT_SECRET"] = "test-secret"
}).Build();
var transport = new Transport { Rows = [dates[0]] };
var client = new IgdbReleaseClient(transport, configuration, TimeProvider.System);
var response = await client.GetReleaseDatesAsync(week, CancellationToken.None);
await client.GetReleaseDatesAsync(week, CancellationToken.None);
Check(response.Count == 1 && transport.TokenRequests == 1 && transport.ApiRequests == 2,
    "OAuth token is reused across IGDB requests");
Check(transport.Queries.All(query => query.Contains("category = 0") && query.Contains("limit 500")
    && query.Contains($"date >= {week.From.ToUnixTimeSeconds()}") && query.Contains($"date < {week.Until.ToUnixTimeSeconds()}")),
    "IGDB query uses exact-day releases and bounded calendar timestamps");
transport.Unauthorized = true;
await client.GetReleaseDatesAsync(week, CancellationToken.None);
Check(transport.TokenRequests == 2, "An expired token is renewed once on HTTP 401");
transport.Rows = Enumerable.Repeat(dates[0], 500).ToArray();
await client.GetReleaseDatesAsync(week, CancellationToken.None);
Check(transport.Queries.Last().Contains("offset 500"), "IGDB results are paginated beyond 500 rows");
var missing = new IgdbReleaseClient(transport, new ConfigurationBuilder().Build(), TimeProvider.System);
try { await missing.GetReleaseDatesAsync(week, CancellationToken.None); throw new Exception("Missing credentials accepted"); }
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
    public Task<IReadOnlyList<IgdbReleaseDate>> GetReleaseDatesAsync(ReleaseWeek week, CancellationToken token)
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
