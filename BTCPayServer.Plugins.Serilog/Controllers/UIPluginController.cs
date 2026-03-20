using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Client;
using BTCPayServer.Plugins.Serilog.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog;
using Serilog.Sinks.Slack;
using Serilog.Sinks.Slack.Models;

namespace BTCPayServer.Plugins.Serilog;

[Route("~/plugins/serilog")]
[Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanModifyServerSettings)]
[AutoValidateAntiforgeryToken]
public class UIPluginController(ISettingsRepository settingsRepository) : Controller
{
    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var logSettings = (await settingsRepository.GetSettingAsync<LogSettings>()) ?? new LogSettings();
        return View(logSettings);
    }

    [HttpPost]
    public async Task<IActionResult> Index(LogSettings model, string command)
    {
        switch (command)
        {
            case "TestSlack":
                if (!model.slackConfig.IsComplete())
                {
                    TempData[WellKnownTempData.ErrorMessage] = "Invalid Slack configuration";
                    return View("Index", model);
                }
                try
                {
                    var cfg = model.slackConfig;
                    var opt = new SlackSinkOptions()
                    {
                        CustomChannel = cfg.Channel,
                        CustomUserName = cfg.UserName,
                        WebHookUrl = cfg.HookUrl
                    };

                    using var slackLogger = new LoggerConfiguration()
                        .WriteTo.Slack(slackSinkOptions: opt, restrictedToMinimumLevel: cfg.MinLevel)
                        .CreateLogger();
                    slackLogger.Write(cfg.MinLevel, "Test Log BTCPay - Slack");
                    TempData[WellKnownTempData.SuccessMessage] = "Slack Log sent. Don't forget to save.";
                }
                catch (Exception e)
                {
                    TempData[WellKnownTempData.ErrorMessage] = $"Slack Log error : {e.Message}";
                }
                break;

            case "TestTelegram":
                if (!model.telegramConfig.IsComplete())
                {
                    TempData[WellKnownTempData.ErrorMessage] = "Invalid Telegram configuration";
                    return View("Index", model);
                }
                try
                {
                    var cfg = model.telegramConfig;
                    using var telegramLogger = new LoggerConfiguration()
                        .WriteTo.Telegram(botToken: cfg.Token, chatId: cfg.ChatID, restrictedToMinimumLevel: cfg.MinLevel, batchSizeLimit: 1)
                        .CreateLogger();
                    telegramLogger.Write(cfg.MinLevel, "Test Log BTCPay - Telegram");
                    TempData[WellKnownTempData.SuccessMessage] = "Telegram Log sent. Don't forget to save.";
                }
                catch (Exception e)
                {
                    TempData[WellKnownTempData.ErrorMessage] = $"Telegram Log error : {e.Message}";
                }
                break;

            case "Save":
                var hasErrors = false;

                if (model.logSlackEnabled && !model.slackConfig.IsComplete())
                {
                    ModelState.AddModelError("logSlackEnabled", "Invalid Slack configuration");
                    hasErrors = true;
                }
                if (model.logTelegramEnabled && !model.telegramConfig.IsComplete())
                {
                    ModelState.AddModelError("logTelegramEnabled", "Invalid Telegram configuration");
                    hasErrors = true;
                }
                if (hasErrors) return View("Index", model);

                await settingsRepository.UpdateSetting(model);
                TempData[WellKnownTempData.SuccessMessage] = "Log settings saved. You have to restart the server for apply.";
                break;

            default:
                break;
        }
        return View("Index", model);
    }
}
