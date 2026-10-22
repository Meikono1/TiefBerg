using System;
using System.Diagnostics;
using System.Threading;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Simulation;
using TiefBerg.Simulation.Control;
using TiefBerg.Simulation.Control.Commands;
using TiefBerg.Simulation.Scheduling;
using TiefBerg.Simulation.Time;

namespace TiefBerg.Tests.Simulation.Control;

// Läuft mit echtem Thread und echter Uhr; gewartet wird nur auf Zustände, nicht auf feste Zeiten
public class SimulationThreadTests
{
    private static readonly TimeSpan WaitLimit = TimeSpan.FromSeconds(10);

    private static SimulationThread Create(World? world = null) =>
        new(NullLoggerFactory.Instance, world ?? World.Create(NullLoggerFactory.Instance, 1, 0), TimeProvider.System);

    private static void WaitFor(SimulationThread thread, Func<SimulationSnapshot, bool> condition)
    {
        var watch = Stopwatch.StartNew();
        while (!condition(thread.Snapshot))
        {
            Assert.True(watch.Elapsed < WaitLimit, $"Zustand nicht erreicht: {thread.Snapshot}");
            Thread.Sleep(5);
        }
    }

    [Fact]
    public void Start_IsPaused()
    {
        SimulationThread thread = Create();
        thread.Start();
        Thread.Sleep(50);

        Assert.Equal(TimeMode.Paused, thread.Snapshot.Mode);
        Assert.Equal(0UL, thread.Snapshot.WorldSeconds);
        thread.Stop();
    }

    [Fact]
    public void RunForOneDay_EndsExactlyAfterOneDayAndPauses()
    {
        SimulationThread thread = Create();
        thread.Start();

        thread.Send(RunForCommand.OneDay);
        WaitFor(thread, s => s.Mode == TimeMode.Paused && s.WorldSeconds > 0);

        Assert.Equal(WorldCalendar.SecondsPerDay, thread.Snapshot.WorldSeconds);
        thread.Stop();
    }

    [Fact]
    public void RunToNextEvent_StopsAtNextDue()
    {
        World world = World.Create(NullLoggerFactory.Instance, 1, 0);
        world.Scheduler.Register(new FailingSystem(throws: false), 3_600);
        SimulationThread thread = Create(world);
        thread.Start();

        thread.Send(new RunToNextEventCommand());
        WaitFor(thread, s => s.Mode == TimeMode.Paused && s.WorldSeconds > 0);

        Assert.Equal(3_600UL, thread.Snapshot.WorldSeconds);
        thread.Stop();
    }

    [Fact]
    public void Resume_TimeRunsInRealTime()
    {
        SimulationThread thread = Create();
        thread.Start();

        thread.Send(new ResumeCommand());
        WaitFor(thread, s => s.WorldSeconds >= 1);

        Assert.Equal(TimeMode.RealTime, thread.Snapshot.Mode);
        thread.Stop();
    }

    [Fact]
    public void CaptureState_WhileRunning_ReturnsCurrentState()
    {
        SimulationThread thread = Create();
        thread.Start();
        thread.Send(RunForCommand.OneMonth);
        WaitFor(thread, s => s.Mode == TimeMode.Paused && s.WorldSeconds > 0);

        WorldState? state = thread.CaptureState();

        Assert.NotNull(state);
        Assert.Equal(WorldCalendar.SecondsPerMonth, state.WorldSeconds);
        thread.Stop();
    }

    [Fact]
    public void CaptureState_AfterStop_ReadsDirectly()
    {
        SimulationThread thread = Create();
        thread.Start();
        thread.Stop();

        Assert.NotNull(thread.CaptureState());
        Assert.False(thread.IsRunning);
    }

    [Fact]
    public void FailingSystem_FaultsAndRefusesToSave()
    {
        World world = World.Create(NullLoggerFactory.Instance, 1, 0);
        world.Scheduler.Register(new FailingSystem(throws: true), 10);
        SimulationThread thread = Create(world);
        thread.Start();

        thread.Send(RunForCommand.OneDay);
        WaitFor(thread, s => s.Faulted);

        Assert.Equal(10UL, thread.Snapshot.WorldSeconds);
        Assert.Null(thread.CaptureState());
        thread.Stop();
    }

    private sealed class FailingSystem : ISimulationSystem
    {
        private readonly bool _throws;

        public FailingSystem(bool throws)
        {
            _throws = throws;
        }

        public string Id => "failing";

        public ulong IntervalSeconds => WorldCalendar.SecondsPerYear;

        public void Run(ulong dueSeconds)
        {
            if (_throws)
            {
                throw new InvalidOperationException("Testfehler");
            }
        }
    }
}
