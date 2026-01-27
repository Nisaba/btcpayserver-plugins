using BTCPayServer.Plugins.TelegramBot.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Migrations
{
    [DbContext(typeof(TelegramBotDbContext))]
    [Migration("20260126_ApiKey")]
    public partial class InitApiKey : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                name: "ApiKey",
                schema: "BTCPayServer.Plugins.TelegramBot",
                table: "Config",
                nullable: false,
                defaultValue: string.Empty);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ApiKey",
                schema: "BTCPayServer.Plugins.TelegramBot",
                table: "Config");
        }
    }
}
