using TradePilot.Api.DTOs;
using TradePilot.Domain.Entities;

namespace TradePilot.Api.Services;

/// <summary>
/// Validates OHLCV candle input. Returns an empty list when the candle is valid.
/// </summary>
public static class CandleValidator
{
    public static List<string> Validate(ImportCandleDto candle, DateTime utcNow)
    {
        var errors = new List<string>();

        if (!Timeframes.IsValid(candle.Timeframe))
        {
            errors.Add($"Timeframe '{candle.Timeframe}' is not supported. Use one of: {string.Join(", ", Timeframes.All)}.");
            return errors;
        }

        if (candle.TimestampUtc == default)
            errors.Add("TimestampUtc is required.");
        else if (candle.TimestampUtc.Kind == DateTimeKind.Local)
            errors.Add("TimestampUtc must be UTC, not local time.");
        else if (candle.TimestampUtc > utcNow)
            errors.Add("TimestampUtc cannot be in the future.");
        else if (!IsAlignedToTimeframe(candle.TimestampUtc, candle.Timeframe))
            errors.Add($"TimestampUtc {candle.TimestampUtc:O} is not aligned to the {candle.Timeframe} timeframe boundary.");

        if (candle.Open <= 0 || candle.High <= 0 || candle.Low <= 0 || candle.Close <= 0)
            errors.Add("Open, High, Low and Close must be greater than 0.");

        if (candle.High < candle.Low)
            errors.Add("High must be greater than or equal to Low.");

        if (candle.High < candle.Open || candle.High < candle.Close)
            errors.Add("High must be greater than or equal to Open and Close.");

        if (candle.Low > candle.Open || candle.Low > candle.Close)
            errors.Add("Low must be less than or equal to Open and Close.");

        if (candle.Volume < 0)
            errors.Add("Volume must be greater than or equal to 0.");

        return errors;
    }

    private static bool IsAlignedToTimeframe(DateTime timestampUtc, string timeframe)
    {
        var ticksIntoDay = timestampUtc.TimeOfDay.Ticks;
        if (ticksIntoDay % TimeSpan.TicksPerMinute != 0)
            return false;

        var minutesIntoDay = ticksIntoDay / TimeSpan.TicksPerMinute;
        var stepMinutes = timeframe switch
        {
            Timeframes.OneMinute => 1,
            Timeframes.FiveMinutes => 5,
            Timeframes.FifteenMinutes => 15,
            Timeframes.OneHour => 60,
            Timeframes.OneDay => 1440,
            _ => 0
        };

        return stepMinutes > 0 && minutesIntoDay % stepMinutes == 0;
    }
}
