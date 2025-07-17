using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.Lendasat.Migrations
{
    [DbContext(typeof(LendasatPluginDbContext))]
    [Migration("20255507_Init")]
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "BTCPayServer.Plugins.Lendasat");

            migrationBuilder.CreateTable(
                name: "LendasatSettings",
                schema: "BTCPayServer.Plugins.Lendasat",
                columns: table => new
                {
                    StoreId = table.Column<string>(nullable: false),
                    APIKey = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LendasatSettings", x => x.StoreId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "LendasatSettings",
                schema: "BTCPayServer.Plugins.Lendasat");
        }
    }
}
