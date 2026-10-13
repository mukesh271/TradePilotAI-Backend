using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradePilot.Api.DTOs;
using TradePilot.Infrastructure.Data;

namespace TradePilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class RiskSettingsController : ControllerBase
{
    private readonly TradePilotDbContext _context;

    public RiskSettingsController(TradePilotDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<RiskSettingsDto>>> Get(
        CancellationToken cancellationToken = default)
    {
        var riskSettings = await _context.RiskSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (riskSettings is null)
            return NotFound(new ApiResponse<RiskSettingsDto>("Risk settings not found."));

        var dto = new RiskSettingsDto
        {
            Id = riskSettings.Id,
            TradingCapital = riskSettings.TradingCapital,
            RiskPerTradePercent = riskSettings.RiskPerTradePercent,
            MaximumDailyLossPercent = riskSettings.MaximumDailyLossPercent,
            MinimumSignalScore = riskSettings.MinimumSignalScore,
            PaperTradingEnabled = riskSettings.PaperTradingEnabled,
            CreatedAtUtc = riskSettings.CreatedAtUtc,
            UpdatedAtUtc = riskSettings.UpdatedAtUtc
        };

        return Ok(new ApiResponse<RiskSettingsDto>(dto));
    }

    [HttpPut]
    public async Task<ActionResult<ApiResponse<RiskSettingsDto>>> Update(
        UpdateRiskSettingsDto dto,
        CancellationToken cancellationToken = default)
    {
        var riskSettings = await _context.RiskSettings.FirstOrDefaultAsync(cancellationToken);

        if (riskSettings is null)
            return NotFound(new ApiResponse<RiskSettingsDto>("Risk settings not found."));

        // Validation
        var errors = new List<string>();
        if (dto.TradingCapital <= 0)
            errors.Add("TradingCapital must be greater than 0.");
        if (dto.RiskPerTradePercent <= 0 || dto.RiskPerTradePercent > 100)
            errors.Add("RiskPerTradePercent must be between 0 and 100.");
        if (dto.MaximumDailyLossPercent <= 0 || dto.MaximumDailyLossPercent > 100)
            errors.Add("MaximumDailyLossPercent must be between 0 and 100.");
        if (dto.MinimumSignalScore < 0 || dto.MinimumSignalScore > 100)
            errors.Add("MinimumSignalScore must be between 0 and 100.");

        if (errors.Any())
            return BadRequest(new ApiResponse<RiskSettingsDto>("Validation failed.", errors));

        riskSettings.TradingCapital = dto.TradingCapital;
        riskSettings.RiskPerTradePercent = dto.RiskPerTradePercent;
        riskSettings.MaximumDailyLossPercent = dto.MaximumDailyLossPercent;
        riskSettings.MinimumSignalScore = dto.MinimumSignalScore;
        riskSettings.PaperTradingEnabled = dto.PaperTradingEnabled;
        riskSettings.UpdatedAtUtc = DateTime.UtcNow;

        await _context.SaveChangesAsync(cancellationToken);

        var responseDto = new RiskSettingsDto
        {
            Id = riskSettings.Id,
            TradingCapital = riskSettings.TradingCapital,
            RiskPerTradePercent = riskSettings.RiskPerTradePercent,
            MaximumDailyLossPercent = riskSettings.MaximumDailyLossPercent,
            MinimumSignalScore = riskSettings.MinimumSignalScore,
            PaperTradingEnabled = riskSettings.PaperTradingEnabled,
            CreatedAtUtc = riskSettings.CreatedAtUtc,
            UpdatedAtUtc = riskSettings.UpdatedAtUtc
        };

        return Ok(new ApiResponse<RiskSettingsDto>(responseDto));
    }
}
