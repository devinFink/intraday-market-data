using MarketData.Application.DailySummaries;
using MarketData.Application.Domain;

namespace MarketData.UnitTests.Application;

public sealed class DailySummaryAggregatorTests
{
    private static readonly TimeZoneInfo NewYork = TimeZoneInfo.FindSystemTimeZoneById("America/New_York");
    private static readonly StockSymbol Symbol = StockSymbol.Parse("TSLA", provider: null);

    private readonly DailySummaryAggregator _aggregator = new();

    [Fact]
    public void Aggregate_AveragesLowsAndHighsAndTotalsVolumePerDay()
    {
        var series = Series(
            Bar("2026-10-19T13:30:00Z", low: 10m, high: 12m, volume: 100),
            Bar("2026-10-19T13:45:00Z", low: 11m, high: 13m, volume: 200),
            Bar("2026-10-19T14:00:00Z", low: 12m, high: 17m, volume: 300),
            Bar("2026-10-20T13:30:00Z", low: 20m, high: 21m, volume: 50));

        var summaries = _aggregator.Aggregate(series);

        summaries.ShouldBe(
        [
            new DailySummary(new DateOnly(2026, 10, 19), LowAverage: 11m, HighAverage: 14m, Volume: 600),
            new DailySummary(new DateOnly(2026, 10, 20), LowAverage: 20m, HighAverage: 21m, Volume: 50),
        ]);
    }

    [Fact]
    public void Aggregate_RoundsAveragesToFourDecimalPlaces()
    {
        // 4/3 = 1.33333..., 7/3 = 2.33333...
        var series = Series(
            Bar("2026-10-19T13:30:00Z", low: 1m, high: 2m),
            Bar("2026-10-19T13:45:00Z", low: 1m, high: 2m),
            Bar("2026-10-19T14:00:00Z", low: 2m, high: 3m));

        var summary = _aggregator.Aggregate(series).ShouldHaveSingleItem();

        summary.LowAverage.ShouldBe(1.3333m);
        summary.HighAverage.ShouldBe(2.3333m);
    }

    [Fact]
    public void Aggregate_RoundsMidpointsAwayFromZero()
    {
        // Banker's rounding would give 1.0000 and 2.0002.
        var series = Series(Bar("2026-10-19T13:30:00Z", low: 1.00005m, high: 2.00025m));

        var summary = _aggregator.Aggregate(series).ShouldHaveSingleItem();

        summary.LowAverage.ShouldBe(1.0001m);
        summary.HighAverage.ShouldBe(2.0003m);
    }

    [Fact]
    public void Aggregate_GroupsByExchangeLocalDateRatherThanUtcDate()
    {
        // 01:00 UTC on Oct 20 is 21:00 on Oct 19 in New York.
        var series = Series(
            Bar("2026-10-20T01:00:00Z", low: 1m, high: 1m),
            Bar("2026-10-20T13:30:00Z", low: 2m, high: 2m));

        var days = _aggregator.Aggregate(series).Select(summary => summary.Day);

        days.ShouldBe([new DateOnly(2026, 10, 19), new DateOnly(2026, 10, 20)]);
    }

    [Fact]
    public void Aggregate_UsesTheOffsetInEffectOnEachDateAcrossDaylightSavingChanges()
    {
        // US daylight saving ends on 2026-11-01. 04:30 UTC on Nov 2 is 23:30 EST on Nov 1,
        // but a fixed EDT (UTC-4) offset would wrongly place it on Nov 2.
        var series = Series(
            Bar("2026-10-30T19:45:00Z", low: 1m, high: 1m),
            Bar("2026-11-02T04:30:00Z", low: 2m, high: 2m),
            Bar("2026-11-02T14:30:00Z", low: 3m, high: 3m));

        var days = _aggregator.Aggregate(series).Select(summary => summary.Day);

        days.ShouldBe([new DateOnly(2026, 10, 30), new DateOnly(2026, 11, 1), new DateOnly(2026, 11, 2)]);
    }

    [Fact]
    public void Aggregate_OrdersDaysAscendingRegardlessOfBarOrder()
    {
        var series = Series(
            Bar("2026-10-21T13:30:00Z", low: 3m, high: 3m),
            Bar("2026-10-19T13:30:00Z", low: 1m, high: 1m),
            Bar("2026-10-20T13:30:00Z", low: 2m, high: 2m));

        var days = _aggregator.Aggregate(series).Select(summary => summary.Day);

        days.ShouldBe([new DateOnly(2026, 10, 19), new DateOnly(2026, 10, 20), new DateOnly(2026, 10, 21)]);
    }

    [Fact]
    public void Aggregate_NoBars_ReturnsNoDays()
    {
        _aggregator.Aggregate(Series()).ShouldBeEmpty();
    }

    private static IntradaySeries Series(params PriceBar[] bars) => new(Symbol, NewYork, bars);

    private static PriceBar Bar(string timestamp, decimal low, decimal high, long volume = 0) =>
        new(DateTimeOffset.Parse(timestamp, System.Globalization.CultureInfo.InvariantCulture), low, high, volume);
}
