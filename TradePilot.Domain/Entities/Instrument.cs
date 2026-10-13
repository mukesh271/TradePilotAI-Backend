namespace TradePilot.Domain.Entities;

public class Instrument
{
    public int Id { get; set; }

    public string Symbol { get; set; } = string.Empty;

    public string Exchange { get; set; } = "NSE";

    public InstrumentType Type { get; set; }

    public bool IsActive { get; set; } = true;

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;

    public DateTime? UpdatedAtUtc { get; set; }

    public ICollection<Signal> Signals { get; set; } = [];
}
