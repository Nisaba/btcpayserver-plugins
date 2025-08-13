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

public class EcwidPluginController(EcwidPluginService ecwidService,
                                   BtcPayService btcPayService) : Controller
{
    private readonly EcwidPluginService _ecwidService = ecwidService;
    private readonly BtcPayService _btcPayService = btcPayService;


    [HttpGet]
    public async Task<IActionResult> Index([FromRoute] string storeId)
    {
        var model = new EcwidModel
        {
            Settings = await _ecwidService.GetStoreSettings(storeId),
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
                        await _ecwidService.UpdateSettings(model.Settings);
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
