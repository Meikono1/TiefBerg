namespace TiefBerg.Simulation.Dice;

// Erfolgsgrade wie PF2e (Abschnitt 7); Reihenfolge = schlecht nach gut, Vergleiche wie >= Success sind gewollt
public enum CheckDegree
{
    CriticalFailure,
    Failure,
    Success,
    CriticalSuccess
}

// Ergebnis einer Probe: W20 + Level + Modifikatoren gegen SG (Abschnitt 7)
public readonly record struct CheckResult(int Die, int Bonus, int Difficulty)
{
    // Abstand für kritische Erfolge/Fehlschläge
    public const int CriticalMargin = 10;

    public int Total => Die + Bonus;

    // TODO: Natürliche 20/1 als Stufe besser/schlechter (PF2e) – offen (Abschnitt 7)
    public CheckDegree Degree => DegreeOf(Total, Difficulty);

    public bool Success => Degree >= CheckDegree.Success;

    public static CheckDegree DegreeOf(int total, int difficulty)
    {
        if (total >= difficulty + CriticalMargin)
        {
            return CheckDegree.CriticalSuccess;
        }

        if (total >= difficulty)
        {
            return CheckDegree.Success;
        }

        return total <= difficulty - CriticalMargin ? CheckDegree.CriticalFailure : CheckDegree.Failure;
    }
}

// Wahrscheinlichkeit je Erfolgsgrad, für die Statistik-Ebene (Abschnitt 10); Summe = 1
public readonly record struct DegreeChances(
    double CriticalFailure, double Failure, double Success, double CriticalSuccess)
{
    public double SuccessOrBetter => Success + CriticalSuccess;

    // Erwartungswert des Schadens bei einem Rettungswurf, statt einzeln zu würfeln
    public double ExpectedSaveDamage(int damage) =>
        Failure * damage
        + CriticalFailure * DiceService.DamageAfterSave(damage, CheckDegree.CriticalFailure)
        + Success * DiceService.DamageAfterSave(damage, CheckDegree.Success);
}
