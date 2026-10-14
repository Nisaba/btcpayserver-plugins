using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.Exolix.Migrations
{
    [DbContext(typeof(ExolixPluginDbContext))]
    [Migration("20261014_SwapToken")]
    public class SwapToken : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<string>(
                name: "ReadTokenHash",
                table: "ExolixTransactions",
                schema: "BTCPayServer.Plugins.Exolix",
                type: "TEXT",
                nullable: true);
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "ReadTokenHash",
                table: "ExolixTransactions",
                schema: "BTCPayServer.Plugins.Exolix");
        }
    }
}
