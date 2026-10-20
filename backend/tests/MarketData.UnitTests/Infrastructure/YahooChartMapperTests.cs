using System.Text.Json;
using MarketData.Application.Domain;
using MarketData.Application.Exceptions;
using MarketData.Infrastructure.YahooFinance;
using MarketData.Infrastructure.YahooFinance.Contracts;
using Microsoft.Extensions.Logging.Abstractions;

namespace MarketData.UnitTests.Infrastructure;

public sealed class YahooChartMapperTests
{
    private static readonly StockSymbol Symbol = StockSymbol.Parse("TSLA", provider: null);

    private readonly YahooChartMapper _mapper = new(NullLogger<YahooChartMapper>.Instance);

    [Fact]
    public void ToIntradaySeries_MapsBarsWithExactPricesAndExchangeTimeZone()
    {
        var result = ParseResult("""
            {
              "meta": { "symbol": "TSLA", "exchangeTimezoneName": "America/New_York", "gmtoffset": -14400 },
              "timestamp": [1792503000, 1792503900],
              "indicators": { "quote": [ {
                "low":    [371.1099853515625, 374.6300048828125],
                "high":   [376.510009765625,  378.1499938964844],
                "volume": [5917439, 3622878]
              } ] }
            }
            """);

        var series = _mapper.ToIntradaySeries(Symbol, result);

        series.Symbol.ShouldBe(Symbol);
        series.ExchangeTimeZone.Id.ShouldBe(TimeZoneInfo.FindSystemTimeZoneById("America/New_York").Id);
        series.Bars.ShouldBe(
        [
            new PriceBar(DateTimeOffset.FromUnixTimeSeconds(1792503000), 371.1099853515625m, 376.510009765625m, 5917439),
            new PriceBar(DateTimeOffset.FromUnixTimeSeconds(1792503900), 374.6300048828125m, 378.1499938964844m, 3622878),
        ]);
    }

    [Fact]
    public void ToIntradaySeries_ReadsPricesInExponentNotation()
    {
        var result = ParseResult("""
            {
              "timestamp": [1],
              "indicators": { "quote": [ { "low": [1.5E-4], "high": [2e-4], "volume": [1] } ] }
            }
            """);

        var bar = _mapper.ToIntradaySeries(Symbol, result).Bars.ShouldHaveSingleItem();

        bar.Low.ShouldBe(0.00015m);
        bar.High.ShouldBe(0.0002m);
    }

    [Fact]
    public void ToIntradaySeries_SkipsBarsWithoutPricesAndTreatsMissingVolumeAsZero()
    {
        var result = ParseResult("""
            {
              "meta": { "exchangeTimezoneName": "America/New_York" },
              "timestamp": [1, 2, 3, 4],
              "indicators": { "quote": [ {
                "low":    [1.5,  null, 3.5,  4.5],
                "high":   [2.5,  2.5,  null, 5.5],
                "volume": [100,  200,  300,  null]
              } ] }
            }
            """);

        var series = _mapper.ToIntradaySeries(Symbol, result);

        series.Bars.Select(bar => (bar.Timestamp.ToUnixTimeSeconds(), bar.Volume)).ShouldBe([(1L, 100L), (4L, 0L)]);
    }

    [Fact]
    public void ToIntradaySeries_UnknownTimeZoneName_FallsBackToFixedGmtOffset()
    {
        var result = ParseResult("""
            { "meta": { "exchangeTimezoneName": "Mars/Olympus_Mons", "gmtoffset": -14400 }, "timestamp": [] }
            """);

        var series = _mapper.ToIntradaySeries(Symbol, result);

        series.ExchangeTimeZone.BaseUtcOffset.ShouldBe(TimeSpan.FromHours(-4));
        series.ExchangeTimeZone.Id.ShouldBe("UTC-04:00");
    }

    [Fact]
    public void ToIntradaySeries_NoTimestamps_ReturnsEmptySeries()
    {
        var result = ParseResult("""{ "meta": { "exchangeTimezoneName": "America/New_York" } }""");

        _mapper.ToIntradaySeries(Symbol, result).Bars.ShouldBeEmpty();
    }

    [Theory]
    [InlineData("""{ "timestamp": [1, 2], "indicators": { "quote": [ { "low": [1.0], "high": [1.0, 2.0] } ] } }""")]
    [InlineData("""{ "timestamp": [1, 2], "indicators": { "quote": [ { "low": [1.0, 2.0], "high": [1.0, 2.0], "volume": [1] } ] } }""")]
    [InlineData("""{ "timestamp": [1, 2], "indicators": { "quote": [] } }""")]
    [InlineData("""{ "timestamp": [1, 2] }""")]
    public void ToIntradaySeries_MisalignedOrMissingQuotes_ThrowsInvalidMarketData(string json)
    {
        var result = ParseResult(json);

        Should.Throw<InvalidMarketDataException>(() => _mapper.ToIntradaySeries(Symbol, result));
    }

    private static YahooChartResult ParseResult(string resultJson)
    {
        var response = JsonSerializer.Deserialize(
            $$"""{ "chart": { "result": [ {{resultJson}} ], "error": null } }""",
            YahooJsonContext.Default.YahooChartResponse);
        return response!.Chart!.Result![0];
    }
}
