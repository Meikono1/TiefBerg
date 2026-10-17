using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Simulation;
using TiefBerg.Simulation.Dice;

namespace TiefBerg.Tests.Simulation;

public class WorldTests
{
    [Fact]
    public void Create_SameSeed_SameRolls()
    {
        World a = World.Create(NullLoggerFactory.Instance, 77, 0);
        World b = World.Create(NullLoggerFactory.Instance, 77, 0);

        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(a.Dice.Roll(20), b.Dice.Roll(20));
        }
    }

    [Fact]
    public void Restore_ContinuesExactlyWhereCaptured()
    {
        World original = World.Create(NullLoggerFactory.Instance, 5, 1_000);
        original.Clock.Advance(500);
        original.Dice.Roll(20);
        WorldState saved = original.CaptureState();

        World restored = World.Restore(NullLoggerFactory.Instance, saved);

        Assert.Equal(5UL, restored.Seed);
        Assert.Equal(1_500UL, restored.Clock.Now);
        for (int i = 0; i < 20; i++)
        {
            Assert.Equal(original.Dice.Roll(20), restored.Dice.Roll(20));
        }
    }

    [Fact]
    public void CaptureState_ContainsRandomState()
    {
        World world = World.Create(NullLoggerFactory.Instance, 5, 0);

        Assert.Equal(RandomSource.FromSeed(5).GetState(), world.CaptureState().Random);
    }
}
