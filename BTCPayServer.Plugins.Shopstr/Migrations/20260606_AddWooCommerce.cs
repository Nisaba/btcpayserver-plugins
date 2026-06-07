using BTCPayServer.Plugins.Shopstr.Data;
using BTCPayServer.Plugins.Shopstr.Models;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.Shopstr.Migrations
{
    [DbContext(typeof(ShopstrDbContext))]
    [Migration("20260606_AddWooCommerce")]
    public partial class AddWooCommerce : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "WooCommerceSettings",
                schema: "BTCPayServer.Plugins.Shopstr",
                columns: table => new
                {
                    StoreId = table.Column<string>(nullable: false),
                    WooCommerceUrl = table.Column<string>(nullable: false),
                    ConsumerKey = table.Column<string>(nullable: false),
                    ConsumerSecret = table.Column<string>(nullable: false),
                    Location = table.Column<string>(nullable: true),
                    FlashSales = table.Column<bool>(nullable: false, defaultValue: false),
                    Condition = table.Column<int>(nullable: false, defaultValue: 0),
                    Restrictions = table.Column<string>(nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_WooCommerceSettings", x => x.StoreId);
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "WooCommerceSettings",
                schema: "BTCPayServer.Plugins.Shopstr");
        }
    }
}
