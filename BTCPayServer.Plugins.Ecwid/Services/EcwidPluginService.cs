using BTCPayServer.Client;
using BTCPayServer.Client.Models;
using BTCPayServer.Plugins.Ecwid.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using static NBitcoin.Scripting.OutputDescriptor;

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
                    Currency = ecwidJson["cart"]["currency"].ToString(),
                    Amount = decimal.Parse(ecwidJson["cart"]["order"]["total"].ToString(), CultureInfo.InvariantCulture),
                    Checkout = new InvoiceDataBase.CheckoutOptions()
                    {
                        DefaultLanguage = ecwidJson["lang"].ToString(),
                        RedirectURL = ecwidJson["returnUrl"].ToString(),
                        RedirectAutomatically = true,
                    },
                    Metadata = JObject.FromObject(new
                    {
                        itemDesc = "From Ecwid",
                        buyerEmail = ecwidJson["cart"]["order"]["email"].ToString(),
                        ecwidStoreId = ecwidJson["storeId"].ToString(),
                        ecwidOrderId = ecwidJson["cart"]["order"]["id"].ToString(),
                        ecwidOrderNumber = ecwidJson["cart"]["order"]["orderNumber"].ToString(),
                        ecwidUrl = ecwidJson["cart"]["order"]["globalReferer"].ToString(),
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
                string decryptData = Aes128Decrypt(appSecretKey, FixBase64String(encryptedData));
                string jsonData = decryptData.Substring(decryptData.IndexOf("{"));
                return JObject.Parse(jsonData);
                //return JsonConvert.DeserializeObject<dynamic>(jsonData);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "EcwidPlugin:GetEcwidPayload()");
                throw;
            }
        }

        private string Aes128Decrypt(string key, string encryptedData)
        {
            byte[] data = Convert.FromBase64String(encryptedData);
            byte[] encryptionKey = Encoding.UTF8.GetBytes(key.Substring(0, 16));

            using (Aes aes = Aes.Create())
            {
                aes.Key = encryptionKey;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    byte[] decryptedBytes = decryptor.TransformFinalBlock(data, 0, data.Length);
                    return Encoding.UTF8.GetString(decryptedBytes);
                }
            }
        }

        private string FixBase64String(string base64)
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
