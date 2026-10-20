using System.ComponentModel.DataAnnotations;

namespace MarketData.Application.DailySummaries;

public sealed class DailySummaryCacheOptions
{
    public const string SectionName = "DailySummaryCache";

    /// <summary>How long computed summaries are reused before the provider is queried again.</summary>
    [Range(typeof(TimeSpan), "00:00:01", "1.00:00:00")]
    public TimeSpan Expiration { get; set; } = TimeSpan.FromMinutes(5);
}
