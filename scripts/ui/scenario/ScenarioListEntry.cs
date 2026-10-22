using Godot;
using TiefBerg.Core.Logging;
using TiefBerg.Data.Scenarios;

namespace TiefBerg.UI.Scenario;

// Eintrag der Szenarioliste links; Auswahl exklusiv über die ButtonGroup
public partial class ScenarioListEntry : Button
{
    private static readonly GameLogger Log = LogManager.GetLogger<ScenarioListEntry>();
    // Gesperrte Einträge bleiben anklickbar, wirken aber ausgegraut (Abschnitt 12)
    private static readonly Color LockedTint = new(1f, 1f, 1f, 0.45f);

    [Signal] public delegate void SelectedEventHandler(string scenarioId);
    // Doppelklick, wirkt wie Weiter
    [Signal] public delegate void ActivatedEventHandler(string scenarioId);

    [Export] private TextureRect _icon;
    [Export] private Label _nameLabel;
    [Export] private TextureRect _lockIcon;
    [Export] private HBoxContainer _entryBox;

    private bool _ready;

    public string ScenarioId { get; private set; }

    public override void _Ready()
    {
        if (!ExportCheck.AllAssigned(Log, this,
                [("Icon", _icon), ("NameLabel", _nameLabel), ("LockIcon", _lockIcon), ("EntryBox", _entryBox)]))
        {
            return;
        }

        if (ButtonGroup == null)
        {
            Log.Warn($"Keine ButtonGroup an '{GetPath()}', mehrere Einträge können gleichzeitig gedrückt wirken.");
        }

        ToggleMode = true;
        Pressed += OnPressed;
        _ready = true;
    }

    // Erst nach AddChild aufrufen, damit die Exports aufgelöst sind
    public void Setup(ScenarioDefinition scenario)
    {
        ScenarioId = scenario.Id;
        if (!_ready)
        {
            Log.Warn($"Listeneintrag für '{scenario.Id}' ist unvollständig und zeigt keine Daten.");
            return;
        }

        _icon.Texture = ScenarioTextures.Icon(scenario.Icon);
        _nameLabel.Text = scenario.TitleKey;
        _lockIcon.Visible = scenario.Locked;
        _entryBox.Modulate = scenario.Locked ? LockedTint : Colors.White;
    }

    // Auswahl von außen (Punkt, News, Tastatur) ohne erneutes Selected-Signal
    public void SetSelected(bool selected) => SetPressedNoSignal(selected);

    public override void _GuiInput(InputEvent @event)
    {
        // Nur beobachten, nicht annehmen: Den Klick selbst verarbeitet der Button
        if (ScenarioId != null
            && @event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true, DoubleClick: true })
        {
            EmitSignal(SignalName.Activated, ScenarioId);
        }
    }

    private void OnPressed()
    {
        if (ScenarioId != null)
        {
            EmitSignal(SignalName.Selected, ScenarioId);
        }
    }
}
