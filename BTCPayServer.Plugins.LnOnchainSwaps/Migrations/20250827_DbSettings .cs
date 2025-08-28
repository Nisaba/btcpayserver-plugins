using BTCPayServer.Plugins.LnOnchainSwaps.Data;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Migrations
{
    [DbContext(typeof(LnOnchainSwapsDbContext))]
    [Migration("20250827_DbSettings")]
    public partial class DbSettings : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {

            migrationBuilder.CreateTable(
                name: "Settings",
                schema: "BTCPayServer.Plugins.LnOnchainSwaps",
                columns: table => new
                {
                    StoreId = table.Column<string>(nullable: false),
                    EncryptedRefundMnemonic = table.Column<string>(nullable: false),
                    RefundPubKey = table.Column<string>(nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Settings", x => x.StoreId);
                });

        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "Settings",
                schema: "BTCPayServer.Plugins.LnOnchainSwaps");
        }
    }
}
