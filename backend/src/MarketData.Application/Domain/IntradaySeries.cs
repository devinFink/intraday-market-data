namespace MarketData.Application.Domain;

/// <summary>Intraday price bars for a symbol, along with the time zone of the exchange it trades on.</summary>
/// <param name="Symbol">The symbol the bars belong to.</param>
/// <param name="ExchangeTimeZone">Time zone used to decide which trading day a bar belongs to.</param>
/// <param name="Bars">Complete bars in chronological order.</param>
public sealed record IntradaySeries(StockSymbol Symbol, TimeZoneInfo ExchangeTimeZone, IReadOnlyList<PriceBar> Bars);
