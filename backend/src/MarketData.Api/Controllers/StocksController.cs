using System.Net.Mime;
using MarketData.Api.Contracts;
using MarketData.Application.DailySummaries;
using MarketData.Application.Domain;
using Microsoft.AspNetCore.Mvc;

namespace MarketData.Api.Controllers;

[ApiController]
[Route("api/v1/stocks")]
public sealed class StocksController(IDailySummaryService dailySummaryService) : ControllerBase
{
    /// <summary>Gets per-day summaries of a stock's 15-minute intraday bars over the last month.</summary>
    /// <remarks>
    /// Bars are grouped by trading date in the exchange's time zone and cover regular trading hours only.
    /// The most recent day may be partial while the market is open.
    /// </remarks>
    /// <param name="symbol">Ticker symbol, e.g. <c>TSLA</c>. Case-insensitive.</param>
    /// <param name="cancellationToken">Cancelled when the client disconnects.</param>
    /// <response code="200">One entry per trading day, oldest first.</response>
    /// <response code="400">The symbol is not in a valid format.</response>
    /// <response code="404">No market data exists for the symbol.</response>
    /// <response code="429">The client has sent too many requests.</response>
    /// <response code="502">The market data provider returned an unexpected response.</response>
    /// <response code="503">The market data provider is temporarily unavailable.</response>
    [HttpGet("{symbol}/daily-summaries")]
    [ProducesResponseType<IReadOnlyList<DailySummaryResponse>>(StatusCodes.Status200OK, MediaTypeNames.Application.Json)]
    [ProducesResponseType<ValidationProblemDetails>(StatusCodes.Status400BadRequest, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status404NotFound, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status429TooManyRequests, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status502BadGateway, MediaTypeNames.Application.ProblemJson)]
    [ProducesResponseType<ProblemDetails>(StatusCodes.Status503ServiceUnavailable, MediaTypeNames.Application.ProblemJson)]
    public async Task<ActionResult<IReadOnlyList<DailySummaryResponse>>> GetDailySummaries(
        string symbol,
        CancellationToken cancellationToken)
    {
        if (!StockSymbol.TryParse(symbol, out var stockSymbol))
        {
            ModelState.AddModelError(nameof(symbol), StockSymbol.FormatDescription);
            return ValidationProblem(ModelState);
        }

        var summaries = await dailySummaryService.GetDailySummariesAsync(
            stockSymbol,
            IntradayQuery.Default,
            cancellationToken);

        return Ok(summaries.Select(DailySummaryResponse.FromDomain).ToList());
    }
}
