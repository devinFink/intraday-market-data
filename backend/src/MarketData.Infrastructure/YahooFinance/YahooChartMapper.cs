using System.Globalization;
using MarketData.Application.Domain;
using MarketData.Application.Exceptions;
using MarketData.Infrastructure.YahooFinance.Contracts;
using Microsoft.Extensions.Logging;

namespace MarketData.Infrastructure.YahooFinance;

/// <summary>Turns a Yahoo chart result into an <see cref="IntradaySeries"/>.</summary>
internal sealed class YahooChartMapper(ILogger<YahooChartMapper> logger)
{
    public IntradaySeries ToIntradaySeries(StockSymbol symbol, YahooChartResult result)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(result);

        var exchangeTimeZone = ResolveExchangeTimeZone(result.Meta);
        var timestamps = result.Timestamp ?? [];

        // A valid symbol with no trading activity in range comes back without timestamps or quotes.
        if (timestamps.Count == 0)
        {
            return new IntradaySeries(symbol, exchangeTimeZone, []);
        }

        var quote = result.Indicators?.Quote is [var first, ..] ? first : null;
        var lows = quote?.Low;
        var highs = quote?.High;
        var volumes = quote?.Volume;

        if (lows?.Count != timestamps.Count
            || highs?.Count != timestamps.Count
            || (volumes is not null && volumes.Count != timestamps.Count))
        {
            throw new InvalidMarketDataException(
                $"Yahoo Finance returned misaligned quote arrays for '{symbol}'.");
        }

        var bars = new List<PriceBar>(timestamps.Count);
        for (var i = 0; i < timestamps.Count; i++)
        {
            // Yahoo emits nulls for intervals with no trades; such bars carry no price information.
            if (lows[i] is not { } low || highs[i] is not { } high)
            {
                continue;
            }

            bars.Add(new PriceBar(DateTimeOffset.FromUnixTimeSeconds(timestamps[i]), low, high, volumes?[i] ?? 0));
        }

        YahooFinanceLog.MappedBars(logger, symbol.Value, bars.Count, timestamps.Count - bars.Count);
        return new IntradaySeries(symbol, exchangeTimeZone, bars);
    }

    private TimeZoneInfo ResolveExchangeTimeZone(YahooChartMeta? meta)
    {
        var timeZoneId = meta?.ExchangeTimezoneName;
        if (!string.IsNullOrWhiteSpace(timeZoneId)
            && TimeZoneInfo.TryFindSystemTimeZoneById(timeZoneId, out var timeZone))
        {
            return timeZone;
        }

        // Without the named zone we can still group correctly within a month that has no DST change.
        var offset = TimeSpan.FromSeconds(meta?.GmtOffset ?? 0);
        YahooFinanceLog.TimeZoneFallback(logger, timeZoneId, offset);

        var sign = offset < TimeSpan.Zero ? '-' : '+';
        var fallbackId = string.Create(CultureInfo.InvariantCulture, $"UTC{sign}{offset.Duration():hh\\:mm}");
        return TimeZoneInfo.CreateCustomTimeZone(fallbackId, offset, fallbackId, fallbackId);
    }
}
