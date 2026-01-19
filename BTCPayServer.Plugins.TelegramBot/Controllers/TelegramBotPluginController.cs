using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Plugins.TelegramBot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.TelegramBot.Controllers
{
    [Route("~/plugins/{storeId}/TelegramBot")]
    [Authorize(Policy = Policies.CanModifyStoreSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [AutoValidateAntiforgeryToken]

    public class TelegramBotPluginController(TelegramBotPluginService pluginService) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index([FromRoute] string storeId)
        {
            var model = await pluginService.GetStoreViewModel(storeId);
            return View(model);
        }
    }
}
