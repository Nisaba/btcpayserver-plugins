using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;

#nullable disable

namespace BTCPayServer.Plugins.Ecwid.Migrations
{
    [DbContext(typeof(EcwidPluginDbContext))]
    partial class EcwidCentralPluginDbContextModelSnapshot : ModelSnapshot
    {
        protected override void BuildModel(ModelBuilder modelBuilder)
        {
#pragma warning disable 612, 618
            modelBuilder
                .HasDefaultSchema("BTCPayServer.Plugins.Ecwid")
                .HasAnnotation("ProductVersion", "6.0.9")
                .HasAnnotation("Relational:MaxIdentifierLength", 63);

            NpgsqlModelBuilderExtensions.UseIdentityByDefaultColumns(modelBuilder);

            modelBuilder.Entity("BTCPayServer.Plugins.Ecwid.Data.EcwidData", b =>
                {
                    b.Property<string>("StoreId")
                        .HasColumnType("text");

                    b.Property<string>("WebhookSecret")
                        .HasColumnType("text");

                    b.Property<string>("ClientSecret")
                        .HasColumnType("text");

                    b.HasKey("StoreId");

                    b.ToTable("EcwidSettings", "BTCPayServer.Plugins.Ecwid");
                });
#pragma warning restore 612, 618
        }
    }
}
