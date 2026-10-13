# TradePilot AI — MVP-2 API Reference (Technical Analysis Engine)

This document describes the MVP-2 backend endpoints for storing OHLCV candles and running deterministic
technical analysis on them. It is intended for frontend integration.

All responses use the standard envelope:

```json
{
  "success": true,
  "data": { },
  "message": null,
  "errors": null
}
```

- `success: false` responses set `message` and optionally `errors` (list of strings), `data` is `null`.
- Base route prefix: `/api`
- All timestamps are UTC (`TimestampUtc`, ISO-8601, e.g. `2025-06-01T09:15:00Z`).
- MVP-2 does **not** fetch live market data. Candles must be imported by the frontend/another service via the
  import endpoint before analysis endpoints return data.
- Analysis results (`Setups`) are **research candidates only** — they are not trading signals and are not
  linked to the existing Signal/PaperTrade features.

---

## Supported Timeframes

`1m`, `5m`, `15m`, `1h`, `1d`

---

## Candles API

### 1. Import candles

`POST /api/candles/import?instrumentId={instrumentId}`

Imports a batch of OHLCV candles for an existing instrument. Invalid candles are rejected individually;
duplicates (same timeframe + timestamp, either within the request or already stored) are skipped.

**Query parameters**

| Name | Type | Required | Description |
|---|---|---|---|
| `instrumentId` | int | yes | Id of an existing instrument |

**Request body** (`ImportCandlesRequestDto`)

```json
{
  "candles": [
	{
	  "timestampUtc": "2025-06-01T09:15:00Z",
	  "timeframe": "5m",
	  "open": 100.25,
	  "high": 101.00,
	  "low": 99.80,
	  "close": 100.75,
	  "volume": 15230
	}
  ]
}
```

| Field | Type | Notes |
|---|---|---|
| `timestampUtc` | DateTime | Must not be default, not in the future, not `Local` kind, and aligned to the timeframe boundary |
| `timeframe` | string | One of the supported timeframes |
| `open`, `high`, `low`, `close` | decimal | Must be > 0; `high >= low`, `high >= open/close`, `low <= open/close` |
| `volume` | decimal | Must be >= 0 |

**Response** (`ImportCandlesResultDto`)

```json
{
  "success": true,
  "data": {
	"requested": 1,
	"imported": 1,
	"skipped": 0,
	"errors": []
  }
}
```

| Field | Description |
|---|---|
| `requested` | Total candles in the request |
| `imported` | Candles actually inserted |
| `skipped` | Candles skipped due to duplication (request or database) |
| `errors` | Validation error messages, prefixed with `Candle[i]:` for the failing index |

**Error responses**
- `400` — empty batch, batch larger than 5000 candles.
- `404` — instrument not found.

---

### 2. Get stored candles

`GET /api/candles/{instrumentId}?timeframe={timeframe}&limit={limit}`

Returns the most recent candles for an instrument/timeframe in ascending chronological order.

**Query parameters**

| Name | Type | Required | Default | Notes |
|---|---|---|---|---|
| `timeframe` | string | yes | — | Must be a supported timeframe |
| `limit` | int | no | 200 | 1–1000 |

**Response** (`List<CandleDto>`)

```json
{
  "success": true,
  "data": [
	{
	  "id": 1,
	  "instrumentId": 5,
	  "timeframe": "5m",
	  "timestampUtc": "2025-06-01T09:15:00Z",
	  "open": 100.25,
	  "high": 101.00,
	  "low": 99.80,
	  "close": 100.75,
	  "volume": 15230
	}
  ]
}
```

**Error responses**
- `400` — unsupported timeframe, invalid limit.

---

## Analysis API

All analysis is computed on demand from stored candles. If there aren't enough candles for an indicator's
warm-up period, that field is returned as `null`. If there aren't enough candles at all, the endpoint returns
`404`.

### Warm-up requirements (minimum candles before a value is non-null)

| Indicator | Minimum candles |
|---|---|
| SMA(n) | n |
| EMA(n) | n |
| RSI(14) | 15 |
| MACD(12,26,9) | 34 |
| ATR(14) | 15 |
| VWAP | 1 (current session only; `null` for `1d` timeframe) |
| Volume SMA(20) | 20 |

### 3. Get latest analysis

`GET /api/analysis/{instrumentId}?timeframe={timeframe}`

Computes indicators and setup evaluations using the last 300 stored candles for the instrument/timeframe.

### 4. Get analysis history

`GET /api/analysis/{instrumentId}/history?timeframe={timeframe}&limit={limit}`

Returns a rolling series of analysis snapshots (one per candle), most recent last, useful for charting
indicator history on the frontend.

| Query param | Type | Default | Notes |
|---|---|---|---|
| `timeframe` | string | — | required |
| `limit` | int | 50 | 1–200 (number of snapshots returned) |

**Response**: `List<TechnicalAnalysisResultDto>` (same shape as below, one entry per candle).

### 5. Calculate analysis with custom parameters

`POST /api/analysis/{instrumentId}/calculate`

Same as "Get latest analysis" but lets the caller override the candle window and strategy thresholds for a
single calculation (not persisted).

**Request body** (`CalculateAnalysisRequestDto`)

```json
{
  "timeframe": "5m",
  "limit": 300,
  "relativeVolumeThreshold": 1.5,
  "breakoutLookback": 20
}
```

| Field | Type | Required | Default | Notes |
|---|---|---|---|---|
| `timeframe` | string | yes | — | supported timeframe |
| `limit` | int | no | 300 | 1–1000, number of candles to load |
| `relativeVolumeThreshold` | decimal | no | 1.5 | must be > 0 |
| `breakoutLookback` | int | no | 20 | 1–500 |

### Analysis response shape (`TechnicalAnalysisResultDto`)

```json
{
  "success": true,
  "data": {
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
		  {
			"name": "Close above prior 20-candle high",
			"passed": true,
			"explanation": "Close 2456.30 is above the prior 20-candle high of 2450.00.",
			"actualValue": 2456.30,
			"expectedValue": 2450.00
		  }
		],
		"conditionsPassed": 1,
		"totalConditions": 1,
		"status": "Complete",
		"explanation": "All conditions met. This is a research candidate only, not a trading signal."
	  }
	]
  }
}
```

### Setup statuses

| Status | Meaning |
|---|---|
| `Complete` | All conditions for the setup passed |
| `Incomplete` | At least one condition failed |
| `InsufficientData` | Not enough candle history to evaluate the setup |

### Evaluated setups

| Strategy name | Direction | Summary |
|---|---|---|
| `Breakout` | Bullish | Close breaks above the prior N-candle high (excludes current candle) |
| `Breakout + Volume` | Bullish | Breakout condition plus relative volume above threshold |
| `Trend Continuation` | Bullish | EMA9 > EMA21 and close above EMA21/SMA50 |
| `Support Reversal` | Bullish | Price reverses off a prior N-candle support level within tolerance |
| `Resistance Reversal` | Bearish | Price reverses off a prior N-candle resistance level within tolerance |

**Error responses**
- `400` — unsupported timeframe, invalid limit/threshold/lookback.
- `404` — instrument not found, or no stored candles for the timeframe.

---

## Example frontend flow

1. `POST /api/candles/import?instrumentId=5` with historical OHLCV data for `5m`.
2. `GET /api/candles/5?timeframe=5m&limit=200` to render a candlestick chart.
3. `GET /api/analysis/5?timeframe=5m` to show indicator overlays and setup cards.
4. `GET /api/analysis/5/history?timeframe=5m&limit=50` to plot indicator lines (RSI/MACD panels) over time.
5. `POST /api/analysis/5/calculate` to let a user tweak breakout lookback / volume threshold and re-run
   analysis without changing stored defaults.

## Limitations (MVP-2)

- No live market data ingestion; candles must be supplied externally.
- Analysis setups are explainable research candidates, not trading signals, and are not connected to the
  existing Signal/PaperTrade/RiskManagement features.
- No authentication/authorization has been added for these endpoints (matches existing MVP-1 endpoints).
