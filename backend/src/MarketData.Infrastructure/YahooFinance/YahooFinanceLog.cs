using System.Net;
using Microsoft.Extensions.Logging;

namespace MarketData.Infrastructure.YahooFinance;

internal static partial class YahooFinanceLog
{
    [LoggerMessage(EventId = 1000, Level = LogLevel.Information,
        Message = "Fetching {Interval} intraday bars for {Symbol} over {Range} from Yahoo Finance")]
    public static partial void FetchingChart(ILogger logger, string symbol, string interval, string range);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Debug,
        Message = "Mapped {BarCount} intraday bars for {Symbol}; skipped {SkippedCount} incomplete bars")]
    public static partial void MappedBars(ILogger logger, string symbol, int barCount, int skippedCount);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information,
        Message = "Yahoo Finance has no data for {Symbol}")]
    public static partial void SymbolNotFound(ILogger logger, string symbol);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Warning,
        Message = "Yahoo Finance responded {StatusCode} for {Symbol}")]
    public static partial void UnsuccessfulStatus(ILogger logger, string symbol, HttpStatusCode statusCode);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Warning,
        Message = "Request to Yahoo Finance for {Symbol} failed")]
    public static partial void RequestFailed(ILogger logger, string symbol, Exception exception);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Error,
        Message = "Yahoo Finance returned an unreadable chart payload for {Symbol}")]
    public static partial void UnreadablePayload(ILogger logger, string symbol, Exception? exception);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Warning,
        Message = "Unknown exchange time zone '{TimeZoneId}'; falling back to fixed UTC offset {Offset}")]
    public static partial void TimeZoneFallback(ILogger logger, string? timeZoneId, TimeSpan offset);
}
