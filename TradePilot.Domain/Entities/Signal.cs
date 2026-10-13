namespace TradePilot.Domain.Entities;

public class Signal
{
    public long Id { get; set; }

    public int InstrumentId { get; set; }

    public Instrument Instrument { get; set; } = null!;

    public int StrategyId { get; set; }

    public Strategy Strategy { get; set; } = null!;

    public string Direction { get; set; } = string.Empty;

    public decimal SignalScore { get; set; }

    public decimal EntryPrice { get; set; }

    public decimal StopLoss { get; set; }

    public decimal Target { get; set; }

    public decimal RiskRewardRatio { get; set; }

    public string TimeFrame { get; set; } = string.Empty;

    public string Explanation { get; set; } = string.Empty;

    public SignalStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ExpiresAtUtc { get; set; }

    public ICollection<SignalCondition> Conditions { get; set; } = [];
}
