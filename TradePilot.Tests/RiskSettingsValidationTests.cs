using Xunit;
using TradePilot.Domain.Entities;

namespace TradePilot.Tests;

public class RiskSettingsValidationTests
{
    [Fact]
    public void RiskSettings_WithDefaultValues_IsValid()
    {
        // Arrange & Act
        var settings = new RiskSettings();

        // Assert
        Assert.Equal(20000, settings.TradingCapital);
        Assert.Equal(1, settings.RiskPerTradePercent);
        Assert.Equal(2, settings.MaximumDailyLossPercent);
        Assert.Equal(75, settings.MinimumSignalScore);
        Assert.True(settings.PaperTradingEnabled);
    }

    [Theory]
    [InlineData(10000)]
    [InlineData(50000)]
    [InlineData(100000)]
    public void RiskSettings_WithValidCapital_IsSet(decimal capital)
    {
        // Arrange & Act
        var settings = new RiskSettings { TradingCapital = capital };

        // Assert
        Assert.Equal(capital, settings.TradingCapital);
    }

    [Theory]
    [InlineData(0.5)]
    [InlineData(1)]
    [InlineData(2)]
    [InlineData(5)]
    public void RiskSettings_WithValidRiskPercent_IsSet(decimal riskPercent)
    {
        // Arrange & Act
        var settings = new RiskSettings { RiskPerTradePercent = riskPercent };

        // Assert
        Assert.Equal(riskPercent, settings.RiskPerTradePercent);
    }

    [Theory]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(80)]
    public void RiskSettings_WithValidMinimumScore_IsSet(decimal minScore)
    {
        // Arrange & Act
        var settings = new RiskSettings { MinimumSignalScore = minScore };

        // Assert
        Assert.Equal(minScore, settings.MinimumSignalScore);
    }

    [Fact]
    public void RiskSettings_CreatedUpdatedAreTracked()
    {
        // Arrange & Act
        var settings = new RiskSettings();

        // Assert
        Assert.NotEqual(default, settings.CreatedAtUtc);
    }

    [Fact]
    public void RiskSettings_UpdatedCanBeSet()
    {
        // Arrange
        var settings = new RiskSettings();
        var newTime = DateTime.UtcNow.AddHours(1);

        // Act
        settings.UpdatedAtUtc = newTime;

        // Assert
        Assert.Equal(newTime, settings.UpdatedAtUtc);
    }
}
