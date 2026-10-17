using System;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

namespace TiefBerg.Diagnostics;

// Ersatz für LoggerExtensions.LogDebug & Co. (im Kern per BannedSymbols.txt gesperrt):
// trägt die aufrufende Methode als EventId.Name ein, damit jede Logzeile Klasse.Methode enthält
public static class LoggerCallerExtensions
{
    public static void Debug(this ILogger log, string message, [CallerMemberName] string member = "") =>
        Write(log, LogLevel.Debug, message, null, member);

    public static void Info(this ILogger log, string message, [CallerMemberName] string member = "") =>
        Write(log, LogLevel.Information, message, null, member);

    public static void Warn(this ILogger log, string message, Exception? exception = null,
        [CallerMemberName] string member = "") =>
        Write(log, LogLevel.Warning, message, exception, member);

    public static void Error(this ILogger log, string message, Exception? exception = null,
        [CallerMemberName] string member = "") =>
        Write(log, LogLevel.Error, message, exception, member);

    public static void Fatal(this ILogger log, string message, Exception? exception = null,
        [CallerMemberName] string member = "") =>
        Write(log, LogLevel.Critical, message, exception, member);

    private static void Write(ILogger log, LogLevel level, string message, Exception? exception, string member)
    {
        if (!log.IsEnabled(level))
        {
            return;
        }

        log.Log(level, new EventId(0, member), message, exception, static (m, _) => m);
    }
}
