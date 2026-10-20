# Intraday Market Data

A full-stack application that shows a month of intraday stock activity as one row per trading day. A self-hosted
.NET 10 API fetches 15-minute bars from Yahoo Finance and summarizes them. A React UI (in progress) lets users look up a
symbol and view the results.

| Part | Status |
|---|---|
| Backend API (`backend/`) | Done |
| Frontend (`frontend/`) | Planned |

## Contents

- [Prerequisites](#prerequisites)
- [Run the backend](#run-the-backend)
- [API](#api)
- [How the numbers are calculated](#how-the-numbers-are-calculated)
- [Configuration](#configuration)
- [Tests](#tests)
- [Docker](#docker)
- [Architecture](#architecture)
- [Production notes](#production-notes)

## Prerequisites

| Tool | Version | Needed for |
|---|---|---|
| [.NET SDK](https://dotnet.microsoft.com/download/dotnet/10.0) | 10.0.100 or later | Backend |
| [Docker](https://docs.docker.com/get-docker/) | Any recent version | Optional: running in a container |

## Run the backend

```bash
cd backend
dotnet run --project src/MarketData.Api --launch-profile http
```

The API listens on `http://localhost:5080`. Try it:

```bash
curl http://localhost:5080/api/v1/stocks/TSLA/daily-summaries
```

In Development, interactive API docs are at <http://localhost:5080/scalar> and the OpenAPI document is at
<http://localhost:5080/openapi/v1.json>. [`MarketData.Api.http`](backend/src/MarketData.Api/MarketData.Api.http) has
ready-made requests for Visual Studio, Rider and VS Code.

## API

### `GET /api/v1/stocks/{symbol}/daily-summaries`

Returns one entry per trading day for the last month of 15-minute bars, oldest first.

`symbol` is a ticker such as `TSLA`, `BRK-B`, `^GSPC` (URL-encoded as `%5EGSPC`) or `EURUSD=X`. It is case-insensitive
and may be 1–15 characters: letters, digits, `.`, `-`, `^` or `=`.

```json
[
  { "day": "2026-09-21", "lowAverage": 373.9714, "highAverage": 375.6923, "volume": 50101814 },
  { "day": "2026-09-22", "lowAverage": 377.0731, "highAverage": 378.6310, "volume": 25661676 }
]
```

| Field | Type | Meaning | Precision |
|---|---|---|---|
| `day` | string (`yyyy-MM-dd`) | Trading date in the exchange's time zone | n/a |
| `lowAverage` | number | Average of the day's 15-minute bar lows | Always exactly 4 decimal places |
| `highAverage` | number | Average of the day's 15-minute bar highs | Always exactly 4 decimal places |
| `volume` | integer | Total shares traded across the day's bars | Whole number |

### Errors

Errors use [RFC 9457 problem details](https://www.rfc-editor.org/rfc/rfc9457) (`application/problem+json`) and include
a `traceId` that matches the server logs.

| Status | When |
|---|---|
| `400 Bad Request` | The symbol format is invalid. `errors.symbol` explains why. |
| `404 Not Found` | Yahoo Finance has no data for the symbol. |
| `429 Too Many Requests` | The client exceeded the rate limit. `Retry-After` gives the seconds to wait. |
| `502 Bad Gateway` | Yahoo Finance returned a response the API could not interpret. |
| `503 Service Unavailable` | Yahoo Finance is unreachable, failing or rate limiting the API. `Retry-After` is set. |

A valid symbol with no trades in the period returns `200` with an empty array.

### Health

| Endpoint | Purpose |
|---|---|
| `GET /health/live` | Liveness: the process is serving requests. |
| `GET /health/ready` | Readiness: the app has started and its configuration is valid. |

## How the numbers are calculated

1. **Fetch.** The API calls `v8/finance/chart/{symbol}?interval=15m&range=1mo&includePrePost=false` with a
   browser-like `User-Agent`. The URL in the brief has no `range`, so Yahoo returns only one day. Without the
   `User-Agent`, Yahoo responds with HTTP 429.
2. **Clean.** Bars with a null low or high carry no price information and are skipped. A null volume counts as 0.
3. **Group by day.** Yahoo timestamps are UTC. Each bar is assigned to the date it falls on in the **exchange's own time
   zone** (`meta.exchangeTimezoneName`, e.g. `America/New_York`), so the result is correct for any exchange and across
   daylight-saving changes.
4. **Aggregate.** `lowAverage` and `highAverage` are the averages of the bars' lows and highs. `volume` is the sum of
   the bars' volumes.
5. **Precision.**
   - Prices are read from Yahoo's JSON text **directly as `decimal`**, so binary floating point never touches them.
   - Averages are rounded **half away from zero** to 4 decimal places.
   - Prices are always written with 4 digits after the point (`49.7500`, not `49.75`).
6. **Scope.** Only regular trading hours are included. While the market is open, the latest day is partial.

A recorded TSLA response and its expected output, computed separately with exact decimal arithmetic, are kept as
[test fixtures](backend/tests/MarketData.IntegrationTests/Fixtures). An integration test checks that the API's output
matches the expected output byte for byte.

## Configuration

Settings are in [`appsettings.json`](backend/src/MarketData.Api/appsettings.json). They can be overridden with
environment variables using `__` as the separator, e.g. `YahooFinance__MaxRetryAttempts=5`. The app validates all
settings at startup and fails fast if any are invalid.

| Key | Default | Description |
|---|---|---|
| `YahooFinance:BaseAddress` | `https://query1.finance.yahoo.com/` | Yahoo Finance API root |
| `YahooFinance:UserAgent` | Browser UA string | Sent with every Yahoo request |
| `YahooFinance:AttemptTimeout` | `00:00:10` | Timeout per HTTP attempt |
| `YahooFinance:TotalRequestTimeout` | `00:00:30` | Timeout across all retries |
| `YahooFinance:MaxRetryAttempts` | `3` | Retries for transient failures (408, 429, 5xx, network) |
| `DailySummaryCache:Expiration` | `00:05:00` | How long results are reused per symbol |
| `Cors:AllowedOrigins` | `[]` (`http://localhost:5173` in Development) | Browser origins allowed to call the API |
| `RateLimiting:PermitLimit` | `60` | Requests per client IP per window |
| `RateLimiting:Window` | `00:01:00` | Rate limit window |

## Tests

```bash
cd backend
dotnet test --solution MarketData.slnx --filter-not-trait "Category=External"
```

| Project | What it covers |
|---|---|
| `MarketData.UnitTests` | <ul><li>Aggregation: averages, totals, rounding, exchange-local grouping, daylight saving</li><li>Symbol validation</li><li>Fixed-precision JSON output</li><li>Yahoo response mapping</li></ul> |
| `MarketData.IntegrationTests` | The real HTTP pipeline in memory, with Yahoo stubbed:<ul><li>exact response body</li><li>the outbound Yahoo request</li><li>caching</li><li>400 and 404 problem details</li></ul> |

Tests tagged `Category=External` call the live Yahoo Finance API. They are excluded above and in CI. To run them:

```bash
dotnet test --solution MarketData.slnx --filter-trait "Category=External"
```

To collect coverage (Cobertura reports are written to `backend/TestResults/`):

```bash
dotnet test --solution MarketData.slnx --filter-not-trait "Category=External" --coverage --coverage-output-format cobertura
```

The tests use xUnit v3. `dotnet test` runs them on
[Microsoft.Testing.Platform](https://learn.microsoft.com/dotnet/core/testing/microsoft-testing-platform-intro), selected
in [`global.json`](backend/global.json). The test projects also reference the VSTest adapter (`Microsoft.NET.Test.Sdk`
and `xunit.runner.visualstudio`), so Visual Studio's Test Explorer, Rider and VS Code's C# Dev Kit find them with no
extra setup.

**Debugging tests in Visual Studio.** Several tests check error handling, so they cause exceptions on purpose:
- invalid converter settings
- malformed symbols
- inconsistent Yahoo data
- an unknown symbol returning 404

The tests catch these exceptions and pass. Under the debugger, though, Visual Studio stops on each one with
*Exception User-Unhandled*. The exception is thrown in project code but caught in library code (Shouldly or ASP.NET
Core), and Visual Studio's *Just My Code* setting treats that as unhandled. To avoid the stops:
- run the tests instead of debugging them (Test Explorer → **Run All Tests**), or
- press **Continue** at each stop, or
- clear **Break when this exception type is user-unhandled** in the exception popup.

## Docker

```bash
docker build -t marketdata-api backend
docker run --rm -p 8080:8080 marketdata-api
```

The image is a chiseled (distroless) ASP.NET runtime that runs as a non-root user. It includes the time zone data that
exchange-local grouping needs. Behind a TLS-terminating reverse proxy, set `ASPNETCORE_FORWARDEDHEADERS_ENABLED=true`,
so HTTPS detection and per-client rate limiting see the original client. Only do this behind a proxy, because otherwise
clients could spoof the headers.

## Architecture

```
backend/
├─ src/
│  ├─ MarketData.Api/             HTTP: controller, contracts, problem details, CORS, rate limiting, OpenAPI
│  ├─ MarketData.Application/     Domain types, IMarketDataProvider, aggregation, cached daily-summary service
│  └─ MarketData.Infrastructure/  Yahoo Finance client, response contracts and mapping, resilience, cache setup
└─ tests/
   ├─ MarketData.UnitTests/
   └─ MarketData.IntegrationTests/
```

Dependencies point inward. **Api → Application ← Infrastructure.** Application has no dependency on ASP.NET Core or
Yahoo Finance.

| Component | Responsibility |
|---|---|
| `StocksController` | Validates the symbol and maps domain results to the response contract. Holds no logic. |
| `DailySummaryService` | Checks the cache, then calls the provider and the aggregator. |
| `DailySummaryAggregator` | Pure function: groups, averages, rounds. Easy to test without mocks. |
| `IMarketDataProvider` | Abstraction over the data source. `YahooFinanceMarketDataProvider` is the one implementation. |
| `YahooChartMapper` | Converts Yahoo's loosely structured response into domain types and rejects inconsistent data. |
| `ProblemDetailsExceptionHandler` | Turns domain exceptions into HTTP status codes in one place. |

Built to grow:

- **New data source** (e.g. Polygon, Alpha Vantage): implement `IMarketDataProvider` and change one registration.
- **New intervals or periods**: add a value to `BarInterval` or `LookbackPeriod` and map it in the provider. The
  endpoint can then accept them as query parameters without breaking existing clients.
- **Breaking API changes**: go under `/api/v2`.

## Production notes

- **Resilience.** Yahoo calls use the standard .NET resilience pipeline: retries with backoff and jitter, a circuit
  breaker, and per-attempt and total timeouts.
- **Caching.** Results are cached for 5 minutes per symbol with `HybridCache`, which also merges simultaneous identical
  requests into one Yahoo call. Errors are never cached. A distributed cache such as Redis can be added as a second
  level without code changes.
- **Rate limiting.** A fixed window per client IP protects both the API and our standing with Yahoo.
- **Errors and logging.** Unhandled errors return a generic 500 that reveals no internals. Logs are structured, and
  JSON-formatted outside Development.
- **Security.** HSTS and HTTPS redirection are enabled outside Development. OpenAPI and Scalar are served only in
  Development.
- **Yahoo Finance caveat.** Yahoo's chart API is unofficial and undocumented, and it may change or rate limit without
  notice. It is isolated behind `IMarketDataProvider` so it can be replaced with a licensed data provider.
