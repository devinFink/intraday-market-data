using System.Globalization;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarketData.Api.Json;

/// <summary>
/// Writes a <see cref="decimal"/> as a JSON number with exactly <see cref="DecimalPlaces"/> fractional digits,
/// so <c>40.29m</c> becomes <c>40.2900</c>. Reading accepts any JSON number.
/// </summary>
internal sealed class FixedDecimalPlacesJsonConverter : JsonConverter<decimal>
{
    private const int MaxDecimalPlaces = 28;
    private readonly string _format;

    public FixedDecimalPlacesJsonConverter(int decimalPlaces)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(decimalPlaces);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(decimalPlaces, MaxDecimalPlaces);

        DecimalPlaces = decimalPlaces;
        _format = "F" + decimalPlaces.ToString(CultureInfo.InvariantCulture);
    }

    public int DecimalPlaces { get; }

    public override decimal Read(ref Utf8JsonReader reader, Type typeToConvert, JsonSerializerOptions options) =>
        reader.GetDecimal();

    public override void Write(Utf8JsonWriter writer, decimal value, JsonSerializerOptions options)
    {
        ArgumentNullException.ThrowIfNull(writer);
        writer.WriteRawValue(value.ToString(_format, CultureInfo.InvariantCulture), skipInputValidation: true);
    }
}
