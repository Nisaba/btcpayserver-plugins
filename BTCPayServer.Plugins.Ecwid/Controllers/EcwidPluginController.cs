using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Plugins.Ecwid.Data;
using BTCPayServer.Plugins.Ecwid.Model;
using BTCPayServer.Plugins.Ecwid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Ecwid;

[Route("~/plugins/{storeId}/Ecwid")]
[Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy= Policies.CanModifyStoreSettings)]
[AutoValidateAntiforgeryToken]

public class EcwidPluginController(EcwidPluginService ecwidService) : Controller
{

    [HttpGet]
    public async Task<IActionResult> Index([FromRoute] string storeId)
    {
        var model = new EcwidModel
        {
            Settings = await ecwidService.GetStoreSettings(storeId),
            EcwidPluginUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}Payment"
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Index(EcwidModel model, string command)
    {
        if (ModelState.IsValid)
        {
            try
            {
                switch (command)
                {
                    case "Save":
                        await ecwidService.UpdateSettings(model.Settings);
                        TempData[WellKnownTempData.SuccessMessage] = "Settings successfuly saved";
                        break;
                }
            }
            catch (Exception ex)
            {
                TempData[WellKnownTempData.ErrorMessage] = $"Error: {ex.Message}";
                throw;
            }
        }
        return RedirectToAction("Index");
    }
}
