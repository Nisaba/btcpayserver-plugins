using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.Ecwid.Migrations
{
    [DbContext(typeof(EcwidPluginDbContext))]
    [Migration("20201117154419_Init")]
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "BTCPayServer.Plugins.Ecwid");

            migrationBuilder.CreateTable(
                name: "EcwidSettings",
                schema: "BTCPayServer.Plugins.Ecwid",
                columns: table => new
                {
                    StoreId = table.Column<string>(nullable: false),
                    WebhookSecret = table.Column<string>(nullable: false),
                    ClientSecret = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_EcwidSettings", x => x.StoreId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "EcwidSettings",
                schema: "BTCPayServer.Plugins.Ecwid");
        }
    }
}
