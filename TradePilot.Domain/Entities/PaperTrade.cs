namespace TradePilot.Domain.Entities;

public class PaperTrade
{
    public long Id { get; set; }

    public long SignalId { get; set; }

    public Signal Signal { get; set; } = null!;

    public decimal EntryPrice { get; set; }

    public decimal ExitPrice { get; set; }

    public int Quantity { get; set; }

    public decimal ProfitLoss { get; set; }

    public TradeStatus Status { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? ClosedAtUtc { get; set; }

    public TradeResult? TradeResult { get; set; }
}
