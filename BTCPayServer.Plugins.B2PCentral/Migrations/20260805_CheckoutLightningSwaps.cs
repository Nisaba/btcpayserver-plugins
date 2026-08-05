
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.B2PCentral.Migrations
{
    [DbContext(typeof(B2PCentralPluginDbContext))]
    [Migration("20260805_CheckoutLightningSwaps")]
    public partial class CheckoutLightningSwaps: Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "LightningCheckoutEnabled",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: false);
               
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CheckoutEnabled",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");

        }
    }
}
