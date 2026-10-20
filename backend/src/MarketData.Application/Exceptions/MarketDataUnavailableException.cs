namespace MarketData.Application.Exceptions;

/// <summary>The market data provider could not be reached or refused the request; retrying later may succeed.</summary>
public sealed class MarketDataUnavailableException(string message, Exception? innerException = null)
    : MarketDataException(message, innerException);
