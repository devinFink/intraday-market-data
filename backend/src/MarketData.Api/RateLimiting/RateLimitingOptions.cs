using System.ComponentModel.DataAnnotations;

namespace MarketData.Api.RateLimiting;

internal sealed class RateLimitingOptions
{
    public const string SectionName = "RateLimiting";

    /// <summary>Requests each client may make per <see cref="Window"/>.</summary>
    [Range(1, 100_000)]
    public int PermitLimit { get; set; } = 60;

    [Range(typeof(TimeSpan), "00:00:01", "01:00:00")]
    public TimeSpan Window { get; set; } = TimeSpan.FromMinutes(1);
}
