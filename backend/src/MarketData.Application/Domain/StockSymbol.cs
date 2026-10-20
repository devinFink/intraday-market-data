using System.Diagnostics.CodeAnalysis;
using System.Text.RegularExpressions;

namespace MarketData.Application.Domain;

/// <summary>
/// A validated, upper-case ticker symbol such as <c>TSLA</c>, <c>BRK-B</c>, <c>^GSPC</c> or <c>EURUSD=X</c>.
/// </summary>
public sealed partial record StockSymbol : IParsable<StockSymbol>
{
    public const int MaxLength = 15;

    /// <summary>Human-readable description of the accepted format, suitable for validation messages.</summary>
    public const string FormatDescription =
        "Symbol must be 1-15 characters, include a letter or digit, and contain only letters, digits, '.', '-', '^' or '='.";

    private StockSymbol(string value) => Value = value;

    public string Value { get; }

    public static bool TryParse([NotNullWhen(true)] string? s, [MaybeNullWhen(false)] out StockSymbol result) =>
        TryParse(s, provider: null, out result);

    public static bool TryParse(
        [NotNullWhen(true)] string? s,
        IFormatProvider? provider,
        [MaybeNullWhen(false)] out StockSymbol result)
    {
        var candidate = s?.Trim().ToUpperInvariant();
        if (candidate is null || !SymbolPattern().IsMatch(candidate))
        {
            result = null;
            return false;
        }

        result = new StockSymbol(candidate);
        return true;
    }

    public static StockSymbol Parse(string s, IFormatProvider? provider) =>
        TryParse(s, provider, out var result) ? result : throw new FormatException(FormatDescription);

    public override string ToString() => Value;

    // Requiring a letter or digit rules out dot-only values such as "..", which would act as URL path segments.
    [GeneratedRegex(@"^(?=.*[A-Z0-9])[A-Z0-9.\-^=]{1,15}$", RegexOptions.CultureInvariant)]
    private static partial Regex SymbolPattern();
}
