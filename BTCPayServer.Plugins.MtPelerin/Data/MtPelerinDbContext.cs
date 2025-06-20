using BTCPayServer.Plugins.MtPelerin.Model;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.MtPelerin;

public class MtPelerinPluginDbContext : DbContext
{
    private readonly bool _designTime;

    public MtPelerinPluginDbContext(DbContextOptions<MtPelerinPluginDbContext> options, bool designTime = false)
        : base(options)
    {
        _designTime = designTime;
    }

    public DbSet<MtPelerinSettings> MtPelerinSettings { get; set; }
    public DbSet<MtPelerinTx> MtPelerinTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("BTCPayServer.Plugins.MtPelerin");
    }
}
