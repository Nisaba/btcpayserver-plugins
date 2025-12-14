using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System;

namespace BTCPayServer.Plugins.B2PCentral.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<B2PCentralPluginDbContext>
{
    public B2PCentralPluginDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<B2PCentralPluginDbContext>();

        // FIXME: Somehow the DateTimeOffset column types get messed up when not using Postgres
        // https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli
        builder.UseNpgsql("User ID=postgres;Host=127.0.0.1;Port=39372;Database=designtimebtcpay");

        return new B2PCentralPluginDbContext(builder.Options, true);
    }
}

public class B2PCentralPluginDbContextFactory : BaseDbContextFactory<B2PCentralPluginDbContext>
{
    public B2PCentralPluginDbContextFactory(IOptions<DatabaseOptions> options) : base(options, "BTCPayServer.Plugins.B2PCentral")
    {
    }

    public override B2PCentralPluginDbContext CreateContext(Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null)
    {
        var builder = new DbContextOptionsBuilder<B2PCentralPluginDbContext>();
        ConfigureBuilder(builder);
        return new B2PCentralPluginDbContext(builder.Options);
    }
}
