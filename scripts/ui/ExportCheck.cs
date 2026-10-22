using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Godot;
using TiefBerg.Core.Logging;

namespace TiefBerg.UI;

// Prüft im Inspector gesetzte [Export]-Referenzen gesammelt, damit jede UI-Klasse fehlende Zuweisungen gleich meldet.
// Es gibt bewusst keinen Ersatz über Node-Pfade: Fehlt eine Zuweisung, bricht die Klasse ab (Fehler in der Szene beheben).
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
}
