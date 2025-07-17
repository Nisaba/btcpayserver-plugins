using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System;

namespace BTCPayServer.Plugins.Lendasat.Services;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LendasatPluginDbContext>
{
    public LendasatPluginDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<LendasatPluginDbContext>();

        // FIXME: Somehow the DateTimeOffset column types get messed up when not using Postgres
        // https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli
        builder.UseNpgsql("User ID=postgres;Host=127.0.0.1;Port=39372;Database=designtimebtcpay");

        return new LendasatPluginDbContext(builder.Options, true);
    }
}

public class LendasatPluginDbContextFactory : BaseDbContextFactory<LendasatPluginDbContext>
{
    public LendasatPluginDbContextFactory(IOptions<DatabaseOptions> options) : base(options, "BTCPayServer.Plugins.Lendasat")
    {
    }

    public override LendasatPluginDbContext CreateContext(Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null)
    {
        var builder = new DbContextOptionsBuilder<LendasatPluginDbContext>();
        ConfigureBuilder(builder);
        return new LendasatPluginDbContext(builder.Options);
    }
}
