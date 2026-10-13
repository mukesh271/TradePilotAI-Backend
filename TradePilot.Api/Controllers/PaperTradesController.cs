using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradePilot.Api.DTOs;
using TradePilot.Domain.Entities;
using TradePilot.Infrastructure.Data;

namespace TradePilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PaperTradesController : ControllerBase
{
    private readonly TradePilotDbContext _context;

    public PaperTradesController(TradePilotDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<List<PaperTradeDto>>>> GetAll(
        CancellationToken cancellationToken = default)
    {
        var trades = await _context.PaperTrades
            .AsNoTracking()
            .Include(t => t.Signal)
            .OrderByDescending(t => t.CreatedAtUtc)
            .Select(t => MapToDto(t))
            .ToListAsync(cancellationToken);

        return Ok(new ApiResponse<List<PaperTradeDto>>(trades));
    }

    [HttpGet("{id}")]
    public async Task<ActionResult<ApiResponse<PaperTradeDto>>> GetById(
        long id,
        CancellationToken cancellationToken = default)
    {
        var trade = await _context.PaperTrades
            .AsNoTracking()
            .Include(t => t.Signal)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (trade is null)
            return NotFound(new ApiResponse<PaperTradeDto>("Paper trade not found."));

        var dto = MapToDto(trade);
        return Ok(new ApiResponse<PaperTradeDto>(dto));
    }

    [HttpPost]
    public async Task<ActionResult<ApiResponse<PaperTradeDto>>> Create(
        CreatePaperTradeDto dto,
        CancellationToken cancellationToken = default)
    {
        // Validation
        var errors = new List<string>();

        if (dto.SignalId <= 0)
            errors.Add("Invalid signal ID.");

        if (dto.Quantity <= 0)
            errors.Add("Quantity must be greater than 0.");

        if (dto.EntryPrice <= 0)
            errors.Add("Entry price must be greater than 0.");

        if (errors.Any())
            return BadRequest(new ApiResponse<PaperTradeDto>("Validation failed.", errors));

        // Check risk settings
        var riskSettings = await _context.RiskSettings
            .AsNoTracking()
            .FirstOrDefaultAsync(cancellationToken);

        if (riskSettings is null || !riskSettings.PaperTradingEnabled)
            return BadRequest(new ApiResponse<PaperTradeDto>("Paper trading is not enabled."));

        // Check signal exists
        var signal = await _context.Signals
            .Include(s => s.Instrument)
            .FirstOrDefaultAsync(s => s.Id == dto.SignalId, cancellationToken);

        if (signal is null)
            return BadRequest(new ApiResponse<PaperTradeDto>($"Signal with ID {dto.SignalId} not found."));

        // Create paper trade
        var paperTrade = new PaperTrade
        {
            SignalId = dto.SignalId,
            Signal = signal,
            EntryPrice = dto.EntryPrice,
            ExitPrice = 0, // Will be set when trade is closed
            Quantity = dto.Quantity,
            ProfitLoss = 0, // Will be calculated when trade is closed
            Status = TradeStatus.Open,
            CreatedAtUtc = DateTime.UtcNow,
            ClosedAtUtc = null
        };

        _context.PaperTrades.Add(paperTrade);
        await _context.SaveChangesAsync(cancellationToken);

        var responseDto = MapToDto(paperTrade);
        return CreatedAtAction(nameof(GetById), new { id = paperTrade.Id }, new ApiResponse<PaperTradeDto>(responseDto));
    }

    [HttpPost("{id}/close")]
    public async Task<ActionResult<ApiResponse<PaperTradeDto>>> Close(
        long id,
        ClosePaperTradeDto dto,
        CancellationToken cancellationToken = default)
    {
        var trade = await _context.PaperTrades
            .Include(t => t.Signal)
            .FirstOrDefaultAsync(t => t.Id == id, cancellationToken);

        if (trade is null)
            return NotFound(new ApiResponse<PaperTradeDto>("Paper trade not found."));

        if (trade.Status != TradeStatus.Open)
            return BadRequest(new ApiResponse<PaperTradeDto>($"Cannot close a trade with status {trade.Status}."));

        var errors = new List<string>();
        if (dto.ExitPrice <= 0)
            errors.Add("Exit price must be greater than 0.");

        if (errors.Any())
            return BadRequest(new ApiResponse<PaperTradeDto>("Validation failed.", errors));

        // Calculate P&L
        var profitLoss = (dto.ExitPrice - trade.EntryPrice) * trade.Quantity;

        trade.ExitPrice = dto.ExitPrice;
        trade.ProfitLoss = profitLoss;
        trade.ClosedAtUtc = DateTime.UtcNow;

        // Determine trade status
        if (dto.ExitPrice >= trade.Signal.Target)
        {
            trade.Status = TradeStatus.TargetHit;
        }
        else if (dto.ExitPrice <= trade.Signal.StopLoss)
        {
            trade.Status = TradeStatus.StopLossHit;
        }
        else
        {
            trade.Status = TradeStatus.ManuallyClosed;
        }

        // Create trade result
        var tradeResult = new TradeResult
        {
            PaperTradeId = trade.Id,
            PaperTrade = trade,
            IsWinner = profitLoss > 0,
            GrossProfitLoss = profitLoss,
            Charges = 0, // Can be configured later
            NetProfitLoss = profitLoss,
            ExitReason = dto.ExitReason,
            CreatedAtUtc = DateTime.UtcNow
        };

        _context.TradeResults.Add(tradeResult);
        await _context.SaveChangesAsync(cancellationToken);

        var responseDto = MapToDto(trade);
        return Ok(new ApiResponse<PaperTradeDto>(responseDto));
    }

    private static PaperTradeDto MapToDto(PaperTrade trade)
    {
        return new PaperTradeDto
        {
            Id = trade.Id,
            SignalId = trade.SignalId,
            SignalDirection = trade.Signal.Direction,
            EntryPrice = trade.EntryPrice,
            ExitPrice = trade.ExitPrice,
            Quantity = trade.Quantity,
            ProfitLoss = trade.ProfitLoss,
            Status = trade.Status.ToString(),
            CreatedAtUtc = trade.CreatedAtUtc,
            ClosedAtUtc = trade.ClosedAtUtc
        };
    }
}
