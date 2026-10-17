using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Godot;
using Microsoft.Extensions.Logging;
using TiefBerg.Core.Logging;
using TiefBerg.Scenario;
using TiefBerg.Scenario.Persistence;
using TiefBerg.Simulation;
using TiefBerg.Simulation.Persistence;

namespace TiefBerg.Core;

// Spielstände auf der Platte (Abschnitt 12). Ein Spielstand ist ein Ordner unter user://saves mit zwei Dateien:
// Welt- und Szenariozustand getrennt, damit das Szenario gewechselt werden kann. Das Format liegt im Kern (Serializer).
//
// Schreiben erst in "<slot>.tmp", dann umbenennen: Ein Absturz beim Speichern zerstört nie den letzten guten Stand.
// Autosaves bekommen je einen eigenen Ordner mit Zeitstempel; nur die ältesten über MaxAutosaves werden entfernt.
public static class SaveService
{
    public const int MaxAutosaves = 5;

    private const string SaveDirectory = "user://saves";
    private const string WorldFileName = "world.sav";
    private const string ScenarioFileName = "scenario.sav";
    private const string AutosavePrefix = "autosave_";
    private const string TempSuffix = ".tmp";
    private const string OldSuffix = ".old";

    private static readonly GameLogger Log = LogManager.GetLogger(nameof(SaveService));
    private static readonly Regex ValidSlotName = new("^[A-Za-z0-9_-]{1,64}$", RegexOptions.Compiled);

    public static bool HasSave() => FindLatestSlot() != null;

    public static bool Save(string slotName, WorldState world, ScenarioState scenario)
    {
        if (!ValidSlotName.IsMatch(slotName))
        {
            Log.Error($"Ungültiger Spielstandname: '{slotName}'");
            return false;
        }

        string slotPath = Path.Combine(RootPath(), slotName);
        string tempPath = slotPath + TempSuffix;
        string oldPath = slotPath + OldSuffix;

        try
        {
            Directory.CreateDirectory(RootPath());
            DeleteIfExists(tempPath);
            Directory.CreateDirectory(tempPath);

            using (FileStream stream = CreateFile(Path.Combine(tempPath, WorldFileName)))
            {
                new WorldStateSerializer(LogManager.SimulationLoggerFactory.CreateLogger<WorldStateSerializer>())
                    .Write(stream, world);
                stream.Flush(true);
            }

            using (FileStream stream = CreateFile(Path.Combine(tempPath, ScenarioFileName)))
            {
                new ScenarioStateSerializer(LogManager.SimulationLoggerFactory.CreateLogger<ScenarioStateSerializer>())
                    .Write(stream, scenario);
                stream.Flush(true);
            }

            // Alten Stand erst beiseitelegen, dann ersetzen; erst danach wird er gelöscht
            DeleteIfExists(oldPath);
            if (Directory.Exists(slotPath))
            {
                Directory.Move(slotPath, oldPath);
            }

            Directory.Move(tempPath, slotPath);
            DeleteIfExists(oldPath);
        }
        catch (Exception e)
        {
            Log.Error($"Speichern von '{slotName}' fehlgeschlagen, letzter Stand bleibt erhalten.", e);
            TryDelete(tempPath);
            return false;
        }

        Log.Info($"Gespeichert: {slotPath}");
        return true;
    }

    public static bool Autosave(WorldState world, ScenarioState scenario)
    {
        string slotName = $"{AutosavePrefix}{DateTime.Now:yyyy-MM-dd_HH-mm-ss-fff}";
        if (!Save(slotName, world, scenario))
        {
            return false;
        }

        PruneAutosaves();
        return true;
    }

    // Lädt den zuletzt geschriebenen Spielstand, egal ob manuell oder Autosave
    public static bool TryLoadLatest(out WorldState world, out ScenarioState scenario)
    {
        string slotName = FindLatestSlot();
        if (slotName == null)
        {
            Log.Warn("Kein Spielstand gefunden.");
            world = null;
            scenario = null;
            return false;
        }

        return TryLoad(slotName, out world, out scenario);
    }

    public static bool TryLoad(string slotName, out WorldState world, out ScenarioState scenario)
    {
        world = null;
        scenario = null;
        string slotPath = Path.Combine(RootPath(), slotName);

        try
        {
            using (FileStream stream = File.OpenRead(Path.Combine(slotPath, WorldFileName)))
            {
                world = new WorldStateSerializer(
                    LogManager.SimulationLoggerFactory.CreateLogger<WorldStateSerializer>()).Read(stream);
            }

            using (FileStream stream = File.OpenRead(Path.Combine(slotPath, ScenarioFileName)))
            {
                scenario = new ScenarioStateSerializer(
                    LogManager.SimulationLoggerFactory.CreateLogger<ScenarioStateSerializer>()).Read(stream);
            }
        }
        catch (Exception e)
        {
            // Defekte Stände bleiben unverändert liegen, damit man sie untersuchen kann; kein stilles Ausweichen auf ältere
            Log.Error($"Laden von '{slotName}' fehlgeschlagen.", e);
            world = null;
            scenario = null;
            return false;
        }

        Log.Info($"Geladen: {slotPath}");
        return true;
    }

    private static string RootPath() => ProjectSettings.GlobalizePath(SaveDirectory);

    // Vollständige Spielstände (beide Dateien vorhanden), ohne halbfertige .tmp/.old-Ordner
    private static string[] ListSlots()
    {
        string root = RootPath();
        if (!Directory.Exists(root))
        {
            return Array.Empty<string>();
        }

        return Directory.GetDirectories(root)
            .Select(Path.GetFileName)
            .Where(name => ValidSlotName.IsMatch(name))
            .Where(name => File.Exists(Path.Combine(root, name, WorldFileName))
                           && File.Exists(Path.Combine(root, name, ScenarioFileName)))
            .ToArray();
    }

    private static string FindLatestSlot()
    {
        string root = RootPath();
        return ListSlots()
            .OrderByDescending(name => File.GetLastWriteTimeUtc(Path.Combine(root, name, WorldFileName)))
            .FirstOrDefault();
    }

    private static void PruneAutosaves()
    {
        // Zeitstempel im Namen ist sortierbar, neueste zuerst
        string[] obsolete = ListSlots()
            .Where(name => name.StartsWith(AutosavePrefix, StringComparison.Ordinal))
            .OrderByDescending(name => name, StringComparer.Ordinal)
            .Skip(MaxAutosaves)
            .ToArray();

        foreach (string name in obsolete)
        {
            if (TryDelete(Path.Combine(RootPath(), name)))
            {
                Log.Info($"Alter Autosave entfernt: {name}");
            }
        }
    }

    private static FileStream CreateFile(string path) =>
        new(path, FileMode.CreateNew, System.IO.FileAccess.Write, FileShare.None);

    private static void DeleteIfExists(string directory)
    {
        if (Directory.Exists(directory))
        {
            Directory.Delete(directory, true);
        }
    }

    private static bool TryDelete(string directory)
    {
        try
        {
            DeleteIfExists(directory);
            return true;
        }
        catch (Exception e)
        {
            Log.Warn($"Ordner konnte nicht gelöscht werden: {directory}", e);
            return false;
        }
    }
}
