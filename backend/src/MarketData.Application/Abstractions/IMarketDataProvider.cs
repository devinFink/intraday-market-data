using MarketData.Application.Domain;
using MarketData.Application.Exceptions;

namespace MarketData.Application.Abstractions;

/// <summary>Source of intraday market data, such as Yahoo Finance.</summary>
public interface IMarketDataProvider
{
    /// <summary>Fetches intraday bars for <paramref name="symbol"/>.</summary>
    /// <exception cref="SymbolNotFoundException">The provider does not know the symbol.</exception>
    /// <exception cref="MarketDataUnavailableException">The provider is unreachable, overloaded or rate limiting us.</exception>
    /// <exception cref="InvalidMarketDataException">The provider returned a response we could not interpret.</exception>
    Task<IntradaySeries> GetIntradaySeriesAsync(
        StockSymbol symbol,
        IntradayQuery query,
        CancellationToken cancellationToken = default);
}
