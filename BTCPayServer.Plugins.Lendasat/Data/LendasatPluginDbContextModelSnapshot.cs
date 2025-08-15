using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;

#nullable disable

namespace BTCPayServer.Plugins.Lendasat.Data
{
    [DbContext(typeof(LendasatPluginDbContext))]
    partial class LendasatPluginDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("BTCPayServer.Plugins.Lendasat")
                .HasAnnotation("ProductVersion", "1.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("BTCPayServer.Plugins.Lendasat.Model.LendasatSettings", b =>
            {
                b.Property<string>("StoreId")
                    .HasColumnType("text");

                b.Property<string>("APIKey")
                    .HasColumnType("text");

                b.HasKey("StoreId");

                b.ToTable("LendasatSettings", "BTCPayServer.Plugins.Lendasat");
            });

        }
    }
}
