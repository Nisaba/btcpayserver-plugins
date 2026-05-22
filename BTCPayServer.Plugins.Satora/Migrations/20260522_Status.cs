using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.Satora.Migrations
{
    [DbContext(typeof(SatoraPluginDbContext))]
    [Migration("20260522_Status")]
    public partial class Status:Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "Status",
                table: "SatoraTransactions",
                schema: "BTCPayServer.Plugins.Satora",
                nullable: true
            );
        }
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "Status",
                table: "SatoraTransactions"
            );
        }
    }
}
