using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Migrations;
using System;
using System.Collections.Generic;

namespace BTCPayServer.Plugins.Exolix.Migrations
{
    [DbContext(typeof(ExolixPluginDbContext))]
    [Migration("20250817_Init")]
    public partial class Index : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateIndex(
                name: "IX_ExolixTransactions_StoreId",
                schema: "BTCPayServer.Plugins.Exolix",
                table: "ExolixTransactions",
                column: "StoreId"
            );
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
        }
    }
}
