
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.B2PCentral.Migrations
{
    [DbContext(typeof(B2PCentralPluginDbContext))]
    [Migration("20260705_AutoSwaps")]
    public partial class AutoSwaps: Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProvidersString",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");

            migrationBuilder.AddColumn<bool>(
                name: "OnChainAutoSwapEnabled",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<bool>(
                name: "LightningAutoSwapEnabled",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: false);

            migrationBuilder.AddColumn<int>(
                name: "OnChainAutoSwapThreshold",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: 100000);

            migrationBuilder.AddColumn<int>(
                name: "LightningAutoSwapThreshold",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: 10000);

            migrationBuilder.AddColumn<int>(
                 name: "OnChainAutoSwapPercent",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: 70);

            migrationBuilder.AddColumn<int>(
                name: "LightningAutoSwapPercent",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: 70);

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProvidersString",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: "0");

            migrationBuilder.DropColumn(
                name: "OnChainAutoSwapEnabled",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");

            migrationBuilder.DropColumn(
                name: "LightningAutoSwapEnabled",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");

            migrationBuilder.DropColumn(
                name: "OnChainAutoSwapThreshold",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");

            migrationBuilder.DropColumn(
                name: "LightningAutoSwapThreshold",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");

            migrationBuilder.DropColumn(
                name: "OnChainAutoSwapPercent",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");

            migrationBuilder.DropColumn(
                name: "LightningAutoSwapPercent",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");

        }

    }
}
