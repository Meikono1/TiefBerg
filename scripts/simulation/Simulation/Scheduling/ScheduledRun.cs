namespace TiefBerg.Simulation.Scheduling;

// Nächste Fälligkeit eines Systems, zugeordnet über ISimulationSystem.Id
public readonly record struct ScheduledRun(string SystemId, ulong DueSeconds);
