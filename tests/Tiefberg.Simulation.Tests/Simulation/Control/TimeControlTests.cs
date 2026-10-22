using System;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Simulation.Control;

namespace TiefBerg.Tests.Simulation.Control;

public class TimeControlTests
{
    private readonly ManualTimeProvider _time = new();

    private TimeControl Create() => new(NullLogger<TimeControl>.Instance, _time);

    [Fact]
    public void New_IsPaused()
    {
        TimeControl control = Create();

        Assert.Equal(TimeMode.Paused, control.Mode);
        Assert.Equal(500UL, control.GetTarget(500));
        Assert.Null(control.GetWaitTime());
    }

    [Fact]
    public void RealTime_OneWorldSecondPerRealSecond()
    {
        TimeControl control = Create();
        control.Resume(1_000);

        _time.Advance(TimeSpan.FromMilliseconds(2_500));

        Assert.Equal(1_002UL, control.GetTarget(1_000));
    }

    [Fact]
    public void RealTime_AfterPause_DoesNotJumpOverPausedTime()
    {
        TimeControl control = Create();
        control.Resume(0);
        _time.Advance(TimeSpan.FromSeconds(3));
        control.Pause(3);

        _time.Advance(TimeSpan.FromSeconds(100));
        control.Resume(3);
        _time.Advance(TimeSpan.FromSeconds(1));

        Assert.Equal(4UL, control.GetTarget(3));
    }

    [Fact]
    public void RealTime_WaitsUntilNextFullSecond()
    {
        TimeControl control = Create();
        control.Resume(0);

        _time.Advance(TimeSpan.FromMilliseconds(1_300));

        Assert.Equal(TimeSpan.FromMilliseconds(700), control.GetWaitTime());
    }

    [Fact]
    public void SkipTo_TargetIsSkipTarget_ThenPausesWhenReached()
    {
        TimeControl control = Create();
        control.SkipTo(100, 5_000);

        Assert.Equal(TimeMode.Skipping, control.Mode);
        Assert.Equal(5_000UL, control.GetTarget(100));
        Assert.Equal(TimeSpan.Zero, control.GetWaitTime());

        control.OnAdvanced(4_999);
        Assert.Equal(TimeMode.Skipping, control.Mode);

        control.OnAdvanced(5_000);
        Assert.Equal(TimeMode.Paused, control.Mode);
        Assert.Null(control.SkipTargetSeconds);
    }

    [Fact]
    public void SkipTo_TargetNotAfterNow_IsIgnored()
    {
        TimeControl control = Create();

        control.SkipTo(100, 100);

        Assert.Equal(TimeMode.Paused, control.Mode);
    }

    [Fact]
    public void SkipToNextEvent_JumpsToNextDue()
    {
        TimeControl control = Create();

        control.SkipToNextEvent(100, 3_600);

        Assert.Equal(3_600UL, control.SkipTargetSeconds);
    }

    [Fact]
    public void SkipToNextEvent_NothingPlanned_StaysPaused()
    {
        TimeControl control = Create();

        control.SkipToNextEvent(100, null);

        Assert.Equal(TimeMode.Paused, control.Mode);
    }

    [Fact]
    public void Resume_DuringSkip_SwitchesToRealTime()
    {
        TimeControl control = Create();
        control.SkipTo(0, 1_000);

        control.Resume(200);

        Assert.Equal(TimeMode.RealTime, control.Mode);
        Assert.Null(control.SkipTargetSeconds);
        Assert.Equal(200UL, control.GetTarget(200));
    }
}
