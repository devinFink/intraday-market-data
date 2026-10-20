using System.Globalization;
using MarketData.Application.Exceptions;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Net.Http.Headers;

namespace MarketData.Api.ErrorHandling;

/// <summary>
/// Converts exceptions that escape the request pipeline into RFC 9457 problem details.
/// Expected market data failures map to specific status codes; anything else is a 500 that reveals no internals.
/// </summary>
internal sealed partial class ProblemDetailsExceptionHandler(
    IProblemDetailsService problemDetailsService,
    ILogger<ProblemDetailsExceptionHandler> logger) : IExceptionHandler
{
    private static readonly TimeSpan UnavailableRetryAfter = TimeSpan.FromSeconds(30);

    public async ValueTask<bool> TryHandleAsync(
        HttpContext httpContext,
        Exception exception,
        CancellationToken cancellationToken)
    {
        if (exception is OperationCanceledException && httpContext.RequestAborted.IsCancellationRequested)
        {
            LogRequestAborted(logger, httpContext.Request.Path);
            httpContext.Response.StatusCode = StatusCodes.Status499ClientClosedRequest;
            return true;
        }

        var problem = ToProblemDetails(exception);
        if (exception is MarketDataException)
        {
            LogMarketDataFailure(logger, problem.Status!.Value, exception.Message);
        }
        else
        {
            LogUnhandledException(logger, exception);
        }

        if (problem.Status == StatusCodes.Status503ServiceUnavailable)
        {
            httpContext.Response.Headers[HeaderNames.RetryAfter] =
                UnavailableRetryAfter.TotalSeconds.ToString(CultureInfo.InvariantCulture);
        }

        httpContext.Response.StatusCode = problem.Status!.Value;
        return await problemDetailsService.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            ProblemDetails = problem,
        });
    }

    private static ProblemDetails ToProblemDetails(Exception exception) => exception switch
    {
        SymbolNotFoundException notFound => new ProblemDetails
        {
            Status = StatusCodes.Status404NotFound,
            Title = "Symbol not found",
            Detail = $"No market data was found for symbol '{notFound.Symbol}'.",
        },
        MarketDataUnavailableException => new ProblemDetails
        {
            Status = StatusCodes.Status503ServiceUnavailable,
            Title = "Market data unavailable",
            Detail = "The market data provider is temporarily unavailable. Please try again shortly.",
        },
        InvalidMarketDataException => new ProblemDetails
        {
            Status = StatusCodes.Status502BadGateway,
            Title = "Invalid market data",
            Detail = "The market data provider returned an unexpected response.",
        },
        _ => new ProblemDetails
        {
            Status = StatusCodes.Status500InternalServerError,
            Title = "An unexpected error occurred",
        },
    };

    [LoggerMessage(EventId = 2000, Level = LogLevel.Debug, Message = "Client aborted request to {Path}")]
    private static partial void LogRequestAborted(ILogger logger, PathString path);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Information, Message = "Market data request failed with {StatusCode}: {Reason}")]
    private static partial void LogMarketDataFailure(ILogger logger, int statusCode, string reason);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Error, Message = "Unhandled exception")]
    private static partial void LogUnhandledException(ILogger logger, Exception exception);
}
