using TradePilot.Domain.Entities;

namespace TradePilot.Api.Services;

/// <summary>
/// Configurable indicator periods. Defaults are conventional values, not proven parameters.
/// </summary>
public record IndicatorSettings
{
    public int SmaShortPeriod { get; init; } = 20;
    public int SmaLongPeriod { get; init; } = 50;
    public int EmaFastPeriod { get; init; } = 9;
    public int EmaSlowPeriod { get; init; } = 21;
    public int RsiPeriod { get; init; } = 14;
    public int MacdFastPeriod { get; init; } = 12;
    public int MacdSlowPeriod { get; init; } = 26;
    public int MacdSignalPeriod { get; init; } = 9;
    public int AtrPeriod { get; init; } = 14;
    public int VolumeSmaPeriod { get; init; } = 20;
}

public sealed record MacdValues(decimal Line, decimal Signal, decimal Histogram);

/// <summary>
/// Latest indicator values. Any value that cannot be computed from the available history is null.
/// </summary>
public sealed record IndicatorSnapshot(
    decimal? Sma20,
    decimal? Sma50,
    decimal? Ema9,
    decimal? Ema21,
    decimal? Rsi14,
    decimal? MacdLine,
    decimal? MacdSignal,
    decimal? MacdHistogram,
    decimal? Vwap,
    decimal? Atr14,
    decimal? VolumeSma20,
    decimal? RelativeVolume);

public interface IIndicatorCalculationService
{
    decimal? Sma(IReadOnlyList<decimal> values, int period);

    decimal? Ema(IReadOnlyList<decimal> values, int period);

    decimal? Rsi(IReadOnlyList<decimal> closes, int period);

    MacdValues? Macd(IReadOnlyList<decimal> closes, int fastPeriod, int slowPeriod, int signalPeriod);

    decimal? Atr(IReadOnlyList<Candle> chronologicalCandles, int period);

    decimal? Vwap(IReadOnlyList<Candle> chronologicalCandles, string timeframe);

    IndicatorSnapshot CalculateSnapshot(
        IReadOnlyList<Candle> candles,
        string timeframe,
        IndicatorSettings settings);
}

/// <summary>
/// Deterministic indicator formulas using decimal arithmetic throughout (no floating-point conversion is required).
/// Warm-up requirements (minimum candles before a value is non-null):
///   SMA(n)            : n
///   EMA(n)            : n (seeded with SMA of first n values)
///   RSI(n)            : n + 1 (Wilder smoothing seeded with simple average of first n changes)
///   MACD(f,s,g)       : s + g - 1 (MACD line from s candles, signal EMA(g) over MACD line)
///   ATR(n)            : n + 1 (Wilder smoothing seeded with simple average of first n true ranges)
///   VWAP              : at least 1 candle in the current intraday session; null for daily timeframes
///   Volume SMA(n)     : n
///   Relative volume   : requires Volume SMA and a non-zero average
/// </summary>
public class IndicatorCalculationService : IIndicatorCalculationService
{
    public decimal? Sma(IReadOnlyList<decimal> values, int period)
    {
        if (period <= 0 || values.Count < period)
            return null;

        decimal sum = 0;
        for (var i = values.Count - period; i < values.Count; i++)
            sum += values[i];

        return sum / period;
    }

    public decimal? Ema(IReadOnlyList<decimal> values, int period)
    {
        if (values.Count == 0)
            return null;

        return EmaSeries(values, period)[^1];
    }

    /// <summary>
    /// RSI with Wilder smoothing. Edge cases are explicit:
    /// - No gains and no losses over the window (flat prices): RSI = 50 (neutral).
    /// - Losses are zero but gains exist: RSI = 100.
    /// - Gains are zero but losses exist: RSI = 0 (computed by the standard formula).
    /// </summary>
    public decimal? Rsi(IReadOnlyList<decimal> closes, int period)
    {
        if (period <= 0 || closes.Count < period + 1)
            return null;

        decimal gainSum = 0;
        decimal lossSum = 0;
        for (var i = 1; i <= period; i++)
        {
            var change = closes[i] - closes[i - 1];
            if (change > 0)
                gainSum += change;
            else
                lossSum -= change;
        }

        var avgGain = gainSum / period;
        var avgLoss = lossSum / period;

        for (var i = period + 1; i < closes.Count; i++)
        {
            var change = closes[i] - closes[i - 1];
            var gain = change > 0 ? change : 0m;
            var loss = change < 0 ? -change : 0m;

            avgGain = (avgGain * (period - 1) + gain) / period;
            avgLoss = (avgLoss * (period - 1) + loss) / period;
        }

        if (avgGain == 0 && avgLoss == 0)
            return 50m;

        if (avgLoss == 0)
            return 100m;

        var relativeStrength = avgGain / avgLoss;
        return 100m - 100m / (1m + relativeStrength);
    }

    public MacdValues? Macd(IReadOnlyList<decimal> closes, int fastPeriod, int slowPeriod, int signalPeriod)
    {
        if (fastPeriod <= 0 || slowPeriod <= fastPeriod || signalPeriod <= 0)
            return null;

        var fastSeries = EmaSeries(closes, fastPeriod);
        var slowSeries = EmaSeries(closes, slowPeriod);

        var macdLineValues = new List<decimal>();
        for (var i = 0; i < closes.Count; i++)
        {
            if (fastSeries[i] is decimal fast && slowSeries[i] is decimal slow)
                macdLineValues.Add(fast - slow);
        }

        if (macdLineValues.Count < signalPeriod)
            return null;

        var signalSeries = EmaSeries(macdLineValues, signalPeriod);
        if (signalSeries[^1] is not decimal signal)
            return null;

        var line = macdLineValues[^1];
        return new MacdValues(line, signal, line - signal);
    }

    /// <summary>
    /// ATR with Wilder smoothing. Expects candles in chronological order.
    /// </summary>
    public decimal? Atr(IReadOnlyList<Candle> chronologicalCandles, int period)
    {
        if (period <= 0 || chronologicalCandles.Count < period + 1)
            return null;

        var trueRanges = new List<decimal>(chronologicalCandles.Count - 1);
        for (var i = 1; i < chronologicalCandles.Count; i++)
            trueRanges.Add(TrueRange(chronologicalCandles[i], chronologicalCandles[i - 1].Close));

        var atr = trueRanges.Take(period).Average();
        for (var i = period; i < trueRanges.Count; i++)
            atr = (atr * (period - 1) + trueRanges[i]) / period;

        return atr;
    }

    /// <summary>
    /// Session VWAP using typical price (High + Low + Close) / 3.
    /// Only intraday timeframes are supported. The session is the UTC calendar date of the latest candle;
    /// candles from earlier sessions are excluded, which resets VWAP at the session boundary.
    /// Returns null when the timeframe is not intraday or when session volume is zero.
    /// </summary>
    public decimal? Vwap(IReadOnlyList<Candle> chronologicalCandles, string timeframe)
    {
        if (!Timeframes.IsIntraday(timeframe) || chronologicalCandles.Count == 0)
            return null;

        var sessionDate = chronologicalCandles[^1].TimestampUtc.Date;

        decimal priceVolume = 0;
        decimal totalVolume = 0;
        foreach (var candle in chronologicalCandles.Where(c => c.TimestampUtc.Date == sessionDate))
        {
            var typicalPrice = (candle.High + candle.Low + candle.Close) / 3m;
            priceVolume += typicalPrice * candle.Volume;
            totalVolume += candle.Volume;
        }

        if (totalVolume == 0)
            return null;

        return priceVolume / totalVolume;
    }

    public IndicatorSnapshot CalculateSnapshot(
        IReadOnlyList<Candle> candles,
        string timeframe,
        IndicatorSettings settings)
    {
        var ordered = candles.OrderBy(c => c.TimestampUtc).ToList();
        var closes = ordered.Select(c => c.Close).ToList();
        var volumes = ordered.Select(c => c.Volume).ToList();

        var macd = Macd(closes, settings.MacdFastPeriod, settings.MacdSlowPeriod, settings.MacdSignalPeriod);
        var volumeSma = Sma(volumes, settings.VolumeSmaPeriod);

        decimal? relativeVolume = null;
        if (volumeSma is decimal averageVolume && averageVolume > 0 && volumes.Count > 0)
            relativeVolume = volumes[^1] / averageVolume;

        return new IndicatorSnapshot(
            Sma20: Sma(closes, settings.SmaShortPeriod),
            Sma50: Sma(closes, settings.SmaLongPeriod),
            Ema9: Ema(closes, settings.EmaFastPeriod),
            Ema21: Ema(closes, settings.EmaSlowPeriod),
            Rsi14: Rsi(closes, settings.RsiPeriod),
            MacdLine: macd?.Line,
            MacdSignal: macd?.Signal,
            MacdHistogram: macd?.Histogram,
            Vwap: Vwap(ordered, timeframe),
            Atr14: Atr(ordered, settings.AtrPeriod),
            VolumeSma20: volumeSma,
            RelativeVolume: relativeVolume);
    }

    private static decimal?[] EmaSeries(IReadOnlyList<decimal> values, int period)
    {
        var result = new decimal?[values.Count];
        if (period <= 0 || values.Count < period)
            return result;

        decimal seed = 0;
        for (var i = 0; i < period; i++)
            seed += values[i];

        var ema = seed / period;
        result[period - 1] = ema;

        var multiplier = 2m / (period + 1);
        for (var i = period; i < values.Count; i++)
        {
            ema = (values[i] - ema) * multiplier + ema;
            result[i] = ema;
        }

        return result;
    }

    private static decimal TrueRange(Candle candle, decimal previousClose) =>
        Math.Max(
            candle.High - candle.Low,
            Math.Max(Math.Abs(candle.High - previousClose), Math.Abs(candle.Low - previousClose)));
}
