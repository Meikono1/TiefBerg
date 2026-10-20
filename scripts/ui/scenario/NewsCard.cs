using Godot;
using TiefBerg.Core.Logging;

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

    public override void _Ready()
    {
        if (!ExportCheck.AllAssigned(Log, this,
                [("NewsBadge", _newsBadge), ("NewsTitle", _newsTitle), ("NewsVersion", _newsVersion), ("NewsText", _newsText)]))
        {
            return;
        }

        // TODO: T-033 – Daten setzen (Szenario-Id, Art neu/geändert/geplant, Titel, Version, Text), Klick/Doppelklick
    }
}
