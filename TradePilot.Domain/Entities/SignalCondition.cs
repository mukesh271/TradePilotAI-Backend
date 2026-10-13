namespace TradePilot.Domain.Entities;

public class SignalCondition
{
    public long Id { get; set; }

    public long SignalId { get; set; }

    public Signal Signal { get; set; } = null!;

    public string Name { get; set; } = string.Empty;

    public decimal Score { get; set; }

    public bool Passed { get; set; }

    public string Explanation { get; set; } = string.Empty;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
