using BTCPayServer.Plugins.Ecwid.Model;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.Ecwid;

public class EcwidPluginDbContext : DbContext
{
    private readonly bool _designTime;

    public EcwidPluginDbContext(DbContextOptions<EcwidPluginDbContext> options, bool designTime = false)
        : base(options)
    {
        _designTime = designTime;
    }

    public DbSet<EcwidSettings> EcwidSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("BTCPayServer.Plugins.Ecwid");
    }
}
