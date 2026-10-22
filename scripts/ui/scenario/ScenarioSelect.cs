using System.Collections.Generic;
using System.Linq;
using Godot;
using TiefBerg.Core;
using TiefBerg.Core.Logging;
using TiefBerg.Data.Scenarios;

namespace TiefBerg.UI.Scenario;

// Hauptszene der Szenarioauswahl (Entscheidungen: Design-Dokument Abschnitt 12).
// Punkt, Listeneintrag und News-Karte melden nur Ids; die Auswahl hält allein diese Klasse und verteilt sie.
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

    private readonly List<ScenarioDefinition> _scenarios = new();
    private readonly List<ScenarioMarker> _markers = new();
    private readonly List<ScenarioListEntry> _entries = new();
    private ScenarioDefinition _selected;

    public override void _Ready()
    {
        // MapModeButtonScene fehlt hier, weil die Kartenmodi erst mit T-037 kommen
        var complete = ExportCheck.AllAssigned(Log, this,
            [("YearLabel", _yearLabel), ("ZoomToPointCheck", _zoomToPointCheck), ("MapModeBar", _mapModeBar),
            ("ScenarioList", _scenarioList), ("PreviewImage", _previewImage), ("ScenarioTitle", _scenarioTitle),
            ("ScenarioTags", _scenarioTags), ("ScenarioDescription", _scenarioDescription), ("LockedHint", _lockedHint),
            ("MapView", _mapView), ("MarkerLayer", _markerLayer), ("MapModeLabel", _mapModeLabel),
            ("NewsList", _newsList), ("BackButton", _backButton), ("ContinueButton", _continueButton),
            ("MarkerScene", _markerScene), ("ListEntryScene", _listEntryScene), ("NewsCardScene", _newsCardScene),
            ("TagChipScene", _tagChipScene), ("PlaceholderPreview", _placeholderPreview)]);
        if (!complete)
        {
            return;
        }

        _backButton.Pressed += OnBackPressed;
        _continueButton.Pressed += OnContinuePressed;
        _mapView.ViewChanged += OnMapViewChanged;

        _scenarios.AddRange(ScenarioFiles.LoadAll());
        if (_scenarios.Count == 0)
        {
            Log.Error("Keine gültigen Szenarien gefunden, Weiter bleibt gesperrt.");
            _continueButton.Disabled = true;
            return;
        }

        BuildList();
        BuildMarkers();
        BuildNews();
        // TODO: T-037 – Kartenmodi aus der Konfiguration in MapModeBar aufbauen, MapModeLabel setzen

        // Vorauswahl: erstes startbares Szenario, ohne Kamerafahrt beim Öffnen
        // TODO: letzte Auswahl merken (Abschnitt 12, Vorschlag)
        Select((_scenarios.FirstOrDefault(s => !s.Locked) ?? _scenarios[0]).Id, false);
        Log.Info($"Szenarioauswahl mit {_scenarios.Count} Szenarien aufgebaut.");
    }

    public override void _Input(InputEvent @event)
    {
        if (_selected == null)
        {
            return;
        }

        // In _Input statt _UnhandledInput: Fokussierte Buttons würden Pfeile und Enter sonst selbst verbrauchen
        if (@event.IsActionPressed("ui_up", true))
        {
            SelectNeighbor(-1);
        }
        else if (@event.IsActionPressed("ui_down", true))
        {
            SelectNeighbor(1);
        }
        else if (@event.IsActionPressed("ui_accept"))
        {
            OnContinuePressed();
        }
        else if (@event.IsActionPressed("ui_cancel"))
        {
            OnBackPressed();
        }
        else
        {
            return;
        }

        GetViewport().SetInputAsHandled();
    }

    private void BuildList()
    {
        foreach (ScenarioDefinition scenario in _scenarios)
        {
            var entry = Instantiate<ScenarioListEntry>(_listEntryScene, _scenarioList);
            if (entry == null)
            {
                return;
            }

            entry.Setup(scenario);
            entry.Selected += OnScenarioSelected;
            entry.Activated += OnScenarioActivated;
            _entries.Add(entry);
        }
    }

    private void BuildMarkers()
    {
        foreach (ScenarioDefinition scenario in _scenarios)
        {
            var marker = Instantiate<ScenarioMarker>(_markerScene, _markerLayer);
            if (marker == null)
            {
                return;
            }

            marker.Setup(scenario);
            marker.Selected += OnScenarioSelected;
            marker.Activated += OnScenarioActivated;
            _markers.Add(marker);
        }

        UpdateMarkerPositions();
    }

    // Neueste Einträge über alle Szenarien, bei gleichem Datum in Listenreihenfolge
    private void BuildNews()
    {
        var latest = _scenarios
            .SelectMany(scenario => scenario.News.Select(news => (scenario, news)))
            .OrderByDescending(item => item.news.Date)
            .Take(_newsCount);

        foreach (var (scenario, news) in latest)
        {
            var card = Instantiate<NewsCard>(_newsCardScene, _newsList);
            if (card == null)
            {
                return;
            }

            card.Setup(scenario, news);
            card.Selected += OnScenarioSelected;
            card.Activated += OnScenarioActivated;
        }
    }

    // Die Modul-Szenen baut der Entwickler; passt der Wurzelknoten nicht zum Script, lieber melden als halb anzeigen
    private T Instantiate<T>(PackedScene scene, Node parent) where T : Node
    {
        Node instance = scene.Instantiate();
        if (instance is T typed)
        {
            parent.AddChild(typed);
            return typed;
        }

        Log.Error($"'{scene.ResourcePath}' hat als Wurzel {instance.GetType().Name} statt {typeof(T).Name}, Modul wird nicht angezeigt.");
        instance.Free();
        return null;
    }

    private void Select(string scenarioId, bool focusMap)
    {
        ScenarioDefinition scenario = _scenarios.FirstOrDefault(s => s.Id == scenarioId);
        if (scenario == null)
        {
            Log.Error($"Auswahl eines unbekannten Szenarios: '{scenarioId}'");
            return;
        }

        _selected = scenario;
        foreach (ScenarioListEntry entry in _entries)
        {
            entry.SetSelected(entry.ScenarioId == scenarioId);
        }

        foreach (ScenarioMarker marker in _markers)
        {
            marker.SetSelected(marker.ScenarioId == scenarioId);
        }

        ShowDetails(scenario);
        if (focusMap && _zoomToPointCheck.ButtonPressed)
        {
            _mapView.FocusOn(ToVector(scenario.MapPosition));
        }

        Log.Debug($"Szenario ausgewählt: {scenario.Id}");
    }

    private void SelectNeighbor(int direction)
    {
        int index = _scenarios.IndexOf(_selected);
        int next = Mathf.Clamp(index + direction, 0, _scenarios.Count - 1);
        if (next != index)
        {
            Select(_scenarios[next].Id, true);
        }
    }

    private void ShowDetails(ScenarioDefinition scenario)
    {
        // TODO: Datum im Spezieskalender statt Anzeigejahr (T-023, T-025)
        _yearLabel.Text = scenario.StartYear.ToString();
        _previewImage.Texture = ScenarioTextures.Preview(scenario.PreviewImage, _placeholderPreview);
        _scenarioTitle.Text = scenario.TitleKey;
        _scenarioDescription.Text = Tr(scenario.DescriptionKey);
        _lockedHint.Visible = scenario.Locked;
        _lockedHint.Text = scenario.UnlockHintKey ?? "";
        _continueButton.Disabled = scenario.Locked;
        ShowTags(scenario.Tags);
    }

    private void ShowTags(ScenarioTags tags)
    {
        foreach (Node chip in _scenarioTags.GetChildren())
        {
            chip.QueueFree();
        }

        IEnumerable<string> keys = tags.SpeciesKeys.Append(tags.RegionKey).Append(tags.DifficultyKey);
        foreach (string key in keys.Where(k => k != null))
        {
            Instantiate<TagChip>(_tagChipScene, _scenarioTags)?.Setup(key);
        }
    }

    private void UpdateMarkerPositions()
    {
        foreach (ScenarioMarker marker in _markers)
        {
            // Der Mittelpunkt des Markers liegt auf der Kartenposition
            marker.Position = _mapView.SourceToView(ToVector(marker.MapPosition)) - marker.Size / 2f;
        }
    }

    private static Vector2 ToVector(MapPoint point) => new(point.X, point.Y);

    private void OnScenarioSelected(string scenarioId) => Select(scenarioId, true);

    private void OnScenarioActivated(string scenarioId)
    {
        Select(scenarioId, false);
        OnContinuePressed();
    }

    private void OnMapViewChanged() => UpdateMarkerPositions();

    private void OnContinuePressed()
    {
        if (_selected == null)
        {
            return;
        }

        if (_selected.Locked)
        {
            Log.Info($"Start von gesperrtem Szenario '{_selected.Id}' abgelehnt.");
            _lockedHint.Visible = true;
            return;
        }

        if (GameManager.Instance == null)
        {
            Log.Error("GameManager-Autoload fehlt, Spielstart nicht möglich.");
            return;
        }

        // TODO: Zwischenschritt (Charakter, Optionen) vor dem Start ist offen (Abschnitt 12)
        GameManager.Instance.StartNewGame(_selected.Id);
    }

    private void OnBackPressed()
    {
        if (GameManager.Instance == null)
        {
            Log.Error("GameManager-Autoload fehlt, Szenenwechsel nicht möglich.");
            return;
        }

        GameManager.Instance.ChangeScene(ScenePaths.MainMenu);
    }
}
