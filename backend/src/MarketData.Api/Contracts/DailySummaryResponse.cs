using MarketData.Api.Json;
using MarketData.Application.Domain;

namespace MarketData.Api.Contracts;

/// <summary>Intraday activity for one trading day.</summary>
/// <param name="Day">Trading date in the exchange's local time zone, formatted <c>yyyy-MM-dd</c>.</param>
/// <param name="LowAverage">Average of the day's 15-minute bar lows, with exactly 4 decimal places.</param>
/// <param name="HighAverage">Average of the day's 15-minute bar highs, with exactly 4 decimal places.</param>
/// <param name="Volume">Total shares traded across the day's 15-minute bars.</param>
public sealed record DailySummaryResponse(
    DateOnly Day,
    [property: JsonDecimalPlaces(DailySummary.PriceDecimalPlaces)] decimal LowAverage,
    [property: JsonDecimalPlaces(DailySummary.PriceDecimalPlaces)] decimal HighAverage,
    long Volume)
{
    public static DailySummaryResponse FromDomain(DailySummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);
        return new DailySummaryResponse(summary.Day, summary.LowAverage, summary.HighAverage, summary.Volume);
    }
}
