namespace TradePilot.Api.DTOs;

public class SignalDto
{
    public long Id { get; set; }
    public int InstrumentId { get; set; }
    public string InstrumentSymbol { get; set; } = string.Empty;
    public int StrategyId { get; set; }
    public string StrategyName { get; set; } = string.Empty;
    public string Direction { get; set; } = string.Empty;
    public decimal SignalScore { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Target { get; set; }
    public decimal RiskRewardRatio { get; set; }
    public string TimeFrame { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ExpiresAtUtc { get; set; }
    public List<SignalConditionDto> Conditions { get; set; } = [];
}

public class CreateSignalDto
{
    public int InstrumentId { get; set; }
    public int StrategyId { get; set; }
    public string Direction { get; set; } = string.Empty;
    public decimal SignalScore { get; set; }
    public decimal EntryPrice { get; set; }
    public decimal StopLoss { get; set; }
    public decimal Target { get; set; }
    public string TimeFrame { get; set; } = string.Empty;
    public string Explanation { get; set; } = string.Empty;
    public DateTime? ExpiresAtUtc { get; set; }
    public List<CreateSignalConditionDto> Conditions { get; set; } = [];
}

public class CreateSignalConditionDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public bool Passed { get; set; }
    public string Explanation { get; set; } = string.Empty;
}
