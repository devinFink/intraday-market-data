using System.Reflection;
using MarketData.Api.Json;
using Microsoft.AspNetCore.OpenApi;
using Microsoft.OpenApi;

namespace MarketData.Api.OpenApi;

/// <summary>
/// Documents members serialized with <see cref="JsonDecimalPlacesAttribute"/> as numbers that are a multiple of
/// their precision (e.g. <c>multipleOf: 0.0001</c>). The schema generator cannot infer this through a custom converter.
/// </summary>
internal sealed class DecimalPlacesSchemaTransformer : IOpenApiSchemaTransformer
{
    public Task TransformAsync(
        OpenApiSchema schema,
        OpenApiSchemaTransformerContext context,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(schema);
        ArgumentNullException.ThrowIfNull(context);

        if (context.JsonPropertyInfo?.AttributeProvider is MemberInfo member
            && member.GetCustomAttribute<JsonDecimalPlacesAttribute>() is { } decimalPlaces)
        {
            schema.Type = JsonSchemaType.Number;
            schema.Format = "decimal";
            schema.MultipleOf = new decimal(1, 0, 0, isNegative: false, scale: (byte)decimalPlaces.DecimalPlaces);
        }

        return Task.CompletedTask;
    }
}
