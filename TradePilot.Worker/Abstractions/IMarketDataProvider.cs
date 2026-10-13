using Microsoft.Extensions.Logging;

namespace TradePilot.Worker.Abstractions;

/// <summary>
/// Snapshot of market data for a specific symbol.
/// </summary>
public class MarketDataSnapshot
{
    public string Symbol { get; set; } = string.Empty;
    public decimal CurrentPrice { get; set; }
    public decimal High { get; set; }
    public decimal Low { get; set; }
    public long Volume { get; set; }
    public DateTime Timestamp { get; set; }
}

/// <summary>
/// Abstraction for future market data providers.
/// To be implemented with real market data API (NSE, broker API, etc.).
/// </summary>
public interface IMarketDataProvider
{
    /// <summary>
    /// Gets the latest market data for a symbol.
    /// </summary>
    Task<MarketDataSnapshot?> GetLatestAsync(
        string symbol,
        CancellationToken cancellationToken);
}

/// <summary>
/// Stub implementation for MVP-1. Does not connect to real market data.
/// </summary>
public class StubMarketDataProvider : IMarketDataProvider
{
    private readonly ILogger<StubMarketDataProvider> _logger;

    public StubMarketDataProvider(ILogger<StubMarketDataProvider> logger)
    {
        _logger = logger;
    }

    public Task<MarketDataSnapshot?> GetLatestAsync(
        string symbol,
        CancellationToken cancellationToken)
    {
        _logger.LogInformation("StubMarketDataProvider: Requested data for {Symbol}", symbol);

        // Return null to indicate no real data is available
        // This will be replaced with real market data provider in MVP-2+
        return Task.FromResult<MarketDataSnapshot?>(null);
    }
}
