using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradePilot.Api.DTOs;
using TradePilot.Api.Services;
using TradePilot.Domain.Entities;
using TradePilot.Infrastructure.Data;

namespace TradePilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class CandlesController : ControllerBase
{
    private const int MaxImportBatchSize = 5000;
    private const int MaxQueryLimit = 1000;

    private readonly TradePilotDbContext _context;

    public CandlesController(TradePilotDbContext context)
    {
        _context = context;
    }

    [HttpPost("import")]
    public async Task<ActionResult<ApiResponse<ImportCandlesResultDto>>> Import(
        [FromQuery] int instrumentId,
        [FromBody] ImportCandlesRequestDto request,
        CancellationToken cancellationToken = default)
    {
        if (request.Candles.Count == 0)
            return BadRequest(new ApiResponse<ImportCandlesResultDto>("No candles supplied.", ["Candles list is empty."]));

        if (request.Candles.Count > MaxImportBatchSize)
            return BadRequest(new ApiResponse<ImportCandlesResultDto>(
                "Batch too large.", [$"Maximum batch size is {MaxImportBatchSize} candles."]));

        var instrumentExists = await _context.Instruments
            .AnyAsync(i => i.Id == instrumentId, cancellationToken);

        if (!instrumentExists)
            return NotFound(new ApiResponse<ImportCandlesResultDto>($"Instrument {instrumentId} was not found."));

        var utcNow = DateTime.UtcNow;
        var errors = new List<string>();
        var valid = new List<ImportCandleDto>();

        for (var i = 0; i < request.Candles.Count; i++)
        {
            var candle = request.Candles[i];
            var candleErrors = CandleValidator.Validate(candle, utcNow);
            if (candleErrors.Count > 0)
            {
                errors.AddRange(candleErrors.Select(e => $"Candle[{i}]: {e}"));
                continue;
            }
            valid.Add(candle);
        }

        var duplicateKeys = valid
            .GroupBy(c => (c.Timeframe, Timestamp: c.TimestampUtc))
            .Where(g => g.Count() > 1)
            .Select(g => g.Key)
            .ToHashSet();

        foreach (var key in duplicateKeys)
            errors.Add($"Duplicate candle in request: {key.Timeframe} {key.Timestamp:O}; only the first occurrence is imported.");

        var timeframes = valid.Select(c => c.Timeframe).Distinct().ToList();
        var minTimestamp = valid.Count > 0 ? valid.Min(c => c.TimestampUtc) : DateTime.MinValue;
        var maxTimestamp = valid.Count > 0 ? valid.Max(c => c.TimestampUtc) : DateTime.MinValue;

        var existingKeys = valid.Count == 0
            ? new HashSet<(string, DateTime)>()
            : (await _context.Candles
                .AsNoTracking()
                .Where(c => c.InstrumentId == instrumentId
                            && timeframes.Contains(c.Timeframe)
                            && c.TimestampUtc >= minTimestamp
                            && c.TimestampUtc <= maxTimestamp)
                .Select(c => new { c.Timeframe, c.TimestampUtc })
                .ToListAsync(cancellationToken))
                .Select(c => (c.Timeframe, c.TimestampUtc))
                .ToHashSet();

        var toInsert = new List<Candle>();
        var skipped = 0;
        var seenInRequest = new HashSet<(string, DateTime)>();

        foreach (var candle in valid)
        {
            var key = (candle.Timeframe, candle.TimestampUtc);
            if (existingKeys.Contains(key) || !seenInRequest.Add(key))
            {
                skipped++;
                continue;
            }

            toInsert.Add(new Candle
            {
                InstrumentId = instrumentId,
                Timeframe = candle.Timeframe,
                TimestampUtc = candle.TimestampUtc,
                Open = candle.Open,
                High = candle.High,
                Low = candle.Low,
                Close = candle.Close,
                Volume = candle.Volume,
                CreatedAtUtc = utcNow
            });
        }

        if (toInsert.Count > 0)
        {
            _context.Candles.AddRange(toInsert);
            await _context.SaveChangesAsync(cancellationToken);
        }

        var result = new ImportCandlesResultDto
        {
            Requested = request.Candles.Count,
            Imported = toInsert.Count,
            Skipped = skipped + (request.Candles.Count - valid.Count),
            Errors = errors
        };

        return Ok(new ApiResponse<ImportCandlesResultDto>(result));
    }

    [HttpGet("{instrumentId:int}")]
    public async Task<ActionResult<ApiResponse<List<CandleDto>>>> Get(
        int instrumentId,
        [FromQuery] string timeframe,
        [FromQuery] int limit = 200,
        CancellationToken cancellationToken = default)
    {
        if (!Timeframes.IsValid(timeframe))
            return BadRequest(new ApiResponse<List<CandleDto>>(
                $"Timeframe '{timeframe}' is not supported.",
                [$"Use one of: {string.Join(", ", Timeframes.All)}."]));

        if (limit <= 0 || limit > MaxQueryLimit)
            return BadRequest(new ApiResponse<List<CandleDto>>(
                "Invalid limit.", [$"Limit must be between 1 and {MaxQueryLimit}."]));

        var candles = await _context.Candles
            .AsNoTracking()
            .Where(c => c.InstrumentId == instrumentId && c.Timeframe == timeframe)
            .OrderByDescending(c => c.TimestampUtc)
            .Take(limit)
            .Select(c => new CandleDto
            {
                Id = c.Id,
                InstrumentId = c.InstrumentId,
                Timeframe = c.Timeframe,
                TimestampUtc = c.TimestampUtc,
                Open = c.Open,
                High = c.High,
                Low = c.Low,
                Close = c.Close,
                Volume = c.Volume
            })
            .ToListAsync(cancellationToken);

        candles.Reverse();
        return Ok(new ApiResponse<List<CandleDto>>(candles));
    }
}
