using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BTCPayServer.Plugins.Serilog.Data;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.Serilog.Services;

public class SerilogService
{
    private readonly SerilogDbContextFactory _PluginDbContextFactory;

    public SerilogService(SerilogDbContextFactory PluginDbContextFactory)
    {
        _PluginDbContextFactory = PluginDbContextFactory;
    }

    public async Task AddTestDataRecord()
    {
        await using var context = _PluginDbContextFactory.CreateContext();

        await context.PluginRecords.AddAsync(new PluginData { Timestamp = DateTimeOffset.UtcNow });
        await context.SaveChangesAsync();
    }

    public async Task<List<PluginData>> Get()
    {
        await using var context = _PluginDbContextFactory.CreateContext();

        return await context.PluginRecords.ToListAsync();
    }
}

