using BTCPayServer.Plugins.Ecwid.Services;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Ecwid
{
    [Route("~/plugins/{storeId}/EcwidPayment")]
//    [Authorize(Policy = Policies.Unrestricted)]
    public class EcwidPaymentController : Controller
    {
        private readonly EcwidPluginService _PluginService;
        private readonly StoreRepository _storeRepository;
        private readonly ILogger<EcwidPaymentController> _logger;

        public EcwidPaymentController(EcwidPluginService PluginService,
                                      StoreRepository storeRepository,
                                      ILogger<EcwidPaymentController> logger)
        {
            _PluginService = PluginService;
            _storeRepository = storeRepository;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> Index([FromForm] string data)
        {
            try
            {
                var storeId = Request.Path.Value.Replace("plugins/", "").Replace("/EcwidPayment", "").Replace("/", "");
                var settings = await _PluginService.GetStoreSettings(storeId);

                var store = await _storeRepository.FindStore(storeId);
                HttpContext.SetStoreData(store);

                var CheckoutLink = await _PluginService.CreateBTCPayInvoice(settings.ClientSecret, Uri.UnescapeDataString(data), storeId);

                return Redirect(CheckoutLink);
            } catch (Exception ex)
            {
                _logger.LogError(ex, "EcwidPlugin:PaymentController()");
                return BadRequest(ex.Message);
            }
        }
    }
}
