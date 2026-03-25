using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.B2PCentral.Migrations
{
    [DbContext(typeof(B2PCentralPluginDbContext))]
    [Migration("20251223_Swaps2")]
    public partial class Swaps2 : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ProviderUrl", 
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "Swaps",
                nullable: false,
                defaultValue: string.Empty);
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ProviderUrl",
                schema: "BTCPayServer.Plugins.B2PCentral",
                table: "Swaps");
        }
    }
}
