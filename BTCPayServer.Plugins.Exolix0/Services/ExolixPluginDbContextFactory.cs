using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System;

namespace BTCPayServer.Plugins.Exolix.Services;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ExolixPluginDbContext>
{
    public ExolixPluginDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ExolixPluginDbContext>();

        // FIXME: Somehow the DateTimeOffset column types get messed up when not using Postgres
        // https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli
        builder.UseNpgsql("User ID=postgres;Host=127.0.0.1;Port=39372;Database=designtimebtcpay");

        return new ExolixPluginDbContext(builder.Options, true);
    }
}

public class ExolixPluginDbContextFactory : BaseDbContextFactory<ExolixPluginDbContext>
{
    public ExolixPluginDbContextFactory(IOptions<DatabaseOptions> options) : base(options, "BTCPayServer.Plugins.Exolix")
    {
    }

    public override ExolixPluginDbContext CreateContext(Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null)
    {
        var builder = new DbContextOptionsBuilder<ExolixPluginDbContext>();
        ConfigureBuilder(builder);
        return new ExolixPluginDbContext(builder.Options);
    }
}
