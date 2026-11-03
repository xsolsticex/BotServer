namespace BotServer.Features.Releases;

public sealed record ReleasePlatform(int Id, string Name, IReadOnlyList<string> Dates);
public sealed record GameRelease(int Id, string Title, string Description, string? ImageUrl,
    string FirstReleaseDate, IReadOnlyList<ReleasePlatform> Platforms);
public sealed record ReleaseCalendar(string PeriodStart, string PeriodEnd, string TimeZone,
    DateTimeOffset UpdatedAt, bool IsStale, IReadOnlyList<GameRelease> Games);

public sealed record ReleasePeriod(DateOnly Start, DateOnly End, DateTimeOffset From, DateTimeOffset Until)
{
    public static ReleasePeriod Current(DateTimeOffset now, TimeZoneInfo zone)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        DateTimeOffset Boundary(DateOnly date) => new(TimeZoneInfo.ConvertTimeToUtc(
            date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), zone));
        return new(today, today.AddDays(1), Boundary(today), Boundary(today.AddDays(2)));
    }
}
