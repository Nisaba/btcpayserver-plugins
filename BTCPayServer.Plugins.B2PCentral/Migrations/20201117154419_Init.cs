using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.B2PCentral.Migrations
{
    [DbContext(typeof(B2PCentralPluginDbContext))]
    [Migration("20201117154419_Init")]
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "BTCPayServer.Plugins.B2PCentral");

            migrationBuilder.CreateTable(
                name: "B2PSettings",
                schema: "BTCPayServer.Plugins.B2PCentral",
                columns: table => new
                {
                    StoreId = table.Column<string>(nullable: false),
                    ApiKey = table.Column<string>(nullable: false),
                    ProvidersString = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_B2PSettings", x => x.StoreId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "B2PSettings",
                schema: "BTCPayServer.Plugins.B2PCentral");
        }
    }
}
