using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.Satora.Migrations
{
    [DbContext(typeof(SatoraPluginDbContext))]
    [Migration("20260601_ClaimTxId")]
    public partial class ClaimTxId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ClaimTxId",
                table: "SatoraTransactions",
                schema: "BTCPayServer.Plugins.Satora",
                nullable: true
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ClaimTxId",
                table: "SatoraTransactions",
                schema: "BTCPayServer.Plugins.Satora"
            );
        }
    }
}
