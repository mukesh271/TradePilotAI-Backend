using Microsoft.EntityFrameworkCore;
using TradePilot.Domain.Entities;

namespace TradePilot.Infrastructure.Data;

public static class DbInitializer
{
    public static async Task InitializeAsync(TradePilotDbContext context)
    {
        try
        {
            // Apply any pending migrations
            // If database already exists, this will only apply new migrations
            await context.Database.MigrateAsync();
        }
        catch (Microsoft.Data.SqlClient.SqlException ex) when (ex.Message.Contains("already exists"))
        {
            // Database already exists, that's fine - just continue
            System.Diagnostics.Debug.WriteLine($"Database already exists: {ex.Message}");
        }
        catch (Microsoft.Data.SqlClient.SqlException ex)
        {
            // Log other SQL exceptions and rethrow for visibility
            System.Diagnostics.Debug.WriteLine($"SQL Exception during migration: {ex.Message}");
            throw;
        }
        catch (InvalidOperationException ex) when (ex.Message.Contains("transient"))
        {
            // Transient failures should be handled by retry logic
            System.Diagnostics.Debug.WriteLine($"Transient failure during migration: {ex.Message}");
            throw;
        }

        // Check if data already exists
        if (await context.RiskSettings.AnyAsync())
            return; // Database already seeded

        // Seed RiskSettings
        var riskSettings = new RiskSettings
        {
            TradingCapital = 20000,
            RiskPerTradePercent = 1,
            MaximumDailyLossPercent = 2,
            MinimumSignalScore = 75,
            PaperTradingEnabled = true,
            CreatedAtUtc = DateTime.UtcNow,
            UpdatedAtUtc = DateTime.UtcNow
        };

        context.RiskSettings.Add(riskSettings);

        // Seed Strategies
        var strategies = new List<Strategy>
        {
            new() { Name = "Breakout", Description = "Trade breakouts above resistance", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { Name = "Breakout + Volume", Description = "Breakout confirmed by volume increase", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { Name = "Support Reversal", Description = "Reversal at support levels", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { Name = "Resistance Reversal", Description = "Reversal at resistance levels", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { Name = "Trend Continuation", Description = "Continuation in established trend", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { Name = "VWAP Momentum", Description = "Momentum trades around VWAP", IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { Name = "RSI Reversal", Description = "Reversal when RSI reaches extremes", IsActive = true, CreatedAtUtc = DateTime.UtcNow }
        };

        context.Strategies.AddRange(strategies);

        // Seed Instruments (Indian market focus)
        var instruments = new List<Instrument>
        {
            new() { Symbol = "NIFTY", Exchange = "NSE", Type = InstrumentType.Equity, IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { Symbol = "BANKNIFTY", Exchange = "NSE", Type = InstrumentType.Equity, IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { Symbol = "RELIANCE", Exchange = "NSE", Type = InstrumentType.Equity, IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { Symbol = "TCS", Exchange = "NSE", Type = InstrumentType.Equity, IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { Symbol = "INFY", Exchange = "NSE", Type = InstrumentType.Equity, IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { Symbol = "HDFCBANK", Exchange = "NSE", Type = InstrumentType.Equity, IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { Symbol = "ICICIBANK", Exchange = "NSE", Type = InstrumentType.Equity, IsActive = true, CreatedAtUtc = DateTime.UtcNow },
            new() { Symbol = "SBIN", Exchange = "NSE", Type = InstrumentType.Equity, IsActive = true, CreatedAtUtc = DateTime.UtcNow }
        };

        context.Instruments.AddRange(instruments);

        await context.SaveChangesAsync();
    }
}
