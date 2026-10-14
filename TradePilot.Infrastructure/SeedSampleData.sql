-- One-time manual seed script for sample data in all tables (Instruments/Strategies/RiskSettings already seeded).
-- Instrument ids: NIFTY=1, BANKNIFTY=2, RELIANCE=3, TCS=4, INFY=5, HDFCBANK=6, ICICIBANK=7, SBIN=8
-- Strategy ids: Breakout=1, Breakout + Volume=2, Support Reversal=3, Resistance Reversal=4, Trend Continuation=5, VWAP Momentum=6, RSI Reversal=7

SET NOCOUNT ON;

DECLARE @Now DATETIME2 = SYSUTCDATETIME();

-- Signals
INSERT INTO Signals (InstrumentId, StrategyId, Direction, SignalScore, EntryPrice, StopLoss, Target, RiskRewardRatio, TimeFrame, Explanation, Status, CreatedAtUtc, ExpiresAtUtc)
VALUES
(3, 1, N'Bullish', 82.5, 2456.30, 2430.00, 2510.00, 2.03, N'5m', N'Price broke above the prior 20-candle high with strong momentum.', 1, @Now, DATEADD(HOUR, 4, @Now)),
(4, 2, N'Bullish', 78.0, 3850.00, 3800.00, 3950.00, 2.00, N'15m', N'Breakout confirmed with above-average volume.', 2, DATEADD(HOUR, -2, @Now), DATEADD(HOUR, 2, @Now)),
(2, 5, N'Bearish', 60.0, 48250.00, 48600.00, 47600.00, 1.86, N'5m', N'EMA9 crossed below EMA21 confirming continuation of the downtrend.', 4, DATEADD(DAY, -1, @Now), DATEADD(HOUR, 4, DATEADD(DAY, -1, @Now)));

DECLARE @Signal1Id BIGINT = (SELECT Id FROM Signals WHERE InstrumentId = 3 AND StrategyId = 1);
DECLARE @Signal2Id BIGINT = (SELECT Id FROM Signals WHERE InstrumentId = 4 AND StrategyId = 2);
DECLARE @Signal3Id BIGINT = (SELECT Id FROM Signals WHERE InstrumentId = 2 AND StrategyId = 5);

-- SignalConditions
INSERT INTO SignalConditions (SignalId, Name, Score, Passed, Explanation, CreatedAtUtc)
VALUES
(@Signal1Id, N'Close above prior 20-candle high', 40, 1, N'Close 2456.30 above prior high 2450.00.', @Now),
(@Signal1Id, N'RSI above 60', 25, 1, N'RSI14 is 64.2.', @Now),
(@Signal1Id, N'Relative volume above 1.5x', 17.5, 1, N'Relative volume is 1.8x the 20-candle average.', @Now),
(@Signal2Id, N'Close above prior 20-candle high', 40, 1, N'Close 3850.00 above prior high 3835.00.', @Now),
(@Signal2Id, N'Relative volume above 1.5x', 38, 1, N'Relative volume is 2.1x the 20-candle average.', @Now),
(@Signal3Id, N'EMA9 below EMA21', 35, 1, N'EMA9 48180 is below EMA21 48320.', @Now),
(@Signal3Id, N'Close below EMA21', 25, 1, N'Close 48250 is below EMA21 48320.', @Now);

-- PaperTrades
INSERT INTO PaperTrades (SignalId, EntryPrice, ExitPrice, Quantity, ProfitLoss, Status, CreatedAtUtc, ClosedAtUtc)
VALUES
(@Signal1Id, 2456.30, 0, 10, 0, 1, @Now, NULL),
(@Signal2Id, 3850.00, 3950.00, 5, 500.00, 2, DATEADD(HOUR, -2, @Now), DATEADD(HOUR, -1, @Now));

DECLARE @ClosedTradeId BIGINT = (SELECT Id FROM PaperTrades WHERE SignalId = @Signal2Id);

-- TradeResults
INSERT INTO TradeResults (PaperTradeId, IsWinner, GrossProfitLoss, Charges, NetProfitLoss, ExitReason, CreatedAtUtc)
VALUES
(@ClosedTradeId, 1, 500.00, 20.00, 480.00, N'Target hit', DATEADD(HOUR, -1, @Now));

-- Candles (5m timeframe, 30 candles for RELIANCE) so /api/analysis returns data immediately
DECLARE @CandleStart DATETIME2 = DATEADD(MINUTE, 15, DATEADD(HOUR, 9, CAST(CAST(SYSUTCDATETIME() AS DATE) AS DATETIME2)));
DECLARE @i INT = 0;
DECLARE @Price DECIMAL(18,4) = 2400.00;
DECLARE @OpenPrice DECIMAL(18,4), @ClosePrice DECIMAL(18,4), @High DECIMAL(18,4), @Low DECIMAL(18,4), @Volume DECIMAL(18,4), @Change DECIMAL(18,4);

WHILE @i < 30
BEGIN
	SET @OpenPrice = @Price;
	SET @Change = (CAST(ABS(CHECKSUM(NEWID())) % 800 AS DECIMAL(18,4)) / 100.0) - 4.0;
	SET @ClosePrice = @OpenPrice + @Change;
	SET @High = (CASE WHEN @OpenPrice > @ClosePrice THEN @OpenPrice ELSE @ClosePrice END) + (CAST(ABS(CHECKSUM(NEWID())) % 300 AS DECIMAL(18,4)) / 100.0);
	SET @Low = (CASE WHEN @OpenPrice < @ClosePrice THEN @OpenPrice ELSE @ClosePrice END) - (CAST(ABS(CHECKSUM(NEWID())) % 300 AS DECIMAL(18,4)) / 100.0);
	SET @Volume = 10000 + (ABS(CHECKSUM(NEWID())) % 5000);

	INSERT INTO Candles (InstrumentId, Timeframe, TimestampUtc, Open, High, Low, Close, Volume, CreatedAtUtc)
	VALUES (3, N'5m', DATEADD(MINUTE, 5 * @i, @CandleStart), ROUND(@OpenPrice, 2), ROUND(@High, 2), ROUND(@Low, 2), ROUND(@ClosePrice, 2), @Volume, @Now);

	SET @Price = @ClosePrice;
	SET @i = @i + 1;
END

SELECT 'Signals' AS TableName, COUNT(*) AS RecordCount FROM Signals
UNION ALL SELECT 'SignalConditions', COUNT(*) FROM SignalConditions
UNION ALL SELECT 'PaperTrades', COUNT(*) FROM PaperTrades
UNION ALL SELECT 'TradeResults', COUNT(*) FROM TradeResults
UNION ALL SELECT 'Candles', COUNT(*) FROM Candles;
