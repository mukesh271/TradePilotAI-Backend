namespace TradePilot.Api.Services;

/// <summary>
/// Result of risk calculations for position sizing.
/// </summary>
public class RiskCalculationResult
{
    public decimal MaximumRiskAmount { get; set; }
    public decimal RiskPerUnit { get; set; }
    public int SuggestedQuantity { get; set; }
    public decimal PotentialLoss { get; set; }
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = [];
}

/// <summary>
/// Service for calculating trading risk and position sizing.
/// </summary>
public interface IRiskManagementService
{
    RiskCalculationResult CalculatePositionRisk(
        decimal capital,
        decimal riskPercent,
        decimal entryPrice,
        decimal stopLoss);
}

public class RiskManagementService : IRiskManagementService
{
    /// <summary>
    /// Calculates position risk and suggested quantity based on capital, risk percentage, and entry/stop-loss prices.
    /// </summary>
    /// <param name="capital">Trading capital available</param>
    /// <param name="riskPercent">Risk as percentage of capital (e.g., 1 = 1%)</param>
    /// <param name="entryPrice">Entry price</param>
    /// <param name="stopLoss">Stop-loss price</param>
    /// <returns>Risk calculation result with suggested quantity</returns>
    public RiskCalculationResult CalculatePositionRisk(
        decimal capital,
        decimal riskPercent,
        decimal entryPrice,
        decimal stopLoss)
    {
        var result = new RiskCalculationResult();

        // Validation
        if (capital <= 0)
            result.Errors.Add("Capital must be greater than 0.");

        if (riskPercent <= 0 || riskPercent > 100)
            result.Errors.Add("Risk percent must be between 0 and 100.");

        if (entryPrice <= 0)
            result.Errors.Add("Entry price must be greater than 0.");

        if (stopLoss <= 0)
            result.Errors.Add("Stop loss price must be greater than 0.");

        if (entryPrice == stopLoss)
            result.Errors.Add("Entry price and stop loss cannot be the same.");

        if (result.Errors.Any())
        {
            result.IsValid = false;
            return result;
        }

        // Calculations
        result.MaximumRiskAmount = capital * (riskPercent / 100);
        result.RiskPerUnit = Math.Abs(entryPrice - stopLoss);

        if (result.RiskPerUnit == 0)
        {
            result.Errors.Add("Risk per unit cannot be zero.");
            result.IsValid = false;
            return result;
        }

        result.SuggestedQuantity = (int)Math.Floor(result.MaximumRiskAmount / result.RiskPerUnit);
        result.PotentialLoss = result.SuggestedQuantity * result.RiskPerUnit;
        result.IsValid = true;

        return result;
    }
}
