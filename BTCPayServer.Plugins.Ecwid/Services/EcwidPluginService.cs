using BTCPayServer.Plugins.Ecwid.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Ecwid.Services
{
    public class EcwidPluginService
    {
        private readonly ILogger<EcwidPluginService> _logger;
        private readonly EcwidPluginDbContext _context;

        public EcwidPluginService(EcwidPluginDbContextFactory pluginDbContextFactory, ILogger<EcwidPluginService> logger)
        {
            _logger = logger;
            _context = pluginDbContextFactory.CreateContext();
        }

        public async Task<EcwidSettings> GetStoreSettings(string storeId)
        {
            try
            {
                var settings = await _context.EcwidSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                if (settings == null)
                {
                    settings = new EcwidSettings { StoreId = storeId, ClientSecret = "" };
                }
                return settings;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Ecwid:GetStoreSettings()");
                throw;
            }
        }

        public async Task UpdateSettings(EcwidSettings settings)
        {
            try
            {
                var dbSettings = await _context.EcwidSettings.FirstOrDefaultAsync(a => a.StoreId == settings.StoreId);
                if (dbSettings == null)
                {
                    _context.EcwidSettings.Add(settings);
                }
                else
                {
                    dbSettings.ClientSecret = settings.ClientSecret;
                    _context.EcwidSettings.Update(dbSettings);
                }

                await _context.SaveChangesAsync();
                return;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "Ecwid:UpdateSettings()");
                throw;
            }
        }

        public dynamic GetEcwidPayload(string appSecretKey, string encryptedData)
        {
            byte[] encryptionKey = Encoding.UTF8.GetBytes(appSecretKey.Substring(0, 16));

            string jsonData = Aes128Decrypt(encryptionKey, encryptedData);

            return JsonConvert.DeserializeObject<dynamic>(jsonData);
        }

        private string Aes128Decrypt(byte[] key, string encryptedData)
        {
            byte[] data = Convert.FromBase64String(encryptedData);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.ECB;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(data, 0, data.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }
    }
}
