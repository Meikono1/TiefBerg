using Godot;
using TiefBerg.Core.Logging;
using TiefBerg.Data.Scenarios;

namespace TiefBerg.UI.Scenario;

// Karte „neueste Szenarien“ in der Fußzeile mit Änderungen und Features
public partial class NewsCard : Button
{
    private static readonly GameLogger Log = LogManager.GetLogger<NewsCard>();

    [Signal] public delegate void SelectedEventHandler(string scenarioId);
    // Doppelklick, wirkt wie Weiter
    [Signal] public delegate void ActivatedEventHandler(string scenarioId);

    [Export] private Label _newsBadge;
    [Export] private Label _newsTitle;
    [Export] private Label _newsVersion;
    [Export] private Label _newsText;

    private bool _ready;

    public string ScenarioId { get; private set; }

    public override void _Ready()
    {
        if (!ExportCheck.AllAssigned(Log, this,
                [("NewsBadge", _newsBadge), ("NewsTitle", _newsTitle), ("NewsVersion", _newsVersion), ("NewsText", _newsText)]))
        {
            return;
        }

        Pressed += OnPressed;
        _ready = true;
    }

    // Erst nach AddChild aufrufen, damit die Exports aufgelöst sind
    public void Setup(ScenarioDefinition scenario, ScenarioNews news)
    {
        ScenarioId = scenario.Id;
        if (!_ready)
        {
            Log.Warn($"News-Karte für '{scenario.Id}' ist unvollständig und zeigt keine Daten.");
            return;
        }

        _newsBadge.Text = BadgeKey(news.Kind);
        _newsTitle.Text = scenario.TitleKey;
        _newsVersion.Text = news.Version;
        _newsText.Text = news.TextKey;
        TooltipText = $"{news.Date:yyyy-MM-dd}";
    }

    public override void _GuiInput(InputEvent @event)
    {
        // Nur beobachten, nicht annehmen: Den Klick selbst verarbeitet der Button
        if (ScenarioId != null
            && @event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: true, DoubleClick: true })
        {
            EmitSignal(SignalName.Activated, ScenarioId);
        }
    }

    private static string BadgeKey(ScenarioNewsKind kind) => kind switch
    {
        ScenarioNewsKind.New => "UI_NEWS_NEW",
        ScenarioNewsKind.Updated => "UI_NEWS_UPDATED",
        _ => "UI_NEWS_PLANNED"
    };

    private void OnPressed()
    {
        if (ScenarioId != null)
        {
            EmitSignal(SignalName.Selected, ScenarioId);
        }
    }
}
