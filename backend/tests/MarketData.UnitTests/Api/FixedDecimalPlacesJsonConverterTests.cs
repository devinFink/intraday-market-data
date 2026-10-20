using System.Text.Json;
using MarketData.Api.Contracts;
using MarketData.Api.Json;

namespace MarketData.UnitTests.Api;

public sealed class FixedDecimalPlacesJsonConverterTests
{
    private static readonly JsonSerializerOptions Options = new()
    {
        Converters = { new FixedDecimalPlacesJsonConverter(decimalPlaces: 4) },
    };

    [Theory]
    [InlineData("40.2958", "40.2958")]
    [InlineData("40.29", "40.2900")]
    [InlineData("40", "40.0000")]
    [InlineData("0", "0.0000")]
    [InlineData("-1.5", "-1.5000")]
    [InlineData("1234567.1", "1234567.1000")]
    public void Write_PadsToExactlyFourDecimalPlaces(string value, string expectedJson)
    {
        var json = JsonSerializer.Serialize(decimal.Parse(value, System.Globalization.CultureInfo.InvariantCulture), Options);

        json.ShouldBe(expectedJson);
    }

    [Fact]
    public void Read_AcceptsAnyJsonNumber()
    {
        JsonSerializer.Deserialize<decimal>("40.29", Options).ShouldBe(40.29m);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(29)]
    public void Constructor_OutOfRangeDecimalPlaces_Throws(int decimalPlaces)
    {
        Should.Throw<ArgumentOutOfRangeException>(() => new FixedDecimalPlacesJsonConverter(decimalPlaces));
    }

    [Fact]
    public void DailySummaryResponse_SerializesInTheRequiredFormatAndPrecision()
    {
        var response = new DailySummaryResponse(new DateOnly(2009, 1, 30), 40.2958m, 49.75m, 49073348);

        var json = JsonSerializer.Serialize(response, JsonSerializerOptions.Web);

        json.ShouldBe("""{"day":"2009-01-30","lowAverage":40.2958,"highAverage":49.7500,"volume":49073348}""");
    }
}
