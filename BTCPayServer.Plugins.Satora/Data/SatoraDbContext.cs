using BTCPayServer.Plugins.Satora.Models;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.Satora;

public class SatoraPluginDbContext : DbContext
{
    private readonly bool _designTime;

    public SatoraPluginDbContext(DbContextOptions<SatoraPluginDbContext> options, bool designTime = false)
        : base(options)
    {
        _designTime = designTime;
    }

    public DbSet<SatoraSettings> SatoraSettings { get; set; }
    public DbSet<SatoraTx> SatoraTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("BTCPayServer.Plugins.Satora");
    }
}
