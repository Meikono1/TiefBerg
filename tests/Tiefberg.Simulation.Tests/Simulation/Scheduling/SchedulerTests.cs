using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Simulation.Scheduling;

namespace TiefBerg.Tests.Simulation.Scheduling;

public class SchedulerTests
{
    private readonly List<string> _runs = new();
    private readonly Scheduler _scheduler = new(NullLogger<Scheduler>.Instance);

    [Fact]
    public void RunDue_NothingDue_RunsNothing()
    {
        _scheduler.Register(new RecordingSystem("A", 10, _runs), 10);

        _scheduler.RunDue(9);

        Assert.Empty(_runs);
        Assert.Equal(10UL, _scheduler.NextDue);
    }

    [Fact]
    public void RunDue_RunsAtDueTimeAndReschedulesByInterval()
    {
        _scheduler.Register(new RecordingSystem("A", 10, _runs), 10);

        _scheduler.RunDue(10);
        _scheduler.RunDue(19);
        _scheduler.RunDue(20);

        Assert.Equal(new[] { "A@10", "A@20" }, _runs);
        Assert.Equal(30UL, _scheduler.NextDue);
    }

    [Fact]
    public void RunDue_OrdersByDueTime()
    {
        _scheduler.Register(new RecordingSystem("Daily", 72_000, _runs), 72_000);
        _scheduler.Register(new RecordingSystem("Hourly", 3_600, _runs), 3_600);

        _scheduler.RunDue(72_000);

        Assert.Equal(21, _runs.Count);
        Assert.Equal("Hourly@3600", _runs[0]);
        // Gleiche Fälligkeit: Daily wurde vor dem 20. Lauf von Hourly eingereiht
        Assert.Equal("Daily@72000", _runs[19]);
        Assert.Equal("Hourly@72000", _runs[20]);
    }

    [Fact]
    public void RunDue_SameDueTime_KeepsRegistrationOrder()
    {
        _scheduler.Register(new RecordingSystem("B", 5, _runs), 5);
        _scheduler.Register(new RecordingSystem("A", 5, _runs), 5);
        _scheduler.Register(new RecordingSystem("C", 5, _runs), 5);

        _scheduler.RunDue(10);

        Assert.Equal(new[] { "B@5", "A@5", "C@5", "B@10", "A@10", "C@10" }, _runs);
    }

    [Fact]
    public void RunDue_AfterTimeJump_CatchesUpEveryMissedRun()
    {
        _scheduler.Register(new RecordingSystem("A", 10, _runs), 10);

        _scheduler.RunDue(35);

        Assert.Equal(new[] { "A@10", "A@20", "A@30" }, _runs);
        Assert.Equal(40UL, _scheduler.NextDue);
    }

    [Fact]
    public void Register_ZeroInterval_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() =>
            _scheduler.Register(new RecordingSystem("A", 0, _runs), 0));
        Assert.Equal(0, _scheduler.Count);
        Assert.Null(_scheduler.NextDue);
    }

    [Fact]
    public void RunDue_SystemThrows_Propagates()
    {
        _scheduler.Register(new ThrowingSystem(), 1);

        Assert.Throws<InvalidOperationException>(() => _scheduler.RunDue(1));
    }

    private sealed class RecordingSystem : ISimulationSystem
    {
        private readonly string _name;
        private readonly List<string> _runs;

        public RecordingSystem(string name, ulong intervalSeconds, List<string> runs)
        {
            _name = name;
            IntervalSeconds = intervalSeconds;
            _runs = runs;
        }

        public ulong IntervalSeconds { get; }

        public void Run(ulong dueSeconds) => _runs.Add($"{_name}@{dueSeconds}");
    }

    private sealed class ThrowingSystem : ISimulationSystem
    {
        public ulong IntervalSeconds => 1;

        public void Run(ulong dueSeconds) => throw new InvalidOperationException("Test");
    }
}
