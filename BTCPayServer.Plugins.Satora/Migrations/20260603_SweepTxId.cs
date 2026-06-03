using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.Satora.Migrations
{
    [DbContext(typeof(SatoraPluginDbContext))]
    [Migration("20260603_SweepTxId")]
    public partial class SweepTxId : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "SweepTxId",
                table: "SatoraTransactions",
                schema: "BTCPayServer.Plugins.Satora",
                nullable: true
            );
            migrationBuilder.AddColumn<string>(
                name: "InvoiceArkadeAddress",
                table: "SatoraTransactions",
                schema: "BTCPayServer.Plugins.Satora",
                nullable: true
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "SweepTxId",
                table: "SatoraTransactions",
                schema: "BTCPayServer.Plugins.Satora"
            );
            migrationBuilder.DropColumn(
                name: "InvoiceArkadeAddress",
                table: "SatoraTransactions",
                schema: "BTCPayServer.Plugins.Satora"
            );
        }
    }
}