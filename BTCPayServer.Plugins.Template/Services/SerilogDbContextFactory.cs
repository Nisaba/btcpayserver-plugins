using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;

namespace BTCPayServer.Plugins.Serilog;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SerilogPluginDbContext>
{
    public SerilogPluginDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<SerilogPluginDbContext> builder = new DbContextOptionsBuilder<SerilogPluginDbContext>();

        builder.UseSqlite("Data Source=temp.db");

        return new SerilogPluginDbContext(builder.Options, true);
    }
}

public class SerilogDbContextFactory : BaseDbContextFactory<SerilogPluginDbContext>
{
    public SerilogDbContextFactory(IOptions<DatabaseOptions> options) : base(options, "BTCPayServer.Plugins.Serilog")
    {
    }

    public override SerilogPluginDbContext CreateContext()
    {
        DbContextOptionsBuilder<SerilogPluginDbContext> builder = new DbContextOptionsBuilder<SerilogPluginDbContext>();
        ConfigureBuilder(builder);
        return new SerilogPluginDbContext(builder.Options);
    }
}
