namespace TradePilot.Api.DTOs;

public class AnalysisConditionDto
{
    public string Name { get; set; } = string.Empty;
    public bool Passed { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public decimal? ActualValue { get; set; }
    public decimal? ExpectedValue { get; set; }
}

public class AnalysisSetupResultDto
{
    public string StrategyName { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public List<AnalysisConditionDto> Conditions { get; set; } = [];
    public int ConditionsPassed { get; set; }
    public int TotalConditions { get; set; }
    public string Status { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
}

public class TechnicalAnalysisResultDto
{
    public int InstrumentId { get; set; }
    public string Symbol { get; set; } = string.Empty;
    public string Timeframe { get; set; } = string.Empty;
    public DateTime LastCandleTimestampUtc { get; set; }
    public decimal LastClose { get; set; }
    public decimal? Sma20 { get; set; }
    public decimal? Sma50 { get; set; }
    public decimal? Ema9 { get; set; }
    public decimal? Ema21 { get; set; }
    public decimal? Rsi14 { get; set; }
    public decimal? MacdLine { get; set; }
    public decimal? MacdSignal { get; set; }
    public decimal? MacdHistogram { get; set; }
    public decimal? Vwap { get; set; }
    public decimal? Atr14 { get; set; }
    public decimal? VolumeSma20 { get; set; }
    public decimal? RelativeVolume { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public List<AnalysisSetupResultDto> Setups { get; set; } = [];
}

public class CalculateAnalysisRequestDto
{
    public string Timeframe { get; set; } = string.Empty;
    public int? Limit { get; set; }
    public decimal? RelativeVolumeThreshold { get; set; }
    public int? BreakoutLookback { get; set; }
}
