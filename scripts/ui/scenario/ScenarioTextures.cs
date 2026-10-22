using System.Collections.Generic;
using Godot;
using TiefBerg.Core.Logging;

namespace TiefBerg.UI.Scenario;

// Lädt Icons und Vorschaubilder der Szenarien. Die Konfiguration nennt Icons nur beim Namen (Abschnitt 12),
// der Pfad zu resources/MapIcons/ steht allein hier. Fehlende Dateien werden einmal als Fehler gemeldet, nicht pro Aufruf.
public static class ScenarioTextures
{
    private const string IconDirectory = "res://resources/MapIcons";

    private static readonly GameLogger Log = LogManager.GetLogger(nameof(ScenarioTextures));
    private static readonly Dictionary<string, Texture2D> Cache = new();

    public static Texture2D Icon(string iconName) => Load($"{IconDirectory}/{iconName}.png");

    // Szenarien ohne eigenes Bild zeigen den Platzhalter; ein angegebenes, aber fehlendes Bild ist ein Datenfehler
    public static Texture2D Preview(string path, Texture2D placeholder)
    {
        if (string.IsNullOrEmpty(path))
        {
            return placeholder;
        }

        return Load(path);
    }

    private static Texture2D Load(string path)
    {
        if (Cache.TryGetValue(path, out Texture2D cached))
        {
            return cached;
        }

        Texture2D texture = null;
        if (ResourceLoader.Exists(path))
        {
            texture = GD.Load<Texture2D>(path);
        }

        if (texture == null)
        {
            Log.Error($"Textur nicht gefunden oder keine Texture2D: {path}");
        }

        Cache[path] = texture;
        return texture;
    }
}
