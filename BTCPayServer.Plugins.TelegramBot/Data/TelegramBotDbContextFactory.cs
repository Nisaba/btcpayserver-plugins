using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Abstractions.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Options;
using Npgsql.EntityFrameworkCore.PostgreSQL.Infrastructure;
using System;

namespace BTCPayServer.Plugins.TelegramBot.Data;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<TelegramBotDbContext>
{
    public TelegramBotDbContext CreateDbContext(string[] args)
    {
        var builder = new DbContextOptionsBuilder<TelegramBotDbContext>();

        // FIXME: Somehow the DateTimeOffset column types get messed up when not using Postgres
        // https://docs.microsoft.com/en-us/ef/core/managing-schemas/migrations/providers?tabs=dotnet-core-cli
        builder.UseNpgsql("User ID=postgres;Host=127.0.0.1;Port=39372;Database=designtimebtcpay");

        return new TelegramBotDbContext(builder.Options, true);
    }
}

public class TelegramBotDbContextFactory : BaseDbContextFactory<TelegramBotDbContext>
{
    public TelegramBotDbContextFactory(IOptions<DatabaseOptions> options) : base(options, "BTCPayServer.Plugins.TelegramBot")
    {
    }

    public override TelegramBotDbContext CreateContext(Action<NpgsqlDbContextOptionsBuilder> npgsqlOptionsAction = null)
    {
        var builder = new DbContextOptionsBuilder<TelegramBotDbContext>();
        ConfigureBuilder(builder);
        return new TelegramBotDbContext(builder.Options);
    }
}
