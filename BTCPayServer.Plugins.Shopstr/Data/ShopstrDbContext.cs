using BTCPayServer.Plugins.Shopstr.Models;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.Shopstr.Data;

public class ShopstrDbContext : DbContext
{
    private readonly bool _designTime;

    public ShopstrDbContext(DbContextOptions<ShopstrDbContext> options, bool designTime = false)
        : base(options)
    {
        _designTime = designTime;
    }

    public DbSet<ShopstrSettings> Settings { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("BTCPayServer.Plugins.Shopstr");
    }
}
