using Microsoft.Extensions.Logging;
using BTCPayServer.Client;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using BTCPayServer.Plugins.Exolix.Model;
using Microsoft.EntityFrameworkCore;

namespace BTCPayServer.Plugins.Exolix.Services
{
    public class ExolixPluginService
    {
        private readonly ILogger<ExolixPluginService> _logger;
        private readonly ExolixPluginDbContext _context;
        private readonly BTCPayServerClient _client;

        public ExolixPluginService(ExolixPluginDbContextFactory pluginDbContextFactory, ILogger<ExolixPluginService> logger, BTCPayServerClient client)
        {
            _logger = logger;
            _context = pluginDbContextFactory.CreateContext();
            _client = client;
        }

        public async Task<ExolixSettings> GetStoreSettings(string storeId)
        {
            try
            {
                var settings = await _context.ExolixSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                if (settings == null)
                {
                    settings = new ExolixSettings { StoreId = storeId, Enabled = false, AcceptedCryptos = new List<string>(), 
                                                    IsEmailToCustomer = false, AllowRefundAddress = false };
                }
                return settings;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "ExolixPlugin:GetStoreSettings()");
                throw;
            }
        }

        public async Task UpdateSettings(ExolixSettings settings)
        {
            try
            {
                var dbSettings = await _context.ExolixSettings.FirstOrDefaultAsync(a => a.StoreId == settings.StoreId);
                if (dbSettings == null)
                {
                    _context.ExolixSettings.Add(settings);
                }
                else
                {
                    dbSettings.Enabled = settings.Enabled;
                    dbSettings.AcceptedCryptos = new List<string>(settings.AcceptedCryptos ?? new List<string>());
                    dbSettings.IsEmailToCustomer = settings.IsEmailToCustomer;
                    dbSettings.AllowRefundAddress = settings.AllowRefundAddress;
                    _context.ExolixSettings.Update(dbSettings);
                }

                await _context.SaveChangesAsync();
                return;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "ExolixPlugin:UpdateSettings()");
                throw;
            }
        }

        public async Task<List<ExolixTx>> GetStoreTransactions(string storeId)
        {
            try
            {
                var txs = await _context.ExolixTransactions.Where(a => a.StoreId == storeId).ToListAsync();
                return txs;
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ExolixPlugin:GetStoreTransactions()");
                throw;
            }
        }

        public async Task AddStoreTransaction (ExolixTx tx)
        {
            try
            {
                await _context.ExolixTransactions.AddAsync(tx);
                await _context.SaveChangesAsync();
            }
            catch (Exception e)
            {
                _logger.LogError(e, "ExolixPlugin:AddStoreTransaction()");
                throw;
            }
        }
    }
}
