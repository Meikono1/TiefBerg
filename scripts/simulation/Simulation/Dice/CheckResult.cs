namespace TiefBerg.Simulation.Dice;

// Vier Erfolgsgrade (Abschnitt 7); Reihenfolge = schlecht nach gut, Vergleiche wie >= Success sind gewollt
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

    // Würfel der Probe; seine höchste und niedrigste Augenzahl verschieben den Erfolgsgrad
    public const int Sides = 20;

    public int Total => Die + Bonus;

    public CheckDegree Degree => DegreeOf(Die, Total, Difficulty);

    public bool Success => Degree >= CheckDegree.Success;

    // Grad aus dem Abstand zum SG, dann natürliche 20 eine Stufe besser, natürliche 1 eine schlechter (Abschnitt 7)
    public static CheckDegree DegreeOf(int die, int total, int difficulty)
    {
        CheckDegree degree = DegreeOf(total, difficulty);

        if (die == Sides && degree < CheckDegree.CriticalSuccess)
        {
            return degree + 1;
        }

        if (die == 1 && degree > CheckDegree.CriticalFailure)
        {
            return degree - 1;
        }

        return degree;
    }

    private static CheckDegree DegreeOf(int total, int difficulty)
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
