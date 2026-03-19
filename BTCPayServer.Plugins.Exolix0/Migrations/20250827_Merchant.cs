using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.Exolix.Migrations
{
    [DbContext(typeof(ExolixPluginDbContext))]
    [Migration("20250827_Merchant")]
    public partial class Merchant : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "ExolixMerchantTransactions",
                schema: "BTCPayServer.Plugins.Exolix",
                columns: table => new
                {
                    TxID = table.Column<string>(nullable: false),
                    StoreId = table.Column<string>(nullable: false),
                    AltcoinTo = table.Column<string>(nullable: false),
                    BTCAmount = table.Column<float>(nullable: false),
                    AltAmount = table.Column<float>(nullable: false),
                    DateT = table.Column<DateTime>(nullable: false),
                    BTCPayPullPaymentId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExolixMerchantTransactions", x => x.TxID);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ExolixMerchantTransactions_StoreId",
                schema: "BTCPayServer.Plugins.Exolix",
                table: "ExolixMerchantTransactions",
                column: "StoreId"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ExolixMerchantTransactions",
                schema: "BTCPayServer.Plugins.Exolix");
        }
    }
}
