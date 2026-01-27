using BTCPayServer.Plugins.Shopstr.Data;
using BTCPayServer.Plugins.Shopstr.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Migrations
{
    [DbContext(typeof(ShopstrDbContext))]
    [Migration("20260127_UpdateSettings")]
    public partial class UpdateSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "FlashSales",
                schema: "BTCPayServer.Plugins.Shopstr",
                table: "Settings",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<ConditionEnum>(
                name: "Condition",
                schema: "BTCPayServer.Plugins.Shopstr",
                table: "Settings",
                nullable: false,
                defaultValue: ConditionEnum.None);

            migrationBuilder.AddColumn<DateTimeOffset>(
                name: "ValidDateT",
                schema: "BTCPayServer.Plugins.Shopstr",
                table: "Settings",
                nullable: true);

            migrationBuilder.AddColumn<string>(
                name: "Restrictions",
                schema: "BTCPayServer.Plugins.Shopstr",
                table: "Settings",
                nullable: false,
                defaultValue: string.Empty);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "FlashSales",
                schema: "BTCPayServer.Plugins.Shopstr",
                table: "Settings");
            migrationBuilder.DropColumn(
                name: "Condition",
                schema: "BTCPayServer.Plugins.Shopstr",
                table: "Settings");
            migrationBuilder.DropColumn(
                name: "ValidDateT",
                schema: "BTCPayServer.Plugins.Shopstr",
                table: "Settings");
            migrationBuilder.DropColumn(
                name: "Restrictions",
                schema: "BTCPayServer.Plugins.Shopstr",
                table: "Settings");
        }
    }
}
