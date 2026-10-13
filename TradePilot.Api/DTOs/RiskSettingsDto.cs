namespace TradePilot.Api.DTOs;

public class RiskSettingsDto
{
    public int Id { get; set; }
    public decimal TradingCapital { get; set; }
    public decimal RiskPerTradePercent { get; set; }
    public decimal MaximumDailyLossPercent { get; set; }
    public decimal MinimumSignalScore { get; set; }
    public bool PaperTradingEnabled { get; set; }
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}

public class UpdateRiskSettingsDto
{
    public decimal TradingCapital { get; set; }
    public decimal RiskPerTradePercent { get; set; }
    public decimal MaximumDailyLossPercent { get; set; }
    public decimal MinimumSignalScore { get; set; }
    public bool PaperTradingEnabled { get; set; }
}
