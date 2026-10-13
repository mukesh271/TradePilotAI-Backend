namespace TradePilot.Api.DTOs;

public class PaperTradeDto
{
    public long Id { get; set; }
    public long SignalId { get; set; }
    public string SignalDirection { get; set; } = string.Empty;
    public decimal EntryPrice { get; set; }
    public decimal ExitPrice { get; set; }
    public int Quantity { get; set; }
    public decimal ProfitLoss { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
    public DateTime? ClosedAtUtc { get; set; }
}

public class CreatePaperTradeDto
{
    public long SignalId { get; set; }
    public decimal EntryPrice { get; set; }
    public int Quantity { get; set; }
}

public class ClosePaperTradeDto
{
    public decimal ExitPrice { get; set; }
    public string ExitReason { get; set; } = string.Empty;
}
