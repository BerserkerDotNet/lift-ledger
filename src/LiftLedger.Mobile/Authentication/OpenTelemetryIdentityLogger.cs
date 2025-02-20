using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Abstractions;

namespace LiftLedger.Mobile.Authentication;

public sealed class OpenTelemetryIdentityLogger : IIdentityLogger
{
    private readonly ILogger<OpenTelemetryIdentityLogger> _logger;

    public OpenTelemetryIdentityLogger(ILogger<OpenTelemetryIdentityLogger> logger)
    {
        _logger = logger;
    }
    
    public bool IsEnabled(EventLogLevel eventLogLevel)
    {
        return eventLogLevel <= EventLogLevel.Informational;
    }

    public void Log(LogEntry entry)
    {
        var logLevel = entry.EventLogLevel switch
        {
            EventLogLevel.Informational => LogLevel.Information,
            EventLogLevel.Warning => LogLevel.Warning,
            EventLogLevel.Critical => LogLevel.Critical,
            EventLogLevel.Error => LogLevel.Error,
            EventLogLevel.Verbose => LogLevel.Debug,
            EventLogLevel.LogAlways => LogLevel.Trace,
            _ => LogLevel.None
        };

        _logger.Log(logLevel, entry.Message);
    }
}