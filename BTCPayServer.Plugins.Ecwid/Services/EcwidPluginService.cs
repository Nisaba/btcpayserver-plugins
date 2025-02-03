using BTCPayServer.Plugins.Ecwid.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using BTCPayServer.Client;
using BTCPayServer.Client.Models;
using Newtonsoft.Json.Linq;
using static Dapper.SqlMapper;
using static System.Runtime.InteropServices.JavaScript.JSType;
using System.Linq;

namespace BTCPayServer.Plugins.Ecwid.Services
{
    public class EcwidPluginService
    {
        private readonly ILogger<EcwidPluginService> _logger;
        private readonly EcwidPluginDbContext _context;
        private readonly BTCPayServerClient _client;

        public EcwidPluginService(EcwidPluginDbContextFactory pluginDbContextFactory, ILogger<EcwidPluginService> logger, BTCPayServerClient client)
        {
            _logger = logger;
            _context = pluginDbContextFactory.CreateContext();
            _client = client;
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
                _logger.LogError(e, "EcwidPlugin:GetStoreSettings()");
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
                _logger.LogError(e, "EcwidPlugin:UpdateSettings()");
                throw;
            }
        }

        public async Task<string> CreateBTCPayInvoice(string ecwidSecretKey, string ecwidData, string storeId)
        {
            try
            {
                var ecwidJson = GetEcwidPayload(ecwidSecretKey, ecwidData);
                var req = new CreateInvoiceRequest()
                {
                    Currency = ecwidJson.Currency,
                    Amount = ecwidJson.Order.Amount,
                    Checkout = new InvoiceDataBase.CheckoutOptions()
                    {
                        DefaultLanguage = ecwidJson.Lang,
                        RedirectURL = ecwidJson.ReturnURL,
                        RedirectAutomatically = true
                    },
                    Metadata = JObject.FromObject(new
                    {
                        itemDesc = "From Ecwid",
                        buyerEmail = ecwidJson.Email,
                        ecwidStoreId = ecwidJson.StoreId,
                        ecwidOrderId = ecwidJson.Order.Id,
                        ecwidOrderNumber = ecwidJson.Order.Number,
                        ecwidUrl = ecwidJson.Order.GlobalReferer,
                    }),
                    Receipt = new InvoiceDataBase.ReceiptOptions() { Enabled = true }
                };
                var invoice = await _client.CreateInvoice(storeId, req);
                return invoice.CheckoutLink;

            }
            catch (Exception e)
            {
                _logger.LogError(e, "EcwidPlugin:CreateBTCPayInvoice()");
                throw;
            }
        }

        private dynamic GetEcwidPayload(string appSecretKey, string encryptedData)
        {
            try
            {
                encryptedData = FixBase64String(encryptedData);
                byte[] encryptedBytes = Convert.FromBase64String(encryptedData);

                byte[] encryptionKey = Encoding.UTF8.GetBytes(appSecretKey).Take(16).ToArray();
                string decryptData = Aes128Decrypt(encryptionKey, encryptedData);
                string jsonData = decryptData.Substring(decryptData.IndexOf("{"));
                _logger.LogWarning(jsonData, "EcwidPlugin:GetEcwidPayload()");

                return JsonConvert.DeserializeObject<dynamic>(jsonData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EcwidPlugin:GetEcwidPayload()");
                throw;
            }
        }

        private string Aes128Decrypt(byte[] key, string encryptedData)
        {
            byte[] data = Convert.FromBase64String(encryptedData);

            using (Aes aes = Aes.Create())
            {
                aes.Key = key;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(data, 0, data.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

        private static string FixBase64String(string base64)
        {
            base64 = base64.Replace('-', '+').Replace('_', '/');
            int mod4 = base64.Length % 4;
            if (mod4 > 0)
            {
                base64 += new string('=', 4 - mod4);
            }
            return base64;
        }

    }
}
