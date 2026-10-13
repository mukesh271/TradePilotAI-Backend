using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradePilot.Api.DTOs;
using TradePilot.Infrastructure.Data;

namespace TradePilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class StrategiesController : ControllerBase
{
    private readonly TradePilotDbContext _context;

    public StrategiesController(TradePilotDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<StrategyDto>>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        var strategies = await _context.Strategies
            .AsNoTracking()
            .Select(s => new StrategyDto
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                IsActive = s.IsActive,
                CreatedAtUtc = s.CreatedAtUtc,
                UpdatedAtUtc = s.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<List<StrategyDto>>(strategies));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<StrategyDto>>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var strategy = await _context.Strategies
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

        if (strategy is null)
            return NotFound(new ApiResponse<StrategyDto>("Strategy not found."));

        var dto = new StrategyDto
        {
            Id = strategy.Id,
            Name = strategy.Name,
            Description = strategy.Description,
            IsActive = strategy.IsActive,
            CreatedAtUtc = strategy.CreatedAtUtc,
            UpdatedAtUtc = strategy.UpdatedAtUtc
        };

        return Ok(new ApiResponse<StrategyDto>(dto));
    }
}
