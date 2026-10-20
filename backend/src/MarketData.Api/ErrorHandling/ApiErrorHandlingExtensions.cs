namespace MarketData.Api.ErrorHandling;

internal static class ApiErrorHandlingExtensions
{
    /// <summary>Returns RFC 9457 problem details for errors, including unhandled exceptions and empty error responses.</summary>
    public static IServiceCollection AddApiErrorHandling(this IServiceCollection services)
    {
        services.AddProblemDetails(options => options.CustomizeProblemDetails = context =>
            context.ProblemDetails.Instance ??= context.HttpContext.Request.Path);
        services.AddExceptionHandler<ProblemDetailsExceptionHandler>();
        return services;
    }
}
