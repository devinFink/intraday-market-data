using MarketData.Application.Domain;

namespace MarketData.Application.Exceptions;

/// <summary>The market data provider has no data for the requested symbol.</summary>
public sealed class SymbolNotFoundException(StockSymbol symbol)
    : MarketDataException($"No market data was found for symbol '{symbol}'.")
{
    public StockSymbol Symbol { get; } = symbol;
}
