using System.Text.Json;
using System.Text.Json.Serialization;

namespace MarketData.Api.Json;

internal static class ApiJsonExtensions
{
    /// <summary>
    /// Applies the API's JSON conventions to MVC and to the minimal-API JSON options, which the OpenAPI schema
    /// generator reads, so the published contract matches what is serialized.
    /// </summary>
    public static IMvcBuilder AddApiJsonOptions(this IMvcBuilder mvc)
    {
        mvc.Services.ConfigureHttpJsonOptions(options => Configure(options.SerializerOptions));
        return mvc.AddJsonOptions(options => Configure(options.JsonSerializerOptions));
    }

    // Numbers are numbers: the web default of also accepting numeric strings would leak into the OpenAPI contract.
    private static void Configure(JsonSerializerOptions options) => options.NumberHandling = JsonNumberHandling.Strict;
}
