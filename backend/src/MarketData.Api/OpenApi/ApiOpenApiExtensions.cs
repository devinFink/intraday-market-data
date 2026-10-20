using Microsoft.OpenApi;

namespace MarketData.Api.OpenApi;

internal static class ApiOpenApiExtensions
{
    public static IServiceCollection AddApiOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options =>
        {
            options.AddDocumentTransformer((document, _, _) =>
            {
                document.Info = new OpenApiInfo
                {
                    Title = "Market Data API",
                    Version = "v1",
                    Description = "Daily summaries of intraday stock market data.",
                };
                return Task.CompletedTask;
            });
            options.AddSchemaTransformer<DecimalPlacesSchemaTransformer>();
        });
        return services;
    }
}
