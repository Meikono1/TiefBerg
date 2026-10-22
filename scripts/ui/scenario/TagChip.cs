using Godot;
using TiefBerg.Core.Logging;

namespace TiefBerg.UI.Scenario;

// Chip für einen Tag des Szenarios (Spezies, Region, Schwierigkeit) im Detailbereich
public partial class TagChip : PanelContainer
{
    private static readonly GameLogger Log = LogManager.GetLogger<TagChip>();

    [Export] private Label _label;

    private bool _ready;

    public override void _Ready()
    {
        if (!ExportCheck.AllAssigned(Log, this, [("Label", _label)]))
        {
            return;
        }

        _ready = true;
    }

    // Erst nach AddChild aufrufen, damit die Exports geprüft sind; key ist ein Übersetzungsschlüssel
    public void Setup(string key)
    {
        if (!_ready)
        {
            Log.Warn($"Tag-Chip für '{key}' ist unvollständig und zeigt keinen Text.");
            return;
        }

        _label.Text = key;
    }
}
