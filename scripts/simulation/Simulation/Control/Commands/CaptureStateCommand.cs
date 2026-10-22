using System.Threading.Tasks;

namespace TiefBerg.Simulation.Control.Commands;

// Liest den Weltzustand zum Speichern im Simulations-Thread, damit nie ein halb gerechneter Tick gespeichert wird.
// Wird über SimulationThread.CaptureState() benutzt.
internal sealed class CaptureStateCommand : ISimulationCommand
{
    private readonly TaskCompletionSource<WorldState> _result =
        new(TaskCreationOptions.RunContinuationsAsynchronously);

    public Task<WorldState> Result => _result.Task;

    public void Execute(SimulationContext context) => _result.TrySetResult(context.World.CaptureState());

    // Der Thread endet, bevor der Befehl dran war
    public void Cancel() => _result.TrySetCanceled();
}
