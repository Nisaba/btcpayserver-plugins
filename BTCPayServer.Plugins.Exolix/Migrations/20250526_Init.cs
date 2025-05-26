using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.Exolix.Migrations
{
    [DbContext(typeof(ExolixPluginDbContext))]
    [Migration("20250526_Init")]
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "BTCPayServer.Plugins.Exolix");

            migrationBuilder.CreateTable(
                name: "ExolixSettings",
                schema: "BTCPayServer.Plugins.Exolix",
                columns: table => new
                {
                    StoreId = table.Column<string>(nullable: false),
                    Enabled = table.Column<bool>(nullable: false),
                    AcceptedCryptos = table.Column<List<string>>(nullable: false),
                    IsEmailToCustomer = table.Column<bool>(nullable: false),
                    AllowRefundAddress = table.Column<bool>(nullable: true)

                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExolixSettings", x => x.StoreId);
                });

            migrationBuilder.CreateTable(
                name: "ExolixTransactions",
                schema: "BTCPayServer.Plugins.Exolix",
                columns: table => new
                {
                    TxID = table.Column<string>(nullable: false),
                    StoreId = table.Column<string>(nullable: false),
                    Enabled = table.Column<bool>(nullable: false),
                    AltcoinFrom = table.Column<string>(nullable: false),
                    BTCAmount = table.Column<decimal>(nullable: false),
                    DateT = table.Column<DateTime>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExolixTransactions", x => x.TxID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExolixSettings",
                schema: "BTCPayServer.Plugins.Exolix");
            migrationBuilder.DropTable(
                name: "ExolixTransactions",
                schema: "BTCPayServer.Plugins.Exolix");
        }
    }
}
