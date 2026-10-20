using MarketData.Application.Abstractions;
using MarketData.Application.Domain;
using Microsoft.Extensions.Caching.Hybrid;
using Microsoft.Extensions.Options;

namespace MarketData.Application.DailySummaries;

/// <summary>
/// Fetches intraday bars and aggregates them, caching the result so repeated requests for the same symbol do not
/// hit the provider. Failures are not cached.
/// </summary>
public sealed class DailySummaryService(
    IMarketDataProvider marketDataProvider,
    IDailySummaryAggregator aggregator,
    HybridCache cache,
    IOptions<DailySummaryCacheOptions> cacheOptions) : IDailySummaryService
{
    private readonly HybridCacheEntryOptions _entryOptions = new()
    {
        Expiration = cacheOptions.Value.Expiration,
        LocalCacheExpiration = cacheOptions.Value.Expiration,
    };

    public async Task<IReadOnlyList<DailySummary>> GetDailySummariesAsync(
        StockSymbol symbol,
        IntradayQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(query);

        return await cache.GetOrCreateAsync(
            CacheKey(symbol, query),
            (marketDataProvider, aggregator, symbol, query),
            static async (state, token) =>
            {
                var series = await state.marketDataProvider
                    .GetIntradaySeriesAsync(state.symbol, state.query, token)
                    .ConfigureAwait(false);
                return state.aggregator.Aggregate(series);
            },
            _entryOptions,
            cancellationToken: cancellationToken).ConfigureAwait(false);
    }

    private static string CacheKey(StockSymbol symbol, IntradayQuery query) =>
        $"daily-summaries:v1:{symbol}:{query.Interval}:{query.Lookback}";
}
