using BTCPayServer.Plugins.Shopstr.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.Peach.Migrations
{
    [DbContext(typeof(ShopstrDbContext))]
    [Migration("20251120_Init")]
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "BTCPayServer.Plugins.Shopstr");

            migrationBuilder.CreateTable(
                name: "ShopstrSettings",
                schema: "BTCPayServer.Plugins.Shopstr",
                columns: table => new
                {
                    StoreId = table.Column<string>(nullable: false),
                    ShopStrShop = table.Column<string>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopstrSettings", x => x.StoreId);
                });

            migrationBuilder.CreateTable(
                name: "ShopAppStoreItem",
                schema: "BTCPayServer.Plugins.Shopstr",
                columns: table => new
                {
                    ItemId = table.Column<string>(nullable: false),
                    StoreId = table.Column<string>(nullable: false),
                    AppId = table.Column<string>(nullable: false),
                    Hash = table.Column<string>()
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ShopAppStoreItem", x => x.StoreId);
                });

            migrationBuilder.CreateIndex(
                name: "IX_ShopAppStoreItem_StoreId",
                schema: "BTCPayServer.Plugins.Shopstr",
                table: "ShopAppStoreItems",
                column: "StoreId"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "ShopstrSettings",
                schema: "BTCPayServer.Plugins.Shopstr");
            migrationBuilder.DropTable(
                name: "ShopAppStoreItems",
                schema: "BTCPayServer.Plugins.Shopstr");
        }
    }
}
