using Godot;
using TiefBerg.Core.Logging;

namespace TiefBerg.UI.Scenario;

// Punkt eines Szenarios auf der Weltkarte; Zustand über die Ring-StyleBoxes im Theme
public partial class ScenarioMarker : Control
{
    private static readonly GameLogger Log = LogManager.GetLogger<ScenarioMarker>();

    [Signal] public delegate void SelectedEventHandler(string scenarioId);
    // Doppelklick, wirkt wie Weiter
    [Signal] public delegate void ActivatedEventHandler(string scenarioId);

    [Export] private Panel _ring;
    [Export] private TextureRect _icon;
    [Export] private TextureRect _lockIcon;
    [Export] private Label _nameLabel;

    public override void _Ready()
    {
        if (!ExportCheck.AllAssigned(Log, this,
                [("Ring", _ring), ("Icon", _icon), ("LockIcon", _lockIcon), ("NameLabel", _nameLabel)]))
        {
            return;
        }

        // TODO: T-033 – Daten setzen (Id, Icon, Name, Tooltip, gesperrt), Hover/Auswahl über Ring, Klick/Doppelklick
    }
}
