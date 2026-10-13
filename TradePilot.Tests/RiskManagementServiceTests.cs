using Xunit;
using TradePilot.Api.Services;

namespace TradePilot.Tests;

public class RiskManagementServiceTests
{
    private readonly RiskManagementService _service = new();

    [Fact]
    public void CalculatePositionRisk_WithValidInputs_ReturnsValidResult()
    {
        // Arrange
        decimal capital = 20000;
        decimal riskPercent = 1;
        decimal entryPrice = 100;
        decimal stopLoss = 95;

        // Act
        var result = _service.CalculatePositionRisk(capital, riskPercent, entryPrice, stopLoss);

        // Assert
        Assert.True(result.IsValid);
        Assert.Empty(result.Errors);
        Assert.Equal(200, result.MaximumRiskAmount); // 20000 * 1%
        Assert.Equal(5, result.RiskPerUnit); // |100 - 95|
        Assert.Equal(40, result.SuggestedQuantity); // 200 / 5
        Assert.Equal(200, result.PotentialLoss); // 40 * 5
    }

    [Fact]
    public void CalculatePositionRisk_WithZeroCapital_ReturnsInvalid()
    {
        // Arrange
        var result = _service.CalculatePositionRisk(0, 1, 100, 95);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Capital must be greater than 0.", result.Errors);
    }

    [Fact]
    public void CalculatePositionRisk_WithNegativeCapital_ReturnsInvalid()
    {
        // Arrange
        var result = _service.CalculatePositionRisk(-1000, 1, 100, 95);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Capital must be greater than 0.", result.Errors);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(0)]
    [InlineData(101)]
    [InlineData(150)]
    public void CalculatePositionRisk_WithInvalidRiskPercent_ReturnsInvalid(decimal riskPercent)
    {
        // Arrange & Act
        var result = _service.CalculatePositionRisk(20000, riskPercent, 100, 95);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Risk percent must be between 0 and 100.", result.Errors);
    }

    [Fact]
    public void CalculatePositionRisk_WithZeroEntryPrice_ReturnsInvalid()
    {
        // Arrange
        var result = _service.CalculatePositionRisk(20000, 1, 0, 95);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Entry price must be greater than 0.", result.Errors);
    }

    [Fact]
    public void CalculatePositionRisk_WithZeroStopLoss_ReturnsInvalid()
    {
        // Arrange
        var result = _service.CalculatePositionRisk(20000, 1, 100, 0);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Stop loss price must be greater than 0.", result.Errors);
    }

    [Fact]
    public void CalculatePositionRisk_WhenEntryEqualsStopLoss_ReturnsInvalid()
    {
        // Arrange
        var result = _service.CalculatePositionRisk(20000, 1, 100, 100);

        // Assert
        Assert.False(result.IsValid);
        Assert.Contains("Entry price and stop loss cannot be the same.", result.Errors);
    }

    [Fact]
    public void CalculatePositionRisk_WithEntryBelowStopLoss_CalculatesCorrectly()
    {
        // Arrange - Short trade scenario
        decimal capital = 20000;
        decimal riskPercent = 2;
        decimal entryPrice = 95;
        decimal stopLoss = 100;

        // Act
        var result = _service.CalculatePositionRisk(capital, riskPercent, entryPrice, stopLoss);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(400, result.MaximumRiskAmount); // 20000 * 2%
        Assert.Equal(5, result.RiskPerUnit); // |95 - 100|
        Assert.Equal(80, result.SuggestedQuantity); // 400 / 5
    }

    [Theory]
    [InlineData(100, 1)]
    [InlineData(500, 5)]
    [InlineData(1000, 10)]
    public void CalculatePositionRisk_WithVaryingCapitalAndRisk_CalculatesCorrectly(decimal capital, decimal expectedMaxRisk)
    {
        // Arrange
        decimal riskPercent = 1;
        decimal entryPrice = 100;
        decimal stopLoss = 95;

        // Act
        var result = _service.CalculatePositionRisk(capital, riskPercent, entryPrice, stopLoss);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(expectedMaxRisk, result.MaximumRiskAmount);
    }

    [Fact]
    public void CalculatePositionRisk_WithSmallRiskPerUnit_CalculatesLargeQuantity()
    {
        // Arrange
        decimal capital = 10000;
        decimal riskPercent = 1;
        decimal entryPrice = 100.1m;
        decimal stopLoss = 100;

        // Act
        var result = _service.CalculatePositionRisk(capital, riskPercent, entryPrice, stopLoss);

        // Assert
        Assert.True(result.IsValid);
        Assert.Equal(100, result.MaximumRiskAmount); // 10000 * 1%
        Assert.Equal(0.1m, result.RiskPerUnit);
        Assert.Equal(1000, result.SuggestedQuantity); // 100 / 0.1
    }
}
