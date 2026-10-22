namespace TiefBerg.Simulation.Control;

// Wie die Weltzeit gerade läuft (Abschnitt 3)
public enum TimeMode
{
    // Zeit steht; so startet jedes Spiel
    Paused,

    // 1 Weltsekunde pro Echtzeitsekunde
    RealTime,

    // So schnell wie möglich bis zu einem Ziel (Abschnitt laufen lassen, nächstes Ereignis), danach Pause
    Skipping,
}
