using Godot;
using TiefBerg.Core.Logging;

namespace TiefBerg.UI.Scenario;

// Eintrag der Szenarioliste links; Auswahl exklusiv über die ButtonGroup
public partial class ScenarioListEntry : Button
{
    private static readonly GameLogger Log = LogManager.GetLogger<ScenarioListEntry>();

    [Signal] public delegate void SelectedEventHandler(string scenarioId);
    // Doppelklick, wirkt wie Weiter
    [Signal] public delegate void ActivatedEventHandler(string scenarioId);

    [Export] private TextureRect _icon;
    [Export] private Label _nameLabel;
    [Export] private TextureRect _lockIcon;
    [Export] private HBoxContainer _entryBox;

    public override void _Ready()
    {
        if (!ExportCheck.AllAssigned(Log, this,
                [("Icon", _icon), ("NameLabel", _nameLabel), ("LockIcon", _lockIcon), ("EntryBox", _entryBox)]))
        {
            return;
        }

        // TODO: T-033 – Daten setzen (Id, Icon, Name, gesperrt → Modulate auf EntryBox), Toggled/Doppelklick
    }
}
