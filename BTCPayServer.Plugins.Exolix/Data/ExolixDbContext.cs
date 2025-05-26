using BTCPayServer.Plugins.Exolix.Model;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.Exolix;

public class ExolixPluginDbContext : DbContext
{
    private readonly bool _designTime;

    public ExolixPluginDbContext(DbContextOptions<ExolixPluginDbContext> options, bool designTime = false)
        : base(options)
    {
        _designTime = designTime;
    }

    public DbSet<ExolixSettings> ExolixSettings { get; set; }
    public DbSet<ExolixTx> ExolixTransactions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("BTCPayServer.Plugins.Exolix");
    }
}
