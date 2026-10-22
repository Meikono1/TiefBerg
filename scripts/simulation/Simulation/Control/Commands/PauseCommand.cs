namespace TiefBerg.Simulation.Control.Commands;

// Hält die Weltzeit an, auch mitten in einem Sprung
public sealed class PauseCommand : ISimulationCommand
{
    public void Execute(SimulationContext context) => context.Time.Pause(context.World.Clock.Now);
}
