using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.Exolix.Migrations
{
    [DbContext(typeof(ExolixPluginDbContext))]
    [Migration("20260415_Lightning")]
    public partial class Lightning : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AddColumn<bool>(
                name: "AllowLightning",
                table: "ExolixSettings",
                schema: "BTCPayServer.Plugins.Exolix",
                nullable: false,
                defaultValue: false
            );

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropColumn(
                name: "AllowLightning",
                table: "ExolixSettings"
            );  
        }
    }
}
