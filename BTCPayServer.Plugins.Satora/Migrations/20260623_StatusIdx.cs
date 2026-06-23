using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.Satora.Migrations
{
    [DbContext(typeof(SatoraPluginDbContext))]
    [Migration("20260623_StatusIdx")]
    public class StatusIdx : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_SatoraTx_Status",
                schema: "BTCPayServer.Plugins.Satora",
                table: "SatoraTransactions",
                column: "Status"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropIndex(
                name: "IX_SatoraTx_Status",
                schema: "BTCPayServer.Plugins.Satora",
                table: "SatoraTransactions"
            );
        }

    }
}
