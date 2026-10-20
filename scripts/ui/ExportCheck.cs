using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using TiefBerg.Core.Logging;

namespace TiefBerg.UI;

// Prüft im Inspector gesetzte [Export]-Referenzen gesammelt, damit jede UI-Klasse fehlende Zuweisungen gleich meldet
public static class ExportCheck
{
    public static bool AllAssigned(GameLogger log, Node owner, (string Name, GodotObject Value)[] exports,
        [CallerMemberName] string member = "")
    {
        var missing = new List<string>();
        foreach (var (name, value) in exports)
        {
            if (value == null)
            {
                missing.Add(name);
            }
        }

        if (missing.Count == 0)
        {
            return true;
        }

        log.Error($"Exports nicht zugewiesen an '{owner.GetPath()}': {string.Join(", ", missing)}", member: member);
        return false;
    }

    // Ersatz für einen nicht zugewiesenen Export über den Pfad aus dem Bauplan; die Lücke wird trotzdem gemeldet
    public static T OrPath<T>(GameLogger log, Node owner, T assigned, string path,
        [CallerMemberName] string member = "") where T : class
    {
        if (assigned != null)
        {
            return assigned;
        }

        var node = owner.GetNodeOrNull<T>(path);
        if (node != null)
        {
            log.Warn($"Export an '{owner.GetPath()}' nicht zugewiesen, nutze Pfad '{path}'.", member: member);
        }

        return node;
    }
}
