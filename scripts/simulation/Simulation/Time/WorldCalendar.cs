using System;

namespace TiefBerg.Simulation.Time;

// Aufbau aller Kalender (Abschnitt 3) und Umrechnung Sekunden seit Kalenderbeginn <-> Datum.
// Direkt aufgerufen ist der Kalenderbeginn Weltzeit 0 (technische Anzeige, z. B. Logs); Spezieskalender rechnen ab ihrer Epoche (Calendar).
// Reine Rechenfunktionen ohne Zustand, daher kein Logger: ungültige Eingaben werfen, der Aufrufer loggt.
public static class WorldCalendar
{
    public const int SecondsPerMinute = 60;
    public const int MinutesPerHour = 60;
    public const int HoursPerDay = 20;
    public const int DaysPerMonth = 23;
    public const int MonthsPerYear = 12;

    public const ulong SecondsPerHour = (ulong)SecondsPerMinute * MinutesPerHour;
    public const ulong SecondsPerDay = SecondsPerHour * HoursPerDay;
    public const ulong SecondsPerMonth = SecondsPerDay * DaysPerMonth;
    public const ulong SecondsPerYear = SecondsPerMonth * MonthsPerYear;

    public static CalendarDate ToDate(ulong seconds)
    {
        ulong year = seconds / SecondsPerYear;
        ulong rest = seconds % SecondsPerYear;
        int month = (int)(rest / SecondsPerMonth);
        rest %= SecondsPerMonth;
        int day = (int)(rest / SecondsPerDay);
        rest %= SecondsPerDay;
        int hour = (int)(rest / SecondsPerHour);
        rest %= SecondsPerHour;
        int minute = (int)(rest / SecondsPerMinute);
        int second = (int)(rest % SecondsPerMinute);

        return new CalendarDate(year + 1, month + 1, day + 1, hour, minute, second);
    }

    public static ulong ToSeconds(CalendarDate date)
    {
        CheckRange(date.Month, 1, MonthsPerYear, nameof(date.Month));
        CheckRange(date.Day, 1, DaysPerMonth, nameof(date.Day));
        CheckRange(date.Hour, 0, HoursPerDay - 1, nameof(date.Hour));
        CheckRange(date.Minute, 0, MinutesPerHour - 1, nameof(date.Minute));
        CheckRange(date.Second, 0, SecondsPerMinute - 1, nameof(date.Second));
        if (date.BeforeEra)
        {
            // Vor Weltzeit 0 gibt es nichts; "vor Zählung" kennen nur Spezieskalender mit Epoche
            throw new ArgumentOutOfRangeException(nameof(date), date, "Datum vor Kalenderbeginn.");
        }

        if (date.Year == 0)
        {
            throw new ArgumentOutOfRangeException(nameof(date), date.Year, "Jahr beginnt bei 1.");
        }

        ulong withinYear = (ulong)(date.Month - 1) * SecondsPerMonth
                           + (ulong)(date.Day - 1) * SecondsPerDay
                           + (ulong)date.Hour * SecondsPerHour
                           + (ulong)date.Minute * SecondsPerMinute
                           + (ulong)date.Second;

        // checked: Jahre jenseits von ulong.MaxValue Sekunden sind ein Aufruferfehler, kein stiller Überlauf
        return checked((date.Year - 1) * SecondsPerYear + withinYear);
    }

    public static string Format(ulong seconds) => ToDate(seconds).ToString();

    private static void CheckRange(int value, int min, int max, string name)
    {
        if (value < min || value > max)
        {
            throw new ArgumentOutOfRangeException(name, value, $"Erlaubt: {min}..{max}.");
        }
    }
}
