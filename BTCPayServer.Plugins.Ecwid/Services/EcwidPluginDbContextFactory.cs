using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System;

namespace BTCPayServer.Plugins.Ecwid.Services;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<EcwidPluginDbContext>
{
    public EcwidPluginDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<EcwidPluginDbContext>();

        // FIXME: Somehow the DateTimeOffset column types get messed up when not using Postgres
        // https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli
        builder.UseNpgsql("User ID=postgres;Host=127.0.0.1;Port=39372;Database=designtimebtcpay");

        return new EcwidPluginDbContext(builder.Options, true);
    }
}

public class EcwidPluginDbContextFactory : BaseDbContextFactory<EcwidPluginDbContext>
{
    public EcwidPluginDbContextFactory(IOptions<DatabaseOptions> options) : base(options, "BTCPayServer.Plugins.Ecwid")
    {
    }

    public override EcwidPluginDbContext CreateContext(Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null)
    {
        var builder = new DbContextOptionsBuilder<EcwidPluginDbContext>();
        ConfigureBuilder(builder);
        return new EcwidPluginDbContext(builder.Options);
    }
}
