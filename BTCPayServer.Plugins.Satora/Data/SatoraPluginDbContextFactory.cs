using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;

namespace BTCPayServer.Plugins.Satora.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<SatoraPluginDbContext>
{
    public SatoraPluginDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<SatoraPluginDbContext>();

        // FIXME: Somehow the DateTimeOffset column types get messed up when not using Postgres
        // https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli
        builder.UseNpgsql("User ID=postgres;Host=127.0.0.1;Port=39372;Database=designtimebtcpay");

        return new SatoraPluginDbContext(builder.Options, true);
    }
}

public class SatoraPluginDbContextFactory : BaseDbContextFactory<SatoraPluginDbContext>
{
    public SatoraPluginDbContextFactory(IOptions<DatabaseOptions> options) : base(options, "BTCPayServer.Plugins.Satora")
    {
    }

    public override SatoraPluginDbContext CreateContext(Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null)
    {
        var builder = new DbContextOptionsBuilder<SatoraPluginDbContext>();
        ConfigureBuilder(builder);
        return new SatoraPluginDbContext(builder.Options);
    }
}
