namespace BotServer.Features.Releases;

public sealed record ReleasePlatform(int Id, string Name, IReadOnlyList<string> Dates);
public sealed record WeeklyRelease(int Id, string Title, string Description, string? ImageUrl,
    string FirstReleaseDate, IReadOnlyList<ReleasePlatform> Platforms);
public sealed record WeeklyReleases(string WeekStart, string WeekEnd, string TimeZone,
    DateTimeOffset UpdatedAt, bool IsStale, IReadOnlyList<WeeklyRelease> Games);

public sealed record ReleaseWeek(DateOnly Start, DateOnly End, DateTimeOffset From, DateTimeOffset Until)
{
    public static ReleaseWeek Current(DateTimeOffset now, TimeZoneInfo zone)
    {
        var today = DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(now, zone).DateTime);
        var monday = today.AddDays(-((int)today.DayOfWeek + 6) % 7);
        DateTimeOffset Boundary(DateOnly date) => new(TimeZoneInfo.ConvertTimeToUtc(
            date.ToDateTime(TimeOnly.MinValue, DateTimeKind.Unspecified), zone));
        return new(monday, monday.AddDays(6), Boundary(monday), Boundary(monday.AddDays(7)));
    }
}
