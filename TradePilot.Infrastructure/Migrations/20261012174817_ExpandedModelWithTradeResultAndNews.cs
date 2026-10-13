using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace TradePilot.Infrastructure.Migrations
{
    /// <inheritdoc />
    public partial class ExpandedModelWithTradeResultAndNews : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Strategies",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Strategies",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "ExpiresAtUtc",
                table: "Signals",
                type: "datetime2",
                nullable: true);

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "SignalConditions",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "RiskSettings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "RiskSettings",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AlterColumn<string>(
                name: "Exchange",
                table: "Instruments",
                type: "nvarchar(450)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(max)");

            migrationBuilder.AddColumn<DateTime>(
                name: "CreatedAtUtc",
                table: "Instruments",
                type: "datetime2",
                nullable: false,
                defaultValue: new DateTime(1, 1, 1, 0, 0, 0, 0, DateTimeKind.Unspecified));

            migrationBuilder.AddColumn<DateTime>(
                name: "UpdatedAtUtc",
                table: "Instruments",
                type: "datetime2",
                nullable: true);

            migrationBuilder.CreateTable(
                name: "News",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: false),
                    Source = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    Url = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Summary = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    Sentiment = table.Column<int>(type: "int", nullable: false),
                    RelevanceScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    PublishedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_News", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StrategyPerformances",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    StrategyId = table.Column<int>(type: "int", nullable: false),
                    TotalTrades = table.Column<int>(type: "int", nullable: false),
                    WinningTrades = table.Column<int>(type: "int", nullable: false),
                    LosingTrades = table.Column<int>(type: "int", nullable: false),
                    WinRate = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    ProfitFactor = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    TotalNetProfitLoss = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AverageWinningTrade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    AverageLosingTrade = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    MaximumDrawdown = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    CalculatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StrategyPerformances", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StrategyPerformances_Strategies_StrategyId",
                        column: x => x.StrategyId,
                        principalTable: "Strategies",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "TradeResults",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false)
                        .Annotation("SqlServer:Identity", "1, 1"),
                    PaperTradeId = table.Column<long>(type: "bigint", nullable: false),
                    IsWinner = table.Column<bool>(type: "bit", nullable: false),
                    GrossProfitLoss = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    Charges = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    NetProfitLoss = table.Column<decimal>(type: "decimal(18,4)", precision: 18, scale: 4, nullable: false),
                    ExitReason = table.Column<string>(type: "nvarchar(max)", nullable: false),
                    CreatedAtUtc = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TradeResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_TradeResults_PaperTrades_PaperTradeId",
                        column: x => x.PaperTradeId,
                        principalTable: "PaperTrades",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Signals_CreatedAtUtc",
                table: "Signals",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_SignalScore",
                table: "Signals",
                column: "SignalScore");

            migrationBuilder.CreateIndex(
                name: "IX_Signals_Status",
                table: "Signals",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_PaperTrades_CreatedAtUtc",
                table: "PaperTrades",
                column: "CreatedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_PaperTrades_Status",
                table: "PaperTrades",
                column: "Status");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_Exchange",
                table: "Instruments",
                column: "Exchange");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_IsActive",
                table: "Instruments",
                column: "IsActive");

            migrationBuilder.CreateIndex(
                name: "IX_Instruments_Symbol",
                table: "Instruments",
                column: "Symbol");

            migrationBuilder.CreateIndex(
                name: "IX_News_PublishedAtUtc",
                table: "News",
                column: "PublishedAtUtc");

            migrationBuilder.CreateIndex(
                name: "IX_News_Sentiment",
                table: "News",
                column: "Sentiment");

            migrationBuilder.CreateIndex(
                name: "IX_StrategyPerformances_StrategyId",
                table: "StrategyPerformances",
                column: "StrategyId");

            migrationBuilder.CreateIndex(
                name: "IX_TradeResults_PaperTradeId",
                table: "TradeResults",
                column: "PaperTradeId",
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "News");

            migrationBuilder.DropTable(
                name: "StrategyPerformances");

            migrationBuilder.DropTable(
                name: "TradeResults");

            migrationBuilder.DropIndex(
                name: "IX_Signals_CreatedAtUtc",
                table: "Signals");

            migrationBuilder.DropIndex(
                name: "IX_Signals_SignalScore",
                table: "Signals");

            migrationBuilder.DropIndex(
                name: "IX_Signals_Status",
                table: "Signals");

            migrationBuilder.DropIndex(
                name: "IX_PaperTrades_CreatedAtUtc",
                table: "PaperTrades");

            migrationBuilder.DropIndex(
                name: "IX_PaperTrades_Status",
                table: "PaperTrades");

            migrationBuilder.DropIndex(
                name: "IX_Instruments_Exchange",
                table: "Instruments");

            migrationBuilder.DropIndex(
                name: "IX_Instruments_IsActive",
                table: "Instruments");

            migrationBuilder.DropIndex(
                name: "IX_Instruments_Symbol",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Strategies");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Strategies");

            migrationBuilder.DropColumn(
                name: "ExpiresAtUtc",
                table: "Signals");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "SignalConditions");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "RiskSettings");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "RiskSettings");

            migrationBuilder.DropColumn(
                name: "CreatedAtUtc",
                table: "Instruments");

            migrationBuilder.DropColumn(
                name: "UpdatedAtUtc",
                table: "Instruments");

            migrationBuilder.AlterColumn<string>(
                name: "Exchange",
                table: "Instruments",
                type: "nvarchar(max)",
                nullable: false,
                oldClrType: typeof(string),
                oldType: "nvarchar(450)");
        }
    }
}
