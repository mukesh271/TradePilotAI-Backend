using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradePilot.Api.DTOs;
using TradePilot.Api.Services;
using TradePilot.Domain.Entities;
using TradePilot.Infrastructure.Data;

namespace TradePilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AnalysisController : ControllerBase
{
    private const int DefaultCandleWindow = 300;
    private const int MaxCandleWindow = 1000;

    private readonly TradePilotDbContext _context;
    private readonly IIndicatorCalculationService _indicators;
    private readonly IStrategyEvaluationService _strategies;

    public AnalysisController(
        TradePilotDbContext context,
        IIndicatorCalculationService indicators,
        IStrategyEvaluationService strategies)
    {
        _context = context;
        _indicators = indicators;
        _strategies = strategies;
    }

    [HttpGet("{instrumentId:int}")]
    public async Task<ActionResult<ApiResponse<TechnicalAnalysisResultDto>>> GetLatest(
        int instrumentId,
        [FromQuery] string timeframe,
        CancellationToken cancellationToken = default)
    {
        return await Analyze(instrumentId, timeframe, DefaultCandleWindow, null, null, cancellationToken);
    }

    [HttpGet("{instrumentId:int}/history")]
    public async Task<ActionResult<ApiResponse<List<TechnicalAnalysisResultDto>>>> GetHistory(
        int instrumentId,
        [FromQuery] string timeframe,
        [FromQuery] int limit = 50,
        CancellationToken cancellationToken = default)
    {
        if (!Timeframes.IsValid(timeframe))
            return BadRequest(new ApiResponse<List<TechnicalAnalysisResultDto>>(
                $"Timeframe '{timeframe}' is not supported.",
                [$"Use one of: {string.Join(", ", Timeframes.All)}."]));

        if (limit <= 0 || limit > 200)
            return BadRequest(new ApiResponse<List<TechnicalAnalysisResultDto>>(
                "Invalid limit.", ["Limit must be between 1 and 200."]));

        var instrument = await _context.Instruments.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == instrumentId, cancellationToken);
        if (instrument is null)
            return NotFound(new ApiResponse<List<TechnicalAnalysisResultDto>>($"Instrument {instrumentId} was not found."));

        var candles = await LoadCandles(instrumentId, timeframe, DefaultCandleWindow + limit, cancellationToken);
        var results = new List<TechnicalAnalysisResultDto>();

        for (var end = Math.Max(1, candles.Count - limit + 1); end <= candles.Count; end++)
        {
            var slice = candles.Take(end).ToList();
            results.Add(BuildResult(instrument.Id, instrument.Symbol, timeframe, slice, new StrategySettings()));
        }

        results.Reverse();
        return Ok(new ApiResponse<List<TechnicalAnalysisResultDto>>(results));
    }

    [HttpPost("{instrumentId:int}/calculate")]
    public async Task<ActionResult<ApiResponse<TechnicalAnalysisResultDto>>> Calculate(
        int instrumentId,
        [FromBody] CalculateAnalysisRequestDto request,
        CancellationToken cancellationToken = default)
    {
        return await Analyze(
            instrumentId,
            request.Timeframe,
            request.Limit ?? DefaultCandleWindow,
            request.RelativeVolumeThreshold,
            request.BreakoutLookback,
            cancellationToken);
    }

    private async Task<ActionResult<ApiResponse<TechnicalAnalysisResultDto>>> Analyze(
        int instrumentId,
        string timeframe,
        int limit,
        decimal? relativeVolumeThreshold,
        int? breakoutLookback,
        CancellationToken cancellationToken)
    {
        if (!Timeframes.IsValid(timeframe))
            return BadRequest(new ApiResponse<TechnicalAnalysisResultDto>(
                $"Timeframe '{timeframe}' is not supported.",
                [$"Use one of: {string.Join(", ", Timeframes.All)}."]));

        if (limit <= 0 || limit > MaxCandleWindow)
            return BadRequest(new ApiResponse<TechnicalAnalysisResultDto>(
                "Invalid limit.", [$"Limit must be between 1 and {MaxCandleWindow}."]));

        if (relativeVolumeThreshold is <= 0)
            return BadRequest(new ApiResponse<TechnicalAnalysisResultDto>(
                "Invalid relative volume threshold.", ["RelativeVolumeThreshold must be greater than 0."]));

        if (breakoutLookback is <= 0 or > 500)
            return BadRequest(new ApiResponse<TechnicalAnalysisResultDto>(
                "Invalid breakout lookback.", ["BreakoutLookback must be between 1 and 500."]));

        var instrument = await _context.Instruments.AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == instrumentId, cancellationToken);
        if (instrument is null)
            return NotFound(new ApiResponse<TechnicalAnalysisResultDto>($"Instrument {instrumentId} was not found."));

        var candles = await LoadCandles(instrumentId, timeframe, limit, cancellationToken);
        if (candles.Count == 0)
            return NotFound(new ApiResponse<TechnicalAnalysisResultDto>(
                $"No stored {timeframe} candles for instrument {instrumentId}."));

        var settings = new StrategySettings
        {
            BreakoutLookback = breakoutLookback ?? new StrategySettings().BreakoutLookback,
            RelativeVolumeThreshold = relativeVolumeThreshold ?? new StrategySettings().RelativeVolumeThreshold
        };

        return Ok(new ApiResponse<TechnicalAnalysisResultDto>(
            BuildResult(instrument.Id, instrument.Symbol, timeframe, candles, settings)));
    }

    private async Task<List<Candle>> LoadCandles(
        int instrumentId,
        string timeframe,
        int limit,
        CancellationToken cancellationToken)
    {
        var recent = await _context.Candles
            .AsNoTracking()
            .Where(c => c.InstrumentId == instrumentId && c.Timeframe == timeframe)
            .OrderByDescending(c => c.TimestampUtc)
            .Take(limit)
            .ToListAsync(cancellationToken);

        recent.Reverse();
        return recent;
    }

    private TechnicalAnalysisResultDto BuildResult(
        int instrumentId,
        string symbol,
        string timeframe,
        List<Candle> candles,
        StrategySettings settings)
    {
        var indicatorSettings = new IndicatorSettings();
        var snapshot = _indicators.CalculateSnapshot(candles, timeframe, indicatorSettings);
        var last = candles[^1];

        return new TechnicalAnalysisResultDto
        {
            InstrumentId = instrumentId,
            Symbol = symbol,
            Timeframe = timeframe,
            LastCandleTimestampUtc = last.TimestampUtc,
            LastClose = last.Close,
            Sma20 = snapshot.Sma20,
            Sma50 = snapshot.Sma50,
            Ema9 = snapshot.Ema9,
            Ema21 = snapshot.Ema21,
            Rsi14 = snapshot.Rsi14,
            MacdLine = snapshot.MacdLine,
            MacdSignal = snapshot.MacdSignal,
            MacdHistogram = snapshot.MacdHistogram,
            Vwap = snapshot.Vwap,
            Atr14 = snapshot.Atr14,
            VolumeSma20 = snapshot.VolumeSma20,
            RelativeVolume = snapshot.RelativeVolume,
            CreatedAtUtc = DateTime.UtcNow,
            Setups = _strategies.Evaluate(candles, snapshot, settings).ToList()
        };
    }
}
