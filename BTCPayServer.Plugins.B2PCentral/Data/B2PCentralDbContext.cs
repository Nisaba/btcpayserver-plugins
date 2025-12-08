using BTCPayServer.Plugins.B2PCentral.Models;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.B2PCentral;

public class B2PCentralPluginDbContext : DbContext
{
    private readonly bool _designTime;

    public B2PCentralPluginDbContext(DbContextOptions<B2PCentralPluginDbContext> options, bool designTime = false)
        : base(options)
    {
        _designTime = designTime;
    }

    public DbSet<B2PSettings> B2PSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("BTCPayServer.Plugins.B2PCentral");
    }
}
