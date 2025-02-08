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
    public class EcwidPaymentController(EcwidPluginService ecwidService,
                                          StoreRepository storeRepository,
                                          ILogger<EcwidPaymentController> logger) : Controller
    {
        private readonly EcwidPluginService _ecwidService = ecwidService;
        private readonly StoreRepository _storeRepository = storeRepository;
        private readonly ILogger<EcwidPaymentController> _logger = logger;


        [HttpPost]
        public async Task<IActionResult> Index([FromForm] string data)
        {
            try
            {
                var storeId = Request.Path.Value.Replace("plugins/", "").Replace("/EcwidPayment", "").Replace("/", "");
                var settings = await _ecwidService.GetStoreSettings(storeId);

                var store = await _storeRepository.FindStore(storeId);
                HttpContext.SetStoreData(store);

                var req = new EcwidPaymentRequest
                {
                    ClientSecret = settings.ClientSecret,
                    BTCPayStoreID = storeId,
                    EncryptedData = Uri.UnescapeDataString(data)
                };

                var CheckoutLink = await _ecwidService.CreateBTCPayInvoice(req);

                return Redirect(CheckoutLink);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "EcwidPlugin:PaymentController()");
                return BadRequest(ex.Message);
            }
        }
    }
}
