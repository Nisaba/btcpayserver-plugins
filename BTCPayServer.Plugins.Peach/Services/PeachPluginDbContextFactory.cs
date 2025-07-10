using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System;

namespace BTCPayServer.Plugins.Peach.Services;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<PeachPluginDbContext>
{
    public PeachPluginDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<PeachPluginDbContext>();

        // FIXME: Somehow the DateTimeOffset column types get messed up when not using Postgres
        // https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli
        builder.UseNpgsql("User ID=postgres;Host=127.0.0.1;Port=39372;Database=designtimebtcpay");

        return new PeachPluginDbContext(builder.Options, true);
    }
}

public class PeachPluginDbContextFactory : BaseDbContextFactory<PeachPluginDbContext>
{
    public PeachPluginDbContextFactory(IOptions<DatabaseOptions> options) : base(options, "BTCPayServer.Plugins.Peach")
    {
    }

    public override PeachPluginDbContext CreateContext(Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null)
    {
        var builder = new DbContextOptionsBuilder<PeachPluginDbContext>();
        ConfigureBuilder(builder);
        return new PeachPluginDbContext(builder.Options);
    }
}
