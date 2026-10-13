namespace TradePilot.Domain.Entities;

public class Strategy
{
    public int Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public string Description { get; set; } = string.Empty;

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Signal> Signals { get; set; } = [];

    public ICollection<StrategyPerformance> PerformanceHistory { get; set; } = [];
}
