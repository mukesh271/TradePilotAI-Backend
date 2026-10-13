namespace TradePilot.Domain.Entities;

/// <summary>
/// Canonical set of supported candle timeframe values used across the solution.
/// </summary>
public static class Timeframes
{
    public const string OneMinute = "1m";
    public const string FiveMinutes = "5m";
    public const string FifteenMinutes = "15m";
    public const string OneHour = "1h";
    public const string OneDay = "1d";

    public static readonly IReadOnlyCollection<string> All = new[]
    {
        OneMinute,
        FiveMinutes,
        FifteenMinutes,
        OneHour,
        OneDay
    };

    public static bool IsValid(string? timeframe) =>
        !string.IsNullOrWhiteSpace(timeframe) && All.Contains(timeframe);

    /// <summary>
    /// Intraday timeframes are the only ones for which a session-based VWAP is meaningful.
    /// </summary>
    public static bool IsIntraday(string? timeframe) =>
        IsValid(timeframe) && timeframe != OneDay;
}
