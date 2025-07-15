using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;

#nullable disable

namespace BTCPayServer.Plugins.Peach.Migrations
{
    [DbContext(typeof(PeachPluginDbContext))]
    partial class PeachPluginDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("BTCPayServer.Plugins.Peach")
                .HasAnnotation("ProductVersion", "1.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("BTCPayServer.Plugins.Peach.Model.PeachSettings", b =>
            {
                b.Property<string>("StoreId")
                    .HasColumnType("text");

                b.Property<string>("PublicKey")
                    .HasColumnType("text");

                b.Property<string>("PrivateKey")
                    .HasColumnType("text");

                b.Property<string>("IsRegistered")
                    .HasColumnType("bit");

                b.HasKey("StoreId");

                b.ToTable("PeachSettings", "BTCPayServer.Plugins.Peach");
            });

            modelBuilder.Entity("BTCPayServer.Plugins.Peach.Model.MeansOfPayments", b =>
            {
                b.Property<string>("StoreId")
                    .HasColumnType("text");

                b.Property<string>("MoP")
                    .HasColumnType("text");

                b.Property<string>("HashPaymentData")
                    .HasColumnType("text");

                b.HasKey("StoreId", "MoP");

                b.ToTable("MeansOfPayment", "BTCPayServer.Plugins.Peach");
            });

        }
    }
}
