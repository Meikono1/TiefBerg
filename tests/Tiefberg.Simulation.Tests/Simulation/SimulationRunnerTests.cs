using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Simulation;
using TiefBerg.Simulation.Scheduling;

namespace TiefBerg.Tests.Simulation;

public class SimulationRunnerTests
{
    private static SimulationRunner CreateRunner() =>
        new(NullLogger<SimulationRunner>.Instance, World.Create(NullLoggerFactory.Instance, 1, 0));

    [Fact]
    public void Tick_BeforeStart_DoesNothing()
    {
        SimulationRunner runner = CreateRunner();

        runner.Tick();

        Assert.False(runner.IsRunning);
        Assert.Equal(0UL, runner.TickCount);
    }

    [Fact]
    public void Tick_AfterStart_CountsTicks()
    {
        SimulationRunner runner = CreateRunner();
        runner.Start();

        runner.Tick();
        runner.Tick();

        Assert.True(runner.IsRunning);
        Assert.Equal(2UL, runner.TickCount);
    }

    [Fact]
    public void Tick_AfterStop_DoesNothing()
    {
        SimulationRunner runner = CreateRunner();
        runner.Start();
        runner.Tick();
        runner.Stop();

        runner.Tick();

        Assert.False(runner.IsRunning);
        Assert.Equal(1UL, runner.TickCount);
    }

    [Fact]
    public void Tick_AdvancesWorldClockByOneSecond()
    {
        SimulationRunner runner = CreateRunner();
        runner.Start();

        runner.Tick();
        runner.Tick();

        Assert.Equal(2UL, runner.Clock.Now);
    }

    [Fact]
    public void Tick_BeforeStart_DoesNotAdvanceWorldClock()
    {
        SimulationRunner runner = CreateRunner();

        runner.Tick();

        Assert.Equal(0UL, runner.Clock.Now);
    }

    [Fact]
    public void Tick_RunsDueSystems()
    {
        SimulationRunner runner = CreateRunner();
        var system = new CountingSystem();
        runner.Scheduler.Register(system, 2);
        runner.Start();

        runner.Tick();
        runner.Tick();
        runner.Tick();
        runner.Tick();

        Assert.Equal(2, system.Runs);
    }

    private sealed class CountingSystem : ISimulationSystem
    {
        public int Runs { get; private set; }

        public string Id => "counting";

        public ulong IntervalSeconds => 2;

        public void Run(ulong dueSeconds) => Runs++;
    }
}
