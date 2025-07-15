using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.Peach.Migrations
{
    [DbContext(typeof(PeachPluginDbContext))]
    [Migration("20251507_MoP")]
    public partial class MoP : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "BTCPayServer.Plugins.Peach");

            migrationBuilder.CreateTable(
                name: "MeansOfPayments",
                schema: "BTCPayServer.Plugins.Peach",
                columns: table => new
                {
                    StoreId = table.Column<string>(nullable: false),
                    MoP = table.Column<string>(nullable: false),
                    HashPaymentData = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_PeachMoP", x => new { x.StoreId, x.MoP });
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "MeansOfPayments",
                schema: "BTCPayServer.Plugins.Peach");
        }
    }
}
