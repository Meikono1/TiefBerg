using Godot;
using TiefBerg.Core.Logging;

namespace TiefBerg.UI.Scenario;

// Button für einen Kartenmodus in der Kopfzeile; Auswahl exklusiv über die ButtonGroup
public partial class MapModeButton : Button
{
    private static readonly GameLogger Log = LogManager.GetLogger<MapModeButton>();

    [Signal] public delegate void ModeSelectedEventHandler(string modeId);

    public override void _Ready()
    {
        if (ButtonGroup == null)
        {
            Log.Warn($"Keine ButtonGroup an '{GetPath()}', Kartenmodi sind nicht exklusiv.");
        }

        // TODO: T-037 – Daten setzen (Id, Icon, Tooltip), bei Toggled ModeSelected senden
    }
}
