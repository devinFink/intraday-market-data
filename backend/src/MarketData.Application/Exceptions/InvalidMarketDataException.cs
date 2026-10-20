namespace MarketData.Application.Exceptions;

/// <summary>The market data provider returned a response that could not be interpreted.</summary>
public sealed class InvalidMarketDataException(string message, Exception? innerException = null)
    : MarketDataException(message, innerException);
