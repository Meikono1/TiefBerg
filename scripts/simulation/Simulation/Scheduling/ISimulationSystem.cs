namespace TiefBerg.Simulation.Scheduling;

// Ein Simulationssystem mit eigenem Takt (Abschnitt 3), z. B. Bedürfnisse stündlich, Wirtschaft täglich.
// Der Scheduler ruft Run nur auf, wenn das System fällig ist; Systeme pollen nicht selbst.
public interface ISimulationSystem
{
    // Feste, eindeutige Kennung (z. B. "needs"). Spielstände ordnen Fälligkeiten darüber zu,
    // daher nie ändern; Klassen dürfen umbenannt werden, die Id nicht.
    string Id { get; }

    // Abstand zwischen zwei Läufen in Sekunden Weltzeit, > 0
    ulong IntervalSeconds { get; }

    // dueSeconds = Fälligkeitszeitpunkt dieses Laufs. Kann vor der aktuellen Weltzeit liegen, wenn nach einem Sprung nachgeholt wird.
    void Run(ulong dueSeconds);
}
