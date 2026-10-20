using System.Text.Json.Serialization;

namespace MarketData.Infrastructure.YahooFinance.Contracts;

// Shape of https://query1.finance.yahoo.com/v8/finance/chart/{symbol}, limited to the fields we use.
// Everything is nullable because the API is undocumented and omits fields freely.
// Prices are read straight from the JSON text as decimal, so no binary floating-point rounding is introduced.

internal sealed record YahooChartResponse(YahooChart? Chart);

internal sealed record YahooChart(IReadOnlyList<YahooChartResult>? Result, YahooChartError? Error);

internal sealed record YahooChartError(string? Code, string? Description);

internal sealed record YahooChartResult(
    YahooChartMeta? Meta,
    IReadOnlyList<long>? Timestamp,
    YahooIndicators? Indicators);

internal sealed record YahooChartMeta(
    string? Symbol,
    string? ExchangeTimezoneName,
    [property: JsonPropertyName("gmtoffset")] int? GmtOffset);

internal sealed record YahooIndicators(IReadOnlyList<YahooQuote>? Quote);

internal sealed record YahooQuote(
    IReadOnlyList<decimal?>? Low,
    IReadOnlyList<decimal?>? High,
    IReadOnlyList<long?>? Volume);

[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(YahooChartResponse))]
internal sealed partial class YahooJsonContext : JsonSerializerContext;
