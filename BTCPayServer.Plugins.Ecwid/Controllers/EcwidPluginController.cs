using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Plugins.Ecwid.Data;
using BTCPayServer.Plugins.Ecwid.Model;
using BTCPayServer.Plugins.Ecwid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Ecwid;

[Route("~/plugins/{storeId}/Ecwid")]
[Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy= Policies.CanModifyStoreSettings)]
public class EcwidPluginController : Controller
{
    private readonly EcwidPluginService _PluginService;

    public EcwidPluginController(EcwidPluginService PluginService)
    {
        _PluginService = PluginService;
    }

    [HttpGet]
    public async Task<IActionResult> Index(string storeId)
    {
        var model = new EcwidModel
        {
            Settings = await _PluginService.GetStoreSettings(storeId),
            EcwidPluginUrl = $"{Request.Scheme}://{Request.Host}{Request.Path}Payment"
        };
        return View(model);
    }

    [HttpPost]
    public async Task<IActionResult> Index(EcwidModel model, string command)
    {
        if (ModelState.IsValid)
        {
            switch (command)
            {
                case "Save":
                    await _PluginService.UpdateSettings(model.Settings);
                    break;
            }
        }
        return View("Index", model);
    }
}
