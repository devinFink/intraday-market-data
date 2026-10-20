using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace MarketData.IntegrationTests.Infrastructure;

/// <summary>
/// Hosts the real API in memory. Outbound HTTP goes to <see cref="YahooFinance"/> unless
/// <paramref name="useLiveYahooFinance"/> is set.
/// </summary>
public sealed class MarketDataApiFactory(bool useLiveYahooFinance = false) : WebApplicationFactory<Program>
{
    public StubHttpMessageHandler YahooFinance { get; } = new();

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        // Every test request comes from the same in-memory client, so lift the per-client limit.
        builder.UseSetting("RateLimiting:PermitLimit", "100000");

        if (!useLiveYahooFinance)
        {
            builder.ConfigureTestServices(services => services.ConfigureHttpClientDefaults(http =>
                http.ConfigurePrimaryHttpMessageHandler(() => YahooFinance)));
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            YahooFinance.Dispose();
        }

        base.Dispose(disposing);
    }
}
