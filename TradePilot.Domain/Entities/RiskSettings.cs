namespace TradePilot.Domain.Entities;

public class RiskSettings
{
    public int Id { get; set; }

    public decimal TradingCapital { get; set; } = 20000;

    public decimal RiskPerTradePercent { get; set; } = 1;

    public decimal MaximumDailyLossPercent { get; set; } = 2;

    public decimal MinimumSignalScore { get; set; } = 75;

    public bool PaperTradingEnabled { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAtUtc { get; set; } = DateTime.UtcNow;
}
