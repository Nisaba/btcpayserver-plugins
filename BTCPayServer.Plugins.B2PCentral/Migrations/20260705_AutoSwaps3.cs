
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.B2PCentral.Migrations
{
    [DbContext(typeof(B2PCentralPluginDbContext))]
    [Migration("20260705_AutoSwaps3")]
    public partial class AutoSwaps3: Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<string>(
                 name: "OnChainAutoSwapCryptoTo",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LightningAutoSwapCryptoTo",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                 name: "OnChainAutoSwapAddressTo",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: "");

            migrationBuilder.AddColumn<string>(
                name: "LightningAutoSwapAddressTo",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: "");

        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "OnChainAutoSwapAddressTo",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");

            migrationBuilder.DropColumn(
                name: "LightningAutoSwapAddressTo",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");

            migrationBuilder.DropColumn(
                name: "OnChainAutoSwapCryptoTo",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");

            migrationBuilder.DropColumn(
                name: "LightningAutoSwapCryptoTo",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");
        }
    }
}
