using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradePilot.Api.DTOs;
using TradePilot.Domain.Entities;
using TradePilot.Infrastructure.Data;

namespace TradePilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SignalsController : ControllerBase
{
    private readonly TradePilotDbContext _context;

    public SignalsController(TradePilotDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<SignalDto>>>> GetAll(
        [FromQuery] string? status = null,
        [FromQuery] decimal? minimumScore = null,
        [FromQuery] string? instrument = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Signals
            .AsNoTracking()
            .Include(s => s.Instrument)
            .Include(s => s.Strategy)
            .Include(s => s.Conditions)
            .AsQueryable();

        if (!string.IsNullOrEmpty(status))
        {
            if (Enum.TryParse<SignalStatus>(status, ignoreCase: true, out var signalStatus))
            {
                query = query.Where(s => s.Status == signalStatus);
            }
        }

        if (minimumScore.HasValue)
        {
            query = query.Where(s => s.SignalScore >= minimumScore.Value);
        }

        if (!string.IsNullOrEmpty(instrument))
        {
            query = query.Where(s => s.Instrument.Symbol.Contains(instrument));
        }

        var signals = await query
            .OrderByDescending(s => s.CreatedAtUtc)
            .Select(s => MapSignalToDto(s))
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<List<SignalDto>>(signals));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<SignalDto>>> GetById(
        long id,
        CancellationToken cancellationToken = default)
    {
        var signal = await _context.Signals
            .AsNoTracking()
            .Include(s => s.Instrument)
            .Include(s => s.Strategy)
            .Include(s => s.Conditions)
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (signal is null)
            return NotFound(new ApiResponse<SignalDto>("Signal not found."));

        var dto = MapSignalToDto(signal);
        return Ok(new ApiResponse<SignalDto>(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<SignalDto>>> Create(
        CreateSignalDto dto,
        CancellationToken cancellationToken = default)
    {
        var errors = ValidateCreateSignalDto(dto);
        if (errors.Any())
            return BadRequest(new ApiResponse<SignalDto>("Validation failed.", errors));

        var instrument = await _context.Instruments.FindAsync(
            new object[] { dto.InstrumentId },
            cancellationToken: cancellationToken);

        if (instrument is null)
            return BadRequest(new ApiResponse<SignalDto>($"Instrument with ID {dto.InstrumentId} not found."));

        var strategy = await _context.Strategies.FindAsync(
            new object[] { dto.StrategyId },
            cancellationToken: cancellationToken);

        if (strategy is null)
            return BadRequest(new ApiResponse<SignalDto>($"Strategy with ID {dto.StrategyId} not found."));

        var signal = new Signal
        {
            InstrumentId = dto.InstrumentId,
            Instrument = instrument,
            StrategyId = dto.StrategyId,
            Strategy = strategy,
            Direction = dto.Direction,
            SignalScore = dto.SignalScore,
            EntryPrice = dto.EntryPrice,
            StopLoss = dto.StopLoss,
            Target = dto.Target,
            RiskRewardRatio = CalculateRiskRewardRatio(dto.EntryPrice, dto.StopLoss, dto.Target),
            TimeFrame = dto.TimeFrame,
            Explanation = dto.Explanation,
            Status = SignalStatus.Detected,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = dto.ExpiresAtUtc,
            Conditions = dto.Conditions.Select(c => new SignalCondition
            {
                Name = c.Name,
                Score = c.Score,
                Passed = c.Passed,
                Explanation = c.Explanation,
                CreatedAtUtc = DateTime.UtcNow
            }).ToList()
        };

        _context.Signals.Add(signal);
        await _context.SaveChangesAsync(cancellationToken);

        var responseDto = MapSignalToDto(signal);
        return CreatedAtAction(nameof(GetById), new { id = signal.Id }, new ApiResponse<SignalDto>(responseDto));
    }

    private static List<string> ValidateCreateSignalDto(CreateSignalDto dto)
    {
        var errors = new List<string>();

        if (dto.SignalScore < 0 || dto.SignalScore > 100)
            errors.Add("SignalScore must be between 0 and 100.");

        if (dto.EntryPrice <= 0)
            errors.Add("EntryPrice must be greater than 0.");

        if (dto.StopLoss <= 0)
            errors.Add("StopLoss must be greater than 0.");

        if (dto.Target <= 0)
            errors.Add("Target must be greater than 0.");

        if (string.IsNullOrWhiteSpace(dto.Direction))
            errors.Add("Direction is required.");

        return errors;
    }

    private static decimal CalculateRiskRewardRatio(decimal entry, decimal stopLoss, decimal target)
    {
        var risk = Math.Abs(entry - stopLoss);
        var reward = Math.Abs(target - entry);

        if (risk == 0)
            return 0;

        return reward / risk;
    }

    private static SignalDto MapSignalToDto(Signal signal)
    {
        return new SignalDto
        {
            Id = signal.Id,
            InstrumentId = signal.InstrumentId,
            InstrumentSymbol = signal.Instrument.Symbol,
            StrategyId = signal.StrategyId,
            StrategyName = signal.Strategy.Name,
            Direction = signal.Direction,
            SignalScore = signal.SignalScore,
            EntryPrice = signal.EntryPrice,
            StopLoss = signal.StopLoss,
            Target = signal.Target,
            RiskRewardRatio = signal.RiskRewardRatio,
            TimeFrame = signal.TimeFrame,
            Explanation = signal.Explanation,
            Status = signal.Status.ToString(),
            CreatedAtUtc = signal.CreatedAtUtc,
            ExpiresAtUtc = signal.ExpiresAtUtc,
            Conditions = signal.Conditions.Select(c => new SignalConditionDto
            {
                Id = c.Id,
                Name = c.Name,
                Score = c.Score,
                Passed = c.Passed,
                Explanation = c.Explanation,
                CreatedAtUtc = c.CreatedAtUtc
            }).ToList()
        };
    }
}
