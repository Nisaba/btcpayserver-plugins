using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.Peach.Migrations
{
    [DbContext(typeof(PeachPluginDbContext))]
    [Migration("20250630_Init")]
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "BTCPayServer.Plugins.Peach");

            migrationBuilder.CreateTable(
                name: "PeachSettings",
                schema: "BTCPayServer.Plugins.Peach",
                columns: table => new
                {
                    StoreId = table.Column<string>(nullable: false),
                    Lang = table.Column<string>(nullable: false),
                    UseBridgeApp = table.Column<bool>(nullable: false),
                    Phone = table.Column<string>(nullable: true),
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeachSettings", x => x.StoreId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "PeachSettings",
                schema: "BTCPayServer.Plugins.Peach");
        }
    }
}
