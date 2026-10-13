# TradePilot AI

TradePilot is a trading signal and paper-trading platform built on .NET 10. It generates rule-based trading signals for financial instruments, manages risk-adjusted position sizing, tracks paper (simulated) trades, and records strategy performance over time — all exposed through a REST API.

## Features

- 📈 **Instrument tracking** — maintain a catalog of tradable instruments (stocks, indices, etc.) on exchanges like NSE
- 🎯 **Signal generation** — strategies produce buy/sell signals with entry price, stop-loss, target, and risk/reward ratio
- 🧮 **Risk management** — automatic position sizing based on trading capital, risk percentage, entry price, and stop-loss
- 📝 **Paper trading** — simulate trades against generated signals and track profit/loss without real money
- 📊 **Strategy performance** — historical performance tracking per strategy
- 📰 **News sentiment** — ingest and score news relevant to instruments
- 🔭 **Background market scanner** — a hosted worker service that periodically scans the market for opportunities
- 📘 **Swagger / OpenAPI** — interactive API documentation available in development

## Tech Stack

| Layer | Technology |
|-------|------------|
| Runtime | .NET 10.0 |
| Web API | ASP.NET Core (Controllers) |
| ORM | Entity Framework Core 10 |
| Database | Microsoft SQL Server |
| API Docs | Swashbuckle (Swagger/OpenAPI) |
| Background Processing | .NET Generic Host (`BackgroundService`) |
| Testing | TradePilot.Tests project |

## Solution Structure

```
TradePilotAI.slnx
├── TradePilot.Domain          # Core entities & enums (no dependencies)
├── TradePilot.Infrastructure  # EF Core DbContext, migrations, DB seeding
├── TradePilot.Api             # ASP.NET Core Web API (controllers, DTOs, services)
├── TradePilot.Worker          # Background market scanner service
└── TradePilot.Tests           # Unit/integration tests
```

### Project Dependency Graph

```
TradePilot.Api  ──┐
TradePilot.Worker ─┼──> TradePilot.Infrastructure ──> TradePilot.Domain
```

## Getting Started

### Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- SQL Server (LocalDB, Express, or full instance)
- Visual Studio 2026 or VS Code

### Setup

1. Clone the repository
   ```powershell
   git clone https://github.com/mukesh271/TradePilotAI-Backend.git
   cd TradePilotAI
   ```

2. Configure the connection string in `TradePilot.Api/appsettings.Development.json` under `ConnectionStrings:TradePilotDb`.

3. Apply EF Core migrations
   ```powershell
   dotnet ef database update --project TradePilot.Infrastructure --startup-project TradePilot.Api
   ```

4. Run the API
   ```powershell
   dotnet run --project TradePilot.Api
   ```

5. Browse the Swagger UI at `https://localhost:7273` (served at root).

### Running the Worker

```powershell
dotnet run --project TradePilot.Worker
```

### Running Tests

```powershell
dotnet test
```

## API Overview

| Controller | Purpose |
|------------|---------|
| `InstrumentsController` | CRUD for tradable instruments |
| `StrategiesController` | Manage trading strategies |
| `SignalsController` | Generate, query, and manage trading signals |
| `PaperTradesController` | Create and track simulated trades |
| `PerformanceController` | View strategy performance statistics |
| `RiskSettingsController` | Configure capital, risk %, and trading limits |

See [PROJECT_DETAILS.md](./PROJECT_DETAILS.md) for a full breakdown of the domain model, architecture, and business logic.

## Contributing

1. Create a feature branch
2. Commit your changes with clear messages
3. Open a pull request against `master`

## License

This project currently has no explicit license. Add a `LICENSE` file if you intend to open-source it.
