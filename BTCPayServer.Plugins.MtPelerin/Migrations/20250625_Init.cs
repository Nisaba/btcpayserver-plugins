using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.MtPelerin.Migrations
{
    [DbContext(typeof(MtPelerinPluginDbContext))]
    [Migration("20250625_Init")]
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "BTCPayServer.Plugins.MtPelerin");

            migrationBuilder.CreateTable(
                name: "MtPelerinSettings",
                schema: "BTCPayServer.Plugins.MtPelerin",
                columns: table => new
                {
                    StoreId = table.Column<string>(nullable: false),
                    Lang = table.Column<string>(nullable: false),
                    UseBridgeApp = table.Column<bool>(nullable: false),
                    Phone = table.Column<string>(nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_MtPelerinSettings", x => x.StoreId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MtPelerinSettings",
                schema: "BTCPayServer.Plugins.MtPelerin");
        }
    }
}
