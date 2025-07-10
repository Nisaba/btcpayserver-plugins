using BTCPayServer.Plugins.Peach.Model;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.Peach;

public class PeachPluginDbContext : DbContext
{
    private readonly bool _designTime;

    public PeachPluginDbContext(DbContextOptions<PeachPluginDbContext> options, bool designTime = false)
        : base(options)
    {
        _designTime = designTime;
    }

    public DbSet<PeachSettings> PeachSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("BTCPayServer.Plugins.Peach");
    }
}
