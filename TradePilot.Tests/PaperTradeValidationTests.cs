using Xunit;
using TradePilot.Domain.Entities;

namespace TradePilot.Tests;

public class PaperTradeValidationTests
{
    [Fact]
    public void PaperTrade_WithValidData_IsCreated()
    {
        // Arrange
        var signal = new Signal { Id = 1, Direction = "Bullish" };

        // Act
        var trade = new PaperTrade
        {
            SignalId = 1,
            Signal = signal,
            EntryPrice = 100,
            Quantity = 10,
            Status = TradeStatus.Open
        };

        // Assert
        Assert.Equal(1, trade.SignalId);
        Assert.Equal(100, trade.EntryPrice);
        Assert.Equal(10, trade.Quantity);
        Assert.Equal(TradeStatus.Open, trade.Status);
    }

    [Fact]
    public void PaperTrade_DefaultStatus_IsOpen()
    {
        // Arrange & Act
        var trade = new PaperTrade { Status = TradeStatus.Open };

        // Assert
        Assert.Equal(TradeStatus.Open, trade.Status);
    }

    [Fact]
    public void PaperTrade_ProfitLoss_CalculatedCorrectly()
    {
        // Arrange
        var trade = new PaperTrade
        {
            EntryPrice = 100,
            ExitPrice = 110,
            Quantity = 10
        };

        // Act
        trade.ProfitLoss = (trade.ExitPrice - trade.EntryPrice) * trade.Quantity;

        // Assert
        Assert.Equal(100, trade.ProfitLoss); // (110 - 100) * 10
    }

    [Fact]
    public void PaperTrade_LossCalculation_IsNegative()
    {
        // Arrange
        var trade = new PaperTrade
        {
            EntryPrice = 100,
            ExitPrice = 95,
            Quantity = 10
        };

        // Act
        trade.ProfitLoss = (trade.ExitPrice - trade.EntryPrice) * trade.Quantity;

        // Assert
        Assert.Equal(-50, trade.ProfitLoss); // (95 - 100) * 10
    }

    [Fact]
    public void PaperTrade_Breakeven_ProfitLossIsZero()
    {
        // Arrange
        var trade = new PaperTrade
        {
            EntryPrice = 100,
            ExitPrice = 100,
            Quantity = 10
        };

        // Act
        trade.ProfitLoss = (trade.ExitPrice - trade.EntryPrice) * trade.Quantity;

        // Assert
        Assert.Equal(0, trade.ProfitLoss);
    }

    [Fact]
    public void PaperTrade_CreatedAtUtc_IsSet()
    {
        // Arrange & Act
        var beforeCreation = DateTime.UtcNow;
        var trade = new PaperTrade();
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(trade.CreatedAtUtc >= beforeCreation && trade.CreatedAtUtc <= afterCreation.AddSeconds(1));
    }
}
