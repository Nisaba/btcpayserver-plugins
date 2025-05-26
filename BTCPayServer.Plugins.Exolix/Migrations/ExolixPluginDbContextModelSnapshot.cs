using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System;
using System.Collections.Generic;

#nullable disable

namespace BTCPayServer.Plugins.Exolix.Migrations
{
    [DbContext(typeof(ExolixPluginDbContext))]
    partial class ExolixPluginDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
            modelBuilder
                .HasDefaultSchema("BTCPayServer.Plugins.Exolix")
                .HasAnnotation("ProductVersion", "1.0.1")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("BTCPayServer.Plugins.Exolix.Model.ExolixSettings", b =>
            {
                b.Property<string>("StoreId")
                    .HasColumnType("text");

                b.Property<bool>("ClientSecret")
                    .HasColumnType("bit");

                b.Property<List<string>>("AcceptedCryptos")
                    .HasColumnType("text");

                b.Property<bool>("IsEmailToCustomer")
                    .HasColumnType("bit");

                b.Property<bool>("AllowRefundAddress")
                    .HasColumnType("bit");

                b.HasKey("StoreId");

                b.ToTable("ExolixSettings", "BTCPayServer.Plugins.Exolix");
            });

            modelBuilder.Entity("BTCPayServer.Plugins.Exolix.Model.ExolixTx", b =>
            {
                b.Property<string>("TxID")
                    .HasColumnType("text");

                b.Property<string>("StoreId")
                    .HasColumnType("text");

                b.Property<string>("AltcoinFrom")
                    .HasColumnType("text");

                b.Property<decimal>("BTCAmount")
                    .HasColumnType("decimal");

                b.Property<DateTime>("DateT")
                    .HasColumnType("DateTime");

                b.HasKey("TxID");
                b.HasIndex("StoreId");

                b.ToTable("ExolixTransactions", "BTCPayServer.Plugins.Exolix");
            });
        }
    }
}
