using System;
using System.Collections.Concurrent;
using System.Threading;
using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;
using TiefBerg.Simulation.Control.Commands;

namespace TiefBerg.Simulation.Control;

// Lässt die Simulation in einem eigenen Thread laufen, unabhängig von der Bildrate (Abschnitt 2, 3).
// Ablauf pro Runde: Befehle ausführen, eine Rechenscheibe bis zur Zielzeit, Snapshot veröffentlichen, ggf. schlafen.
// Die Darstellung schickt nur Befehle (Send) und liest nur den Snapshot; der Weltzustand gehört allein diesem Thread.
public sealed class SimulationThread
{
    private static readonly TimeSpan CaptureTimeout = TimeSpan.FromSeconds(5);
    private static readonly TimeSpan StopTimeout = TimeSpan.FromSeconds(5);

    private readonly ILogger<SimulationThread> _log;
    private readonly SimulationRunner _runner;
    private readonly TimeControl _time;
    private readonly SimulationContext _context;
    private readonly ConcurrentQueue<ISimulationCommand> _commands = new();
    private readonly AutoResetEvent _wake = new(false);

    private Thread? _thread;
    private volatile bool _stopRequested;
    private volatile bool _faulted;
    private SimulationSnapshot _snapshot;

    public SimulationThread(ILoggerFactory loggers, World world, TimeProvider time)
    {
        _log = loggers.CreateLogger<SimulationThread>();
        _runner = new SimulationRunner(loggers.CreateLogger<SimulationRunner>(), world, time);
        _time = new TimeControl(loggers.CreateLogger<TimeControl>(), time);
        _context = new SimulationContext(world, _time);
        _snapshot = CreateSnapshot();
    }

    public World World => _runner.World;

    public SimulationSnapshot Snapshot => Volatile.Read(ref _snapshot);

    public bool IsRunning => _thread != null;

    // Startet pausiert (Abschnitt 3)
    public void Start()
    {
        if (_thread != null)
        {
            _log.Warn("Simulations-Thread läuft bereits.");
            return;
        }

        _stopRequested = false;
        _thread = new Thread(Run) { IsBackground = true, Name = "Simulation" };
        _thread.Start();
        _log.Info("Simulations-Thread gestartet.");
    }

    public void Send(ISimulationCommand command)
    {
        if (_thread == null || _faulted)
        {
            _log.Warn($"Befehl {command.GetType().Name} verworfen: Simulation läuft nicht.");
            return;
        }

        _commands.Enqueue(command);
        _wake.Set();
    }

    // Weltzustand zum Speichern. null, wenn er nicht sicher lesbar ist (Fehler im Thread, Zeitüberschreitung).
    public WorldState? CaptureState()
    {
        if (_faulted)
        {
            _log.Error("Weltzustand nach einem Fehler im Simulations-Thread wird nicht gespeichert.");
            return null;
        }

        // Ohne laufenden Thread schreibt niemand mehr, der Zustand ist direkt lesbar
        if (_thread == null)
        {
            return World.CaptureState();
        }

        var command = new CaptureStateCommand();
        Send(command);
        try
        {
            if (command.Result.Wait(CaptureTimeout))
            {
                return command.Result.Result;
            }

            _log.Error($"Weltzustand nicht innerhalb von {CaptureTimeout.TotalSeconds} s gelesen.");
        }
        catch (AggregateException e)
        {
            _log.Error("Weltzustand konnte nicht gelesen werden.", e);
        }

        return null;
    }

    public void Stop()
    {
        if (_thread == null)
        {
            return;
        }

        _stopRequested = true;
        _wake.Set();
        if (!_thread.Join(StopTimeout))
        {
            _log.Error($"Simulations-Thread hat nicht innerhalb von {StopTimeout.TotalSeconds} s beendet.");
            return;
        }

        _thread = null;
        _log.Info("Simulations-Thread beendet.");
    }

    private void Run()
    {
        try
        {
            while (!_stopRequested)
            {
                ExecuteCommands();

                ulong target = _time.GetTarget(World.Clock.Now);
                bool reached = _runner.AdvanceTo(target, PerfBudgets.SliceMs);
                if (reached)
                {
                    _time.OnAdvanced(World.Clock.Now);
                }

                Publish();
                if (!reached)
                {
                    continue;
                }

                TimeSpan? wait = _time.GetWaitTime();
                if (wait is not TimeSpan timeout)
                {
                    _wake.WaitOne();
                }
                else if (timeout > TimeSpan.Zero)
                {
                    _wake.WaitOne(timeout);
                }
            }
        }
        catch (Exception e)
        {
            // Kein Weiterrechnen mit halb aktualisiertem Zustand; die Darstellung sieht den Fehler im Snapshot
            _faulted = true;
            _log.Error("Simulation angehalten: Fehler im Simulations-Thread.", e);
            Publish();
        }
        finally
        {
            CancelPendingCommands();
        }
    }

    private void ExecuteCommands()
    {
        while (_commands.TryDequeue(out ISimulationCommand? command))
        {
            command.Execute(_context);
        }
    }

    private void CancelPendingCommands()
    {
        while (_commands.TryDequeue(out ISimulationCommand? command))
        {
            if (command is CaptureStateCommand capture)
            {
                capture.Cancel();
            }
            else
            {
                _log.Warn($"Befehl {command.GetType().Name} beim Beenden verworfen.");
            }
        }
    }

    private void Publish() => Volatile.Write(ref _snapshot, CreateSnapshot());

    private SimulationSnapshot CreateSnapshot() =>
        new(World.Clock.Now, _time.Mode, _time.SkipTargetSeconds, _faulted);
}
