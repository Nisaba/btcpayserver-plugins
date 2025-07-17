using BTCPayServer.Plugins.Lendasat.Models;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.Lendasat;

public class LendasatPluginDbContext : DbContext
{
    private readonly bool _designTime;

    public LendasatPluginDbContext(DbContextOptions<LendasatPluginDbContext> options, bool designTime = false)
        : base(options)
    {
        _designTime = designTime;
    }

    public DbSet<LendasatSettings> LendasatSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("BTCPayServer.Plugins.Lendasat");
    }
}
