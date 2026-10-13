using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradePilot.Api.DTOs;
using TradePilot.Domain.Entities;
using TradePilot.Infrastructure.Data;

namespace TradePilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class InstrumentsController : ControllerBase
{
    private readonly TradePilotDbContext _context;

    public InstrumentsController(TradePilotDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<InstrumentDto>>>> GetAll(
        [FromQuery] string? type = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.Instruments.AsNoTracking();

        if (!string.IsNullOrEmpty(type))
        {
            if (Enum.TryParse<InstrumentType>(type, ignoreCase: true, out var instrumentType))
            {
                query = query.Where(i => i.Type == instrumentType);
            }
        }

        var instruments = await query
            .Select(i => new InstrumentDto
            {
                Id = i.Id,
                Symbol = i.Symbol,
                Exchange = i.Exchange,
                Type = i.Type.ToString(),
                IsActive = i.IsActive,
                CreatedAtUtc = i.CreatedAtUtc,
                UpdatedAtUtc = i.UpdatedAtUtc
            })
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<List<InstrumentDto>>(instruments));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<InstrumentDto>>> GetById(
        int id,
        CancellationToken cancellationToken = default)
    {
        var instrument = await _context.Instruments
            .AsNoTracking()
            .FirstOrDefaultAsync(i => i.Id == id, cancellationToken);

        if (instrument is null)
            return NotFound(new ApiResponse<InstrumentDto>("Instrument not found."));

        var dto = new InstrumentDto
        {
            Id = instrument.Id,
            Symbol = instrument.Symbol,
            Exchange = instrument.Exchange,
            Type = instrument.Type.ToString(),
            IsActive = instrument.IsActive,
            CreatedAtUtc = instrument.CreatedAtUtc,
            UpdatedAtUtc = instrument.UpdatedAtUtc
        };

        return Ok(new ApiResponse<InstrumentDto>(dto));
    }
}
