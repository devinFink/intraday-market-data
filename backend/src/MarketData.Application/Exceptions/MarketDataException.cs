namespace MarketData.Application.Exceptions;

/// <summary>Base type for expected failures when retrieving market data.</summary>
public abstract class MarketDataException : Exception
{
    protected MarketDataException(string message, Exception? innerException = null)
        : base(message, innerException)
    {
    }
}
