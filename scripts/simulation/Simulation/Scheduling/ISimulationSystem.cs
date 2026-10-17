namespace TiefBerg.Simulation.Scheduling;

// Ein Simulationssystem mit eigenem Takt (Abschnitt 3), z. B. Bedürfnisse stündlich, Wirtschaft täglich.
// Der Scheduler ruft Run nur auf, wenn das System fällig ist; Systeme pollen nicht selbst.
public interface ISimulationSystem
{
    // Abstand zwischen zwei Läufen in Sekunden Weltzeit, > 0
    ulong IntervalSeconds { get; }

    // dueSeconds = Fälligkeitszeitpunkt dieses Laufs. Kann vor der aktuellen Weltzeit liegen, wenn nach einem Sprung nachgeholt wird.
    void Run(ulong dueSeconds);
}
