using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TiefBerg.Data.World;

// Liest die Tabellen zu den Weltrastern (data/world/klimazonen.yaml, fluesse.yaml). Beide werden von den Werkzeugen
// unter tools/ erzeugt; ein Fehler darin macht die Welt unbrauchbar, deshalb wird die ganze Tabelle abgelehnt.
public sealed class WorldTableLoader
{
    private readonly ILogger<WorldTableLoader> _log;
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public WorldTableLoader(ILogger<WorldTableLoader> log)
    {
        _log = log;
    }

    public ZoneTable? TryLoadZones(TextFile file)
    {
        ZoneTableYaml? raw = Parse<ZoneTableYaml>(file);
        if (raw?.Zones == null || raw.Zones.Count == 0)
        {
            _log.Error($"Zonentabelle {file.Name}: keine Zonen.");
            return null;
        }

        var zones = new List<ZoneDefinition>();
        var seen = new HashSet<int>();
        foreach (ZoneYaml zone in raw.Zones)
        {
            if (zone.Id is not (>= 1 and <= 255) || string.IsNullOrWhiteSpace(zone.NameKey) || !seen.Add(zone.Id.Value))
            {
                _log.Error($"Zonentabelle {file.Name}: Zone {zone.Id} ungültig, doppelt oder ohne name_key.");
                return null;
            }

            ZoneKind? kind = zone.Kind switch
            {
                "land" => ZoneKind.Land,
                "sea" => ZoneKind.Sea,
                "inland_water" => ZoneKind.InlandWater,
                _ => null,
            };
            if (kind == null)
            {
                _log.Error($"Zonentabelle {file.Name}: Zone {zone.Id} hat kind '{zone.Kind}' (land, sea oder inland_water).");
                return null;
            }

            zones.Add(new ZoneDefinition((byte)zone.Id.Value, zone.NameKey.Trim(), kind.Value));
        }

        _log.Info($"Zonentabelle {file.Name}: {zones.Count} Zonen.");
        return new ZoneTable(zones);
    }

    public RiverTable? TryLoadRivers(TextFile file)
    {
        RiverTableYaml? raw = Parse<RiverTableYaml>(file);
        if (raw?.Classes == null || raw.Classes.Count == 0)
        {
            _log.Error($"Flusstabelle {file.Name}: keine Klassen.");
            return null;
        }

        var classes = new List<RiverClass>();
        var seen = new HashSet<int>();
        foreach (RiverClassYaml river in raw.Classes)
        {
            // 0 ist im Raster „kein Fluss“ und darf keine Klasse sein
            if (river.Id is not (>= 1 and <= 255) || string.IsNullOrWhiteSpace(river.NameKey) || river.Seasonal == null
                || river.MinDischargeM3s is not >= 0 || !seen.Add(river.Id.Value))
            {
                _log.Error($"Flusstabelle {file.Name}: Klasse {river.Id} ungültig, doppelt oder unvollständig.");
                return null;
            }

            classes.Add(new RiverClass((byte)river.Id.Value, river.NameKey.Trim(), river.Seasonal.Value,
                river.MinDischargeM3s.Value));
        }

        _log.Info($"Flusstabelle {file.Name}: {classes.Count} Klassen.");
        return new RiverTable(classes);
    }

    private T? Parse<T>(TextFile file) where T : class
    {
        try
        {
            return _deserializer.Deserialize<T?>(file.Text);
        }
        catch (YamlException e)
        {
            _log.Error($"Tabelle {file.Name}: kein gültiges YAML oder unbekannter Schlüssel.", e);
            return null;
        }
    }

    // Rohformen wie YamlDotNet sie befüllt; alles nullable, damit fehlende Felder erkannt werden
    private sealed class ZoneTableYaml
    {
        public string? Raster { get; set; }
        public List<ZoneYaml>? Zones { get; set; }
    }

    private sealed class ZoneYaml
    {
        public int? Id { get; set; }
        public string? NameKey { get; set; }
        public string? Kind { get; set; }
        public List<int>? Color { get; set; }
    }

    private sealed class RiverTableYaml
    {
        public string? Raster { get; set; }
        public List<RiverClassYaml>? Classes { get; set; }
    }

    private sealed class RiverClassYaml
    {
        public int? Id { get; set; }
        public string? NameKey { get; set; }
        public bool? Seasonal { get; set; }
        public double? MinDischargeM3s { get; set; }
        public List<int>? Color { get; set; }
    }
}
