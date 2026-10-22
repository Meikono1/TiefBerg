using TiefBerg.Simulation.Time;

namespace TiefBerg.Simulation.Control.Commands;

// Lässt einen Abschnitt so schnell wie möglich laufen, danach Pause (Abschnitt 3).
// Abschnitte sind feste Dauern ab jetzt, nicht bis zum Tages-, Monats- oder Jahresanfang.
public sealed class RunForCommand : ISimulationCommand
{
    public static readonly RunForCommand OneDay = new(WorldCalendar.SecondsPerDay);
    public static readonly RunForCommand OneMonth = new(WorldCalendar.SecondsPerMonth);
    public static readonly RunForCommand OneYear = new(WorldCalendar.SecondsPerYear);

    public RunForCommand(ulong seconds)
    {
        Seconds = seconds;
    }

    public ulong Seconds { get; }

    public void Execute(SimulationContext context)
    {
        ulong now = context.World.Clock.Now;
        context.Time.SkipTo(now, checked(now + Seconds));
    }
}
