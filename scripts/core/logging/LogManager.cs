using System;
using System.IO;
using System.Linq;
using System.Text;
using Godot;
using Microsoft.Extensions.Logging;
using MsLogLevel = Microsoft.Extensions.Logging.LogLevel;

namespace TiefBerg.Core.Logging;

public static class LogManager
{
    private const string LogDirectory = "user://logs";
    private const string LogFilePrefix = "game_";
    private const int MaxLogFiles = 10;

    private static readonly object WriteLock = new();
    private static StreamWriter _writer;
    private static bool _initialized;

    public static LogLevel MinLevel { get; set; } = LogLevel.Info;
    public static string CurrentLogFilePath { get; private set; }

    // Liefert die ILogger<T> für den Simulationskern (Konstruktor-Injektion), null vor Initialize()
    public static ILoggerFactory SimulationLoggerFactory { get; private set; }

    // Aktuelle Weltzeit in Sekunden für Kern-Logmeldungen, null solange keine Welt läuft
    // TODO: Beim Start der Simulation setzen, sobald die Weltzeit existiert (T-009)
    public static Func<ulong> WorldTimeSource { get; set; }

    public static GameLogger GetLogger<T>() => new GameLogger(typeof(T).Name);

    public static GameLogger GetLogger(string category) => new GameLogger(category);

    public static void Initialize()
    {
        lock (WriteLock)
        {
            if (_initialized)
            {
                return;
            }

            MinLevel = OS.IsDebugBuild() ? LogLevel.Debug : LogLevel.Info;

            try
            {
                string directory = ProjectSettings.GlobalizePath(LogDirectory);
                Directory.CreateDirectory(directory);
                CleanupOldLogs(directory);

                string fileName = $"{LogFilePrefix}{DateTime.Now:yyyy-MM-dd_HH-mm-ss}.log";
                CurrentLogFilePath = Path.Combine(directory, fileName);
                _writer = new StreamWriter(CurrentLogFilePath, false, Encoding.UTF8) { AutoFlush = true };
            }
            catch (Exception e)
            {
                // Kein Datei-Log möglich -> Spiel läuft trotzdem weiter, nur Konsole
                GD.PushError($"[LogManager] Log-Datei konnte nicht erstellt werden: {e.Message}");
                _writer = null;
                CurrentLogFilePath = null;
            }

            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;

            // Filterung übernimmt SimulationLoggerProvider über MinLevel; die Factory selbst lässt alles durch
            SimulationLoggerFactory = LoggerFactory.Create(builder => builder
                .SetMinimumLevel(MsLogLevel.Trace)
                .AddProvider(new SimulationLoggerProvider()));
            _initialized = true;
        }

        Write(LogLevel.Info, nameof(LogManager), $"Logging initialisiert. MinLevel={MinLevel}");
    }

    public static void Shutdown()
    {
        Write(LogLevel.Info, nameof(LogManager), "Logging wird beendet.");

        lock (WriteLock)
        {
            if (!_initialized)
            {
                return;
            }

            AppDomain.CurrentDomain.UnhandledException -= OnUnhandledException;
            SimulationLoggerFactory?.Dispose();
            SimulationLoggerFactory = null;
            WorldTimeSource = null;
            _writer?.Flush();
            _writer?.Dispose();
            _writer = null;
            _initialized = false;
        }
    }

    // Format jeder Zeile: [Systemzeit] [Level] [Klasse.Methode] Meldung – Grundlage für die Log-Auswertung
    public static void Write(LogLevel level, string category, string message, Exception exception = null,
        string member = null)
    {
        if (level < MinLevel)
        {
            return;
        }

        string source = string.IsNullOrEmpty(member) ? category : $"{category}.{member}";
        string line = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] [{level,-5}] [{source}] {message}";
        if (exception != null)
        {
            line += System.Environment.NewLine + exception;
        }

        WriteToConsole(level, line);

        lock (WriteLock)
        {
            try
            {
                _writer?.WriteLine(line);
            }
            catch (Exception e)
            {
                GD.PushError($"[LogManager] Schreiben in Log-Datei fehlgeschlagen: {e.Message}");
            }
        }
    }

    private static void WriteToConsole(LogLevel level, string line)
    {
        switch (level)
        {
            case LogLevel.Warn:
                GD.PushWarning(line);
                break;
            case LogLevel.Error:
            case LogLevel.Fatal:
                GD.PushError(line);
                break;
            default:
                GD.Print(line);
                break;
        }
    }

    private static void CleanupOldLogs(string directory)
    {
        // Dateinamen enthalten den Zeitstempel -> alphabetisch = chronologisch
        var oldFiles = Directory.GetFiles(directory, $"{LogFilePrefix}*.log")
            .OrderByDescending(f => f)
            .Skip(MaxLogFiles - 1);

        foreach (string file in oldFiles)
        {
            try
            {
                File.Delete(file);
            }
            catch (Exception e)
            {
                GD.PushWarning($"[LogManager] Alte Log-Datei konnte nicht gelöscht werden: {file} ({e.Message})");
            }
        }
    }

    private static void OnUnhandledException(object sender, UnhandledExceptionEventArgs args)
    {
        // Hinweis: Godot fängt Exceptions in Callbacks (_Ready, _Process, Signals) selbst ab.
        // Hier landen nur wirklich unbehandelte Exceptions, z.B. aus eigenen Threads.
        Write(LogLevel.Fatal, nameof(LogManager), "Unbehandelte Exception.", args.ExceptionObject as Exception);

        lock (WriteLock)
        {
            _writer?.Flush();
        }
    }
}