using BTCPayServer.Plugins.TelegramBot.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;

namespace BTCPayServer.Plugins.TelegramBot.Migrations
{
    [DbContext(typeof(TelegramBotDbContext))]
    [Migration("20260124_InitBaseUrl")]
    public partial class InitBaseUrl : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "Config",
                schema: "BTCPayServer.Plugins.TelegramBot",
                columns: table => new
                {
                    BaseUrl = table.Column<string>(nullable: false),
                });
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Config",
                schema: "BTCPayServer.Plugins.TelegramBot");
        }
    }
}
