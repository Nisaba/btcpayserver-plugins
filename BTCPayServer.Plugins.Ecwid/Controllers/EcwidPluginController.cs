using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Plugins.Ecwid.Data;
using BTCPayServer.Plugins.Ecwid.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace BTCPayServer.Plugins.Ecwid;

[Route("~/plugins/{storeId}/Ecwid")]
[Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie)]
public class EcwidPluginController : Controller
{
    private readonly EcwidPluginService _PluginService;

    public EcwidPluginController(EcwidPluginService PluginService)
    {
        _PluginService = PluginService;
    }

    [HttpGet]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy= Policies.CanViewStoreSettings)]
    public async Task<IActionResult> Index(string storeId)
    {
        return View(await _PluginService.GetStoreSettings(storeId));
    }

    [HttpPost]
    [Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanModifyStoreSettings)]
    public async Task<IActionResult> Index(EcwidSettings model, string command)
    {
        if (ModelState.IsValid)
        {
            switch (command)
            {
                case "Save":
                    await _PluginService.UpdateSettings(model);
                    break;
            }
        }
        return View("Index", model);
    }
}
