using System.ComponentModel.DataAnnotations;

namespace MarketData.Infrastructure.YahooFinance;

public sealed class YahooFinanceOptions
{
    public const string SectionName = "YahooFinance";

    [Required]
    public Uri BaseAddress { get; set; } = new("https://query1.finance.yahoo.com/");

    /// <summary>Yahoo rejects requests without a browser-like User-Agent with HTTP 429.</summary>
    [Required(AllowEmptyStrings = false)]
    public string UserAgent { get; set; } = "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36";

    /// <summary>Timeout for a single HTTP attempt.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:01:00")]
    public TimeSpan AttemptTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>Timeout for the whole request, including retries.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "00:05:00")]
    public TimeSpan TotalRequestTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>Retries for transient failures (HTTP 408, 429, 5xx and network errors).</summary>
    [Range(0, 10)]
    public int MaxRetryAttempts { get; set; } = 3;
}
