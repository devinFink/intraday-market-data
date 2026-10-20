using System.Text.Json.Serialization;

namespace MarketData.Api.Json;

/// <summary>Serializes the decorated <see cref="decimal"/> property with a fixed number of decimal places.</summary>
[AttributeUsage(AttributeTargets.Property | AttributeTargets.Field)]
internal sealed class JsonDecimalPlacesAttribute(int decimalPlaces) : JsonConverterAttribute
{
    public int DecimalPlaces { get; } = decimalPlaces;

    public override JsonConverter? CreateConverter(Type typeToConvert) =>
        typeToConvert == typeof(decimal)
            ? new FixedDecimalPlacesJsonConverter(DecimalPlaces)
            : throw new InvalidOperationException(
                $"{nameof(JsonDecimalPlacesAttribute)} can only be applied to decimal members, not {typeToConvert}.");
}
