using BotServer.Features.Releases;
using BotServer.Integrations.Igdb;
using System.Text.Json;

namespace BotServer.Web.Endpoints;

public static class ReleaseEndpoints
{
    public static IEndpointRouteBuilder MapReleaseEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/releases", (IWebHostEnvironment env) =>
            Results.File(Path.Combine(env.WebRootPath, "pages", "releases.html"), "text/html"));
        app.MapGet("/api/releases", GetAsync);
        // Keep the previous URL available with the new today/tomorrow calendar.
        app.MapGet("/api/releases/week", GetAsync);
        return app;
    }

    private static async Task<IResult> GetAsync(ReleaseService releases, CancellationToken cancellationToken)
    {
        try { return Results.Ok(await releases.GetAsync(cancellationToken)); }
        catch (Exception error) when (error is HttpRequestException or JsonException or IgdbConfigurationException
            || error is OperationCanceledException && !cancellationToken.IsCancellationRequested)
        {
            return Results.Problem(statusCode: 503, title: "Lanzamientos no disponibles",
                detail: "No se ha podido consultar IGDB. Revisa la configuración del servidor e inténtalo más tarde.");
        }
    }
}
