using Godot;
using TiefBerg.Core.Logging;
using TiefBerg.Data.Scenarios;

namespace TiefBerg.UI.Scenario;

// Punkt eines Szenarios auf der Weltkarte. Zustand über die Theme-Variante des Rings (MarkerRingNormal/-Hover/
// -Selected/-Locked, je eine Variante von Panel), weil sich die schwarzen Icons nicht einfärben lassen (Abschnitt 12, Vorschlag).
public partial class ScenarioMarker : Control
{
    private const string RingNormal = "MarkerRingNormal";
    private const string RingHover = "MarkerRingHover";
    private const string RingSelected = "MarkerRingSelected";
    private const string RingLocked = "MarkerRingLocked";

    private static readonly GameLogger Log = LogManager.GetLogger<ScenarioMarker>();

    [Signal] public delegate void SelectedEventHandler(string scenarioId);
    // Doppelklick, wirkt wie Weiter
    [Signal] public delegate void ActivatedEventHandler(string scenarioId);

    [Export] private Panel _ring;
    [Export] private TextureRect _icon;
    [Export] private TextureRect _lockIcon;
    [Export] private Label _nameLabel;

    private bool _ready;
    private bool _hovered;
    private bool _selected;
    private bool _locked;

    public string ScenarioId { get; private set; }

    public MapPoint MapPosition { get; private set; }

    public override void _Ready()
    {
        _ring = ExportCheck.OrPath(Log, this, _ring, "Ring");
        _icon = ExportCheck.OrPath(Log, this, _icon, "Icon");
        _lockIcon = ExportCheck.OrPath(Log, this, _lockIcon, "LockIcon");
        _nameLabel = ExportCheck.OrPath(Log, this, _nameLabel, "NameLabel");
        if (!ExportCheck.AllAssigned(Log, this,
                [("Ring", _ring), ("Icon", _icon), ("LockIcon", _lockIcon), ("NameLabel", _nameLabel)]))
        {
            return;
        }

        MouseEntered += OnMouseEntered;
        MouseExited += OnMouseExited;
        _ready = true;
    }

    // Erst nach AddChild aufrufen, damit die Exports aufgelöst sind
    public void Setup(ScenarioDefinition scenario)
    {
        ScenarioId = scenario.Id;
        MapPosition = scenario.MapPosition;
        _locked = scenario.Locked;
        if (!_ready)
        {
            Log.Warn($"Marker für '{scenario.Id}' ist unvollständig und zeigt keine Daten.");
            return;
        }

        _icon.Texture = ScenarioTextures.Icon(scenario.Icon);
        _nameLabel.Text = scenario.TitleKey;
        _lockIcon.Visible = scenario.Locked;
        TooltipText = scenario.Tags.DifficultyKey == null
            ? Tr(scenario.TitleKey)
            : $"{Tr(scenario.TitleKey)} ({Tr(scenario.Tags.DifficultyKey)})";
        UpdateRing();
    }

    public void SetSelected(bool selected)
    {
        _selected = selected;
        UpdateRing();
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (ScenarioId == null
            || @event is not InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true } click)
        {
            return;
        }

        // Ohne AcceptEvent würde MapView den Klick als Beginn eines Ziehens werten
        AcceptEvent();
        EmitSignal(click.DoubleClick ? SignalName.Activated : SignalName.Selected, ScenarioId);
    }

    private void UpdateRing()
    {
        if (!_ready)
        {
            return;
        }

        // Auswahl geht vor Hover, damit der gewählte Punkt beim Überfahren erkennbar bleibt
        _ring.ThemeTypeVariation = _selected ? RingSelected
            : _hovered ? RingHover
            : _locked ? RingLocked
            : RingNormal;
    }

    private void OnMouseEntered()
    {
        _hovered = true;
        UpdateRing();
    }

    private void OnMouseExited()
    {
        _hovered = false;
        UpdateRing();
    }
}
