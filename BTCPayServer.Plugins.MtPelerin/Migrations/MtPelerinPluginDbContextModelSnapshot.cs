using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;

#nullable disable

namespace BTCPayServer.Plugins.MtPelerin.Migrations
{
    [DbContext(typeof(MtPelerinPluginDbContext))]
    partial class MtPelerinPluginDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("BTCPayServer.Plugins.MtPelerin")
                .HasAnnotation("ProductVersion", "1.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("BTCPayServer.Plugins.MtPelerin.Model.MtPelerinSettings", b =>
            {
                b.Property<string>("StoreId")
                    .HasColumnType("text");

                b.Property<string>("UseBridgeApp")
                    .HasColumnType("bit");

                b.Property<string>("Lang")
                    .HasColumnType("text");

                b.Property<string>("Phone")
                    .HasColumnType("text");

                b.HasKey("StoreId");

                b.ToTable("MtPelerinSettings", "BTCPayServer.Plugins.MtPelerin");
            });

        }
    }
}
