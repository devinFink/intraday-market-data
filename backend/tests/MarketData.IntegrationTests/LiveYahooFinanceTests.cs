using System.Net;
using System.Text.Json;
using MarketData.IntegrationTests.Infrastructure;

namespace MarketData.IntegrationTests;

/// <summary>
/// Smoke tests against the real Yahoo Finance API. Excluded from CI because they depend on the network and on
/// an undocumented third-party API; run with <c>--filter-trait "Category=External"</c>.
/// </summary>
[Trait("Category", "External")]
public sealed class LiveYahooFinanceTests : IDisposable
{
    private readonly MarketDataApiFactory _factory = new(useLiveYahooFinance: true);

    [Fact]
    public async Task Get_Tsla_ReturnsAboutAMonthOfTradingDays()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/stocks/TSLA/daily-summaries", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.OK);
        using var days = await JsonDocument.ParseAsync(
            await response.Content.ReadAsStreamAsync(TestContext.Current.CancellationToken),
            cancellationToken: TestContext.Current.CancellationToken);
        days.RootElement.GetArrayLength().ShouldBeInRange(15, 24);
    }

    [Fact]
    public async Task Get_UnknownSymbol_Returns404()
    {
        using var client = _factory.CreateClient();

        var response = await client.GetAsync(new Uri("/api/v1/stocks/NOTAREAL123/daily-summaries", UriKind.Relative), TestContext.Current.CancellationToken);

        response.StatusCode.ShouldBe(HttpStatusCode.NotFound);
    }

    public void Dispose() => _factory.Dispose();
}
