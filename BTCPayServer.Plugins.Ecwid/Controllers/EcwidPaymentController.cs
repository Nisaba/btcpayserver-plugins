using BTCPayServer.Plugins.Ecwid.Model;
using BTCPayServer.Plugins.Ecwid.Services;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Ecwid
{
    [Route("~/plugins/{storeId}/EcwidPayment")]
    public class EcwidPaymentController(EcwidPluginService pluginService,
                                          StoreRepository storeRepository,
                                          ILogger<EcwidPaymentController> logger) : Controller
    {
        private readonly EcwidPluginService _pluginService = pluginService;
        private readonly StoreRepository _storeRepository = storeRepository;
        private readonly ILogger<EcwidPaymentController> _logger = logger;


        [HttpPost]
        public async Task<IActionResult> Index([FromForm] string data)
        {
            try
            {
                var storeId = Request.Path.Value.Replace("plugins/", "").Replace("/EcwidPayment", "").Replace("/", "");
                var settings = await _pluginService.GetStoreSettings(storeId);

                var store = await _storeRepository.FindStore(storeId);
                HttpContext.SetStoreData(store);

                var req = new EcwidPaymentRequest
                {
                    ClientSecret = settings.ClientSecret,
                    BTCPayStoreID = storeId,
                    EncryptedData = data,
                    RedirectUrl = $"{Request.Scheme}://{Request.Host}{Request.Path.ToString().Replace("Payment", "Webhook")}"
                };

                var CheckoutLink = await _pluginService.CreateBTCPayInvoice(req);

                return Redirect(CheckoutLink);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "EcwidPlugin:PaymentController()");
                return BadRequest(ex.Message);
            }
        }
    }
}
