using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System;

namespace BTCPayServer.Plugins.Shopstr.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<ShopstrDbContext>
{
    public ShopstrDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<ShopstrDbContext>();

        // FIXME: Somehow the DateTimeOffset column types get messed up when not using Postgres
        // https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli
        builder.UseNpgsql("User ID=postgres;Host=127.0.0.1;Port=39372;Database=designtimebtcpay");

        return new ShopstrDbContext(builder.Options, true);
    }
}

public class ShopstrDbContextFactory : BaseDbContextFactory<ShopstrDbContext>
{
    public ShopstrDbContextFactory(IOptions<DatabaseOptions> options) : base(options, "BTCPayServer.Plugins.Shopstr")
    {
    }

    public override ShopstrDbContext CreateContext(Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null)
    {
        var builder = new DbContextOptionsBuilder<ShopstrDbContext>();
        ConfigureBuilder(builder);
        return new ShopstrDbContext(builder.Options);
    }
}
