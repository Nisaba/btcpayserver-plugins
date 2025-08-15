using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System;

namespace BTCPayServer.Plugins.LnOnchainSwaps.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<LnOnchainSwapsDbContext>
{
    public LnOnchainSwapsDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<LnOnchainSwapsDbContext>();

        // FIXME: Somehow the DateTimeOffset column types get messed up when not using Postgres
        // https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli
        builder.UseNpgsql("User ID=postgres;Host=127.0.0.1;Port=39372;Database=designtimebtcpay");

        return new LnOnchainSwapsDbContext(builder.Options, true);
    }
}

public class LnOnchainSwapsDbContextFactory : BaseDbContextFactory<LnOnchainSwapsDbContext>
{
    public LnOnchainSwapsDbContextFactory(IOptions<DatabaseOptions> options) : base(options, "BTCPayServer.Plugins.LnOnchainSwaps")
    {
    }

    public override LnOnchainSwapsDbContext CreateContext(Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null)
    {
        var builder = new DbContextOptionsBuilder<LnOnchainSwapsDbContext>();
        ConfigureBuilder(builder);
        return new LnOnchainSwapsDbContext(builder.Options);
    }
}
