using BTCPayServer.Plugins.TelegramBot.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Migrations
{
    [DbContext(typeof(TelegramBotDbContext))]
    [Migration("20260125_Invoices")]
    public partial class InitInvoices : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "TelegramInvoices",
                schema: "BTCPayServer.Plugins.TelegramBot",
                columns: table => new
                {
                    BTCPayInvoiceId = table.Column<string>(nullable: false),
                    StoreId = table.Column<string>(nullable: false),
                    AppName = table.Column<string>(nullable: false),
                    DateT = table.Column<DateTime>(nullable: false),
                    Amount = table.Column<decimal>(nullable: false),
                    Currency = table.Column<string>(nullable: false),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramBot_Invoices", x => x.BTCPayInvoiceId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_TelegramInvoices_StoreId",
                schema: "BTCPayServer.Plugins.TelegramBot",
                table: "TelegramInvoices",
                column: "StoreId"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "TelegramInvoices",
                schema: "BTCPayServer.Plugins.TelegramBot");
        }
    }
}
