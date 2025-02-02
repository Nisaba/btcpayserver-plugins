using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Plugins.Ecwid.Data;
using BTCPayServer.Plugins.Ecwid.Services;
using BTCPayServer.Services.Stores;
using Microsoft.AspNetCore.Authorization;
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
        public async Task<IActionResult> Index([FromBody] string data)
        {
            try
            {
                var storeId = Request.Path.Value.Replace("plugins/", "").Replace("/EcwidPayment", "").Replace("/", "");
                _logger.LogWarning($"StoreId: {storeId}", "EcwidPlugin:PaymentController()");
                var settings = await _PluginService.GetStoreSettings(storeId);

                var sData = data.PadRight(data.Length + (4 - (data.Length % 4)), '=');
                var ecwidData = _PluginService.GetEcwidPayload(settings.ClientSecret, sData);
                _logger.LogWarning($"ecwidData: {ecwidData}", "EcwidPlugin:PaymentController()");


                return Ok();
            } catch (Exception ex)
            {
                _logger.LogError(ex, "EcwidPlugin:PaymentController()");
                return BadRequest(ex.Message);
            }
        }
    }
}
