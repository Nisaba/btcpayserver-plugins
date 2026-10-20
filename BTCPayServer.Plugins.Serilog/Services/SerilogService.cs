using BTCPayServer.Abstractions.Contracts;
using BTCPayServer.Plugins.Serilog.Data;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Sinks.Slack;
using Serilog.Sinks.Slack.Models;
using SerilogLogger = global::Serilog.Core.Logger;

namespace BTCPayServer.Plugins.Serilog.Services;

public class SerilogService : IDisposable
{
    private readonly ISettingsRepository _settingsRepository;
    private readonly ReloadableLogger _pluginLogger;
    private bool _disposed;

    public SerilogService(ISettingsRepository settingsRepository, ILoggerFactory factory)
    {
        _settingsRepository = settingsRepository;
        _pluginLogger = new ReloadableLogger(SerilogLogger.None);
        factory.AddSerilog(_pluginLogger);
    }

    public async Task InitSerilogConfig(CancellationToken cancellationToken = default)
    {
        var serilogSetting = (await _settingsRepository.GetSettingAsync<LogSettings>()) ?? new LogSettings();
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

        _pluginLogger.Reload(loggerConfig.CreateLogger());
    }

    public void Dispose()
    {
        if (_disposed)
            return;
        _disposed = true;
        _pluginLogger.Dispose();
    }
}
