using Serilog.Events;
using SerilogILogger = global::Serilog.ILogger;
using SerilogLogger = global::Serilog.Core.Logger;

namespace BTCPayServer.Plugins.Serilog.Services;

/// <summary>
/// A stable <see cref="SerilogILogger"/> wrapper that delegates to an inner logger
/// which can be swapped at runtime (e.g. when plugin settings change).
/// This allows registering a single, immutable logger instance with the
/// Microsoft.Extensions.Logging pipeline while still being able to reconfigure
/// the underlying sinks, WITHOUT ever touching the shared static logger
/// owned by BTCPay Server.
/// </summary>
public sealed class ReloadableLogger : SerilogILogger, IDisposable
{
    private readonly object _sync = new();
    private SerilogILogger _inner;

    public ReloadableLogger(SerilogILogger inner)
    {
        _inner = inner ?? throw new ArgumentNullException(nameof(inner));
    }

    /// <summary>
    /// Atomically replaces the inner logger and disposes the previous one.
    /// </summary>
    public void Reload(SerilogILogger newInner)
    {
        ArgumentNullException.ThrowIfNull(newInner);

        SerilogILogger previous;
        lock (_sync)
        {
            previous = _inner;
            _inner = newInner;
        }

        (previous as IDisposable)?.Dispose();
    }

    public void Write(LogEvent logEvent)
    {
        // Serilog logger instances are thread-safe; a simple read is enough.
        _inner.Write(logEvent);
    }

    public void Dispose()
    {
        lock (_sync)
        {
            (_inner as IDisposable)?.Dispose();
            _inner = SerilogLogger.None;
        }
    }
}
