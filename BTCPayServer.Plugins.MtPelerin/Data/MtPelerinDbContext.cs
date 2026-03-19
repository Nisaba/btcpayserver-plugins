using BTCPayServer.Plugins.MtPelerin.Model;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.MtPelerin;

public class MtPelerinPluginDbContext : DbContext
{
    public DbSet<MtPelerinSettings> MtPelerinSettings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("BTCPayServer.Plugins.MtPelerin");
    }
}
