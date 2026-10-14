# TradePilot AI — Full API Reference (Frontend Integration)

Base URL: `http://localhost:<port>/api` (CORS currently allows `http://localhost:4200`).

All responses use the standard envelope:

```json
{
  "success": true,
  "data": { },
  "message": null,
  "errors": null
}
```

`success: false` responses set `message` (and optionally `errors: string[]`), `data` is `null`.

This maps directly to the frontend `API_ENDPOINTS` keys:

```ts
export const API_ENDPOINTS = {
  instruments: '/instruments',
  strategies: '/strategies',
  riskSettings: '/RiskSettings',
  signals: '/signals',
  paperTrades: '/paper-trades',
  performance: '/performance',
  candles: '/candles',
  candleImport: '/candles/import',
  analysis: '/analysis'
};
```

> Note: actual route casing is `/riskSettings` (ASP.NET routing is case-insensitive, so `/RiskSettings` also works). The `paperTrades` endpoint does **not** use a hyphen — see section 5 below; update the frontend constant from `/paper-trades` to `/paperTrades`.

---

## Frontend Action Items (changes required to bind to current backend)

1. **Update `API_ENDPOINTS.paperTrades`** from `'/paper-trades'` to `'/paperTrades'` (controller route has no hyphen).
2. **CORS**: API only allows requests from `http://localhost:4200` currently (configured in `appsettings.json` → `Cors:AllowedOrigins`). If the frontend runs on a different port/host, ask backend to add it.
3. **New MVP-2 endpoints available** to bind for the technical-analysis page: `candles`, `candleImport`, `analysis` (sections 7 and 8 below) — these did not exist before and require candle data to be imported before analysis will return results.
4. **Nullable indicator fields**: `sma20`, `sma50`, `ema9`, `ema21`, `rsi14`, `macdLine`, `macdSignal`, `macdHistogram`, `vwap`, `atr14`, `volumeSma20`, `relativeVolume` on the analysis response can be `null` when there isn't enough candle history — handle this in the UI (see "Notes for frontend integration" at the bottom).
5. **Analysis setups are not trading signals** — do not wire `setups[]` results into the existing Signals/PaperTrades UI flows; they are a separate read-only research view.

---

## 1. Instruments — `GET {instruments}`

### `GET /api/instruments`
Query: `type` (optional, one of the `InstrumentType` enum names, e.g. `Equity`, `Index`, `Future`, `Option` — check `TradePilot.Domain/Entities/InstrumentType.cs` for exact values used in your DB)

Response: `ApiResponse<InstrumentDto[]>`

```json
{ "id": 1, "symbol": "RELIANCE", "exchange": "NSE", "type": "Equity", "isActive": true, "createdAtUtc": "...", "updatedAtUtc": "..." }
```

### `GET /api/instruments/{id}`
Response: `ApiResponse<InstrumentDto>`. `404` if not found.

---

## 2. Strategies — `GET {strategies}`

### `GET /api/strategies`
Response: `ApiResponse<StrategyDto[]>`

```json
{ "id": 1, "name": "Breakout", "description": "...", "isActive": true, "createdAtUtc": "...", "updatedAtUtc": "..." }
```

### `GET /api/strategies/{id}`
Response: `ApiResponse<StrategyDto>`. `404` if not found.

---

## 3. Risk Settings — `{riskSettings}` (route: `/api/RiskSettings`)

### `GET /api/riskSettings`
Response: `ApiResponse<RiskSettingsDto>`. `404` if no row exists yet.

```json
{
  "id": 1,
  "tradingCapital": 100000,
  "riskPerTradePercent": 2,
  "maximumDailyLossPercent": 5,
  "minimumSignalScore": 70,
  "paperTradingEnabled": true,
  "createdAtUtc": "...",
  "updatedAtUtc": "..."
}
```

### `PUT /api/riskSettings`
Request body (`UpdateRiskSettingsDto`):

```json
{
  "tradingCapital": 100000,
  "riskPerTradePercent": 2,
  "maximumDailyLossPercent": 5,
  "minimumSignalScore": 70,
  "paperTradingEnabled": true
}
```

Validation (`400` with `errors[]` if violated):
- `tradingCapital > 0`
- `0 < riskPerTradePercent <= 100`
- `0 < maximumDailyLossPercent <= 100`
- `0 <= minimumSignalScore <= 100`

Response: `ApiResponse<RiskSettingsDto>`. `404` if no settings row exists yet (seeded by default on first run).

---

## 4. Signals — `{signals}`

### `GET /api/signals`
Query (all optional): `status` (e.g. `Detected`, `Active`, `Expired` — see `SignalStatus` enum), `minimumScore` (decimal), `instrument` (substring match on symbol)

Response: `ApiResponse<SignalDto[]>`

```json
{
  "id": 10,
  "instrumentId": 1,
  "instrumentSymbol": "RELIANCE",
  "strategyId": 1,
  "strategyName": "Breakout",
  "direction": "Bullish",
  "signalScore": 82.5,
  "entryPrice": 2456.30,
  "stopLoss": 2430.00,
  "target": 2510.00,
  "riskRewardRatio": 2.03,
  "timeFrame": "5m",
  "explanation": "...",
  "status": "Detected",
  "createdAtUtc": "...",
  "expiresAtUtc": null,
  "conditions": [
	{ "id": 1, "name": "RSI > 60", "score": 20, "passed": true, "explanation": "...", "createdAtUtc": "..." }
  ]
}
```

### `GET /api/signals/{id}`
Response: `ApiResponse<SignalDto>`. `404` if not found.

### `POST /api/signals`
Request body (`CreateSignalDto`):

```json
{
  "instrumentId": 1,
  "strategyId": 1,
  "direction": "Bullish",
  "signalScore": 82.5,
  "entryPrice": 2456.30,
  "stopLoss": 2430.00,
  "target": 2510.00,
  "timeFrame": "5m",
  "explanation": "...",
  "expiresAtUtc": null,
  "conditions": [
	{ "name": "RSI > 60", "score": 20, "passed": true, "explanation": "..." }
  ]
}
```

Validation (`400`): `signalScore` 0–100, `entryPrice/stopLoss/target > 0`, `direction` required. `instrumentId`/`strategyId` must reference existing rows (`400` if not found).

Response: `201 Created` with `ApiResponse<SignalDto>` (location header points to `GET /api/signals/{id}`).

---

## 5. Paper Trades — `{paperTrades}` (route: `/api/PaperTrades`)

> **Frontend action required**: change the `paperTrades` constant from `'/paper-trades'` to `'/paperTrades'`. The controller route is `[controller]` = `PaperTrades`, which does not contain a hyphen, so `/paper-trades` will 404.

### `GET /api/paperTrades`
Response: `ApiResponse<PaperTradeDto[]>`

```json
{
  "id": 5,
  "signalId": 10,
  "signalDirection": "Bullish",
  "entryPrice": 2456.30,
  "exitPrice": 0,
  "quantity": 10,
  "profitLoss": 0,
  "status": "Open",
  "createdAtUtc": "...",
  "closedAtUtc": null
}
```

### `GET /api/paperTrades/{id}`
Response: `ApiResponse<PaperTradeDto>`. `404` if not found.

### `POST /api/paperTrades`
Request body (`CreatePaperTradeDto`):

```json
{ "signalId": 10, "entryPrice": 2456.30, "quantity": 10 }
```

Validation (`400`): `signalId > 0`, `quantity > 0`, `entryPrice > 0`. Also `400` if paper trading is disabled in risk settings, or if `signalId` doesn't exist.

Response: `201 Created` with `ApiResponse<PaperTradeDto>`.

### `POST /api/paperTrades/{id}/close`
Request body (`ClosePaperTradeDto`):

```json
{ "exitPrice": 2500.00, "exitReason": "Target hit manually" }
```

Validation (`400`): `exitPrice > 0`; trade must currently be `Open` (otherwise `400`). `404` if trade not found.

Response: `ApiResponse<PaperTradeDto>` with updated `status` (`TargetHit` / `StopLossHit` / `ManuallyClosed`), `exitPrice`, `profitLoss`, `closedAtUtc`.

---

## 6. Performance — `{performance}`

### `GET /api/performance`
Overall performance across all closed trades.

Response: `ApiResponse<PerformanceDto>`

```json
{
  "calculatedAtUtc": "...",
  "totalTrades": 20,
  "winningTrades": 12,
  "losingTrades": 8,
  "winRate": 60.00,
  "profitFactor": 1.85,
  "totalNetProfitLoss": 4500.00,
  "averageWinningTrade": 650.00,
  "averageLosingTrade": -350.00,
  "maximumDrawdown": 1200.00
}
```

### `GET /api/performance/strategy/{strategyId}`
Response: `ApiResponse<StrategyPerformanceDto>` (same fields as above, plus `strategyId`, `strategyName`). `404` if strategy not found.

---

## 7. Candles — `{candles}` / `{candleImport}`

See full details in `MVP2_API_REFERENCE.md`. Quick summary:

### `POST /api/candles/import?instrumentId={instrumentId}`
Body (`ImportCandlesRequestDto`):

```json
{
  "candles": [
	{ "timestampUtc": "2025-06-01T09:15:00Z", "timeframe": "5m", "open": 100.25, "high": 101.00, "low": 99.80, "close": 100.75, "volume": 15230 }
  ]
}
```

Response (`ImportCandlesResultDto`): `{ "requested": 1, "imported": 1, "skipped": 0, "errors": [] }`

`400` if batch empty or > 5000 candles. `404` if instrument not found.

### `GET /api/candles/{instrumentId}?timeframe={timeframe}&limit={limit}`
`timeframe` required (`1m`/`5m`/`15m`/`1h`/`1d`), `limit` optional (default 200, max 1000).

Response: `ApiResponse<CandleDto[]>` in ascending chronological order.

```json
{ "id": 1, "instrumentId": 5, "timeframe": "5m", "timestampUtc": "2025-06-01T09:15:00Z", "open": 100.25, "high": 101.00, "low": 99.80, "close": 100.75, "volume": 15230 }
```

`400` for unsupported timeframe / invalid limit.

---

## 8. Analysis — `{analysis}`

See full details, indicator warm-up rules, and setup definitions in `MVP2_API_REFERENCE.md`. Quick summary:

### `GET /api/analysis/{instrumentId}?timeframe={timeframe}`
Latest indicator snapshot + setup evaluations using the last 300 stored candles.

### `GET /api/analysis/{instrumentId}/history?timeframe={timeframe}&limit={limit}`
Rolling series of snapshots (default `limit` 50, max 200), oldest to newest — useful for charting indicator lines.

### `POST /api/analysis/{instrumentId}/calculate`
Body (`CalculateAnalysisRequestDto`):

```json
{ "timeframe": "5m", "limit": 300, "relativeVolumeThreshold": 1.5, "breakoutLookback": 20 }
```

All fields except `timeframe` are optional.

Response shape (`TechnicalAnalysisResultDto`):

```json
{
  "instrumentId": 5,
  "symbol": "RELIANCE",
  "timeframe": "5m",
  "lastCandleTimestampUtc": "2025-06-01T09:45:00Z",
  "lastClose": 2456.30,
  "sma20": 2440.15,
  "sma50": 2420.80,
  "ema9": 2451.20,
  "ema21": 2435.60,
  "rsi14": 62.4,
  "macdLine": 5.2,
  "macdSignal": 3.1,
  "macdHistogram": 2.1,
  "vwap": 2448.90,
  "atr14": 12.35,
  "volumeSma20": 18250,
  "relativeVolume": 1.8,
  "createdAtUtc": "2025-06-01T09:46:02Z",
  "setups": [
	{
	  "strategyName": "Breakout",
	  "direction": "Bullish",
	  "conditions": [
		{ "name": "Close above prior 20-candle high", "passed": true, "explanation": "...", "actualValue": 2456.30, "expectedValue": 2450.00 }
	  ],
	  "conditionsPassed": 1,
	  "totalConditions": 1,
	  "status": "Complete",
	  "explanation": "All conditions met. This is a research candidate only, not a trading signal."
	}
  ]
}
```

`400` for unsupported timeframe/invalid params. `404` if instrument not found or no candles stored for that timeframe.

---

## Notes for frontend integration

- Indicator fields (`sma20`, `rsi14`, `macdLine`, etc.) can be `null` when there isn't enough candle history yet — render as "—" or hide until available rather than treating as zero.
- `setups[].status` drives UI state: `Complete` (highlight), `Incomplete` (show failed conditions), `InsufficientData` (disable/gray out).
- Analysis/setups are explicitly **not** trading signals — don't wire them into the existing Signals/PaperTrades flows automatically.
- CORS is currently restricted to `http://localhost:4200`; update `Cors:AllowedOrigins` in `appsettings.json` for other environments.
