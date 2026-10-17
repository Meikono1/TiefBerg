using System;

namespace TiefBerg.Simulation.Time;

// Kalender einer Spezies (Abschnitt 3): gleicher Aufbau wie WorldCalendar, aber eigene Epoche.
// EpochSeconds = Weltzeit, zu der Jahr 1 dieses Kalenders beginnt; alles davor liegt "vor Zählung" (BeforeEra).
// Reine Umrechnung ohne Zustand, daher kein Logger: ungültige Eingaben werfen, der Aufrufer loggt.
public sealed class Calendar
{
    public Calendar(string id, ulong epochSeconds)
    {
        Id = id;
        EpochSeconds = epochSeconds;
    }

    // TODO: Kalender aus den Speziesdaten (YAML) laden, Monatsnamen über Übersetzungsschlüssel (Abschnitt 5, 12; T-023)
    public string Id { get; }

    public ulong EpochSeconds { get; }

    public CalendarDate ToDate(ulong worldSeconds)
    {
        if (worldSeconds >= EpochSeconds)
        {
            return WorldCalendar.ToDate(worldSeconds - EpochSeconds);
        }

        // Vor Jahr 1: Jahr 1 v. Z. umfasst die letzten SecondsPerYear Sekunden vor der Epoche
        ulong secondsBefore = EpochSeconds - worldSeconds;
        ulong yearsBefore = (secondsBefore - 1) / WorldCalendar.SecondsPerYear + 1;
        ulong withinYear = yearsBefore * WorldCalendar.SecondsPerYear - secondsBefore;
        return WorldCalendar.ToDate(withinYear) with { Year = yearsBefore, BeforeEra = true };
    }

    public ulong ToWorldSeconds(CalendarDate date)
    {
        if (!date.BeforeEra)
        {
            // checked: Datum jenseits von ulong.MaxValue ist ein Aufruferfehler
            return checked(EpochSeconds + WorldCalendar.ToSeconds(date));
        }

        if (date.Year == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(date), date, "Jahr beginnt bei 1, auch vor Zählung.");
        }

        ulong withinYear = WorldCalendar.ToSeconds(date with { Year = 1, BeforeEra = false });
        ulong secondsBefore = checked(date.Year * WorldCalendar.SecondsPerYear - withinYear);
        if (secondsBefore > EpochSeconds)
        {
            throw new ArgumentOutOfRangeException(nameof(date), date,
                $"Datum liegt vor Weltzeit 0 im Kalender {Id}.");
        }

        return EpochSeconds - secondsBefore;
    }

    public string Format(ulong worldSeconds) => ToDate(worldSeconds).ToString();
}
