using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.Satora.Migrations
{
    [DbContext(typeof(SatoraPluginDbContext))]
    [Migration("20260529_Seed2")]
    public partial class Seed2: Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
             name: "CryptedSeed",
             table: "SatoraTransactions",
             schema: "BTCPayServer.Plugins.Satora"
         );

            migrationBuilder.AddColumn<string>(
                name: "CryptedSeed",
                table: "SatoraSettings",
                schema: "BTCPayServer.Plugins.Satora",
                nullable: true
            );
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "CryptedSeed",
                table: "SatoraSettings",
                schema: "BTCPayServer.Plugins.Satora"
            );
        }
    }
}
