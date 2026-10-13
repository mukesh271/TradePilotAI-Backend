namespace TradePilot.Domain.Entities;

/// <summary>
/// Represents a single OHLCV (Open/High/Low/Close/Volume) candle for an instrument
/// at a specific timeframe and UTC timestamp. Used as the input for technical analysis.
/// </summary>
public class Candle
{
    public long Id { get; set; }

    public int InstrumentId { get; set; }

    public Instrument Instrument { get; set; } = null!;

    /// <summary>
    /// UTC timestamp representing the open time of the candle.
    /// </summary>
    public DateTime TimestampUtc { get; set; }

    /// <summary>
    /// Candle timeframe, e.g. "1m", "5m", "15m", "1h", "1d".
    /// </summary>
    public string Timeframe { get; set; } = string.Empty;

    public decimal Open { get; set; }

    public decimal High { get; set; }

    public decimal Low { get; set; }

    public decimal Close { get; set; }

    public decimal Volume { get; set; }

    public DateTime CreatedAtUtc { get; set; } = DateTime.UtcNow;
}
