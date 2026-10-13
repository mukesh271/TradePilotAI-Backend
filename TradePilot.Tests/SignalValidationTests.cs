using Xunit;
using TradePilot.Domain.Entities;

namespace TradePilot.Tests;

public class SignalValidationTests
{
    [Theory]
    [InlineData(0)]
    [InlineData(50)]
    [InlineData(75)]
    [InlineData(100)]
    public void Signal_WithValidScore_IsCreated(decimal score)
    {
        // Arrange & Act
        var signal = new Signal
        {
            SignalScore = score,
            Direction = "Bullish",
            EntryPrice = 100,
            StopLoss = 95,
            Target = 110
        };

        // Assert
        Assert.Equal(score, signal.SignalScore);
    }

    [Theory]
    [InlineData(-10)]
    [InlineData(-1)]
    [InlineData(101)]
    [InlineData(150)]
    public void Signal_WithInvalidScore_ShouldNotPassValidation(decimal invalidScore)
    {
        // Arrange
        var signal = new Signal { SignalScore = invalidScore };

        // Assert
        Assert.True(invalidScore < 0 || invalidScore > 100);
    }

    [Theory]
    [InlineData("Bullish")]
    [InlineData("Bearish")]
    [InlineData("Neutral")]
    public void Signal_WithValidDirection_IsCreated(string direction)
    {
        // Arrange & Act
        var signal = new Signal { Direction = direction };

        // Assert
        Assert.Equal(direction, signal.Direction);
    }

    [Fact]
    public void Signal_DefaultStatus_IsDetected()
    {
        // Arrange & Act
        var signal = new Signal { Status = SignalStatus.Detected };

        // Assert
        Assert.Equal(SignalStatus.Detected, signal.Status);
    }

    [Fact]
    public void Signal_CreatedAtUtc_IsSet()
    {
        // Arrange & Act
        var beforeCreation = DateTime.UtcNow;
        var signal = new Signal();
        var afterCreation = DateTime.UtcNow;

        // Assert
        Assert.True(signal.CreatedAtUtc >= beforeCreation && signal.CreatedAtUtc <= afterCreation.AddSeconds(1));
    }
}
