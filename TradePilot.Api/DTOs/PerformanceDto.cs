namespace TradePilot.Api.DTOs;

public class PerformanceDto
{
    public DateTime CalculatedAtUtc { get; set; }
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal TotalNetProfitLoss { get; set; }
    public decimal AverageWinningTrade { get; set; }
    public decimal AverageLosingTrade { get; set; }
    public decimal MaximumDrawdown { get; set; }
}

public class StrategyPerformanceDto
{
    public long Id { get; set; }
    public int StrategyId { get; set; }
    public string StrategyName { get; set; } = string.Empty;
    public int TotalTrades { get; set; }
    public int WinningTrades { get; set; }
    public int LosingTrades { get; set; }
    public decimal WinRate { get; set; }
    public decimal ProfitFactor { get; set; }
    public decimal TotalNetProfitLoss { get; set; }
    public decimal AverageWinningTrade { get; set; }
    public decimal AverageLosingTrade { get; set; }
    public decimal MaximumDrawdown { get; set; }
    public DateTime CalculatedAtUtc { get; set; }
}
