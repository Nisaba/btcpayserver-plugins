using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using BTCPayServer.Plugins.Serilog.Data;
using Microsoft.EntityFrameworkCore;
using BTCPayServer.Logging;

using Serilog.Sinks.Slack.Models;
using Serilog;
using SerilogLib = Serilog;
using Serilog.Sinks.Slack;
using BTCPayServer.Abstractions.Contracts;
using NLog.Fluent;
using Microsoft.Extensions.Logging;

namespace BTCPayServer.Plugins.Serilog.Services;

public class SerilogService
{
    private readonly ISettingsRepository _SettingsRepository;
    private readonly Logs _logs;
    private readonly ILoggerFactory _factory;

    public SerilogService(ISettingsRepository settingsRepository, Logs logs, ILoggerFactory factory)//, ILoggingBuilder logBuilder)
    {
        _SettingsRepository = settingsRepository;
        _logs = logs;
        _factory = factory;
    }

    public async Task InitSerilogConfig()
    {
        var serilogSetting = (await _SettingsRepository.GetSettingAsync<LogSettings>()) ?? new LogSettings();
        DoSerilogConfig(serilogSetting);
    }

    public void DoSerilogConfig(LogSettings logSettings)
    {
        var LoggerConfig = new SerilogLib.LoggerConfiguration();
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
        if (logSettings.logSlackEnabled)
        {
            var cfg = logSettings.slackConfig;
            var opt = new SlackSinkOptions()
            {
                CustomChannel = cfg.Channel,
                CustomUserName = cfg.UserName,
                WebHookUrl = cfg.HookUrl
            };

            LoggerConfig.WriteTo.Slack(slackSinkOptions: opt, restrictedToMinimumLevel: cfg.MinLevel);
        }
        if (logSettings.logTelegramEnabled)
        {
            var cfg = logSettings.telegramConfig;
            LoggerConfig.WriteTo.Telegram(botToken: cfg.Token, chatId: cfg.ChatID, restrictedToMinimumLevel: cfg.MinLevel);
        }
        SerilogLib.Log.Logger = LoggerConfig.CreateLogger();
        _factory.AddSerilog(SerilogLib.Log.Logger);
    }


}

