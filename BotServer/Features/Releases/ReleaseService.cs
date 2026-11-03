using System.Text.Json;
using BotServer.Integrations.Igdb;

namespace BotServer.Features.Releases;

public sealed class ReleaseService(IIgdbReleaseClient igdb, TimeProvider clock, ILogger<ReleaseService> logger)
{
    private static readonly TimeZoneInfo Zone = TimeZoneInfo.FindSystemTimeZoneById("Europe/Madrid");
    private readonly SemaphoreSlim _gate = new(1, 1);
    private ReleaseCalendar? _cached;
    private DateTimeOffset _refreshAt;
    private DateTimeOffset _retryAt;

    public async Task<ReleaseCalendar> GetAsync(CancellationToken cancellationToken = default)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var now = clock.GetUtcNow();
            var period = ReleasePeriod.Current(now, Zone);
            var key = period.Start.ToString("yyyy-MM-dd");
            var samePeriod = _cached?.PeriodStart == key;
            if (samePeriod && now < _refreshAt) return _cached!;
            if (now < _retryAt)
            {
                if (samePeriod) return _cached! with { IsStale = true };
                throw new HttpRequestException("IGDB is temporarily unavailable.");
            }
            try
            {
                var dates = await igdb.GetReleaseDatesAsync(period, cancellationToken);
                _cached = new(key, period.End.ToString("yyyy-MM-dd"), Zone.Id, clock.GetUtcNow(), false, BuildGames(dates, period));
                _refreshAt = clock.GetUtcNow().Add(_cached.Games.Count == 0 ? TimeSpan.FromMinutes(10) : TimeSpan.FromHours(3));
                _retryAt = default;
                logger.LogInformation("IGDB period {Start} to {End}: {Rows} release records, {Games} unique games.",
                    key, _cached.PeriodEnd, dates.Count, _cached.Games.Count);
                if (dates.Count > 0 && _cached.Games.Count == 0)
                    logger.LogWarning("IGDB returned release records but none contained an exact day and a valid game/platform.");
                return _cached;
            }
            catch (Exception error) when (error is HttpRequestException or JsonException or IgdbConfigurationException
                || error is OperationCanceledException && !cancellationToken.IsCancellationRequested)
            {
                _retryAt = clock.GetUtcNow().AddMinutes(1);
                // Do not log upstream request bodies or credentials.
                logger.LogWarning("IGDB releases could not be refreshed ({ErrorType}).", error.GetType().Name);
                if (samePeriod) return _cached! with { IsStale = true };
                throw;
            }
        }
        finally { _gate.Release(); }
    }

    public static IReadOnlyList<GameRelease> BuildGames(IEnumerable<IgdbReleaseDate> rows, ReleasePeriod period)
    {
        string LocalDate(long timestamp) => TimeZoneInfo.ConvertTime(DateTimeOffset.FromUnixTimeSeconds(timestamp), Zone).ToString("yyyy-MM-dd");
        return rows.Where(row => row.HasExactDay && row.Date >= period.From.ToUnixTimeSeconds()
                && row.Date < period.Until.ToUnixTimeSeconds() && row.Game?.Id > 0
                && !string.IsNullOrWhiteSpace(row.Game.Name) && row.Platform?.Id > 0
                && !string.IsNullOrWhiteSpace(row.Platform.Name))
            .GroupBy(row => row.Game!.Id)
            .Where(group => !group.Any(row => row.Game!.IsFreeToPlay))
            .Select(group =>
            {
                var game = group.First().Game!;
                var summary = game.Summary?.Trim();
                summary = string.IsNullOrWhiteSpace(summary) ? "Descripción no disponible." : summary.Length > 260 ? summary[..257] + "…" : summary;
                var platforms = group.GroupBy(row => row.Platform!.Id).Select(platform => new ReleasePlatform(platform.Key,
                    platform.First().Platform!.Abbreviation ?? platform.First().Platform!.Name!,
                    platform.Select(row => LocalDate(row.Date)).Distinct().Order().ToArray())).OrderBy(platform => platform.Name).ToArray();
                string? image = game.Cover?.Url;
                if (image?.StartsWith("//") == true) image = "https:" + image;
                if (!Uri.TryCreate(image, UriKind.Absolute, out var uri) || uri.Scheme != "https" || uri.Host != "images.igdb.com") image = null;
                else image = image!.Replace("/t_thumb/", "/t_cover_big/", StringComparison.Ordinal);
                return new GameRelease(game.Id, game.Name!, summary, image,
                    LocalDate(group.Min(row => row.Date)), platforms);
            })
            .OrderBy(game => game.FirstReleaseDate).ThenBy(game => game.Title).ToArray();
    }
}
