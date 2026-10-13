using Microsoft.EntityFrameworkCore;
using TradePilot.Domain.Entities;

namespace TradePilot.Infrastructure.Data;

public class TradePilotDbContext : DbContext
{
    public TradePilotDbContext(
        DbContextOptions<TradePilotDbContext> options)
        : base(options)
    {
    }

    public DbSet<Instrument> Instruments => Set<Instrument>();

    public DbSet<RiskSettings> RiskSettings => Set<RiskSettings>();

    public DbSet<Strategy> Strategies => Set<Strategy>();

    public DbSet<Signal> Signals => Set<Signal>();

    public DbSet<SignalCondition> SignalConditions => Set<SignalCondition>();

    public DbSet<PaperTrade> PaperTrades => Set<PaperTrade>();

    public DbSet<TradeResult> TradeResults => Set<TradeResult>();

    public DbSet<StrategyPerformance> StrategyPerformances => Set<StrategyPerformance>();

    public DbSet<News> News => Set<News>();

    public DbSet<Candle> Candles => Set<Candle>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Instrument configuration
        modelBuilder.Entity<Instrument>()
            .Property(x => x.Symbol)
            .HasMaxLength(100)
            .IsRequired();

        modelBuilder.Entity<Instrument>()
            .HasIndex(x => x.Symbol);

        modelBuilder.Entity<Instrument>()
            .HasIndex(x => x.Exchange);

        modelBuilder.Entity<Instrument>()
            .HasIndex(x => x.IsActive);

        // Strategy configuration
        modelBuilder.Entity<Strategy>()
            .Property(x => x.Name)
            .HasMaxLength(100)
            .IsRequired();

        // RiskSettings configuration
        modelBuilder.Entity<RiskSettings>()
            .Property(x => x.TradingCapital)
            .HasPrecision(18, 4);

        modelBuilder.Entity<RiskSettings>()
            .Property(x => x.RiskPerTradePercent)
            .HasPrecision(5, 2);

        modelBuilder.Entity<RiskSettings>()
            .Property(x => x.MaximumDailyLossPercent)
            .HasPrecision(5, 2);

        modelBuilder.Entity<RiskSettings>()
            .Property(x => x.MinimumSignalScore)
            .HasPrecision(5, 2);

        // Signal configuration
        modelBuilder.Entity<Signal>()
            .Property(x => x.SignalScore)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Signal>()
            .Property(x => x.EntryPrice)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Signal>()
            .Property(x => x.StopLoss)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Signal>()
            .Property(x => x.Target)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Signal>()
            .Property(x => x.RiskRewardRatio)
            .HasPrecision(5, 2);

        modelBuilder.Entity<Signal>()
            .HasIndex(x => x.InstrumentId);

        modelBuilder.Entity<Signal>()
            .HasIndex(x => x.StrategyId);

        modelBuilder.Entity<Signal>()
            .HasIndex(x => x.Status);

        modelBuilder.Entity<Signal>()
            .HasIndex(x => x.CreatedAtUtc);

        modelBuilder.Entity<Signal>()
            .HasIndex(x => x.SignalScore);

        // SignalCondition configuration
        modelBuilder.Entity<SignalCondition>()
            .Property(x => x.Score)
            .HasPrecision(5, 2);

        // PaperTrade configuration
        modelBuilder.Entity<PaperTrade>()
            .Property(x => x.EntryPrice)
            .HasPrecision(18, 4);

        modelBuilder.Entity<PaperTrade>()
            .Property(x => x.ExitPrice)
            .HasPrecision(18, 4);

        modelBuilder.Entity<PaperTrade>()
            .Property(x => x.ProfitLoss)
            .HasPrecision(18, 4);

        modelBuilder.Entity<PaperTrade>()
            .HasIndex(x => x.SignalId);

        modelBuilder.Entity<PaperTrade>()
            .HasIndex(x => x.Status);

        modelBuilder.Entity<PaperTrade>()
            .HasIndex(x => x.CreatedAtUtc);

        // TradeResult configuration
        modelBuilder.Entity<TradeResult>()
            .Property(x => x.GrossProfitLoss)
            .HasPrecision(18, 4);

        modelBuilder.Entity<TradeResult>()
            .Property(x => x.Charges)
            .HasPrecision(18, 4);

        modelBuilder.Entity<TradeResult>()
            .Property(x => x.NetProfitLoss)
            .HasPrecision(18, 4);

        // StrategyPerformance configuration
        modelBuilder.Entity<StrategyPerformance>()
            .Property(x => x.WinRate)
            .HasPrecision(5, 2);

        modelBuilder.Entity<StrategyPerformance>()
            .Property(x => x.ProfitFactor)
            .HasPrecision(18, 4);

        modelBuilder.Entity<StrategyPerformance>()
            .Property(x => x.TotalNetProfitLoss)
            .HasPrecision(18, 4);

        modelBuilder.Entity<StrategyPerformance>()
            .Property(x => x.AverageWinningTrade)
            .HasPrecision(18, 4);

        modelBuilder.Entity<StrategyPerformance>()
            .Property(x => x.AverageLosingTrade)
            .HasPrecision(18, 4);

        modelBuilder.Entity<StrategyPerformance>()
            .Property(x => x.MaximumDrawdown)
            .HasPrecision(18, 4);

        // News configuration
        modelBuilder.Entity<News>()
            .Property(x => x.Title)
            .HasMaxLength(500)
            .IsRequired();

        modelBuilder.Entity<News>()
            .Property(x => x.Source)
            .HasMaxLength(200)
            .IsRequired();

        modelBuilder.Entity<News>()
            .Property(x => x.RelevanceScore)
            .HasPrecision(5, 2);

        modelBuilder.Entity<News>()
            .HasIndex(x => x.PublishedAtUtc);

        modelBuilder.Entity<News>()
            .HasIndex(x => x.Sentiment);

        // Candle configuration
        modelBuilder.Entity<Candle>()
            .Property(x => x.Timeframe)
            .HasMaxLength(10)
            .IsRequired();

        modelBuilder.Entity<Candle>()
            .Property(x => x.Open)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Candle>()
            .Property(x => x.High)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Candle>()
            .Property(x => x.Low)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Candle>()
            .Property(x => x.Close)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Candle>()
            .Property(x => x.Volume)
            .HasPrecision(18, 4);

        modelBuilder.Entity<Candle>()
            .HasOne(x => x.Instrument)
            .WithMany()
            .HasForeignKey(x => x.InstrumentId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<Candle>()
            .HasIndex(x => new { x.InstrumentId, x.Timeframe, x.TimestampUtc })
            .IsUnique();
    }
}
