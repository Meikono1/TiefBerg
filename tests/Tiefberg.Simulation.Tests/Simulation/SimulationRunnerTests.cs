using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Simulation;
using TiefBerg.Simulation.Scheduling;
using TiefBerg.Tests.Simulation.Control;

namespace TiefBerg.Tests.Simulation;

public class SimulationRunnerTests
{
    private const double NoBudgetLimit = double.MaxValue;

    private static SimulationRunner CreateRunner(TimeProvider? time = null, ulong startSeconds = 0) =>
        new(NullLogger<SimulationRunner>.Instance, World.Create(NullLoggerFactory.Instance, 1, startSeconds),
            time ?? new ManualTimeProvider());

    [Fact]
    public void AdvanceTo_WithoutSystems_JumpsToTarget()
    {
        SimulationRunner runner = CreateRunner();

        bool reached = runner.AdvanceTo(1_000_000, NoBudgetLimit);

        Assert.True(reached);
        Assert.Equal(1_000_000UL, runner.Clock.Now);
    }

    [Fact]
    public void AdvanceTo_TargetInPast_KeepsWorldTime()
    {
        SimulationRunner runner = CreateRunner(startSeconds: 500);

        bool reached = runner.AdvanceTo(100, NoBudgetLimit);

        Assert.True(reached);
        Assert.Equal(500UL, runner.Clock.Now);
    }

    [Fact]
    public void AdvanceTo_RunsSystemsAtTheirDueTime()
    {
        SimulationRunner runner = CreateRunner();
        var system = new RecordingSystem(runner, 10);
        runner.Scheduler.Register(system, 10);

        runner.AdvanceTo(35, NoBudgetLimit);

        Assert.Equal([10UL, 20UL, 30UL], system.Dues);
        // Beim Lauf steht die Uhr genau auf der Fälligkeit
        Assert.Equal(system.Dues, system.ClockAtRun);
        Assert.Equal(35UL, runner.Clock.Now);
    }

    [Fact]
    public void AdvanceTo_DueOnTarget_RunsSystem()
    {
        SimulationRunner runner = CreateRunner();
        var system = new RecordingSystem(runner, 10);
        runner.Scheduler.Register(system, 10);

        runner.AdvanceTo(10, NoBudgetLimit);

        Assert.Equal([10UL], system.Dues);
    }

    [Fact]
    public void AdvanceTo_DueBeforeNow_CatchesUpWithoutMovingClock()
    {
        SimulationRunner runner = CreateRunner(startSeconds: 100);
        var system = new RecordingSystem(runner, 30);
        runner.Scheduler.Register(system, 40);

        runner.AdvanceTo(100, NoBudgetLimit);

        Assert.Equal([40UL, 70UL, 100UL], system.Dues);
        Assert.Equal([100UL, 100UL, 100UL], system.ClockAtRun);
    }

    [Fact]
    public void AdvanceTo_BudgetUsedUp_StopsAndContinuesNextCall()
    {
        var time = new ManualTimeProvider();
        SimulationRunner runner = CreateRunner(time);
        // Jeder Lauf kostet 4 ms Echtzeit; Budget 6 ms -> nach dem zweiten Lauf ist Schluss
        var system = new RecordingSystem(runner, 10, () => time.Advance(TimeSpan.FromMilliseconds(4)));
        runner.Scheduler.Register(system, 10);

        bool first = runner.AdvanceTo(100, 6);

        Assert.False(first);
        Assert.Equal([10UL, 20UL], system.Dues);
        Assert.Equal(20UL, runner.Clock.Now);

        bool second = runner.AdvanceTo(100, NoBudgetLimit);

        Assert.True(second);
        Assert.Equal(10, system.Dues.Count);
        Assert.Equal(100UL, runner.Clock.Now);
    }

    [Fact]
    public void AdvanceTo_SameResultIndependentOfBudget()
    {
        var time = new ManualTimeProvider();
        SimulationRunner sliced = CreateRunner(time);
        var slicedSystem = new RecordingSystem(sliced, 7, () => time.Advance(TimeSpan.FromMilliseconds(3)));
        sliced.Scheduler.Register(slicedSystem, 3);
        SimulationRunner whole = CreateRunner();
        var wholeSystem = new RecordingSystem(whole, 7);
        whole.Scheduler.Register(wholeSystem, 3);

        while (!sliced.AdvanceTo(500, 5))
        {
        }

        whole.AdvanceTo(500, NoBudgetLimit);

        Assert.Equal(wholeSystem.Dues, slicedSystem.Dues);
        Assert.Equal(whole.Clock.Now, sliced.Clock.Now);
    }

    private sealed class RecordingSystem : ISimulationSystem
    {
        private readonly SimulationRunner _runner;
        private readonly Action? _onRun;

        public RecordingSystem(SimulationRunner runner, ulong interval, Action? onRun = null)
        {
            _runner = runner;
            IntervalSeconds = interval;
            _onRun = onRun;
        }

        public List<ulong> Dues { get; } = [];

        public List<ulong> ClockAtRun { get; } = [];

        public string Id => "recording";

        public ulong IntervalSeconds { get; }

        public void Run(ulong dueSeconds)
        {
            Dues.Add(dueSeconds);
            ClockAtRun.Add(_runner.Clock.Now);
            _onRun?.Invoke();
        }
    }
}
