using Godot;
using Microsoft.Extensions.Logging;
using TiefBerg.Core.Logging;
using TiefBerg.Data;
using TiefBerg.Data.Peoples;
using TiefBerg.Data.World;
using TiefBerg.Simulation.Geography;

namespace TiefBerg.Core;

// Liest die Weltdaten aus res://data (Raster, Tabellen, Völker) und berechnet daraus die Heimatgebiete (Abschnitt 4, 5).
// Über Godots FileAccess statt System.IO, weil res:// im exportierten Spiel in der .pck liegt.
// Fehlt eine Datei oder ist sie defekt, gibt es keine Welt: Ergebnis null, Fehler im Log.
// TODO: Mod-Ordner zusätzlich einlesen, sobald es Modding gibt (Abschnitt 2)
public static class WorldDataFiles
{
    private const string ZonesRaster = "res://data/world/klimazonen.bin";
    private const string ZonesTable = "res://data/world/klimazonen.yaml";
    private const string RiversRaster = "res://data/world/fluesse.bin";
    private const string RiversTable = "res://data/world/fluesse.yaml";
    private const string ProfilesFile = "res://data/peoples/lebensraum.yaml";
    private const string CulturesFile = "res://data/peoples/kulturen.yaml";

    private static readonly GameLogger Log = LogManager.GetLogger(nameof(WorldDataFiles));

    public static HomelandMap LoadHomelands()
    {
        ILoggerFactory loggers = LogManager.SimulationLoggerFactory;
        var reader = new GridRasterReader(loggers.CreateLogger<GridRasterReader>());
        var tables = new WorldTableLoader(loggers.CreateLogger<WorldTableLoader>());

        GridRaster zones = ReadBytes(ZonesRaster) is { } zoneBytes ? reader.TryRead(zoneBytes, GridRasterReader.ZonesMagic) : null;
        GridRaster rivers = ReadBytes(RiversRaster) is { } riverBytes ? reader.TryRead(riverBytes, GridRasterReader.RiversMagic) : null;
        ZoneTable zoneTable = ReadText(ZonesTable) is { } zoneText ? tables.TryLoadZones(zoneText) : null;
        RiverTable riverTable = ReadText(RiversTable) is { } riverText ? tables.TryLoadRivers(riverText) : null;
        if (zones == null || rivers == null || zoneTable == null || riverTable == null)
        {
            Log.Error("Weltdaten unvollständig, Heimatgebiete werden nicht berechnet.");
            return null;
        }

        TextFile? profiles = ReadText(ProfilesFile);
        TextFile? cultures = ReadText(CulturesFile);
        PeopleData peoples = profiles != null && cultures != null
            ? new PeopleDataLoader(loggers.CreateLogger<PeopleDataLoader>()).TryLoad(profiles.Value, cultures.Value, zoneTable)
            : null;
        if (peoples == null)
        {
            Log.Error("Völkerdaten fehlen oder sind defekt, Heimatgebiete werden nicht berechnet.");
            return null;
        }

        return new HomelandCalculator(loggers.CreateLogger<HomelandCalculator>())
            .Calculate(zones, zoneTable, rivers, riverTable, peoples);
    }

    private static BinaryFile? ReadBytes(string path)
    {
        byte[] bytes = FileAccess.GetFileAsBytes(path);
        Error error = FileAccess.GetOpenError();
        if (error != Error.Ok)
        {
            Log.Error($"Datei {path} nicht lesbar ({error}).");
            return null;
        }

        return new BinaryFile(path, bytes);
    }

    private static TextFile? ReadText(string path)
    {
        string text = FileAccess.GetFileAsString(path);
        Error error = FileAccess.GetOpenError();
        if (error != Error.Ok)
        {
            Log.Error($"Datei {path} nicht lesbar ({error}).");
            return null;
        }

        return new TextFile(path, text);
    }
}
