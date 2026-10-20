namespace MarketData.Application.Domain;

/// <summary>A single complete intraday price bar (candle).</summary>
/// <param name="Timestamp">Start of the bar's interval.</param>
/// <param name="Low">Lowest traded price during the interval.</param>
/// <param name="High">Highest traded price during the interval.</param>
/// <param name="Volume">Shares traded during the interval.</param>
public readonly record struct PriceBar(DateTimeOffset Timestamp, decimal Low, decimal High, long Volume);
