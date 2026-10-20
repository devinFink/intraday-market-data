using System.Net;
using System.Text.Json;
using MarketData.IntegrationTests.Infrastructure;

namespace MarketData.IntegrationTests;

public sealed class DailySummariesEndpointTests : IDisposable
{
    private const string ProblemJson = "application/problem+json";

    private readonly MarketDataApiFactory _factory = new();
    private readonly HttpClient _client;

    public DailySummariesEndpointTests() => _client = _factory.CreateClient();

    [Fact]
    public async Task Get_KnownSymbol_ReturnsDailySummariesInTheRequiredFormatAndPrecision()
    {
        _factory.YahooFinance.Responder = _ => Fixtures.JsonResponse(HttpStatusCode.OK, Fixtures.TslaChart);

        var response = await _client.GetAsync(new Uri("/api/v1/stocks/tsla/daily-summaries", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        response.Content.Headers.ContentType?.MediaType.ShouldBe("application/json");
        var body = await response.Content.ReadAsStringAsync(TestContext.Current.CancellationToken);
        body.ShouldBe(Fixtures.Read(Fixtures.TslaExpectedDailySummaries));
    }

    [Fact]
    public async Task Get_KnownSymbol_RequestsOneMonthOfFifteenMinuteBarsWithABrowserUserAgent()
    {
        _factory.YahooFinance.Responder = _ => Fixtures.JsonResponse(HttpStatusCode.OK, Fixtures.TslaChart);

        await _client.GetAsync(new Uri("/api/v1/stocks/tsla/daily-summaries", UriKind.Relative), TestContext.Current.CancellationToken);

        var request = _factory.YahooFinance.Requests.ShouldHaveSingleItem();
        request.RequestUri!.GetLeftPart(UriPartial.Path).ShouldBe("https://query1.finance.yahoo.com/v8/finance/chart/TSLA");
        request.RequestUri.Query.ShouldBe("?interval=15m&range=1mo&includePrePost=false");
        request.Headers.UserAgent.ToString().ShouldStartWith("Mozilla/5.0");
    }

    [Fact]
    public async Task Get_RepeatedRequest_IsServedFromCache()
    {
        _factory.YahooFinance.Responder = _ => Fixtures.JsonResponse(HttpStatusCode.OK, Fixtures.TslaChart);
        var uri = new Uri("/api/v1/stocks/TSLA/daily-summaries", UriKind.Relative);

        var first = await _client.GetStringAsync(uri, TestContext.Current.CancellationToken);
        var second = await _client.GetStringAsync(uri, TestContext.Current.CancellationToken);

        second.ShouldBe(first);
        _factory.YahooFinance.Requests.Count.ShouldBe(1);
    }

    [Theory]
    [InlineData("BAD%24%24")]
    [InlineData("ABCDEFGHIJKLMNOP")]
    [InlineData("..%2E")]
    public async Task Get_MalformedSymbol_Returns400ValidationProblemWithoutCallingTheProvider(string symbol)
    {
        var response = await _client.GetAsync(new Uri($"/api/v1/stocks/{symbol}/daily-summaries", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemJson);
        using var problem = await ReadJsonAsync(response);
        problem.RootElement.GetProperty("status").GetInt32().ShouldBe(400);
        problem.RootElement.GetProperty("errors").GetProperty("symbol").GetArrayLength().ShouldBe(1);
        _factory.YahooFinance.Requests.ShouldBeEmpty();
    }

    [Fact]
    public async Task Get_UnknownSymbol_Returns404Problem()
    {
        _factory.YahooFinance.Responder = _ => Fixtures.JsonResponse(HttpStatusCode.NotFound, Fixtures.NotFoundChart);

        var response = await _client.GetAsync(new Uri("/api/v1/stocks/NOTAREAL123/daily-summaries", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
        response.Content.Headers.ContentType?.MediaType.ShouldBe(ProblemJson);
        using var problem = await ReadJsonAsync(response);
        problem.RootElement.GetProperty("status").GetInt32().ShouldBe(404);
        problem.RootElement.GetProperty("title").GetString().ShouldBe("Symbol not found");
        problem.RootElement.GetProperty("detail").GetString().ShouldBe("No market data was found for symbol 'NOTAREAL123'.");
        problem.RootElement.GetProperty("instance").GetString().ShouldBe("/api/v1/stocks/NOTAREAL123/daily-summaries");
        problem.RootElement.TryGetProperty("traceId", out _).ShouldBeTrue();
    }

    public void Dispose()
    {
        _client.Dispose();
        _factory.Dispose();
    }

    private static async Task<JsonDocument> ReadJsonAsync(HttpResponseMessage response) =>
        await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
}
