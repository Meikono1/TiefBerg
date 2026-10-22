namespace TiefBerg.Simulation.Control.Commands;

// Befehl der Darstellung an die Simulation (Command-Pattern, Abschnitt 2). Wird im Simulations-Thread zwischen zwei
// Rechenscheiben ausgeführt, nie mitten in einem Tick.
public interface ISimulationCommand
{
    void Execute(SimulationContext context);
}
