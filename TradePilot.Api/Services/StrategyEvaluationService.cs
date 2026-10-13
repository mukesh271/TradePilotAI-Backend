using TradePilot.Api.DTOs;
using TradePilot.Domain.Entities;

namespace TradePilot.Api.Services;

/// <summary>
/// Configurable thresholds for the initial setup rules. Defaults are starting points only and are not proven profitable.
/// </summary>
public record StrategySettings
{
    public int BreakoutLookback { get; init; } = 20;
    public int ReversalLookback { get; init; } = 20;
    public decimal RelativeVolumeThreshold { get; init; } = 1.5m;
    public decimal ZoneTolerancePercent { get; init; } = 0.5m;
}

public interface IStrategyEvaluationService
{
    IReadOnlyList<AnalysisSetupResultDto> Evaluate(
        IReadOnlyList<Candle> candles,
        IndicatorSnapshot snapshot,
        StrategySettings settings);
}

/// <summary>
/// Evaluates deterministic technical setups. A "Complete" status means all rule conditions passed on the
/// latest completed candle. It is a research candidate only and never a trading recommendation or order.
/// </summary>
public class StrategyEvaluationService : IStrategyEvaluationService
{
    public const string StatusComplete = "Complete";
    public const string StatusIncomplete = "Incomplete";
    public const string StatusInsufficientData = "InsufficientData";

    private const string Bullish = "Bullish";
    private const string Bearish = "Bearish";
    private const string NoDirection = "None";

    public IReadOnlyList<AnalysisSetupResultDto> Evaluate(
        IReadOnlyList<Candle> candles,
        IndicatorSnapshot snapshot,
        StrategySettings settings)
    {
        var ordered = candles.OrderBy(c => c.TimestampUtc).ToList();

        return
        [
            EvaluateBreakout(ordered, settings),
            EvaluateBreakoutWithVolume(ordered, snapshot, settings),
            EvaluateTrendContinuation(ordered, snapshot),
            EvaluateSupportReversal(ordered, settings),
            EvaluateResistanceReversal(ordered, settings)
        ];
    }

    /// <summary>
    /// Bullish breakout: latest close must exceed the highest high of the previous N candles.
    /// The latest candle is excluded from the reference range.
    /// </summary>
    private static AnalysisSetupResultDto EvaluateBreakout(IReadOnlyList<Candle> candles, StrategySettings settings)
    {
        const string name = "Breakout";
        var lookback = settings.BreakoutLookback;

        if (candles.Count < lookback + 1)
            return InsufficientData(name, Bullish, $"Requires at least {lookback + 1} candles; {candles.Count} available.");

        var reference = ReferenceRange(candles, lookback);
        var referenceHigh = reference.Max(c => c.High);
        var last = candles[^1];

        var conditions = new List<AnalysisConditionDto>
        {
            new()
            {
                Name = "Close above reference high",
                Passed = last.Close > referenceHigh,
                ActualValue = last.Close,
                ExpectedValue = referenceHigh,
                Explanation = last.Close > referenceHigh
                    ? $"Close {last.Close} is above the {lookback}-candle reference high {referenceHigh}."
                    : $"Close {last.Close} is not above the {lookback}-candle reference high {referenceHigh}."
            }
        };

        return BuildResult(name, Bullish, conditions);
    }

    /// <summary>
    /// Breakout conditions plus relative volume at or above the configured threshold.
    /// </summary>
    private static AnalysisSetupResultDto EvaluateBreakoutWithVolume(
        IReadOnlyList<Candle> candles,
        IndicatorSnapshot snapshot,
        StrategySettings settings)
    {
        const string name = "Breakout + Volume";
        var breakout = EvaluateBreakout(candles, settings);

        if (breakout.Status == StatusInsufficientData)
            return InsufficientData(name, Bullish, breakout.Explanation);

        var conditions = new List<AnalysisConditionDto>(breakout.Conditions);
        var threshold = settings.RelativeVolumeThreshold;

        if (snapshot.RelativeVolume is decimal relativeVolume)
        {
            var passed = relativeVolume >= threshold;
            conditions.Add(new AnalysisConditionDto
            {
                Name = "Relative volume above threshold",
                Passed = passed,
                ActualValue = relativeVolume,
                ExpectedValue = threshold,
                Explanation = passed
                    ? $"Relative volume {relativeVolume:0.##} is at or above the threshold {threshold:0.##}."
                    : $"Relative volume {relativeVolume:0.##} is below the threshold {threshold:0.##}."
            });
        }
        else
        {
            conditions.Add(new AnalysisConditionDto
            {
                Name = "Relative volume above threshold",
                Passed = false,
                ExpectedValue = threshold,
                Explanation = "Relative volume is unavailable because the volume average has insufficient history or a zero average."
            });
        }

        return BuildResult(name, Bullish, conditions);
    }

    /// <summary>
    /// Trend continuation: EMA9 vs EMA21 ordering and latest close relative to EMA21 and SMA50.
    /// Bullish when EMA9 is above EMA21; bearish when below.
    /// </summary>
    private static AnalysisSetupResultDto EvaluateTrendContinuation(
        IReadOnlyList<Candle> candles,
        IndicatorSnapshot snapshot)
    {
        const string name = "Trend Continuation";

        if (candles.Count == 0 ||
            snapshot.Ema9 is not decimal ema9 ||
            snapshot.Ema21 is not decimal ema21 ||
            snapshot.Sma50 is not decimal sma50)
        {
            return InsufficientData(name, NoDirection, "Requires at least 50 candles for SMA50 (EMA9 and EMA21 need fewer).");
        }

        var close = candles[^1].Close;
        var isBearish = ema9 < ema21;
        var direction = ema9 == ema21 ? NoDirection : isBearish ? Bearish : Bullish;

        var conditions = new List<AnalysisConditionDto>
        {
            Compare(
                isBearish ? "EMA9 below EMA21" : "EMA9 above EMA21",
                ema9, ema21, above: !isBearish),
            Compare(
                isBearish ? "Close below EMA21" : "Close above EMA21",
                close, ema21, above: !isBearish),
            Compare(
                isBearish ? "Close below SMA50" : "Close above SMA50",
                close, sma50, above: !isBearish)
        };

        return BuildResult(name, direction, conditions);
    }

    /// <summary>
    /// Support reversal: latest low approaches the support zone (lowest low of the previous N candles),
    /// close holds above support, and the latest candle is bullish (close above open).
    /// </summary>
    private static AnalysisSetupResultDto EvaluateSupportReversal(IReadOnlyList<Candle> candles, StrategySettings settings)
    {
        const string name = "Support Reversal";
        var lookback = settings.ReversalLookback;

        if (candles.Count < lookback + 1)
            return InsufficientData(name, Bullish, $"Requires at least {lookback + 1} candles; {candles.Count} available.");

        var support = ReferenceRange(candles, lookback).Min(c => c.Low);
        var last = candles[^1];
        var approachLimit = support * (1m + settings.ZoneTolerancePercent / 100m);

        var conditions = new List<AnalysisConditionDto>
        {
            new()
            {
                Name = "Price approaches support",
                Passed = last.Low <= approachLimit,
                ActualValue = last.Low,
                ExpectedValue = approachLimit,
                Explanation = last.Low <= approachLimit
                    ? $"Low {last.Low} is within {settings.ZoneTolerancePercent}% of support {support}."
                    : $"Low {last.Low} is not within {settings.ZoneTolerancePercent}% of support {support}."
            },
            new()
            {
                Name = "Close holds above support",
                Passed = last.Close > support,
                ActualValue = last.Close,
                ExpectedValue = support,
                Explanation = last.Close > support
                    ? $"Close {last.Close} is above support {support}."
                    : $"Close {last.Close} is not above support {support}."
            },
            new()
            {
                Name = "Bullish reversal candle",
                Passed = last.Close > last.Open,
                ActualValue = last.Close,
                ExpectedValue = last.Open,
                Explanation = last.Close > last.Open
                    ? $"Close {last.Close} is above open {last.Open}."
                    : $"Close {last.Close} is not above open {last.Open}."
            }
        };

        return BuildResult(name, Bullish, conditions);
    }

    /// <summary>
    /// Resistance reversal: latest high approaches the resistance zone (highest high of the previous N candles),
    /// close stays below resistance, and the latest candle is bearish (close below open).
    /// </summary>
    private static AnalysisSetupResultDto EvaluateResistanceReversal(IReadOnlyList<Candle> candles, StrategySettings settings)
    {
        const string name = "Resistance Reversal";
        var lookback = settings.ReversalLookback;

        if (candles.Count < lookback + 1)
            return InsufficientData(name, Bearish, $"Requires at least {lookback + 1} candles; {candles.Count} available.");

        var resistance = ReferenceRange(candles, lookback).Max(c => c.High);
        var last = candles[^1];
        var approachLimit = resistance * (1m - settings.ZoneTolerancePercent / 100m);

        var conditions = new List<AnalysisConditionDto>
        {
            new()
            {
                Name = "Price approaches resistance",
                Passed = last.High >= approachLimit,
                ActualValue = last.High,
                ExpectedValue = approachLimit,
                Explanation = last.High >= approachLimit
                    ? $"High {last.High} is within {settings.ZoneTolerancePercent}% of resistance {resistance}."
                    : $"High {last.High} is not within {settings.ZoneTolerancePercent}% of resistance {resistance}."
            },
            new()
            {
                Name = "Close holds below resistance",
                Passed = last.Close < resistance,
                ActualValue = last.Close,
                ExpectedValue = resistance,
                Explanation = last.Close < resistance
                    ? $"Close {last.Close} is below resistance {resistance}."
                    : $"Close {last.Close} is not below resistance {resistance}."
            },
            new()
            {
                Name = "Bearish reversal candle",
                Passed = last.Close < last.Open,
                ActualValue = last.Close,
                ExpectedValue = last.Open,
                Explanation = last.Close < last.Open
                    ? $"Close {last.Close} is below open {last.Open}."
                    : $"Close {last.Close} is not below open {last.Open}."
            }
        };

        return BuildResult(name, Bearish, conditions);
    }

    private static IReadOnlyList<Candle> ReferenceRange(IReadOnlyList<Candle> candles, int lookback) =>
        candles.Skip(candles.Count - lookback - 1).Take(lookback).ToList();

    private static AnalysisConditionDto Compare(string name, decimal actual, decimal expected, bool above)
    {
        var passed = above ? actual > expected : actual < expected;
        return new AnalysisConditionDto
        {
            Name = name,
            Passed = passed,
            ActualValue = actual,
            ExpectedValue = expected,
            Explanation = $"Value {actual} is {(passed ? "" : "not ")}{(above ? "above" : "below")} {expected}."
        };
    }

    private static AnalysisSetupResultDto BuildResult(
        string name,
        string direction,
        List<AnalysisConditionDto> conditions)
    {
        var passedCount = conditions.Count(c => c.Passed);
        var total = conditions.Count;
        var failed = conditions.Where(c => !c.Passed).Select(c => c.Name).ToList();
        var allPassed = passedCount == total;

        var explanation = allPassed
            ? $"All {total} conditions passed. Research candidate only; not a confirmed trading signal."
            : $"{passedCount}/{total} conditions passed. Failed: {string.Join(", ", failed)}. Not a confirmed trading signal.";

        return new AnalysisSetupResultDto
        {
            StrategyName = name,
            Direction = direction,
            Conditions = conditions,
            ConditionsPassed = passedCount,
            TotalConditions = total,
            Status = allPassed ? StatusComplete : StatusIncomplete,
            Explanation = explanation
        };
    }

    private static AnalysisSetupResultDto InsufficientData(string name, string direction, string message) =>
        new()
        {
            StrategyName = name,
            Direction = direction,
            Conditions = [],
            ConditionsPassed = 0,
            TotalConditions = 0,
            Status = StatusInsufficientData,
            Explanation = $"Insufficient data. {message}"
        };
}
