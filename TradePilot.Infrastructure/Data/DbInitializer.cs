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

        // Seed Signals with conditions (ids are now available after the save above)
        var breakoutStrategy = strategies.First(s => s.Name == "Breakout");
        var breakoutVolumeStrategy = strategies.First(s => s.Name == "Breakout + Volume");
        var trendStrategy = strategies.First(s => s.Name == "Trend Continuation");

        var reliance = instruments.First(i => i.Symbol == "RELIANCE");
        var tcs = instruments.First(i => i.Symbol == "TCS");
        var bankNifty = instruments.First(i => i.Symbol == "BANKNIFTY");

        var signal1 = new Signal
        {
            InstrumentId = reliance.Id,
            Instrument = reliance,
            StrategyId = breakoutStrategy.Id,
            Strategy = breakoutStrategy,
            Direction = "Bullish",
            SignalScore = 82.5m,
            EntryPrice = 2456.30m,
            StopLoss = 2430.00m,
            Target = 2510.00m,
            RiskRewardRatio = 2.03m,
            TimeFrame = "5m",
            Explanation = "Price broke above the prior 20-candle high with strong momentum.",
            Status = SignalStatus.Detected,
            CreatedAtUtc = DateTime.UtcNow,
            ExpiresAtUtc = DateTime.UtcNow.AddHours(4),
            Conditions =
            [
                new SignalCondition { Name = "Close above prior 20-candle high", Score = 40, Passed = true, Explanation = "Close 2456.30 above prior high 2450.00.", CreatedAtUtc = DateTime.UtcNow },
                new SignalCondition { Name = "RSI above 60", Score = 25, Passed = true, Explanation = "RSI14 is 64.2.", CreatedAtUtc = DateTime.UtcNow },
                new SignalCondition { Name = "Relative volume above 1.5x", Score = 17.5m, Passed = true, Explanation = "Relative volume is 1.8x the 20-candle average.", CreatedAtUtc = DateTime.UtcNow }
            ]
        };

        var signal2 = new Signal
        {
            InstrumentId = tcs.Id,
            Instrument = tcs,
            StrategyId = breakoutVolumeStrategy.Id,
            Strategy = breakoutVolumeStrategy,
            Direction = "Bullish",
            SignalScore = 78.0m,
            EntryPrice = 3850.00m,
            StopLoss = 3800.00m,
            Target = 3950.00m,
            RiskRewardRatio = 2.0m,
            TimeFrame = "15m",
            Explanation = "Breakout confirmed with above-average volume.",
            Status = SignalStatus.Alerted,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            ExpiresAtUtc = DateTime.UtcNow.AddHours(2),
            Conditions =
            [
                new SignalCondition { Name = "Close above prior 20-candle high", Score = 40, Passed = true, Explanation = "Close 3850.00 above prior high 3835.00.", CreatedAtUtc = DateTime.UtcNow.AddHours(-2) },
                new SignalCondition { Name = "Relative volume above 1.5x", Score = 38, Passed = true, Explanation = "Relative volume is 2.1x the 20-candle average.", CreatedAtUtc = DateTime.UtcNow.AddHours(-2) }
            ]
        };

        var signal3 = new Signal
        {
            InstrumentId = bankNifty.Id,
            Instrument = bankNifty,
            StrategyId = trendStrategy.Id,
            Strategy = trendStrategy,
            Direction = "Bearish",
            SignalScore = 60.0m,
            EntryPrice = 48250.00m,
            StopLoss = 48600.00m,
            Target = 47600.00m,
            RiskRewardRatio = 1.86m,
            TimeFrame = "5m",
            Explanation = "EMA9 crossed below EMA21 confirming continuation of the downtrend.",
            Status = SignalStatus.Expired,
            CreatedAtUtc = DateTime.UtcNow.AddDays(-1),
            ExpiresAtUtc = DateTime.UtcNow.AddDays(-1).AddHours(4),
            Conditions =
            [
                new SignalCondition { Name = "EMA9 below EMA21", Score = 35, Passed = true, Explanation = "EMA9 48180 is below EMA21 48320.", CreatedAtUtc = DateTime.UtcNow.AddDays(-1) },
                new SignalCondition { Name = "Close below EMA21", Score = 25, Passed = true, Explanation = "Close 48250 is below EMA21 48320.", CreatedAtUtc = DateTime.UtcNow.AddDays(-1) }
            ]
        };

        context.Signals.AddRange(signal1, signal2, signal3);
        await context.SaveChangesAsync();

        // Seed PaperTrades: one open, one closed with a trade result
        var openTrade = new PaperTrade
        {
            SignalId = signal1.Id,
            Signal = signal1,
            EntryPrice = 2456.30m,
            ExitPrice = 0,
            Quantity = 10,
            ProfitLoss = 0,
            Status = TradeStatus.Open,
            CreatedAtUtc = DateTime.UtcNow,
            ClosedAtUtc = null
        };

        var closedTrade = new PaperTrade
        {
            SignalId = signal2.Id,
            Signal = signal2,
            EntryPrice = 3850.00m,
            ExitPrice = 3950.00m,
            Quantity = 5,
            ProfitLoss = 500.00m,
            Status = TradeStatus.TargetHit,
            CreatedAtUtc = DateTime.UtcNow.AddHours(-2),
            ClosedAtUtc = DateTime.UtcNow.AddHours(-1)
        };

        context.PaperTrades.AddRange(openTrade, closedTrade);
        await context.SaveChangesAsync();

        context.TradeResults.Add(new TradeResult
        {
            PaperTradeId = closedTrade.Id,
            PaperTrade = closedTrade,
            IsWinner = true,
            GrossProfitLoss = 500.00m,
            Charges = 20.00m,
            NetProfitLoss = 480.00m,
            ExitReason = "Target hit",
            CreatedAtUtc = DateTime.UtcNow.AddHours(-1)
        });

        // Seed Candles (5m timeframe, last 30 candles) for RELIANCE so /api/analysis has data immediately
        var candleStart = DateTime.UtcNow.Date.AddHours(9).AddMinutes(15);
        var candles = new List<Candle>();
        var price = 2400.00m;
        var random = new Random(42);

        for (var i = 0; i < 30; i++)
        {
            var open = price;
            var change = (decimal)(random.NextDouble() * 10 - 4);
            var close = open + change;
            var high = Math.Max(open, close) + (decimal)(random.NextDouble() * 3);
            var low = Math.Min(open, close) - (decimal)(random.NextDouble() * 3);
            var volume = 10000 + random.Next(0, 5000);

            candles.Add(new Candle
            {
                InstrumentId = reliance.Id,
                Timeframe = Timeframes.FiveMinutes,
                TimestampUtc = candleStart.AddMinutes(5 * i),
                Open = Math.Round(open, 2),
                High = Math.Round(high, 2),
                Low = Math.Round(low, 2),
                Close = Math.Round(close, 2),
                Volume = volume,
                CreatedAtUtc = DateTime.UtcNow
            });

            price = close;
        }

        context.Candles.AddRange(candles);

        await context.SaveChangesAsync();
    }
}
