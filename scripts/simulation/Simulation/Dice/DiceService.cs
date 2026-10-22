using System;
using System.IO;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;

namespace TiefBerg.Simulation.Dice;

// Zentraler, seedbarer Würfeldienst (Abschnitt 7). Jeder Wurf wird auf Debug geloggt, mit Aufrufer.
// Nicht thread-sicher; parallele Arbeit bekommt eigene Dienste mit RandomSource.Derive.
public sealed class DiceService
{
    public const int MinLevel = 0;
    public const int MaxLevel = 30;

    private readonly ILogger<DiceService> _log;
    private readonly RandomSource _random;

    public DiceService(ILogger<DiceService> log, RandomSource random)
    {
        _log = log;
        _random = random;
    }

    // Für Spielstände (T-012)
    public RandomSource Random => _random;

    public int Roll(int sides, [CallerFilePath] string callerFile = "", [CallerMemberName] string callerMember = "")
    {
        CheckPositive(sides, nameof(sides));
        int result = RollOne(sides);

        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.Debug($"W{sides} = {result} ({Caller(callerFile, callerMember)})");
        }

        return result;
    }

    // Summe mehrerer Würfel, z. B. 2W6
    public int Roll(int count, int sides, [CallerFilePath] string callerFile = "",
        [CallerMemberName] string callerMember = "")
    {
        CheckPositive(count, nameof(count));
        CheckPositive(sides, nameof(sides));

        bool logDice = _log.IsEnabled(LogLevel.Debug);
        int[]? dice = logDice ? new int[count] : null;
        int sum = 0;
        for (int i = 0; i < count; i++)
        {
            int die = RollOne(sides);
            sum += die;
            if (dice != null)
            {
                dice[i] = die;
            }
        }

        if (dice != null)
        {
            _log.Debug($"{count}W{sides} = {sum} [{string.Join(", ", dice)}] ({Caller(callerFile, callerMember)})");
        }

        return sum;
    }

    public CheckResult Check(int level, int modifier, int difficulty, [CallerFilePath] string callerFile = "",
        [CallerMemberName] string callerMember = "")
    {
        CheckLevel(level);
        // TODO: SG aus einem Level ableiten, sobald die Formel feststeht (Abschnitt 7)
        var result = new CheckResult(RollOne(CheckResult.Sides), level + modifier, difficulty);

        if (_log.IsEnabled(LogLevel.Debug))
        {
            _log.Debug($"Probe W20 {result.Die} + Level {level} + Mod {modifier} = {result.Total} gegen SG {difficulty}: " +
                       $"{result.Degree} ({Caller(callerFile, callerMember)})");
        }

        return result;
    }

    // Erfolgsgrade derselben Probe als Wahrscheinlichkeit ohne Wurf, für die Statistik-Ebene (Abschnitt 7, 10)
    public static DegreeChances Chances(int level, int modifier, int difficulty)
    {
        CheckLevelStatic(level);
        // Alle Augenzahlen durchgehen: exakt, inkl. natürlicher 20/1, und folgt jeder Änderung an CheckResult.DegreeOf
        var counts = new int[4];
        for (int die = 1; die <= CheckResult.Sides; die++)
        {
            counts[(int)CheckResult.DegreeOf(die, die + level + modifier, difficulty)]++;
        }

        const double sides = CheckResult.Sides;
        return new DegreeChances(counts[0] / sides, counts[1] / sides, counts[2] / sides, counts[3] / sides);
    }

    public static double SuccessChance(int level, int modifier, int difficulty) =>
        Chances(level, modifier, difficulty).SuccessOrBetter;

    // Rettungswurf gegen Schaden (Abschnitt 7). Halber Schaden wird abgerundet; damage / 2 tut das nur für damage >= 0,
    // daher ist negativer Schaden ausgeschlossen.
    public static int DamageAfterSave(int damage, CheckDegree degree)
    {
        if (damage < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(damage), damage, "Schaden darf nicht negativ sein.");
        }

        return degree switch
        {
            CheckDegree.CriticalSuccess => 0,
            CheckDegree.Success => damage / 2,
            CheckDegree.Failure => damage,
            CheckDegree.CriticalFailure => checked(damage * 2),
            _ => throw new ArgumentOutOfRangeException(nameof(degree), degree, "Unbekannter Erfolgsgrad.")
        };
    }

    private int RollOne(int sides) => (int)_random.NextUInt((uint)sides) + 1;

    private void CheckPositive(int value, string name)
    {
        if (value < 1)
        {
            var e = new ArgumentOutOfRangeException(name, value, "Muss mindestens 1 sein.");
            _log.Error("Ungültiger Wurf.", e);
            throw e;
        }
    }

    private void CheckLevel(int level)
    {
        try
        {
            CheckLevelStatic(level);
        }
        catch (ArgumentOutOfRangeException e)
        {
            _log.Error("Ungültige Probe.", e);
            throw;
        }
    }

    private static void CheckLevelStatic(int level)
    {
        if (level < MinLevel || level > MaxLevel)
        {
            throw new ArgumentOutOfRangeException(nameof(level), level, $"Level liegt in {MinLevel}..{MaxLevel}.");
        }
    }

    // Nur bei aktivem Debug-Log aufgerufen
    private static string Caller(string file, string member) => $"{Path.GetFileNameWithoutExtension(file)}.{member}";
}
