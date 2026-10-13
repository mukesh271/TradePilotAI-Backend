using TradePilot.Api.DTOs;
using TradePilot.Api.Services;
using TradePilot.Domain.Entities;
using Xunit;

namespace TradePilot.Tests;

public class IndicatorCalculationServiceTests
{
    private readonly IndicatorCalculationService _sut = new();

    [Fact]
    public void Sma_ReturnsAverageOfLastPeriodValues()
    {
        var result = _sut.Sma([1m, 2m, 3m, 4m, 5m], 3);
        Assert.Equal(4m, result);
    }

    [Fact]
    public void Sma_ReturnsNullWhenInsufficientHistory()
    {
        Assert.Null(_sut.Sma([1m, 2m], 3));
    }

    [Fact]
    public void Ema_SeedsWithSmaOfFirstPeriodValues()
    {
        var result = _sut.Ema([1m, 2m, 3m], 3);
        Assert.Equal(2m, result);
    }

    [Fact]
    public void Rsi_ReturnsNeutralFiftyWhenPricesAreFlat()
    {
        var closes = Enumerable.Repeat(10m, 20).ToList();
        Assert.Equal(50m, _sut.Rsi(closes, 14));
    }

    [Fact]
    public void Rsi_ReturnsHundredWhenThereAreNoLosses()
    {
        var closes = Enumerable.Range(1, 20).Select(i => (decimal)i).ToList();
        Assert.Equal(100m, _sut.Rsi(closes, 14));
    }

    [Fact]
    public void Rsi_ReturnsNullWhenInsufficientHistory()
    {
        Assert.Null(_sut.Rsi([1m, 2m, 3m], 14));
    }

    [Fact]
    public void Macd_ReturnsNullWhenInsufficientHistory()
    {
        Assert.Null(_sut.Macd(Enumerable.Repeat(10m, 20).ToList(), 12, 26, 9));
    }

    [Fact]
    public void Vwap_IsNullForDailyTimeframe()
    {
        var candles = new List<Candle> { MakeCandle(new DateTime(2025, 1, 1, 0, 0, 0, DateTimeKind.Utc), 10, 12, 8, 11, 100) };
        Assert.Null(_sut.Vwap(candles, Timeframes.OneDay));
    }

    [Fact]
    public void Vwap_UsesOnlyLatestSession()
    {
        var candles = new List<Candle>
        {
            MakeCandle(new DateTime(2025, 1, 1, 9, 15, 0, DateTimeKind.Utc), 100, 100, 100, 100, 1000),
            MakeCandle(new DateTime(2025, 1, 2, 9, 15, 0, DateTimeKind.Utc), 10, 12, 8, 10, 100)
        };

        Assert.Equal(10m, _sut.Vwap(candles, Timeframes.FiveMinutes));
    }

    [Fact]
    public void Atr_ReturnsAverageTrueRangeForSeededWindow()
    {
        var candles = new List<Candle>
        {
            MakeCandle(Utc(0), 10, 12, 8, 10, 1),
            MakeCandle(Utc(1), 10, 13, 9, 11, 1),
            MakeCandle(Utc(2), 11, 14, 10, 12, 1)
        };

        Assert.Equal(4m, _sut.Atr(candles, 2));
    }

    private static DateTime Utc(int minutes) => new DateTime(2025, 1, 1, 9, 15, 0, DateTimeKind.Utc).AddMinutes(minutes);

    private static Candle MakeCandle(DateTime timestamp, decimal open, decimal high, decimal low, decimal close, decimal volume) =>
        new()
        {
            InstrumentId = 1,
            Timeframe = Timeframes.FiveMinutes,
            TimestampUtc = timestamp,
            Open = open,
            High = high,
            Low = low,
            Close = close,
            Volume = volume
        };
}

public class StrategyEvaluationServiceTests
{
    private readonly StrategyEvaluationService _sut = new();

    [Fact]
    public void Breakout_CompletesWhenCloseExceedsReferenceHigh()
    {
        var candles = Rising(21, closeBoost: 50m);
        var snapshot = new IndicatorSnapshot(null, null, null, null, null, null, null, null, null, null, null, 2m);

        var setups = _sut.Evaluate(candles, snapshot, new StrategySettings { BreakoutLookback = 20 });
        var breakout = setups.Single(s => s.StrategyName == "Breakout");

        Assert.Equal(StrategyEvaluationService.StatusComplete, breakout.Status);
    }

    [Fact]
    public void BreakoutWithVolume_IncompleteWhenRelativeVolumeBelowThreshold()
    {
        var candles = Rising(21, closeBoost: 50m);
        var snapshot = new IndicatorSnapshot(null, null, null, null, null, null, null, null, null, null, null, 1.0m);

        var setups = _sut.Evaluate(candles, snapshot, new StrategySettings { BreakoutLookback = 20, RelativeVolumeThreshold = 1.5m });
        var setup = setups.Single(s => s.StrategyName == "Breakout + Volume");

        Assert.Equal(StrategyEvaluationService.StatusIncomplete, setup.Status);
        Assert.False(setup.Conditions.Single(c => c.Name == "Relative volume above threshold").Passed);
    }

    [Fact]
    public void Setups_ReturnInsufficientDataWhenHistoryIsShort()
    {
        var candles = Rising(3, closeBoost: 0m);
        var snapshot = new IndicatorSnapshot(null, null, null, null, null, null, null, null, null, null, null, null);

        var setups = _sut.Evaluate(candles, snapshot, new StrategySettings());

        Assert.All(setups.Where(s => s.StrategyName is "Breakout" or "Support Reversal" or "Resistance Reversal"),
            s => Assert.Equal(StrategyEvaluationService.StatusInsufficientData, s.Status));
    }

    private static List<Candle> Rising(int count, decimal closeBoost)
    {
        var start = new DateTime(2025, 1, 1, 9, 15, 0, DateTimeKind.Utc);
        var list = new List<Candle>();
        for (var i = 0; i < count; i++)
        {
            var close = 100m + i;
            list.Add(new Candle
            {
                InstrumentId = 1,
                Timeframe = Timeframes.FiveMinutes,
                TimestampUtc = start.AddMinutes(5 * i),
                Open = close - 0.5m,
                High = close + 1m,
                Low = close - 1m,
                Close = i == count - 1 ? close + closeBoost : close,
                Volume = 100
            });
        }
        return list;
    }
}

public class CandleValidatorTests
{
    private static readonly DateTime Now = new(2025, 6, 1, 12, 0, 0, DateTimeKind.Utc);

    private static ImportCandleDto Valid() => new()
    {
        TimestampUtc = new DateTime(2025, 5, 30, 9, 15, 0, DateTimeKind.Utc),
        Timeframe = Timeframes.FiveMinutes,
        Open = 10,
        High = 12,
        Low = 9,
        Close = 11,
        Volume = 100
    };

    [Fact]
    public void Validate_AcceptsValidCandle()
    {
        Assert.Empty(CandleValidator.Validate(Valid(), Now));
    }

    [Fact]
    public void Validate_RejectsUnsupportedTimeframe()
    {
        var candle = Valid();
        candle.Timeframe = "3m";
        Assert.NotEmpty(CandleValidator.Validate(candle, Now));
    }

    [Fact]
    public void Validate_RejectsHighBelowLow()
    {
        var candle = Valid();
        candle.High = 8;
        Assert.NotEmpty(CandleValidator.Validate(candle, Now));
    }

    [Fact]
    public void Validate_RejectsNegativeVolume()
    {
        var candle = Valid();
        candle.Volume = -1;
        Assert.NotEmpty(CandleValidator.Validate(candle, Now));
    }

    [Fact]
    public void Validate_RejectsFutureTimestamp()
    {
        var candle = Valid();
        candle.TimestampUtc = Now.AddDays(1);
        Assert.NotEmpty(CandleValidator.Validate(candle, Now));
    }
}
