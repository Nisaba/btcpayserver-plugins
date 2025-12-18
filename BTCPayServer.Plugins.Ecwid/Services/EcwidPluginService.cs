using BTCPayServer.Client;
using BTCPayServer.Client.Models;
using BTCPayServer.Plugins.Ecwid.Data;
using BTCPayServer.Plugins.Ecwid.Model;
using BTCPayServer.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json.Linq;
using System;
using System.Globalization;
using System.IO;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Ecwid.Services
{
    public class EcwidPluginService(EcwidPluginDbContextFactory pluginDbContextFactory, ILogger<EcwidPluginService> logger)
    {
        public async Task<EcwidSettings> GetStoreSettings(string storeId)
        {
            try
            {
                await using var _context = pluginDbContextFactory.CreateContext();
                var settings = await _context.EcwidSettings.FirstOrDefaultAsync(a => a.StoreId == storeId);
                if (settings == null)
                {
                    settings = new EcwidSettings { StoreId = storeId, ClientSecret = ""};
                }
                return settings;

            }
            catch (Exception e)
            {
                logger.LogError(e, "EcwidPlugin:GetStoreSettings()");
                throw;
            }
        }

        public async Task UpdateSettings(EcwidSettings settings)
        {
            try
            {
                await using var _context = pluginDbContextFactory.CreateContext();
                var dbSettings = await _context.EcwidSettings.FirstOrDefaultAsync(a => a.StoreId == settings.StoreId);
                if (dbSettings == null)
                {
                    _context.EcwidSettings.Add(settings);
                }
                else
                {
                    dbSettings.ClientSecret = settings.ClientSecret.Trim();
                    _context.EcwidSettings.Update(dbSettings);
                }

                await _context.SaveChangesAsync();
                return;

            }
            catch (Exception e)
            {
                logger.LogError(e, "EcwidPlugin:UpdateSettings()");
                throw;
            }
        }

        public async Task<string> CreateBTCPayInvoice(EcwidPaymentRequest request, Uri btcpayUri)
        {
            try
            {
                var ecwidJson = GetEcwidPayload(request.ClientSecret, request.EncryptedData);

                var invoiceReq = new CreateInvoiceRequest()
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
                        ecwidRefTransactionId = ecwidJson["cart"]["order"]["referenceTransactionId"].ToString(),
                        ecwidUrl = ecwidJson["cart"]["order"]["globalReferer"].ToString(),
                        ecwidToken = ecwidJson["token"].ToString()
                    }),
                    Receipt = new InvoiceDataBase.ReceiptOptions() { Enabled = true }
                };

                var client = new BTCPayServerClient(btcpayUri);
                var invoice = await client.CreateInvoice(request.BTCPayStoreID, invoiceReq);
                return invoice.CheckoutLink;

            }
            catch (Exception e)
            {
                logger.LogError(e, "EcwidPlugin:CreateBTCPayInvoice()");
                throw;
            }
        }

        public async Task UpdateOrder(EcwidCallbackModel model)
        {
            try
            {
                using HttpClient client = new HttpClient();
                client.DefaultRequestHeaders.Add("Authorization", $"Bearer {model.Token}");

                var data = new { paymentStatus = model.PaymentStatus };
                string jsonData = JsonSerializer.Serialize(data);
                HttpContent content = new StringContent(jsonData, Encoding.UTF8, "application/json");

                var url = $"https://app.ecwid.com/api/v3/{model.StoreId}/orders/{model.TransactionId}";
                using (var rep = await client.PutAsync(url, content))
                {
                    rep.EnsureSuccessStatusCode();
                    await rep.Content.ReadAsStringAsync();
                }

            } catch (Exception e)
            {
                logger.LogError(e, "EcwidPlugin:UpdateOrder()");
                throw;
            }
        }

        private JObject GetEcwidPayload(string appSecretKey, string encryptedData)
        {
            try
            {
                string encryptionKey = appSecretKey.Substring(0, 16);
                string jsonData = Aes128Decrypt(encryptionKey, encryptedData);
                return JObject.Parse(jsonData);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "EcwidPlugin:GetEcwidPayload()");
                throw;
            }
        }

        private string Aes128Decrypt(string key, string encryptedData)
        {
            int mod4 = encryptedData.Length % 4;
            if (mod4 > 0)
            {
                encryptedData += new string('=', 4 - mod4);
            }
            string base64Original = encryptedData.Replace('-', '+').Replace('_', '/');

            byte[] decoded = Convert.FromBase64String(base64Original);

            byte[] iv = new byte[16];
            Array.Copy(decoded, iv, 16);

            byte[] payload = new byte[decoded.Length - 16];
            Array.Copy(decoded, 16, payload, 0, payload.Length);

            using (Aes aes = Aes.Create())
            {
                aes.Key = Encoding.UTF8.GetBytes(key);
                aes.IV = iv;
                aes.Mode = CipherMode.CBC;
                aes.Padding = PaddingMode.PKCS7;

                using (ICryptoTransform decryptor = aes.CreateDecryptor())
                {
                    //byte[] decryptedBytes = decryptor.TransformFinalBlock(payload, 0, payload.Length);
                    //return Encoding.UTF8.GetString(decryptedBytes);

                    using (MemoryStream msDecrypt = new MemoryStream(payload))
                    {
                        using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                        {
                            using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                            {
                                return srDecrypt.ReadToEnd();
                            }
                        }
                    }
                }
            }
        }

    }
}
