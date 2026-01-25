using BTCPayServer.Plugins.TelegramBot.Models;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.TelegramBot.Data;

public class TelegramBotDbContext : DbContext
{
    private readonly bool _designTime;

    public TelegramBotDbContext(DbContextOptions<TelegramBotDbContext> options, bool designTime = false)
        : base(options)
    {
        _designTime = designTime;
    }

    public DbSet<TelegramBotSettings> Settings { get; set; }
    public DbSet<TelegramBotConfig> Config { get; set; }
    public DbSet<TelegramBotInvoices> TelegramInvoices { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);
        modelBuilder.HasDefaultSchema("BTCPayServer.Plugins.TelegramBot");
    }
}
