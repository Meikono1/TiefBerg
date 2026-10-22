namespace TiefBerg.Simulation.Control;

// Worauf Befehle im Simulations-Thread zugreifen dürfen
public sealed record SimulationContext(World World, TimeControl Time);
