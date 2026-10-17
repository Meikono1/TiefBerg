using System;
using TiefBerg.Simulation.Dice;

namespace TiefBerg.Tests.Simulation.Dice;

public class RandomSourceTests
{
    [Fact]
    public void NextULong_MatchesXoshiro256StarStarReference()
    {
        // Referenzwerte von xoshiro256** für den Zustand {1, 2, 3, 4}
        RandomSource random = RandomSource.FromState(new RandomState(1, 2, 3, 4));

        Assert.Equal(11520UL, random.NextULong());
        Assert.Equal(0UL, random.NextULong());
        Assert.Equal(1509978240UL, random.NextULong());
    }

    [Fact]
    public void FromSeed_SameSeed_SameSequence()
    {
        RandomSource a = RandomSource.FromSeed(42);
        RandomSource b = RandomSource.FromSeed(42);

        for (int i = 0; i < 100; i++)
        {
            Assert.Equal(a.NextULong(), b.NextULong());
        }
    }

    [Fact]
    public void FromSeed_DifferentSeeds_DifferentSequence()
    {
        Assert.NotEqual(RandomSource.FromSeed(1).NextULong(), RandomSource.FromSeed(2).NextULong());
    }

    [Fact]
    public void GetState_SetState_ContinuesSameSequence()
    {
        RandomSource original = RandomSource.FromSeed(7);
        original.NextULong();
        RandomState saved = original.GetState();
        ulong expected = original.NextULong();

        RandomSource restored = RandomSource.FromState(saved);

        Assert.Equal(expected, restored.NextULong());
    }

    [Fact]
    public void Derive_IndependentOfConsumedValues()
    {
        RandomSource consumed = RandomSource.FromSeed(99);
        consumed.NextULong();

        Assert.Equal(RandomSource.Derive(99, 5).NextULong(), RandomSource.Derive(99, 5).NextULong());
        Assert.NotEqual(RandomSource.Derive(99, 5).NextULong(), RandomSource.Derive(99, 6).NextULong());
    }

    [Fact]
    public void SetState_AllZero_Throws()
    {
        Assert.Throws<ArgumentException>(() => RandomSource.FromState(new RandomState(0, 0, 0, 0)));
    }

    [Fact]
    public void NextUInt_StaysInRangeAndHitsEveryValue()
    {
        RandomSource random = RandomSource.FromSeed(3);
        var seen = new bool[6];

        for (int i = 0; i < 10_000; i++)
        {
            uint value = random.NextUInt(6);
            Assert.InRange(value, 0u, 5u);
            seen[value] = true;
        }

        Assert.All(seen, Assert.True);
    }

    [Fact]
    public void NextUInt_Zero_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => RandomSource.FromSeed(1).NextUInt(0));
    }

    [Fact]
    public void NextDouble_InUnitInterval()
    {
        RandomSource random = RandomSource.FromSeed(4);

        for (int i = 0; i < 1_000; i++)
        {
            Assert.InRange(random.NextDouble(), 0.0, 0.9999999999999999);
        }
    }
}
