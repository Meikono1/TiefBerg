using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;
using TiefBerg.Simulation.Dice;
using TiefBerg.Simulation.Scheduling;
using TiefBerg.Simulation.Time;

namespace TiefBerg.Simulation;

// Speicherbarer Weltzustand (Abschnitt 12). Kennt kein Szenario; der Szenariozustand wird getrennt gespeichert.
public sealed record WorldState(ulong Seed, ulong WorldSeconds, RandomState Random, IReadOnlyList<ScheduledRun> Schedule);

// Bündelt alles, was eine Welt ausmacht: Seed, Weltzeit, Scheduler und Würfeldienst.
// Gleicher Seed -> gleiche Welt (Abschnitt 2); ein geladener Zustand setzt exakt dort fort, wo gespeichert wurde.
public sealed class World
{
    private readonly ILogger<World> _log;

    private World(ILoggerFactory loggers, ulong seed, ulong startSeconds)
    {
        _log = loggers.CreateLogger<World>();
        Seed = seed;
        Clock = new WorldClock(loggers.CreateLogger<WorldClock>(), startSeconds);
        Scheduler = new Scheduler(loggers.CreateLogger<Scheduler>());
        Dice = new DiceService(loggers.CreateLogger<DiceService>(), RandomSource.FromSeed(seed));
        RegisterSystems();
    }

    public ulong Seed { get; }

    public WorldClock Clock { get; }

    public Scheduler Scheduler { get; }

    public DiceService Dice { get; }

    // TODO: Startzeit kommt aus Welt-/Szenariodaten (Abschnitt 3, 11; T-025)
    public static World Create(ILoggerFactory loggers, ulong seed, ulong startSeconds)
    {
        var world = new World(loggers, seed, startSeconds);
        world._log.Info($"Neue Welt erzeugt, Seed {seed}.");
        return world;
    }

    public static World Restore(ILoggerFactory loggers, WorldState state)
    {
        var world = new World(loggers, state.Seed, state.WorldSeconds);
        world.Dice.Random.SetState(state.Random);
        world.Scheduler.ApplySchedule(state.Schedule);
        world._log.Info($"Welt geladen, Seed {state.Seed}.");
        return world;
    }

    public WorldState CaptureState() =>
        new(Seed, Clock.Now, Dice.Random.GetState(), Scheduler.GetSchedule());

    // Neue und geladene Welten registrieren dieselben Systeme; Restore übernimmt danach nur die Fälligkeiten
    private void RegisterSystems()
    {
        // TODO: Systeme (Bedürfnisse stündlich, Wirtschaft täglich, Diplomatie wöchentlich …) registrieren (Abschnitt 3)
    }
}
