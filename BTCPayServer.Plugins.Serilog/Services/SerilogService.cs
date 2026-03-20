using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Plugins.Serilog.Data;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.Slack;
using Serilog.Sinks.Slack.Models;

namespace BTCPayServer.Plugins.Serilog.Services;

public class SerilogService(ISettingsRepository settingsRepository, ILoggerFactory factory)
{
    private bool _configured;

    public async Task InitSerilogConfig(CancellationToken cancellationToken = default)
    {
        var serilogSetting = (await settingsRepository.GetSettingAsync<LogSettings>()) ?? new LogSettings();
        DoSerilogConfig(serilogSetting);
    }

    public void DoSerilogConfig(LogSettings logSettings)
    {
        var loggerConfig = new LoggerConfiguration();

        if (logSettings.logSlackEnabled)
        {
            var cfg = logSettings.slackConfig;
            var opt = new SlackSinkOptions()
            {
                CustomChannel = cfg.Channel,
                CustomUserName = cfg.UserName,
                WebHookUrl = cfg.HookUrl
            };

            loggerConfig.WriteTo.Slack(slackSinkOptions: opt, restrictedToMinimumLevel: cfg.MinLevel);
        }

        if (logSettings.logTelegramEnabled)
        {
            var cfg = logSettings.telegramConfig;
            loggerConfig.WriteTo.Telegram(botToken: cfg.Token, chatId: cfg.ChatID, restrictedToMinimumLevel: cfg.MinLevel);
        }

        (Log.Logger as IDisposable)?.Dispose();
        Log.Logger = loggerConfig.CreateLogger();

        if (!_configured)
        {
            factory.AddSerilog(Log.Logger);
            _configured = true;
        }
    }
}
