namespace MarketData.Api.Cors;

internal sealed class ApiCorsOptions
{
    public const string SectionName = "Cors";

    /// <summary>Browser origins allowed to call the API, e.g. <c>http://localhost:5173</c>. Empty disallows all.</summary>
    public IList<string> AllowedOrigins { get; } = [];
}
