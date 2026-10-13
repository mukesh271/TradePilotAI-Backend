namespace TradePilot.Api.DTOs;

public class SignalConditionDto
{
    public long Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public decimal Score { get; set; }
    public bool Passed { get; set; }
    public string Explanation { get; set; } = string.Empty;
    public DateTime CreatedAtUtc { get; set; }
}
