using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.Exolix.Migrations
{
    [DbContext(typeof(ExolixPluginDbContext))]
    [Migration("20260624_MoreCoins")]
    public partial class MoreCoins : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowMoreExolixCoins",
                table: "ExolixSettings",
                schema: "BTCPayServer.Plugins.Exolix",
                nullable: false,
                defaultValue: true
            );

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowMoreExolixCoins",
                table: "ExolixSettings"
            );
        }
    }
}
