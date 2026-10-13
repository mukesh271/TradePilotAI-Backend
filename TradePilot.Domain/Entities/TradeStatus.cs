namespace TradePilot.Domain.Entities;

public enum TradeStatus
{
    Open = 1,
    TargetHit = 2,
    StopLossHit = 3,
    ManuallyClosed = 4,
    Expired = 5
}
