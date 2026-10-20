using MarketData.Application.Domain;

namespace MarketData.Application.DailySummaries;

/// <summary>Rolls intraday bars up into one summary per trading day.</summary>
public interface IDailySummaryAggregator
{
    /// <returns>One summary per trading day that has at least one bar, ordered by day ascending.</returns>
    IReadOnlyList<DailySummary> Aggregate(IntradaySeries series);
}
