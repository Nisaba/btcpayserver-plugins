
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.B2PCentral.Migrations
{
    [DbContext(typeof(B2PCentralPluginDbContext))]
    [Migration("20260716_CheckoutSwaps")]
    public partial class CheckoutSwaps: Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "CheckoutEnabled",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OnChainCheckoutSwapProvider",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LightningCheckoutSwapProvider",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: 0);


            migrationBuilder.AddColumn<bool>(
                name: "IsCheckoutSwap",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "Swaps",
                nullable: false,
                defaultValue: false);

               
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckoutEnabled",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");

            migrationBuilder.DropColumn(
                name: "OnChainCheckoutSwapProvider",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");

            migrationBuilder.DropColumn(
                name: "LightningCheckoutSwapProvider",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");


            migrationBuilder.DropColumn(
                name: "IsCheckoutSwap",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "Swaps");

        }
    }
}
