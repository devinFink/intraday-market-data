using System.Net.Http.Headers;
using System.Net.Mime;
using MarketData.Application.Abstractions;
using MarketData.Infrastructure.YahooFinance;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Http.Resilience;
using Microsoft.Extensions.Options;

namespace MarketData.Infrastructure;

public static class DependencyInjection
{
    /// <summary>Registers the Yahoo Finance market data provider and the hybrid cache.</summary>
    public static IServiceCollection AddInfrastructure(this IServiceCollection services)
    {
        services.AddOptions<YahooFinanceOptions>()
            .BindConfiguration(YahooFinanceOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<YahooChartMapper>();

        services
            .AddHttpClient<IMarketDataProvider, YahooFinanceMarketDataProvider>((serviceProvider, client) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<YahooFinanceOptions>>().Value;
                client.BaseAddress = options.BaseAddress;
                client.DefaultRequestHeaders.TryAddWithoutValidation("User-Agent", options.UserAgent);
                client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue(MediaTypeNames.Application.Json));
            })
            .AddStandardResilienceHandler()
            .Configure((HttpStandardResilienceOptions resilience, IServiceProvider serviceProvider) =>
            {
                var options = serviceProvider.GetRequiredService<IOptions<YahooFinanceOptions>>().Value;
                resilience.AttemptTimeout.Timeout = options.AttemptTimeout;
                resilience.TotalRequestTimeout.Timeout = options.TotalRequestTimeout;
                resilience.Retry.MaxRetryAttempts = options.MaxRetryAttempts;
                // The circuit breaker must sample for at least twice the attempt timeout.
                var minimumSampling = options.AttemptTimeout * 2;
                if (resilience.CircuitBreaker.SamplingDuration < minimumSampling)
                {
                    resilience.CircuitBreaker.SamplingDuration = minimumSampling;
                }
            });

        services.AddHybridCache();

        return services;
    }
}
