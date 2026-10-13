using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TradePilot.Api.DTOs;
using TradePilot.Domain.Entities;
using TradePilot.Infrastructure.Data;

namespace TradePilot.Api.Controllers;

[ApiController]
[Route("api/[controller]")]
public class PerformanceController : ControllerBase
{
    private readonly TradePilotDbContext _context;

    public PerformanceController(TradePilotDbContext context)
    {
        _context = context;
    }

    [HttpGet]
    public async Task<ActionResult<ApiResponse<PerformanceDto>>> GetOverallPerformance(
        CancellationToken cancellationToken = default)
    {
        var tradeResults = await _context.TradeResults
            .AsNoTracking()
            .ToListAsync(cancellationToken);

        if (!tradeResults.Any())
            return Ok(new ApiResponse<PerformanceDto>(new PerformanceDto
            {
                CalculatedAtUtc = DateTime.UtcNow,
                TotalTrades = 0,
                WinningTrades = 0,
                LosingTrades = 0,
                WinRate = 0,
                ProfitFactor = 0,
                TotalNetProfitLoss = 0,
                AverageWinningTrade = 0,
                AverageLosingTrade = 0,
                MaximumDrawdown = 0
            }));

        var totalTrades = tradeResults.Count;
        var winningTrades = tradeResults.Count(t => t.IsWinner);
        var losingTrades = totalTrades - winningTrades;
        var winRate = totalTrades > 0 ? (decimal)winningTrades / totalTrades * 100 : 0;
        var totalNetProfitLoss = tradeResults.Sum(t => t.NetProfitLoss);

        var winningTradesAmount = tradeResults.Where(t => t.IsWinner).Sum(t => t.NetProfitLoss);
        var losingTradesAmount = tradeResults.Where(t => !t.IsWinner).Sum(t => Math.Abs(t.NetProfitLoss));

        var profitFactor = losingTradesAmount > 0 && winningTradesAmount > 0
            ? winningTradesAmount / losingTradesAmount
            : 0;

        var averageWinningTrade = winningTrades > 0
            ? winningTradesAmount / winningTrades
            : 0;

        var averageLosingTrade = losingTrades > 0
            ? losingTradesAmount / losingTrades
            : 0;

        var performance = new PerformanceDto
        {
            CalculatedAtUtc = DateTime.UtcNow,
            TotalTrades = totalTrades,
            WinningTrades = winningTrades,
            LosingTrades = losingTrades,
            WinRate = Math.Round(winRate, 2),
            ProfitFactor = Math.Round(profitFactor, 2),
            TotalNetProfitLoss = Math.Round(totalNetProfitLoss, 2),
            AverageWinningTrade = Math.Round(averageWinningTrade, 2),
            AverageLosingTrade = Math.Round(averageLosingTrade, 2),
            MaximumDrawdown = CalculateMaximumDrawdown(tradeResults)
        };

        return Ok(new ApiResponse<PerformanceDto>(performance));
    }

    [HttpGet("strategy/{strategyId}")]
    public async Task<ActionResult<ApiResponse<StrategyPerformanceDto>>> GetStrategyPerformance(
        int strategyId,
        CancellationToken cancellationToken = default)
    {
        var strategy = await _context.Strategies
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Id == strategyId, cancellationToken);

        if (strategy is null)
            return NotFound(new ApiResponse<StrategyPerformanceDto>("Strategy not found."));

        var signals = await _context.Signals
            .AsNoTracking()
            .Where(s => s.StrategyId == strategyId)
            .ToListAsync(cancellationToken);

        var tradeResults = await _context.TradeResults
            .AsNoTracking()
            .Include(tr => tr.PaperTrade)
            .Where(tr => signals.Select(s => s.Id).Contains(tr.PaperTrade.SignalId))
            .ToListAsync(cancellationToken);

        if (!tradeResults.Any())
            return Ok(new ApiResponse<StrategyPerformanceDto>(new StrategyPerformanceDto
            {
                CalculatedAtUtc = DateTime.UtcNow,
                TotalTrades = 0,
                WinningTrades = 0,
                LosingTrades = 0,
                WinRate = 0,
                ProfitFactor = 0,
                TotalNetProfitLoss = 0,
                AverageWinningTrade = 0,
                AverageLosingTrade = 0,
                MaximumDrawdown = 0
            }));

        var totalTrades = tradeResults.Count;
        var winningTrades = tradeResults.Count(t => t.IsWinner);
        var losingTrades = totalTrades - winningTrades;
        var winRate = totalTrades > 0 ? (decimal)winningTrades / totalTrades * 100 : 0;
        var totalNetProfitLoss = tradeResults.Sum(t => t.NetProfitLoss);

        var winningTradesAmount = tradeResults.Where(t => t.IsWinner).Sum(t => t.NetProfitLoss);
        var losingTradesAmount = tradeResults.Where(t => !t.IsWinner).Sum(t => Math.Abs(t.NetProfitLoss));

        var profitFactor = losingTradesAmount > 0 && winningTradesAmount > 0
            ? winningTradesAmount / losingTradesAmount
            : 0;

        var averageWinningTrade = winningTrades > 0 ? winningTradesAmount / winningTrades : 0;
        var averageLosingTrade = losingTrades > 0 ? losingTradesAmount / losingTrades : 0;

        var performanceDto = new StrategyPerformanceDto
        {
            StrategyId = strategyId,
            StrategyName = strategy.Name,
            TotalTrades = totalTrades,
            WinningTrades = winningTrades,
            LosingTrades = losingTrades,
            WinRate = Math.Round(winRate, 2),
            ProfitFactor = Math.Round(profitFactor, 2),
            TotalNetProfitLoss = Math.Round(totalNetProfitLoss, 2),
            AverageWinningTrade = Math.Round(averageWinningTrade, 2),
            AverageLosingTrade = Math.Round(averageLosingTrade, 2),
            MaximumDrawdown = CalculateMaximumDrawdown(tradeResults),
            CalculatedAtUtc = DateTime.UtcNow
        };

        return Ok(new ApiResponse<StrategyPerformanceDto>(performanceDto));
    }

    private static decimal CalculateMaximumDrawdown(List<TradeResult> tradeResults)
    {
        if (!tradeResults.Any())
            return 0;

        decimal peak = 0;
        decimal maxDrawdown = 0;
        decimal runningBalance = 0;

        foreach (var result in tradeResults.OrderBy(t => t.CreatedAtUtc))
        {
            runningBalance += result.NetProfitLoss;

            if (runningBalance > peak)
            {
                peak = runningBalance;
            }

            var drawdown = peak - runningBalance;
            if (drawdown > maxDrawdown)
            {
                maxDrawdown = drawdown;
            }
        }

        return Math.Round(maxDrawdown, 2);
    }
}
