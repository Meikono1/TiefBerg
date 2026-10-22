using System.Collections.Generic;
using System.Linq;
using Godot;
using Microsoft.Extensions.Logging;
using TiefBerg.Core.Logging;
using TiefBerg.Data.Scenarios;

namespace TiefBerg.Core;

// Liest die Szenariodateien aus res://data/scenarios und reicht den Text an den Kern weiter (Abschnitt 11, 12).
// Über Godots FileAccess statt System.IO, weil res:// im exportierten Spiel in der .pck liegt.
// TODO: Mod-Ordner zusätzlich einlesen, sobald es Modding gibt (Abschnitt 2)
public static class ScenarioFiles
{
    private const string ScenarioDirectory = "res://data/scenarios";
    private const string Extension = ".yaml";

    private static readonly GameLogger Log = LogManager.GetLogger(nameof(ScenarioFiles));

    public static IReadOnlyList<ScenarioDefinition> LoadAll()
    {
        var loader = new ScenarioDefinitionLoader(
            LogManager.SimulationLoggerFactory.CreateLogger<ScenarioDefinitionLoader>());
        return loader.LoadAll(ReadSources());
    }

    private static List<ScenarioSource> ReadSources()
    {
        var sources = new List<ScenarioSource>();
        if (!DirAccess.DirExistsAbsolute(ScenarioDirectory))
        {
            Log.Error($"Szenarioordner fehlt: {ScenarioDirectory}");
            return sources;
        }

        foreach (string fileName in DirAccess.GetFilesAt(ScenarioDirectory).Where(f => f.EndsWith(Extension)))
        {
            string path = $"{ScenarioDirectory}/{fileName}";
            string text = FileAccess.GetFileAsString(path);
            Error error = FileAccess.GetOpenError();
            if (error != Error.Ok)
            {
                Log.Warn($"Szenariodatei {path} nicht lesbar ({error}), übersprungen.");
                continue;
            }

            sources.Add(new ScenarioSource(path, text));
        }

        return sources;
    }
}
