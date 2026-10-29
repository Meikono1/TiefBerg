namespace TiefBerg.Simulation.Control;

// Unveränderlicher Stand für die Darstellung (Abschnitt 2): Godot liest nur diesen, nie den laufenden Weltzustand.
// Enthält nur, was Ansichten brauchen; weitere Daten kommen mit den Ansichten dazu (T-095).
public sealed record SimulationSnapshot(ulong WorldSeconds, TimeMode Mode, ulong? SkipTargetSeconds, bool Faulted);
