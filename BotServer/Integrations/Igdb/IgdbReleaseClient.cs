using System.Net;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using BotServer.Features.Releases;

namespace BotServer.Integrations.Igdb;

public interface IIgdbReleaseClient
{
    Task<IReadOnlyList<IgdbReleaseDate>> GetReleaseDatesAsync(ReleasePeriod period, CancellationToken cancellationToken);
}

public sealed class IgdbConfigurationException : Exception
{
    public IgdbConfigurationException() : base("Configura CLIENT_ID y CLIENT_SECRET para consultar IGDB.") { }
}

public sealed class IgdbReleaseClient(IHttpClientFactory clients, IConfiguration configuration, TimeProvider clock) : IIgdbReleaseClient
{
    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _token;
    private DateTimeOffset _tokenExpires;
    private DateTimeOffset _nextRequest;
    private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true };

    public async Task<IReadOnlyList<IgdbReleaseDate>> GetReleaseDatesAsync(ReleasePeriod period, CancellationToken cancellationToken)
    {
        var clientId = configuration["CLIENT_ID"];
        var secret = configuration["CLIENT_SECRET"];
        if (string.IsNullOrWhiteSpace(clientId) || string.IsNullOrWhiteSpace(secret)) throw new IgdbConfigurationException();
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var result = new List<IgdbReleaseDate>();
            for (var page = 0; page < 20; page++)
            {
                // category is deprecated. Expand the current date_format relation and check precision locally.
                var query = $"fields date,date_format.format,game.id,game.name,game.summary,game.cover.url,game.keywords.slug,platform.id,platform.name,platform.abbreviation; " +
                    $"where date >= {period.From.ToUnixTimeSeconds()} & date < {period.Until.ToUnixTimeSeconds()}; " +
                    $"sort date asc; limit 500; offset {page * 500};";
                var rows = await RequestPageAsync(clientId, secret, query, cancellationToken);
                result.AddRange(rows);
                if (rows.Count < 500) return result;
            }
            throw new HttpRequestException("IGDB exceeded the pagination limit; refusing to return an incomplete period.");
        }
        finally { _gate.Release(); }
    }

    private async Task<List<IgdbReleaseDate>> RequestPageAsync(string clientId, string secret, string query, CancellationToken cancellationToken)
    {
        for (var attempt = 0; attempt < 2; attempt++)
        {
            await EnsureTokenAsync(clientId, secret, cancellationToken);
            var wait = _nextRequest - clock.GetUtcNow();
            if (wait > TimeSpan.Zero) await Task.Delay(wait, clock, cancellationToken);
            _nextRequest = clock.GetUtcNow().AddMilliseconds(350); // Below IGDB's four requests per second.
            using var request = new HttpRequestMessage(HttpMethod.Post, "release_dates");
            request.Headers.Add("Client-ID", clientId);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _token);
            request.Content = new StringContent(query, Encoding.UTF8, "text/plain");
            using var client = clients.CreateClient("Igdb");
            using var response = await client.SendAsync(request, cancellationToken);
            if (response.StatusCode == HttpStatusCode.Unauthorized && attempt == 0)
            {
                _token = null;
                continue;
            }
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<IgdbReleaseDate>>(JsonOptions, cancellationToken)
                ?? throw new JsonException("IGDB returned no release list.");
        }
        throw new HttpRequestException("IGDB authorization failed.");
    }

    private async Task EnsureTokenAsync(string clientId, string secret, CancellationToken cancellationToken)
    {
        if (_token != null && clock.GetUtcNow() < _tokenExpires) return;
        using var content = new FormUrlEncodedContent(new Dictionary<string, string>
        {
            ["client_id"] = clientId, ["client_secret"] = secret, ["grant_type"] = "client_credentials"
        });
        using var client = clients.CreateClient("IgdbOAuth");
        using var response = await client.PostAsync("oauth2/token", content, cancellationToken);
        response.EnsureSuccessStatusCode();
        var token = await response.Content.ReadFromJsonAsync<OAuthToken>(cancellationToken);
        if (string.IsNullOrWhiteSpace(token?.AccessToken) || token.ExpiresIn <= 0) throw new JsonException("Invalid IGDB application token.");
        _token = token.AccessToken;
        _tokenExpires = clock.GetUtcNow().AddSeconds(Math.Max(1, token.ExpiresIn - 60));
    }

    private sealed record OAuthToken([property: JsonPropertyName("access_token")] string AccessToken,
        [property: JsonPropertyName("expires_in")] int ExpiresIn);
}

public sealed record IgdbReleaseDate(long Date, int? Category, IgdbGame? Game, IgdbPlatform? Platform,
    [property: JsonPropertyName("date_format")] IgdbDateFormat? DateFormat = null)
{
    public bool HasExactDay => DateFormat != null
        ? string.Concat((DateFormat.Format ?? "").Where(char.IsLetter)).ToUpperInvariant() is "YYYYMMMMDD" or "YYYYMMDD"
        : Category == 0; // Compatibility with stored fixtures/legacy responses; missing category is not zero.
}
public sealed record IgdbDateFormat(string? Format);
public sealed record IgdbGame(int Id, string? Name, string? Summary, IgdbCover? Cover,
    IReadOnlyList<IgdbKeyword>? Keywords = null)
{
    public bool IsFreeToPlay => Keywords?.Any(keyword =>
        string.Concat((keyword.Slug ?? "").Where(char.IsLetterOrDigit)).ToLowerInvariant()
            is "freetoplay" or "f2p" or "free2play") == true;
}
public sealed record IgdbKeyword(string? Slug);
public sealed record IgdbCover(string? Url);
public sealed record IgdbPlatform(int Id, string? Name, string? Abbreviation);
