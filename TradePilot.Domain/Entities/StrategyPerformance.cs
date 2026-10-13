namespace TradePilot.Domain.Entities;

public class StrategyPerformance
{
    public long Id { get; set; }

    public int StrategyId { get; set; }

    public Strategy Strategy { get; set; } = null!;

    public int TotalTrades { get; set; }

    public int WinningTrades { get; set; }

    public int LosingTrades { get; set; }

    public decimal WinRate { get; set; }

    public decimal ProfitFactor { get; set; }

    public decimal TotalNetProfitLoss { get; set; }

    public decimal AverageWinningTrade { get; set; }

    public decimal AverageLosingTrade { get; set; }

    public decimal MaximumDrawdown { get; set; }

    public DateTime CalculatedAtUtc { get; set; } = DateTime.UtcNow;
}
