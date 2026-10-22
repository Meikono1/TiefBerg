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

    // Ersatz für einen nicht zugewiesenen Export über den erwarteten Node-Pfad in der Szene; die Lücke wird trotzdem gemeldet
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

    // Wie OrPath, aber für Ressourcen (z. B. Modul-Szenen) über ihren Standardpfad unter res://
    public static T OrResource<T>(GameLogger log, Node owner, T assigned, string resourcePath,
        [CallerMemberName] string member = "") where T : Resource
    {
        if (assigned != null)
        {
            return assigned;
        }

        if (!ResourceLoader.Exists(resourcePath))
        {
            return null;
        }

        var resource = GD.Load<T>(resourcePath);
        if (resource != null)
        {
            log.Warn($"Export an '{owner.GetPath()}' nicht zugewiesen, nutze '{resourcePath}'.", member: member);
        }

        return resource;
    }
}
