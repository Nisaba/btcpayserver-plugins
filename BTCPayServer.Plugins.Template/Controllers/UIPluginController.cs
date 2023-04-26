using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BTCPayServer.Abstractions.Constants;
using BTCPayServer.Client;
using BTCPayServer.Models.ServerViewModels;
using BTCPayServer.Plugins.Serilog.Data;
using BTCPayServer.Plugins.Serilog.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Serilog.Sinks.Slack.Models;
using Serilog;
using Serilog.Sinks.Slack;

namespace BTCPayServer.Plugins.Serilog;

[Route("~/plugins/serilog")]
[Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanViewProfile)]
public class UIPluginController : Controller
{
    private readonly SerilogService _PluginService;

    public UIPluginController(SerilogService PluginService)
    {
        _PluginService = PluginService;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        return View(new PluginPageViewModel { Data = await _PluginService.Get() });
    }

    [HttpPost]
    public async Task<IActionResult> Index(PluginPageViewModel model, string command)
    {
        var oldLogger = Log.Logger;
        switch (command)
        {
            /*case "TestEmail":
                if (model.Settings.logEmailEnabled && !model.Settings.emailConfig.IsComplete())
                {
                    ModelState.AddModelError("Settings.logEmailEnabled", "Invalid Email configuration");
                    return View("Logs", model);
                }
                try
                {
                    var LoggerTestConfig = new LoggerConfiguration();
                    var emailTestSetttings = await _SettingsRepository.GetSettingAsync<EmailSettings>();
                    if (emailTestSetttings == null)
                    {
                        TempData[WellKnownTempData.ErrorMessage] = $"Email settings not set !";
                    }
                    else
                    {
                        var cfg = model.Settings.emailConfig;
                        var opt = new Serilog.Sinks.Email.EmailConnectionInfo
                        {
                            EmailSubject = "BTCPay Server Log",
                            FromEmail = emailTestSetttings.From,
                            ToEmail = cfg.To,
                            MailServer = emailTestSetttings.Server,
                            Port = emailTestSetttings.Port ?? 25,
                            NetworkCredentials = new System.Net.NetworkCredential(emailTestSetttings.Login, emailTestSetttings.Password),
                            ServerCertificateValidationCallback = (senderX, certificate, chain, sslPolicyErrors) => true
                        };
                        opt.EnableSsl = (opt.Port != 25);

                        LoggerTestConfig.WriteTo.Email(connectionInfo: opt, outputTemplate: cfg.Template, restrictedToMinimumLevel: cfg.MinLevel, batchPostingLimit: cfg.NbMaxEventsInMail, period: cfg.PeriodTimeSpan);
                        Log.Logger = LoggerTestConfig.CreateLogger();
                        Log.Write(cfg.MinLevel, "Test Log BTCPay - Email");
                        Log.CloseAndFlush();
                        TempData[WellKnownTempData.SuccessMessage] = "Email Log sent";
                    }
                }
                catch (Exception e)
                {
                    TempData[WellKnownTempData.ErrorMessage] = $"Email Log error : {e.Message}";
                }
                Log.Logger = oldLogger;
                break;*/
            case "TestSlack":
                if (model.Settings.logSlackEnabled && !model.Settings.slackConfig.IsComplete())
                {
                    ModelState.AddModelError("Settings.logSlackEnabled", "Invalid Slack configuration");
                    return View("Logs", model);
                }
                try
                {
                    var LoggerTestConfig = new LoggerConfiguration();
                    var cfg = model.Settings.slackConfig;
                    var opt = new SlackSinkOptions()
                    {
                        CustomChannel = cfg.Channel,
                        CustomUserName = cfg.UserName,
                        WebHookUrl = cfg.HookUrl
                    };

                    LoggerTestConfig.WriteTo.Slack(slackSinkOptions: opt, restrictedToMinimumLevel: cfg.MinLevel);
                    Log.Logger = LoggerTestConfig.CreateLogger();
                    Log.Write(cfg.MinLevel, "Test Log BTCPay - Slack");
                    TempData[WellKnownTempData.SuccessMessage] = "Slack Log sent. Don't forget to save.";
                }
                catch (Exception e)
                {
                    TempData[WellKnownTempData.ErrorMessage] = $"Slack Log error : {e.Message}";
                }
                Log.Logger = oldLogger;
                break;
            case "TestTelegram":
                if (model.Settings.logTelegramEnabled && !model.Settings.telegramConfig.IsComplete())
                {
                    ModelState.AddModelError("Settings.logTelegramEnabled", "Invalid Telegram configuration");
                    return View("Logs", model);
                }
                try
                {
                    var LoggerTestConfig = new LoggerConfiguration();
                    var cfg = model.Settings.telegramConfig;
                    LoggerTestConfig.WriteTo.Telegram(cfg.Token, cfg.ChatID, (int?)cfg.MinLevel);
                    Log.Logger = LoggerTestConfig.CreateLogger();
                    Log.Write(cfg.MinLevel, "Test Log BTCPay - Telegram");
                    TempData[WellKnownTempData.SuccessMessage] = "Telegram Log sent. Don't forget to save.";
                }
                catch (Exception e)
                {
                    TempData[WellKnownTempData.ErrorMessage] = $"Telegram Log error : {e.Message}";
                }
                Log.Logger = oldLogger;
                break;
            case "Save":
                /*EmailSettings? emailSetttings = new EmailSettings();
                if (model.Settings.logEmailEnabled)
                {
                    if (!model.Settings.emailConfig.IsComplete())
                    { 
                        ModelState.AddModelError("Settings.logEmailEnabled", "Invalid Email configuration");
                        return View("Logs", model);
                    }
                    emailSetttings = await _SettingsRepository.GetSettingAsync<EmailSettings>();
                    if (emailSetttings == null)
                    {
                        ModelState.AddModelError("Settings.logEmailEnabled", "Email settings not set ");
                        return View("Logs", model);
                    }
                }*/
                if (model.Settings.logSlackEnabled && !model.Settings.slackConfig.IsComplete())
                {
                    ModelState.AddModelError("Settings.logSlackEnabled", "Invalid Slack configuration");
                    return View("Logs", model);
                }
                if (model.Settings.logTelegramEnabled && !model.Settings.telegramConfig.IsComplete())
                {
                    ModelState.AddModelError("Settings.logTelegramEnabled", "Invalid Telegram configuration");
                    return View("Logs", model);
                }


                var LoggerConfig = new LoggerConfiguration();
                /*if (model.Settings.logEmailEnabled)
                {
                    var cfg = model.Settings.emailConfig;
                        var opt = new Serilog.Sinks.Email.EmailConnectionInfo
                        {
                            EmailSubject = "BTCPay Server Log",
                            FromEmail = emailSetttings.From,
                            ToEmail = cfg.To,
                            MailServer = emailSetttings.Server,
                            Port = emailSetttings.Port ?? 25,
                            NetworkCredentials = new System.Net.NetworkCredential(emailSetttings.Login, emailSetttings.Password),
                            ServerCertificateValidationCallback = (senderX, certificate, chain, sslPolicyErrors) => true
                        };
                        opt.EnableSsl = (opt.Port != 25);
                        LoggerConfig.WriteTo.Email(connectionInfo: opt, outputTemplate: cfg.Template, restrictedToMinimumLevel: cfg.MinLevel, batchPostingLimit: cfg.NbMaxEventsInMail, period: cfg.PeriodTimeSpan);

                }*/
                if (model.Settings.logSlackEnabled)
                {
                    var cfg = model.Settings.slackConfig;
                    var opt = new SlackSinkOptions()
                    {
                        CustomChannel = cfg.Channel,
                        CustomUserName = cfg.UserName,
                        WebHookUrl = cfg.HookUrl
                    };

                    LoggerConfig.WriteTo.Slack(slackSinkOptions: opt, restrictedToMinimumLevel: cfg.MinLevel);
                }
                if (model.Settings.logTelegramEnabled)
                {
                    var cfg = model.Settings.telegramConfig;
                    LoggerConfig.WriteTo.Telegram(cfg.Token, cfg.ChatID, (int?)cfg.MinLevel);
                }
                Log.Logger = LoggerConfig.CreateLogger();

                // TODO
                //await _SettingsRepository.UpdateSetting(model.Settings);
                // 5068687121:AAEpTcMQZZV8snsCHzlnm3k2gyBHqCFcsIw   -999875018
                TempData[WellKnownTempData.SuccessMessage] = "Log settings saved. You have to restart the server for apply.";
                break;
            default:
                break;
        }
        return View("Logs", model);
    }


}

public class PluginPageViewModel
{
    // TO REMOVE
    public List<PluginData> Data { get; set; }

    public string Log { get; set; }
    public int LogFileCount { get; set; }
    public int LogFileOffset { get; set; }

    public LogSettings Settings { get; set; }

    public PluginPageViewModel()
    {

    }

    public PluginPageViewModel(LogSettings settings)
    {
        Settings = settings;
    }

}
