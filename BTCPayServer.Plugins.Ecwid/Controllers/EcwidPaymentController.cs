using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Plugins.Ecwid.Model;
using BTCPayServer.Plugins.Ecwid.Services;
using BTCPayServer.Services;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Ecwid
{
    [Route("~/plugins/{storeId}/EcwidPayment")]
    public class EcwidPaymentController(EcwidPluginService ecwidService,
                                          StoreRepository storeRepository,
                                          ILogger<EcwidPaymentController> logger, SettingsRepository settingsRepository) : Controller
    {

        [HttpPost]
        public async Task<IActionResult> Index([FromRoute] string storeId, [FromForm] string data)
        {
            try
            {
                var settings = await ecwidService.GetStoreSettings(storeId);
                if (string.IsNullOrEmpty(settings.ClientSecret))
                {
                    throw new Exception("Ecwid Plugin is not configured. Please configure the plugin first.");
                }

                var store = await storeRepository.FindStore(storeId);
                HttpContext.SetStoreData(store);

                var req = new EcwidPaymentRequest
                {
                    ClientSecret = settings.ClientSecret,
                    BTCPayStoreID = storeId,
                    EncryptedData = Uri.UnescapeDataString(data)
                };

                var serverSettings = await settingsRepository.GetSettingAsync<ServerSettings>();
                var baseUrl = serverSettings.BaseUrl;
                if (string.IsNullOrWhiteSpace(baseUrl) || !Uri.TryCreate(baseUrl, UriKind.Absolute, out var btcpayUri))
                {
                    throw new Exception("BTCPay Server BaseUrl is not configured ou invalide.");
                }
                var CheckoutLink = await ecwidService.CreateBTCPayInvoice(req, btcpayUri);

                return Redirect(CheckoutLink);
            } catch (Exception ex)
            {
                logger.LogError(ex, "EcwidPlugin:PaymentController()");
                return BadRequest(ex.Message);
            }
        }
    }
}
