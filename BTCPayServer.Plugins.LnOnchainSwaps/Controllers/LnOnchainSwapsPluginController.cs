using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Plugins.LnOnchainSwaps.Models;
using BTCPayServer.Plugins.LnOnchainSwaps.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BTCPayServer.PluginsLnOnchainSwaps.Controllers
{
    [Route("~/plugins/{storeId}/LnOnchainSwaps")]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanModifyStoreSettings)]
    [Authorize(Policy = Policies.CanCreateNonApprovedPullPayments, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [Authorize(Policy = Policies.CanManagePayouts, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [AutoValidateAntiforgeryToken]
    public class LnOnchainSwapsPluginController(LnOnchainSwapsPluginService pluginService) : Controller
    {
        private readonly LnOnchainSwapsPluginService _pluginService = pluginService;

        [HttpGet]
        public IActionResult Index([FromRoute] string storeId)
        {
            var model = new LnOnchainSwapsViewModel()
            {
                StoreId = storeId,
                IsPayoutCreated = false
            };
            return View(model);
        }
    }
}
