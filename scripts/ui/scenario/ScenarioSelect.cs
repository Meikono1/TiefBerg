using Godot;
using TiefBerg.Core.Logging;

namespace TiefBerg.UI.Scenario;

// Hauptszene der Szenarioauswahl, Layout nach EU4 (Bauplan: defines/UI Szenarioauswahl.md)
public partial class ScenarioSelect : Control
{
    private static readonly GameLogger Log = LogManager.GetLogger<ScenarioSelect>();

    [ExportGroup("Kopfzeile")]
    [Export] private Label _yearLabel;
    [Export] private CheckButton _zoomToPointCheck;
    [Export] private HBoxContainer _mapModeBar;

    [ExportGroup("Linke Spalte")]
    [Export] private VBoxContainer _scenarioList;
    [Export] private TextureRect _previewImage;
    [Export] private Label _scenarioTitle;
    [Export] private HFlowContainer _scenarioTags;
    [Export] private RichTextLabel _scenarioDescription;
    [Export] private Label _lockedHint;

    [ExportGroup("Karte")]
    [Export] private MapPanZoom _mapView;
    [Export] private Control _markerLayer;
    [Export] private Label _mapModeLabel;

    [ExportGroup("Fußzeile")]
    [Export] private HBoxContainer _newsList;
    [Export] private Button _backButton;
    [Export] private Button _continueButton;

    [ExportGroup("Module")]
    [Export] private PackedScene _markerScene;
    [Export] private PackedScene _listEntryScene;
    [Export] private PackedScene _newsCardScene;
    [Export] private PackedScene _mapModeButtonScene;
    [Export] private PackedScene _tagChipScene;

    [ExportGroup("Einstellungen")]
    [Export] private Texture2D _placeholderPreview;
    [Export] private int _newsCount = 3;

    public override void _Ready()
    {
        // TagChipScene ist optional und fehlt deshalb hier
        var complete = ExportCheck.AllAssigned(Log, this,
            [("YearLabel", _yearLabel), ("ZoomToPointCheck", _zoomToPointCheck), ("MapModeBar", _mapModeBar),
            ("ScenarioList", _scenarioList), ("PreviewImage", _previewImage), ("ScenarioTitle", _scenarioTitle),
            ("ScenarioTags", _scenarioTags), ("ScenarioDescription", _scenarioDescription), ("LockedHint", _lockedHint),
            ("MapView", _mapView), ("MarkerLayer", _markerLayer), ("MapModeLabel", _mapModeLabel),
            ("NewsList", _newsList), ("BackButton", _backButton), ("ContinueButton", _continueButton),
            ("MarkerScene", _markerScene), ("ListEntryScene", _listEntryScene), ("NewsCardScene", _newsCardScene),
            ("MapModeButtonScene", _mapModeButtonScene), ("PlaceholderPreview", _placeholderPreview)]);
        if (!complete)
        {
            return;
        }

        Log.Debug("Szenarioauswahl initialisiert.");
        // TODO: T-033 – Szenarien aus der Konfiguration laden (T-031), Liste/Punkte/News/Kartenmodi aufbauen,
        //       Auswahl synchronisieren, Weiter/Zurück, Tastatur (Pfeil hoch/runter, Enter, Esc)
    }
}
