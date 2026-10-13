namespace TradePilot.Api.DTOs;

public class CandleDto
{
    public long Id { get; set; }
    public int InstrumentId { get; set; }
    public string Timeframe { get; set; } = string.Empty;
    public DateTime TimestampUtc { get; set; }
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}

public class ImportCandleDto
{
    public DateTime TimestampUtc { get; set; }
    public string Timeframe { get; set; } = string.Empty;
    public decimal Open { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public decimal Close { get; set; }
    public decimal Volume { get; set; }
}

public class ImportCandlesRequestDto
{
    public List<ImportCandleDto> Candles { get; set; } = [];
}

public class ImportCandlesResultDto
{
    public int Requested { get; set; }
    public int Imported { get; set; }
    public int Skipped { get; set; }
    public List<string> Errors { get; set; } = [];
}
