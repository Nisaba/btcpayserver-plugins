using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Abstractions.Extensions;
using BTCPayServer.Abstractions.Models;
using BTCPayServer.Client;
using BTCPayServer.Data;
using BTCPayServer.Plugins.TelegramBot.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.TelegramBot.Controllers
{
    [Route("~/plugins/{storeId}/TelegramBot")]
    [Authorize(Policy = Policies.CanModifyStoreSettings, AuthenticationSchemes = AuthenticationSchemes.Cookie)]
    [AutoValidateAntiforgeryToken]

    public class TelegramBotPluginController(TelegramBotPluginService pluginService, UserManager<ApplicationUser> userManager) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index([FromRoute] string storeId)
        {
            var userId = userManager.GetUserId(User);
            await pluginService.InitConfig($"{Request.Scheme}://{Request.Host}{Request.PathBase}", userId);
            var model = await pluginService.GetStoreViewModel(storeId);
            return View(model);
        }

        [HttpPost]
        [Route("SaveSettings")]
        public async Task<IActionResult> SaveSettings([FromRoute] string storeId, [FromForm] string appId, [FromForm] string botToken, [FromForm] bool isEnabled)
        {
            try
            {
                await pluginService.UpdateSettings(storeId, appId, botToken, isEnabled);
                TempData.SetStatusMessageModel(new StatusMessageModel()
                {
                    Message = "Settings updated",
                    Severity = StatusMessageModel.StatusSeverity.Success
                });
            }
            catch (System.Exception ex)
            {
                TempData.SetStatusMessageModel(new StatusMessageModel()
                {
                    Message = $"Error updating Settings: {ex.Message}",
                    Severity = StatusMessageModel.StatusSeverity.Error
                });
            }
            return RedirectToAction("Index", new { storeId = storeId });
        }

    }
}
