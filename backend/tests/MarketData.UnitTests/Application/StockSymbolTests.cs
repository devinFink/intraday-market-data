using MarketData.Application.Domain;

namespace MarketData.UnitTests.Application;

public sealed class StockSymbolTests
{
    [Theory]
    [InlineData("TSLA", "TSLA")]
    [InlineData("tsla", "TSLA")]
    [InlineData("  msft  ", "MSFT")]
    [InlineData("BRK-B", "BRK-B")]
    [InlineData("BRK.B", "BRK.B")]
    [InlineData("^GSPC", "^GSPC")]
    [InlineData("EURUSD=X", "EURUSD=X")]
    [InlineData("0700.HK", "0700.HK")]
    [InlineData("ABCDEFGHIJKLMNO", "ABCDEFGHIJKLMNO")]
    public void TryParse_ValidSymbol_ReturnsTrimmedUpperCaseSymbol(string input, string expected)
    {
        StockSymbol.TryParse(input, out var symbol).ShouldBeTrue();

        symbol.Value.ShouldBe(expected);
        symbol.ToString().ShouldBe(expected);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData("ABCDEFGHIJKLMNOP")]
    [InlineData("TS LA")]
    [InlineData("TSLA$")]
    [InlineData("TSLA/X")]
    [InlineData("..")]
    [InlineData("-^=")]
    [InlineData("TSLÄ")]
    public void TryParse_InvalidSymbol_ReturnsFalse(string? input)
    {
        StockSymbol.TryParse(input, out var symbol).ShouldBeFalse();

        symbol.ShouldBeNull();
    }

    [Fact]
    public void Parse_InvalidSymbol_ThrowsFormatExceptionDescribingTheFormat()
    {
        var exception = Should.Throw<FormatException>(() => StockSymbol.Parse("not a symbol", provider: null));

        exception.Message.ShouldBe(StockSymbol.FormatDescription);
    }

    [Fact]
    public void Equality_IsByNormalizedValue()
    {
        StockSymbol.Parse("tsla", provider: null).ShouldBe(StockSymbol.Parse(" TSLA ", provider: null));
    }
}
