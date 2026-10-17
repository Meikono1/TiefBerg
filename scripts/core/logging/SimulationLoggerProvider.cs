using System;
using Microsoft.Extensions.Logging;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace TiefBerg.Core.Logging;

// Leitet die ILogger<T> des Simulationskerns in den LogManager um: eine Log-Datei, ein Format.
// Jede Meldung bekommt die aktuelle Weltzeit vorangestellt (Abschnitt 12), dazu Klasse.Methode über EventId.Name.
public sealed class SimulationLoggerProvider : ILoggerProvider
{
    public ILogger CreateLogger(string categoryName) => new SimulationLogger(ShortCategory(categoryName));

    public void Dispose()
    {
    }

    // "TiefBerg.Simulation.Scheduler" -> "Scheduler", damit die Kategorien zu GameLogger passen
    private static string ShortCategory(string categoryName)
    {
        int lastDot = categoryName.LastIndexOf('.');
        return lastDot >= 0 ? categoryName[(lastDot + 1)..] : categoryName;
    }

    private static LogLevel? Map(MsLogLevel level) => level switch
    {
        MsLogLevel.Trace or MsLogLevel.Debug => LogLevel.Debug,
        MsLogLevel.Information => LogLevel.Info,
        MsLogLevel.Warning => LogLevel.Warn,
        MsLogLevel.Error => LogLevel.Error,
        MsLogLevel.Critical => LogLevel.Fatal,
        _ => null
    };

    private sealed class SimulationLogger : ILogger
    {
        private readonly string _category;

        public SimulationLogger(string category)
        {
            _category = category;
        }

        // TODO: Scopes (z. B. Siedlung, Person) unterstützen, sobald sie gebraucht werden
        public IDisposable BeginScope<TState>(TState state) where TState : notnull => null;

        public bool IsEnabled(MsLogLevel logLevel)
        {
            LogLevel? level = Map(logLevel);
            return level != null && level >= LogManager.MinLevel;
        }

        public void Log<TState>(MsLogLevel logLevel, EventId eventId, TState state, Exception exception,
            Func<TState, Exception, string> formatter)
        {
            LogLevel? level = Map(logLevel);
            if (level == null || level < LogManager.MinLevel)
            {
                return;
            }

            // TODO: Weltzeit als Kalenderdatum formatieren, sobald T-009 steht (Abschnitt 3)
            // TODO: Bei vorgezogenen Berechnungen (Abschnitt 3) gilt nicht die globale Weltzeit, sondern die des berechneten Ticks
            Func<ulong> worldTime = LogManager.WorldTimeSource;
            string time = worldTime != null ? worldTime().ToString() : "-";
            // EventId.Name trägt die aufrufende Methode (TiefBerg.Diagnostics.LoggerCallerExtensions)
            LogManager.Write(level.Value, _category, $"[T={time}] {formatter(state, exception)}", exception,
                eventId.Name);
        }
    }
}
