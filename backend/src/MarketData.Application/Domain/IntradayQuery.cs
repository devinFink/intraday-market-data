namespace MarketData.Application.Domain;

/// <summary>Size of each intraday bar.</summary>
public enum BarInterval
{
    FifteenMinutes,
}

/// <summary>How far back from now to fetch intraday bars.</summary>
public enum LookbackPeriod
{
    OneMonth,
}

/// <summary>Describes which intraday bars to fetch.</summary>
public sealed record IntradayQuery(BarInterval Interval, LookbackPeriod Lookback)
{
    /// <summary>15-minute bars covering the last month.</summary>
    public static IntradayQuery Default { get; } = new(BarInterval.FifteenMinutes, LookbackPeriod.OneMonth);
}
