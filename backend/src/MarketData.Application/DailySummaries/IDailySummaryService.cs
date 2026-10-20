using MarketData.Application.Domain;
using MarketData.Application.Exceptions;

namespace MarketData.Application.DailySummaries;

/// <summary>Produces daily summaries of intraday market data.</summary>
public interface IDailySummaryService
{
    /// <returns>One summary per trading day, ordered by day ascending. Empty if the symbol has no bars in range.</returns>
    /// <exception cref="SymbolNotFoundException">The symbol is unknown.</exception>
    /// <exception cref="MarketDataUnavailableException">Market data is temporarily unavailable.</exception>
    /// <exception cref="InvalidMarketDataException">The market data provider returned an unusable response.</exception>
    Task<IReadOnlyList<DailySummary>> GetDailySummariesAsync(
        StockSymbol symbol,
        IntradayQuery query,
        CancellationToken cancellationToken = default);
}
