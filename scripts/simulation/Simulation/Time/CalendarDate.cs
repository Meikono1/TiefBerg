namespace TiefBerg.Simulation.Time;

// Kalenderdarstellung einer Weltzeit, nur für Anzeige und Logs (Abschnitt 3). Gerechnet wird immer mit der Weltzeit in Sekunden.
// Jahr, Monat und Tag zählen ab 1, Stunde/Minute/Sekunde ab 0. BeforeEra = vor Jahr 1 des Kalenders (wie "v. Chr."),
// Year zählt dann rückwärts; ein Jahr 0 gibt es nicht. Monate und Tage laufen auch vor Jahr 1 vorwärts.
public readonly record struct CalendarDate(
    ulong Year, int Month, int Day, int Hour, int Minute, int Second, bool BeforeEra = false)
{
    // Technisches Format für Logs; Spielertexte kommen später über Übersetzungsschlüssel (Abschnitt 12)
    public override string ToString() =>
        $"{Year}-{Month:D2}-{Day:D2} {Hour:D2}:{Minute:D2}:{Second:D2}{(BeforeEra ? " v. Z." : "")}";
}
