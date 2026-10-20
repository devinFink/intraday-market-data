using System.Net;
using System.Text.Json;
using MarketData.Application.Abstractions;
using MarketData.Application.Domain;
using MarketData.Application.Exceptions;
using MarketData.Infrastructure.YahooFinance.Contracts;
using Microsoft.Extensions.Logging;
using Polly;

namespace MarketData.Infrastructure.YahooFinance;

/// <summary>
/// <see cref="IMarketDataProvider"/> backed by Yahoo Finance's (unofficial) v8 chart API.
/// Transient failures are retried by the resilience pipeline configured on the <see cref="HttpClient"/>.
/// </summary>
internal sealed class YahooFinanceMarketDataProvider(
    HttpClient httpClient,
    YahooChartMapper mapper,
    ILogger<YahooFinanceMarketDataProvider> logger) : IMarketDataProvider
{
    private const string NotFoundErrorCode = "Not Found";

    public async Task<IntradaySeries> GetIntradaySeriesAsync(
        StockSymbol symbol,
        IntradayQuery query,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(symbol);
        ArgumentNullException.ThrowIfNull(query);

        var interval = ToYahooInterval(query.Interval);
        var range = ToYahooRange(query.Lookback);
        var requestUri = new Uri(
            $"v8/finance/chart/{Uri.EscapeDataString(symbol.Value)}?interval={interval}&range={range}&includePrePost=false",
            UriKind.Relative);

        YahooFinanceLog.FetchingChart(logger, symbol.Value, interval, range);

        using var response = await SendAsync(symbol, requestUri, cancellationToken).ConfigureAwait(false);
        EnsureUsableStatus(symbol, response);

        var payload = await ReadPayloadAsync(symbol, response, cancellationToken).ConfigureAwait(false);
        var chart = payload?.Chart;

        if (chart?.Error is { } error)
        {
            if (error.Code == NotFoundErrorCode)
            {
                YahooFinanceLog.SymbolNotFound(logger, symbol.Value);
                throw new SymbolNotFoundException(symbol);
            }

            throw Unreadable(symbol, $"Yahoo Finance reported error '{error.Code}': {error.Description}");
        }

        if (chart?.Result is not [var result, ..])
        {
            throw Unreadable(symbol, "Yahoo Finance returned a chart without results.");
        }

        return mapper.ToIntradaySeries(symbol, result);
    }

    private async Task<HttpResponseMessage> SendAsync(
        StockSymbol symbol,
        Uri requestUri,
        CancellationToken cancellationToken)
    {
        try
        {
            return await httpClient
                .GetAsync(requestUri, HttpCompletionOption.ResponseHeadersRead, cancellationToken)
                .ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex) when (ex is HttpRequestException or ExecutionRejectedException or OperationCanceledException)
        {
            // Network failure, resilience timeout, open circuit, or HttpClient timeout.
            YahooFinanceLog.RequestFailed(logger, symbol.Value, ex);
            throw new MarketDataUnavailableException("Yahoo Finance could not be reached.", ex);
        }
    }

    private void EnsureUsableStatus(StockSymbol symbol, HttpResponseMessage response)
    {
        var statusCode = response.StatusCode;
        if (statusCode == HttpStatusCode.NotFound)
        {
            YahooFinanceLog.SymbolNotFound(logger, symbol.Value);
            throw new SymbolNotFoundException(symbol);
        }

        if (response.IsSuccessStatusCode)
        {
            return;
        }

        YahooFinanceLog.UnsuccessfulStatus(logger, symbol.Value, statusCode);
        if (statusCode is HttpStatusCode.TooManyRequests or HttpStatusCode.RequestTimeout || (int)statusCode >= 500)
        {
            throw new MarketDataUnavailableException($"Yahoo Finance responded with HTTP {(int)statusCode}.");
        }

        throw new InvalidMarketDataException($"Yahoo Finance responded with unexpected HTTP {(int)statusCode}.");
    }

    private async Task<YahooChartResponse?> ReadPayloadAsync(
        StockSymbol symbol,
        HttpResponseMessage response,
        CancellationToken cancellationToken)
    {
        try
        {
            var stream = await response.Content.ReadAsStreamAsync(cancellationToken).ConfigureAwait(false);
            await using (stream.ConfigureAwait(false))
            {
                return await JsonSerializer
                    .DeserializeAsync(stream, YahooJsonContext.Default.YahooChartResponse, cancellationToken)
                    .ConfigureAwait(false);
            }
        }
        catch (JsonException ex)
        {
            throw Unreadable(symbol, "Yahoo Finance returned malformed JSON.", ex);
        }
    }

    private InvalidMarketDataException Unreadable(StockSymbol symbol, string message, Exception? innerException = null)
    {
        YahooFinanceLog.UnreadablePayload(logger, symbol.Value, innerException);
        return new InvalidMarketDataException(message, innerException);
    }

    private static string ToYahooInterval(BarInterval interval) => interval switch
    {
        BarInterval.FifteenMinutes => "15m",
        _ => throw new ArgumentOutOfRangeException(nameof(interval), interval, "Unsupported bar interval."),
    };

    private static string ToYahooRange(LookbackPeriod lookback) => lookback switch
    {
        LookbackPeriod.OneMonth => "1mo",
        _ => throw new ArgumentOutOfRangeException(nameof(lookback), lookback, "Unsupported lookback period."),
    };
}
