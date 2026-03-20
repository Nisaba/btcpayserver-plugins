using Serilog.Events;
using System.ComponentModel.DataAnnotations;

namespace BTCPayServer.Plugins.Serilog.Data;

public class LogSettings
{
    [Display(Name = "Send log to a Slack Channel")]
    public bool logSlackEnabled { get; set; }
    public LogSlackConfig slackConfig { get; set; }

    [Display(Name = "Send log to a Telegram Canal")]
    public bool logTelegramEnabled { get; set; }
    public LogTelegramConfig telegramConfig { get; set; }

    public LogSettings()
    {
        telegramConfig = new LogTelegramConfig();
        slackConfig = new LogSlackConfig();
    }
}

public abstract class LogConfig
{
    [Display(Name = "Minimum Notification level : ")]
    public LogEventLevel MinLevel { get; set; } = LogEventLevel.Information;

    public abstract bool IsComplete();
}

public class LogSlackConfig : LogConfig
{
    [Display(Name = "Channel")]
    public string Channel { get; set; } = string.Empty;

    [Display(Name = "User Name")]
    public string UserName { get; set; } = string.Empty;

    [Display(Name = "Hook Url")]
    public string HookUrl { get; set; } = string.Empty;

    public override bool IsComplete()
    {
        return !string.IsNullOrWhiteSpace(Channel)
            && !string.IsNullOrWhiteSpace(UserName)
            && !string.IsNullOrWhiteSpace(HookUrl)
            && Uri.TryCreate(HookUrl, UriKind.Absolute, out _);
    }
}

public class LogTelegramConfig : LogConfig
{
    [Display(Name = "Token")]
    public string Token { get; set; } = string.Empty;

    [Display(Name = "Chat ID")]
    public string ChatID { get; set; } = string.Empty;

    public override bool IsComplete()
    {
        return !string.IsNullOrWhiteSpace(Token)
            && !string.IsNullOrWhiteSpace(ChatID);
    }
}
