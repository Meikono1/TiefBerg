using System;

namespace TiefBerg.Core.Logging;

public sealed class GameLogger
{
    private readonly string _category;

    internal GameLogger(string category)
    {
        _category = category;
    }

    public bool IsDebugEnabled => LogManager.MinLevel <= LogLevel.Debug;

    public void Debug(string message) => LogManager.Write(LogLevel.Debug, _category, message);

    public void Info(string message) => LogManager.Write(LogLevel.Info, _category, message);

    public void Warn(string message, Exception exception = null) =>
        LogManager.Write(LogLevel.Warn, _category, message, exception);

    public void Error(string message, Exception exception = null) =>
        LogManager.Write(LogLevel.Error, _category, message, exception);

    public void Fatal(string message, Exception exception = null) =>
        LogManager.Write(LogLevel.Fatal, _category, message, exception);
}