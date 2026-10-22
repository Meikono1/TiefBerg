namespace TiefBerg.Simulation.Control.Commands;

// Lässt die Weltzeit in Echtzeit laufen (Abschnitt 3); bricht einen laufenden Sprung ab
public sealed class ResumeCommand : ISimulationCommand
{
    public void Execute(SimulationContext context) => context.Time.Resume(context.World.Clock.Now);
}
