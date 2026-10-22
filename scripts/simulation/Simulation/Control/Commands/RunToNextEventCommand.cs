namespace TiefBerg.Simulation.Control.Commands;

// Springt bis zum nächsten Ereignis, danach Pause (Abschnitt 3). Was als Ereignis zählt, legt TimeControl fest.
public sealed class RunToNextEventCommand : ISimulationCommand
{
    public void Execute(SimulationContext context) =>
        context.Time.SkipToNextEvent(context.World.Clock.Now, context.World.Scheduler.NextDue);
}
