using Microsoft.AspNetCore.Cors.Infrastructure;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace MarketData.Api.Cors;

internal static class ApiCorsExtensions
{
    public const string FrontendPolicy = "Frontend";

    /// <summary>Adds a read-only CORS policy for the origins listed under <c>Cors:AllowedOrigins</c>.</summary>
    public static IServiceCollection AddApiCors(this IServiceCollection services)
    {
        services.AddOptions<ApiCorsOptions>().BindConfiguration(ApiCorsOptions.SectionName);
        services.AddCors();
        services.AddOptions<CorsOptions>()
            .Configure<IOptions<ApiCorsOptions>>((cors, apiCors) => cors.AddPolicy(FrontendPolicy, policy => policy
                .WithOrigins([.. apiCors.Value.AllowedOrigins])
                .WithMethods(HttpMethods.Get)
                .AllowAnyHeader()
                .WithExposedHeaders(HeaderNames.RetryAfter)));
        return services;
    }
}
