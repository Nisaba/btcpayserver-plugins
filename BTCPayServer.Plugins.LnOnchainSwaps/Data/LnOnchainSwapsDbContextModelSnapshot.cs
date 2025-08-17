using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;

#nullable disable

namespace BTCPayServer.Plugins.LnOnchainSwaps.Data
{
    [DbContext(typeof(LnOnchainSwapsDbContext))]
    partial class LnOnchainSwapsDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("BTCPayServer.Plugins.LnOnchainSwaps")
                .HasAnnotation("ProductVersion", "1.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("BTCPayServer.Plugins.LnOnchainSwaps.Models.BoltzSwaps", b =>
            {
                b.Property<string>("SwapId")
                    .HasColumnType("text");

                b.Property<string>("StoreId")
                    .HasColumnType("text");

                b.Property<string>("Type")
                    .HasColumnType("text");

                b.Property<string>("PreImageHash")
                    .HasColumnType("text");

                b.Property<string>("Destination")
                    .HasColumnType("text");

                b.Property<string>("ExpectedAmount")
                    .HasColumnType("decimal(18,8)");

                b.Property<string>("BTCPayPayoutId")
                    .HasColumnType("text");

                b.Property<string>("Json")
                    .HasColumnType("text");

                b.HasKey("SwapId");
                b.HasIndex("StoreId");

                b.ToTable("BoltzSwaps", "BTCPayServer.Plugins.LnOnchainSwaps");
            });

        }
    }
}
