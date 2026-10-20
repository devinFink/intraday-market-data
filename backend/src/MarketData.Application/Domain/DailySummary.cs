namespace MarketData.Application.Domain;

/// <summary>Intraday activity for one trading day.</summary>
/// <param name="Day">Trading date in the exchange's local time zone.</param>
/// <param name="LowAverage">Mean of the day's intraday bar lows, rounded to <see cref="PriceDecimalPlaces"/>.</param>
/// <param name="HighAverage">Mean of the day's intraday bar highs, rounded to <see cref="PriceDecimalPlaces"/>.</param>
/// <param name="Volume">Total shares traded across the day's intraday bars.</param>
public sealed record DailySummary(DateOnly Day, decimal LowAverage, decimal HighAverage, long Volume)
{
    /// <summary>Number of decimal places averaged prices are rounded to.</summary>
    public const int PriceDecimalPlaces = 4;
}
