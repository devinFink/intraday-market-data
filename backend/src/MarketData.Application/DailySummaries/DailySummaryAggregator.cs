using MarketData.Application.Domain;

namespace MarketData.Application.DailySummaries;

/// <summary>
/// Groups bars by the exchange-local date they start on, then averages their lows and highs and totals their volume.
/// All arithmetic is done in <see cref="decimal"/>; averages are rounded half away from zero to
/// <see cref="DailySummary.PriceDecimalPlaces"/> decimal places.
/// </summary>
public sealed class DailySummaryAggregator : IDailySummaryAggregator
{
    public IReadOnlyList<DailySummary> Aggregate(IntradaySeries series)
    {
        ArgumentNullException.ThrowIfNull(series);

        return series.Bars
            .GroupBy(bar => ToExchangeDate(bar.Timestamp, series.ExchangeTimeZone))
            .OrderBy(day => day.Key)
            .Select(day => Summarize(day.Key, day))
            .ToList();
    }

    private static DateOnly ToExchangeDate(DateTimeOffset timestamp, TimeZoneInfo exchangeTimeZone) =>
        DateOnly.FromDateTime(TimeZoneInfo.ConvertTime(timestamp, exchangeTimeZone).DateTime);

    private static DailySummary Summarize(DateOnly day, IEnumerable<PriceBar> bars)
    {
        var count = 0;
        var lowTotal = 0m;
        var highTotal = 0m;
        var volumeTotal = 0L;

        foreach (var bar in bars)
        {
            count++;
            lowTotal += bar.Low;
            highTotal += bar.High;
            volumeTotal = checked(volumeTotal + bar.Volume);
        }

        return new DailySummary(day, RoundPrice(lowTotal / count), RoundPrice(highTotal / count), volumeTotal);
    }

    private static decimal RoundPrice(decimal value) =>
        decimal.Round(value, DailySummary.PriceDecimalPlaces, MidpointRounding.AwayFromZero);
}
