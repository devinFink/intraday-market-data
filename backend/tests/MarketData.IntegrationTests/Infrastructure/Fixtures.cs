using System.Net;
using System.Net.Mime;
using System.Text;

namespace MarketData.IntegrationTests.Infrastructure;

internal static class Fixtures
{
    /// <summary>Recorded Yahoo Finance response: TSLA, 15-minute bars, one month, ending 2026-10-20.</summary>
    public const string TslaChart = "yahoo-chart-tsla-15m-1mo.json";

    /// <summary>Yahoo Finance's 404 body for an unknown symbol.</summary>
    public const string NotFoundChart = "yahoo-chart-not-found.json";

    /// <summary>
    /// The API response expected for <see cref="TslaChart"/>, computed independently of the API with exact
    /// decimal arithmetic.
    /// </summary>
    public const string TslaExpectedDailySummaries = "expected-daily-summaries-tsla.json";

    public static string Read(string name) =>
        File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "Fixtures", name)).Trim();

    public static HttpResponseMessage JsonResponse(HttpStatusCode statusCode, string fixtureName) =>
        new(statusCode)
        {
            Content = new StringContent(Read(fixtureName), Encoding.UTF8, MediaTypeNames.Application.Json),
        };
}
