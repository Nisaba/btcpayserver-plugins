using BTCPayServer.Plugins.TelegramBot.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Migrations
{
    [DbContext(typeof(TelegramBotDbContext))]
    [Migration("20260122_InitBaseUrl")]
    public partial class InitBaseUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Config",
                schema: "BTCPayServer.Plugins.TelegramBot",
                columns: table => new
                {
                    BaseUrl = table.Column<string>(nullable: false),
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Config",
                schema: "BTCPayServer.Plugins.TelegramBot");
        }
    }
}
