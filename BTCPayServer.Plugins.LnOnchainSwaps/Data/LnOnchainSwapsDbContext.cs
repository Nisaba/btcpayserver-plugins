using BTCPayServer.Plugins.LnOnchainSwaps.Models;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Data;

public class LnOnchainSwapsDbContext : DbContext
{
    private readonly bool _designTime;

    public LnOnchainSwapsDbContext(DbContextOptions<LnOnchainSwapsDbContext> options, bool designTime = false)
        : base(options)
    {
        _designTime = designTime;
    }

    public DbSet<Settings> Settings { get; set; }
    public DbSet<BoltzSwap> BoltzSwaps { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("BTCPayServer.Plugins.LnOnchainSwaps");
    }
}
