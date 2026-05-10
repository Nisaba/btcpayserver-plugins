using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.Satora.Migrations
{
    [DbContext(typeof(SatoraPluginDbContext))]
    [Migration("20260508_Init")]
    public partial class Init : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.EnsureSchema(
                name: "BTCPayServer.Plugins.Satora");

            migrationBuilder.CreateTable(
                name: "SatoraSettings",
                schema: "BTCPayServer.Plugins.Satora",
                columns: table => new
                {
                    StoreId = table.Column<string>(nullable: false),
                    Enabled = table.Column<bool>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SatoraSettings", x => x.StoreId);
                });

            migrationBuilder.CreateTable(
                name: "SatoraTransactions",
                schema: "BTCPayServer.Plugins.Satora",
                columns: table => new
                {
                    TxID = table.Column<string>(nullable: false),
                    StoreId = table.Column<string>(nullable: false),
                    Stablecoin = table.Column<string>(nullable: false),
                    Blockchain = table.Column<string>(nullable: false),
                    BTCAmount = table.Column<float>(nullable: false),
                    DateT = table.Column<DateTime>(nullable: false),
                    BTCPayInvoiceId = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_SatoraTransactions", x => x.TxID);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "SatoraSettings",
                schema: "BTCPayServer.Plugins.Satora");
            migrationBuilder.DropTable(
                name: "SatoraTransactions",
                schema: "BTCPayServer.Plugins.Satora");
        }
    }
}
