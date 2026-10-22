using System;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Simulation.Dice;

namespace TiefBerg.Tests.Simulation.Dice;

public class DiceServiceTests
{
    private static DiceService Create(ulong seed) => new(NullLogger<DiceService>.Instance, RandomSource.FromSeed(seed));

    [Fact]
    public void Roll_SameSeed_SameResults()
    {
        DiceService a = Create(1234);
        DiceService b = Create(1234);

        for (int i = 0; i < 50; i++)
        {
            Assert.Equal(a.Roll(20), b.Roll(20));
        }
    }

    [Fact]
    public void Roll_StaysWithinOneToSides()
    {
        DiceService dice = Create(1);

        for (int i = 0; i < 1_000; i++)
        {
            Assert.InRange(dice.Roll(20), 1, 20);
        }
    }

    [Fact]
    public void Roll_MultipleDice_SumInRange()
    {
        DiceService dice = Create(2);

        for (int i = 0; i < 1_000; i++)
        {
            Assert.InRange(dice.Roll(2, 6), 2, 12);
        }
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Roll_InvalidSides_Throws(int sides)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(1).Roll(sides));
    }

    [Fact]
    public void Check_SuccessMatchesTotalAgainstDifficulty()
    {
        DiceService dice = Create(5);

        for (int i = 0; i < 200; i++)
        {
            CheckResult result = dice.Check(5, 2, 18);

            Assert.InRange(result.Die, 1, 20);
            Assert.Equal(result.Die + 7, result.Total);
            Assert.Equal(result.Total >= 18, result.Success);
        }
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(31)]
    public void Check_LevelOutOfRange_Throws(int level)
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => Create(1).Check(level, 0, 10));
    }

    [Theory]
    [InlineData(0, 0, 11, 0.5)] // braucht 11+ auf W20
    [InlineData(0, 0, 1, 0.95)] // natürliche 1 trifft SG 1 knapp, eine Stufe schlechter Fehlschlag
    [InlineData(0, 0, 21, 0.05)] // nur die natürliche 20 hebt den Fehlschlag auf Erfolg
    [InlineData(0, 0, 31, 0.0)] // auch mit 20 kritisch daneben, die Stufe hebt nur auf Fehlschlag
    [InlineData(15, 0, 10, 0.95)] // natürliche 1: 16 gegen SG 10 ist Erfolg, eine Stufe schlechter Fehlschlag
    [InlineData(15, 0, 6, 1.0)] // Drache Stufe 15 gegen leichte Probe: auch die 1 ist noch kritisch, bleibt Erfolg
    [InlineData(1, 2, 20, 0.2)] // braucht 17+
    public void SuccessChance_MatchesD20(int level, int modifier, int difficulty, double expected)
    {
        Assert.Equal(expected, DiceService.SuccessChance(level, modifier, difficulty), 10);
    }

    [Theory]
    [InlineData(25, 15, CheckDegree.CriticalSuccess)]
    [InlineData(24, 15, CheckDegree.Success)]
    [InlineData(15, 15, CheckDegree.Success)]
    [InlineData(14, 15, CheckDegree.Failure)]
    [InlineData(6, 15, CheckDegree.Failure)]
    [InlineData(5, 15, CheckDegree.CriticalFailure)]
    public void DegreeOf_UsesTenAsCriticalMargin(int total, int difficulty, CheckDegree expected)
    {
        // Augenzahl 10 verschiebt keinen Grad
        Assert.Equal(expected, CheckResult.DegreeOf(10, total, difficulty));
    }

    [Theory]
    [InlineData(20, 20, 15, CheckDegree.CriticalSuccess)] // Erfolg → kritischer Erfolg
    [InlineData(20, 14, 15, CheckDegree.Success)] // Fehlschlag → Erfolg
    [InlineData(20, 5, 15, CheckDegree.Failure)] // kritischer Fehlschlag → Fehlschlag
    [InlineData(20, 30, 15, CheckDegree.CriticalSuccess)] // bleibt an der Obergrenze
    [InlineData(1, 25, 15, CheckDegree.Success)] // kritischer Erfolg → Erfolg
    [InlineData(1, 15, 15, CheckDegree.Failure)] // Erfolg → Fehlschlag
    [InlineData(1, 6, 15, CheckDegree.CriticalFailure)] // Fehlschlag → kritischer Fehlschlag
    [InlineData(1, 1, 15, CheckDegree.CriticalFailure)] // bleibt an der Untergrenze
    public void DegreeOf_NaturalTwentyAndOneShiftOneStep(int die, int total, int difficulty, CheckDegree expected)
    {
        Assert.Equal(expected, CheckResult.DegreeOf(die, total, difficulty));
    }

    [Theory]
    [InlineData(CheckDegree.CriticalSuccess, 0)]
    [InlineData(CheckDegree.Success, 5)]
    [InlineData(CheckDegree.Failure, 11)]
    [InlineData(CheckDegree.CriticalFailure, 22)]
    public void DamageAfterSave_ScalesByDegree(CheckDegree degree, int expected)
    {
        // 11 ist ungerade: halber Schaden wird abgerundet
        Assert.Equal(expected, DiceService.DamageAfterSave(11, degree));
    }

    [Fact]
    public void DamageAfterSave_NegativeDamage_Throws()
    {
        Assert.Throws<ArgumentOutOfRangeException>(() => DiceService.DamageAfterSave(-1, CheckDegree.Success));
    }

    [Fact]
    public void Chances_CountsEachDieFace()
    {
        // Bonus 0 gegen SG 15: 1–5 kritisch daneben, 6–14 daneben, 15–19 Erfolg, 20 kritisch (natürliche 20 hebt)
        DegreeChances chances = DiceService.Chances(0, 0, 15);

        Assert.Equal(0.25, chances.CriticalFailure, 10);
        Assert.Equal(0.45, chances.Failure, 10);
        Assert.Equal(0.25, chances.Success, 10);
        Assert.Equal(0.05, chances.CriticalSuccess, 10);
        Assert.Equal(0.25 * 20 + 0.45 * 10 + 0.25 * 5, chances.ExpectedSaveDamage(10), 10);
    }

    [Fact]
    public void Check_DegreeMatchesTotal()
    {
        DiceService dice = Create(9);

        for (int i = 0; i < 200; i++)
        {
            CheckResult result = dice.Check(3, 0, 13);
            Assert.Equal(CheckResult.DegreeOf(result.Die, result.Total, 13), result.Degree);
        }
    }
}
