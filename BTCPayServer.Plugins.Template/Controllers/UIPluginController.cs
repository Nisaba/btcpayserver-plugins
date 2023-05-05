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
using SerilogLib = Serilog;
using Serilog.Sinks.Slack;
using BTCPayServer.Abstractions.Contracts;

namespace BTCPayServer.Plugins.Serilog;

[Route("~/plugins/serilog")]
[Authorize(AuthenticationSchemes = AuthenticationSchemes.Cookie, Policy = Policies.CanModifyServerSettings)]
public class UIPluginController : Controller
{
    private readonly ISettingsRepository _SettingsRepository;
    private readonly SerilogService _PluginService;

    public UIPluginController(SerilogService PluginService, ISettingsRepository settingsRepository)
    {
        _PluginService = PluginService;
        _SettingsRepository = settingsRepository;
    }

    [HttpGet]
    public async Task<IActionResult> Index()
    {
        var logSettings = (await _SettingsRepository.GetSettingAsync<LogSettings>()) ?? new LogSettings();
        return View(logSettings);
    }

    [HttpPost]
    public async Task<IActionResult> Index(LogSettings model, string command)
    {
        var oldLogger = SerilogLib.Log.Logger;
        switch (command)
        {
            /*case "TestEmail":
                if (model.Settings.logEmailEnabled && !model.Settings.emailConfig.IsComplete())
                {
                    TempData[WellKnownTempData.ErrorMessage] = "Invalid Email configuration";
                    return View("Index", model);
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
                if (!model.slackConfig.IsComplete())
                {
                    TempData[WellKnownTempData.ErrorMessage] = "Invalid Slack configuration";
                    return View("Index", model);
                }
                try
                {
                    var LoggerTestConfig = new SerilogLib.LoggerConfiguration();
                    var cfg = model.slackConfig;
                    var opt = new SlackSinkOptions()
                    {
                        CustomChannel = cfg.Channel,
                        CustomUserName = cfg.UserName,
                        WebHookUrl = cfg.HookUrl
                    };

                    LoggerTestConfig.WriteTo.Slack(slackSinkOptions: opt, restrictedToMinimumLevel: cfg.MinLevel);
                    SerilogLib.Log.Logger = LoggerTestConfig.CreateLogger();
                    SerilogLib.Log.Write(cfg.MinLevel, "Test Log BTCPay - Slack");
                    TempData[WellKnownTempData.SuccessMessage] = "Slack Log sent. Don't forget to save.";
                }
                catch (Exception e)
                {
                    TempData[WellKnownTempData.ErrorMessage] = $"Slack Log error : {e.Message}";
                }
                SerilogLib.Log.Logger = oldLogger;
                break;
            case "TestTelegram":
                if (!model.telegramConfig.IsComplete())
                {
                    TempData[WellKnownTempData.ErrorMessage] = "Invalid Telegram configuration";
                    return View("Index", model);
                }
                try
                {
                    SerilogLib.Debugging.SelfLog.Enable(Console.Error);
                    var LoggerTestConfig = new SerilogLib.LoggerConfiguration();
                    var cfg = model.telegramConfig;
                    LoggerTestConfig.WriteTo.Telegram(botToken:cfg.Token, chatId:cfg.ChatID, restrictedToMinimumLevel: cfg.MinLevel, batchSizeLimit : 1);
                    SerilogLib.Log.Logger = LoggerTestConfig.CreateLogger();
                    SerilogLib.Log.Write(cfg.MinLevel, "Test Log BTCPay - Telegram");
                    SerilogLib.Log.CloseAndFlush();
                    TempData[WellKnownTempData.SuccessMessage] = "Telegram Log sent. Don't forget to save.";
                }
                catch (Exception e)
                {
                    TempData[WellKnownTempData.ErrorMessage] = $"Telegram Log error : {e.Message}";
                }
                SerilogLib.Log.Logger = oldLogger;
                break;
            case "Save":
                 var bFlag = false;
                /*EmailSettings? emailSetttings = new EmailSettings();
                if (model.logEmailEnabled)
                {
                        if (!model.emailConfig.IsComplete())
                        { 
                            ModelState.AddModelError("Settings.logEmailEnabled", "Invalid Email configuration");
                            bFlag = true;;
                        } else {
                        emailSetttings = await _SettingsRepository.GetSettingAsync<EmailSettings>();
                        if (emailSetttings == null)
                        {
                            ModelState.AddModelError("Settings.logEmailEnabled", "Email settings not set ");
                            bFlag = true;;
                        }
                    }
                }*/

                if (model.logSlackEnabled && !model.slackConfig.IsComplete())
                {
                    ModelState.AddModelError("Settings.logSlackEnabled", "Invalid Slack configuration");
                    bFlag = true;
                }
                if (model.logTelegramEnabled && !model.telegramConfig.IsComplete())
                {
                    ModelState.AddModelError("Settings.logTelegramEnabled", "Invalid Telegram configuration");
                    bFlag = true;
                }
                if (bFlag) return View("Index", model);

                //_PluginService.DoSerilogConfig(model.Settings);

                await _SettingsRepository.UpdateSetting(model);
                // 5068687121:AAEpTcMQZZV8snsCHzlnm3k2gyBHqCFcsIw   -999875018
                TempData[WellKnownTempData.SuccessMessage] = "Log settings saved. You have to restart the server for apply.";
                break;
            default:
                break;
        }
        return View("Index", model);
    }


}

