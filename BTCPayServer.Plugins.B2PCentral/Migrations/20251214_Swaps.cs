using BTCPayServer.Plugins.B2PCentral.Models.Swaps;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;

namespace BTCPayServer.Plugins.B2PCentral.Migrations
{
    [DbContext(typeof(B2PCentralPluginDbContext))]
    [Migration("20251214_Swaps")]
    public partial class MigrationSwaps : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Swaps",
                schema: "BTCPayServer.Plugins.B2PCentral",
                columns: table => new
                {
                    StoreId = table.Column<string>(nullable: false),
                    SwapId = table.Column<string>(nullable: false),
                    DateT = table.Column<DateTime>(nullable: false),
                    Provider = table.Column<SwapProvidersEnum>(nullable: false),
                    FollowUrl = table.Column<string>(nullable: false),
                    FromAmount = table.Column<decimal>(nullable: false),
                    ToAmount = table.Column<decimal>(nullable: false),
                    ToCrypto = table.Column<string>(nullable: false),
                    ToNetwork = table.Column<string>(nullable: false),
                    BTCPayPullPaymentId = table.Column<string>(nullable: false),
                    BTCPayPayoutId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_B2PSwaps", x => new { x.StoreId, x.SwapId });
                });
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Swaps",
                schema: "BTCPayServer.Plugins.B2PCentral");
        }
    }
}
