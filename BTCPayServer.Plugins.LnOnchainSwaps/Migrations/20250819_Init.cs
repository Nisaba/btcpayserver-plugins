using BTCPayServer.Plugins.LnOnchainSwaps.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Migrations
{
    [DbContext(typeof(LnOnchainSwapsDbContext))]
    [Migration("20250819_Init")]
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "BTCPayServer.Plugins.LnOnchainSwaps");

            migrationBuilder.CreateTable(
                name: "BoltzSwaps",
                schema: "BTCPayServer.Plugins.LnOnchainSwaps",
                columns: table => new
                {
                    SwapId = table.Column<string>(nullable: false),
                    StoreId = table.Column<string>(nullable: false),
                    Type = table.Column<string>(nullable: false),
                    PreImage = table.Column<string>(nullable: false),
                    PreImageHash = table.Column<string>(nullable: false),
                    Destination = table.Column<string>(nullable: false),
                    ExpectedAmount = table.Column<decimal>(nullable: false),
                    BTCPayPayoutId = table.Column<string>(nullable: false),
                    Json = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_BoltzSwaps", x => x.SwapId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_BoltzSwaps_StoreId",
                schema: "BTCPayServer.Plugins.LnOnchainSwaps",
                table: "BoltzSwaps",
                column: "StoreId"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "BoltzSwaps",
                schema: "BTCPayServer.Plugins.LnOnchainSwaps");
        }
    }
}
