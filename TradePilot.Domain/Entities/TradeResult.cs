namespace TradePilot.Domain.Entities;

public class TradeResult
{
    public long Id { get; set; }

    public long PaperTradeId { get; set; }

    public PaperTrade PaperTrade { get; set; } = null!;

    public bool IsWinner { get; set; }

    public decimal GrossProfitLoss { get; set; }

    public decimal Charges { get; set; }

    public decimal NetProfitLoss { get; set; }

    public string ExitReason { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
