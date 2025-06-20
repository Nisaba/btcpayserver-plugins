using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System;

namespace BTCPayServer.Plugins.MtPelerin.Services;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<MtPelerinPluginDbContext>
{
    public MtPelerinPluginDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<MtPelerinPluginDbContext>();

        // FIXME: Somehow the DateTimeOffset column types get messed up when not using Postgres
        // https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli
        builder.UseNpgsql("User ID=postgres;Host=127.0.0.1;Port=39372;Database=designtimebtcpay");

        return new MtPelerinPluginDbContext(builder.Options, true);
    }
}

public class MtPelerinPluginDbContextFactory : BaseDbContextFactory<MtPelerinPluginDbContext>
{
    public MtPelerinPluginDbContextFactory(IOptions<DatabaseOptions> options) : base(options, "BTCPayServer.Plugins.MtPelerin")
    {
    }

    public override MtPelerinPluginDbContext CreateContext(Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null)
    {
        var builder = new DbContextOptionsBuilder<MtPelerinPluginDbContext>();
        ConfigureBuilder(builder);
        return new MtPelerinPluginDbContext(builder.Options);
    }
}
