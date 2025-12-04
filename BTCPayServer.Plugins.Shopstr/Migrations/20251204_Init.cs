using BTCPayServer.Plugins.Shopstr.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Migrations
{
    [DbContext(typeof(ShopstrDbContext))]
    [Migration("20251204_Init")]
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "BTCPayServer.Plugins.Shopstr");

            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "BTCPayServer.Plugins.Shopstr",
                columns: table => new
                {
                    StoreId = table.Column<string>(nullable: false),
                    AppId = table.Column<string>(nullable: false),
                    Location = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Shopstr_Settings", x => new { x.StoreId, x.AppId });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings",
                schema: "BTCPayServer.Plugins.Shopstr");
        }
    }
}
