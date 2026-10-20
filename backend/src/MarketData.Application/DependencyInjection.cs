using MarketData.Application.DailySummaries;
using Microsoft.Extensions.DependencyInjection;

namespace MarketData.Application;

public static class DependencyInjection
{
    /// <summary>
    /// Registers application services. Requires an <see cref="Abstractions.IMarketDataProvider"/> and a
    /// <see cref="Microsoft.Extensions.Caching.Hybrid.HybridCache"/> to be registered by the host.
    /// </summary>
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddOptions<DailySummaryCacheOptions>()
            .BindConfiguration(DailySummaryCacheOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddSingleton<IDailySummaryAggregator, DailySummaryAggregator>();
        services.AddScoped<IDailySummaryService, DailySummaryService>();

        return services;
    }
}
