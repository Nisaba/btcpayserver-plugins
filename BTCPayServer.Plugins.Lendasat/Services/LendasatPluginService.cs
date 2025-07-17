using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System;
using BTCPayServer.Plugins.Lendasat.Models;
using Microsoft.Extensions.Logging;

namespace BTCPayServer.Plugins.Lendasat.Services
{
    public class LendasatPluginService
    {
        private readonly LendasatPluginDbContext _context;
        private readonly ILogger<LendasatPluginService> _logger;
        public LendasatPluginService(LendasatPluginDbContextFactory pluginDbContextFactory,
                                     ILogger<LendasatPluginService> logger)
        {
            _context = pluginDbContextFactory.CreateContext();
            _logger = logger;
        }

        public async Task<LendasatSettings> GetStoreSettings(string storeId)
        {
            try
            {
                var settings = await _context.LendasatSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                if (settings == null)
                {
                    settings = new LendasatSettings { StoreId = storeId, APIKey = string.Empty };
                }
                return settings;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "LendasatPlugin:GetStoreSettings()");
                throw;
            }
        }

        public async Task UpdateSettings(LendasatSettings settings)
        {
            try
            {
                var dbSettings = await _context.LendasatSettings.FirstOrDefaultAsync(a => a.StoreId == settings.StoreId);
                if (dbSettings == null)
                {
                    _context.LendasatSettings.Add(settings);
                }
                else
                {
                    dbSettings.APIKey = settings.APIKey;
                    _context.LendasatSettings.Update(dbSettings);
                }

                await _context.SaveChangesAsync();
                return;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "LendasatPlugin:UpdateSettings()");
                throw;
            }
        }
    }
}
