using BTCPayServer.Plugins.TelegramBot.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Migrations
{
    [DbContext(typeof(TelegramBotDbContext))]
    [Migration("20260120_Init")]
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "BTCPayServer.Plugins.TelegramBot");

            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "BTCPayServer.Plugins.TelegramBot",
                columns: table => new
                {
                    StoreId = table.Column<string>(nullable: false),
                    AppId = table.Column<string>(nullable: false),
                    BotToken = table.Column<string>(nullable: false),
                    IsEnabled = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_TelegramBot_Settings", x => new { x.StoreId, x.AppId });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings",
                schema: "BTCPayServer.Plugins.TelegramBot");
        }
    }
}
