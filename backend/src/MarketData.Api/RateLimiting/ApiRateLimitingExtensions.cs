using System.Globalization;
using System.Threading.RateLimiting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Extensions.Options;
using Microsoft.Net.Http.Headers;

namespace MarketData.Api.RateLimiting;

internal static class ApiRateLimitingExtensions
{
    public const string PerClientPolicy = "PerClient";

    /// <summary>
    /// Adds a fixed-window limit per client IP. This protects the upstream provider, which rate limits us in turn.
    /// Behind a reverse proxy, enable forwarded headers so the client IP is the caller's, not the proxy's.
    /// </summary>
    public static IServiceCollection AddApiRateLimiting(this IServiceCollection services)
    {
        services.AddOptions<RateLimitingOptions>()
            .BindConfiguration(RateLimitingOptions.SectionName)
            .ValidateDataAnnotations()
            .ValidateOnStart();

        services.AddRateLimiter(limiter =>
        {
            limiter.RejectionStatusCode = StatusCodes.Status429TooManyRequests;
            limiter.OnRejected = WriteRejectionAsync;
            limiter.AddPolicy(PerClientPolicy, httpContext =>
            {
                var options = httpContext.RequestServices.GetRequiredService<IOptions<RateLimitingOptions>>().Value;
                return RateLimitPartition.GetFixedWindowLimiter(
                    httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                    _ => new FixedWindowRateLimiterOptions
                    {
                        PermitLimit = options.PermitLimit,
                        Window = options.Window,
                        QueueLimit = 0,
                    });
            });
        });

        return services;
    }

    private static async ValueTask WriteRejectionAsync(OnRejectedContext context, CancellationToken cancellationToken)
    {
        var httpContext = context.HttpContext;
        if (context.Lease.TryGetMetadata(MetadataName.RetryAfter, out var retryAfter))
        {
            httpContext.Response.Headers[HeaderNames.RetryAfter] =
                Math.Ceiling(retryAfter.TotalSeconds).ToString(CultureInfo.InvariantCulture);
        }

        var problemDetailsService = httpContext.RequestServices.GetRequiredService<IProblemDetailsService>();
        await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails =
            {
                Type = "https://tools.ietf.org/html/rfc6585#section-4",
                Status = StatusCodes.Status429TooManyRequests,
                Title = "Too many requests",
                Detail = "Rate limit exceeded. Retry after the number of seconds in the Retry-After header.",
            },
        });
    }
}
