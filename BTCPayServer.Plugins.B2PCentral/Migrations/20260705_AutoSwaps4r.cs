
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.B2PCentral.Migrations
{
    [DbContext(typeof(B2PCentralPluginDbContext))]
    [Migration("20260705_AutoSwaps4")]
    public partial class AutoSwaps4: Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.AddColumn<bool>(
                name: "IsAutoSwap",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "Swaps",
                nullable: false,
                defaultValue: false);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.DropColumn(
                name: "IsAutoSwap",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "Swaps");

        }
    }
}
