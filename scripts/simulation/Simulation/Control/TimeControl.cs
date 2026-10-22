using System;
using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;
using TiefBerg.Simulation.Time;

namespace TiefBerg.Simulation.Control;

// Legt fest, bis zu welcher Weltzeit die Simulation gerade rechnen soll (Abschnitt 3).
// Dauerzustände sind nur Pause und Echtzeit; Zeitraffer gibt es als Sprung um einen Abschnitt oder bis zum nächsten
// Ereignis, danach gilt Pause. Nicht thread-sicher; nur der Simulations-Thread ändert ihn, über Befehle.
public sealed class TimeControl
{
    private readonly ILogger<TimeControl> _log;
    private readonly TimeProvider _time;

    // Echtzeit zählt ab diesem Paar, damit nach einer Pause kein Sprung um die pausierte Zeit entsteht
    private ulong _anchorSeconds;
    private long _anchorTimestamp;

    public TimeControl(ILogger<TimeControl> log, TimeProvider time)
    {
        _log = log;
        _time = time;
    }

    public TimeMode Mode { get; private set; } = TimeMode.Paused;

    // Nur bei TimeMode.Skipping gesetzt
    public ulong? SkipTargetSeconds { get; private set; }

    public void Pause(ulong nowSeconds)
    {
        Mode = TimeMode.Paused;
        SkipTargetSeconds = null;
        _log.Info($"Pause bei {WorldCalendar.Format(nowSeconds)}.");
    }

    public void Resume(ulong nowSeconds)
    {
        _anchorSeconds = nowSeconds;
        _anchorTimestamp = _time.GetTimestamp();
        Mode = TimeMode.RealTime;
        SkipTargetSeconds = null;
        _log.Info($"Echtzeit ab {WorldCalendar.Format(nowSeconds)}.");
    }

    public void SkipTo(ulong nowSeconds, ulong targetSeconds)
    {
        if (targetSeconds <= nowSeconds)
        {
            _log.Warn($"Sprungziel {WorldCalendar.Format(targetSeconds)} liegt nicht nach {WorldCalendar.Format(nowSeconds)}, ignoriert.");
            return;
        }

        Mode = TimeMode.Skipping;
        SkipTargetSeconds = targetSeconds;
        _log.Info($"Sprung von {WorldCalendar.Format(nowSeconds)} bis {WorldCalendar.Format(targetSeconds)}.");
    }

    // Annahme (Abschnitt 3, O - Vorschlag): Ereignis = nächste Fälligkeit im Scheduler.
    // TODO: Auf Ereignisse für den Spieler umstellen, sobald es das Weltereignislog gibt (Abschnitt 12, T-055)
    public void SkipToNextEvent(ulong nowSeconds, ulong? nextDueSeconds)
    {
        if (nextDueSeconds is not ulong due)
        {
            _log.Warn("Sprung zum nächsten Ereignis angefordert, aber nichts ist geplant.");
            return;
        }

        // Liegt die Fälligkeit nicht in der Zukunft, wird sie gerade nachgeholt; dann ist das nächste Ereignis jetzt
        SkipTo(nowSeconds, Math.Max(due, nowSeconds + 1));
    }

    public ulong GetTarget(ulong nowSeconds)
    {
        switch (Mode)
        {
            case TimeMode.Paused:
                return nowSeconds;
            case TimeMode.RealTime:
                ulong elapsed = (ulong)_time.GetElapsedTime(_anchorTimestamp).TotalSeconds;
                return Math.Max(nowSeconds, checked(_anchorSeconds + elapsed));
            case TimeMode.Skipping when SkipTargetSeconds is ulong target:
                return target;
            default:
                var e = new InvalidOperationException($"Zeitmodus {Mode} ohne gültiges Ziel.");
                _log.Error("Zielzeit nicht bestimmbar.", e);
                throw e;
        }
    }

    // Nach Erreichen des Sprungziels gilt Pause (Abschnitt 3)
    public void OnAdvanced(ulong nowSeconds)
    {
        if (Mode == TimeMode.Skipping && SkipTargetSeconds is ulong target && nowSeconds >= target)
        {
            _log.Info("Sprungziel erreicht.");
            Pause(nowSeconds);
        }
    }

    // Wie lange der Simulations-Thread schlafen darf, bis wieder Zeit zu rechnen ist. null = bis ein Befehl kommt.
    public TimeSpan? GetWaitTime()
    {
        switch (Mode)
        {
            case TimeMode.Paused:
                return null;
            case TimeMode.RealTime:
                double elapsedMs = _time.GetElapsedTime(_anchorTimestamp).TotalMilliseconds;
                return TimeSpan.FromMilliseconds(Math.Ceiling(1000.0 - elapsedMs % 1000.0));
            default:
                return TimeSpan.Zero;
        }
    }
}
