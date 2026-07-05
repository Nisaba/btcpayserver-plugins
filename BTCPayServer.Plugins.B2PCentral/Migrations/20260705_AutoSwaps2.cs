
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.B2PCentral.Migrations
{
    [DbContext(typeof(B2PCentralPluginDbContext))]
    [Migration("20260705_AutoSwaps2")]
    public partial class AutoSwaps2: Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<int>(
                 name: "OnChainAutoSwapProvider",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: 0);

            migrationBuilder.AddColumn<int>(
                name: "LightningAutoSwapProvider",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings",
                nullable: false,
                defaultValue: 0);

        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "OnChainAutoSwapProvider",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");

            migrationBuilder.DropColumn(
                name: "LightningAutoSwapProvider",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "B2PSettings");
        }
    }
}
