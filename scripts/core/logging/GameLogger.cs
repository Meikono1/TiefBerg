using System;
using System.Runtime.CompilerServices;

namespace TiefBerg.Core.Logging;

// Die aufrufende Methode setzt der Compiler über [CallerMemberName] ein; nie selbst übergeben
public sealed class GameLogger
{
    private readonly string _category;

    internal GameLogger(string category)
    {
        _category = category;
    }

    public bool IsDebugEnabled => LogManager.MinLevel <= LogLevel.Debug;

    public void Debug(string message, [CallerMemberName] string member = "") =>
        LogManager.Write(LogLevel.Debug, _category, message, null, member);

    public void Info(string message, [CallerMemberName] string member = "") =>
        LogManager.Write(LogLevel.Info, _category, message, null, member);

    public void Warn(string message, Exception exception = null, [CallerMemberName] string member = "") =>
        LogManager.Write(LogLevel.Warn, _category, message, exception, member);

    public void Error(string message, Exception exception = null, [CallerMemberName] string member = "") =>
        LogManager.Write(LogLevel.Error, _category, message, exception, member);

    public void Fatal(string message, Exception exception = null, [CallerMemberName] string member = "") =>
        LogManager.Write(LogLevel.Fatal, _category, message, exception, member);
}
