using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TiefBerg.Data.World;
using TiefBerg.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TiefBerg.Data.Peoples;

// Liest Lebensraum-Profile (data/peoples/lebensraum.yaml) und Kulturgruppen mit ihren Völkern (data/peoples/kulturen.yaml),
// lore/voelker/verbreitung.md. Ein Fehler in den Profilen lehnt alles ab, weil jedes Volk darauf aufbaut.
// Ein defektes Volk oder eine defekte Kultur wird dagegen geloggt und übersprungen, damit eine fehlerhafte (Mod-)Angabe
// nicht die ganze Welt verhindert (Muster wie bei den Szenarien).
public sealed class PeopleDataLoader
{
    private static readonly Regex ValidId = new("^[a-z0-9_]{1,64}$", RegexOptions.Compiled);

    private readonly ILogger<PeopleDataLoader> _log;
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public PeopleDataLoader(ILogger<PeopleDataLoader> log)
    {
        _log = log;
    }

    public PeopleData? TryLoad(TextFile profileFile, TextFile cultureFile, ZoneTable zones)
    {
        Dictionary<string, HabitatProfile>? profiles = LoadProfiles(profileFile, zones);
        if (profiles == null)
        {
            return null;
        }

        CulturesYaml? raw = Parse<CulturesYaml>(cultureFile);
        if (raw?.Cultures == null)
        {
            _log.Error($"Kulturdatei {cultureFile.Name}: keine Kulturen.");
            return null;
        }

        var cultures = new List<CultureDefinition>();
        // Kultur und Volk dürfen gleich heißen (Kultur goblin, Volk goblin); eindeutig nur untereinander
        var seenIds = new SeenIds(new HashSet<string>(), new HashSet<string>());
        int skipped = 0;
        foreach (CultureYaml culture in raw.Cultures)
        {
            CultureDefinition? definition = ValidateCulture(culture, profiles, seenIds, cultureFile.Name, ref skipped);
            if (definition != null)
            {
                cultures.Add(definition);
            }
        }

        int peoples = cultures.Sum(c => c.Peoples.Count);
        _log.Info($"{profiles.Count} Lebensraum-Profile, {cultures.Count} Kulturen mit {peoples} Völkern geladen, {skipped} übersprungen.");
        return new PeopleData(profiles.Values.ToList(), cultures);
    }

    private Dictionary<string, HabitatProfile>? LoadProfiles(TextFile file, ZoneTable zones)
    {
        ProfilesYaml? raw = Parse<ProfilesYaml>(file);
        if (raw?.Profiles == null || raw.Profiles.Count == 0)
        {
            _log.Error($"Profildatei {file.Name}: keine Profile.");
            return null;
        }

        var profiles = new Dictionary<string, HabitatProfile>();
        foreach ((string id, Dictionary<int, double>? values) in raw.Profiles)
        {
            if (!ValidId.IsMatch(id) || values == null)
            {
                _log.Error($"Profildatei {file.Name}: Profil '{id}' ungültig.");
                return null;
            }

            var suitability = new Dictionary<byte, double>();
            foreach ((int zoneId, double value) in values)
            {
                if (zoneId is < 0 or > 255 || zones.Find((byte)zoneId) == null)
                {
                    _log.Error($"Profildatei {file.Name}: Profil '{id}' nennt unbekannte Zone {zoneId}.");
                    return null;
                }

                if (value is < 0 or > 1)
                {
                    _log.Error($"Profildatei {file.Name}: Profil '{id}', Zone {zoneId}: Eignung {value} liegt nicht zwischen 0 und 1.");
                    return null;
                }

                suitability[(byte)zoneId] = value;
            }

            profiles[id] = new HabitatProfile(id, suitability);
        }

        return profiles;
    }

    private CultureDefinition? ValidateCulture(CultureYaml raw, Dictionary<string, HabitatProfile> profiles,
        SeenIds seenIds, string sourceName, ref int skipped)
    {
        string? id = raw.Id?.Trim();
        if (id == null || !ValidId.IsMatch(id) || string.IsNullOrWhiteSpace(raw.NameKey) || !TryColor(raw.Color, out RgbColor color)
            || raw.Peoples == null || raw.Peoples.Count == 0)
        {
            _log.Warn($"Kultur '{raw.Id}' aus {sourceName} übersprungen: id, name_key, color (3 Werte 0–255) und peoples nötig.");
            skipped++;
            return null;
        }

        if (!seenIds.Cultures.Add(id))
        {
            _log.Warn($"Kultur '{id}' aus {sourceName} übersprungen: Id ist schon vergeben.");
            skipped++;
            return null;
        }

        var peoples = new List<PeopleDefinition>();
        foreach (PeopleYaml people in raw.Peoples)
        {
            var errors = new List<string>();
            PeopleDefinition? definition = ValidatePeople(people, id, profiles, errors);
            if (definition == null || !seenIds.Peoples.Add(definition.Id))
            {
                if (definition != null)
                {
                    errors.Add("Id ist schon vergeben");
                }

                _log.Warn($"Volk '{people.Id}' der Kultur '{id}' aus {sourceName} übersprungen: {string.Join("; ", errors)}");
                skipped++;
                continue;
            }

            peoples.Add(definition);
        }

        if (peoples.Count == 0)
        {
            _log.Warn($"Kultur '{id}' aus {sourceName} übersprungen: kein gültiges Volk.");
            skipped++;
            return null;
        }

        return new CultureDefinition(id, raw.NameKey.Trim(), color, peoples);
    }

    // TODO: Prüfen, ob es zum Volk eine Spezies gibt, sobald Speziesdaten existieren (Abschnitt 5, T-020)
    private static PeopleDefinition? ValidatePeople(PeopleYaml raw, string cultureId,
        Dictionary<string, HabitatProfile> profiles, List<string> errors)
    {
        string? id = raw.Id?.Trim();
        if (id == null || !ValidId.IsMatch(id))
        {
            errors.Add($"id '{raw.Id}' darf nur a-z, 0-9 und _ enthalten");
        }

        if (string.IsNullOrWhiteSpace(raw.NameKey))
        {
            errors.Add("name_key fehlt");
        }

        if (raw.Center is not { Count: 2 } || raw.Center[0] is < -180 or > 180 || raw.Center[1] is < -90 or > 90)
        {
            errors.Add("center braucht [Länge −180..180, Breite −90..90]");
        }

        if (raw.Radius is not > 0)
        {
            errors.Add("radius muss größer als 0 sein");
        }

        var habitat = new List<HabitatProfile>();
        foreach (string profileId in raw.Profiles ?? new List<string>())
        {
            if (profiles.TryGetValue(profileId, out HabitatProfile? profile))
            {
                habitat.Add(profile);
            }
            else
            {
                errors.Add($"unbekanntes Profil '{profileId}'");
            }
        }

        if (habitat.Count == 0)
        {
            errors.Add("profiles fehlt");
        }

        List<string> bonus = raw.Bonus ?? new List<string>();
        foreach (string unknown in bonus.Where(b => b is not ("river" or "coast")))
        {
            errors.Add($"unbekannter bonus '{unknown}' (river oder coast)");
        }

        if (errors.Count > 0 || id == null || raw.NameKey == null || raw.Center == null || raw.Radius == null)
        {
            return null;
        }

        return new PeopleDefinition(id, raw.NameKey.Trim(), cultureId, raw.Center[0], raw.Center[1], raw.Radius.Value,
            habitat, bonus.Contains("river"), bonus.Contains("coast"));
    }

    private static bool TryColor(List<int>? raw, out RgbColor color)
    {
        color = default;
        if (raw is not { Count: 3 } || raw.Any(c => c is < 0 or > 255))
        {
            return false;
        }

        color = new RgbColor((byte)raw[0], (byte)raw[1], (byte)raw[2]);
        return true;
    }

    private T? Parse<T>(TextFile file) where T : class
    {
        try
        {
            return _deserializer.Deserialize<T?>(file.Text);
        }
        catch (YamlException e)
        {
            _log.Error($"Datei {file.Name}: kein gültiges YAML oder unbekannter Schlüssel.", e);
            return null;
        }
    }

    private sealed record SeenIds(HashSet<string> Cultures, HashSet<string> Peoples);

    // Rohformen wie YamlDotNet sie befüllt; alles nullable, damit fehlende Felder erkannt werden.
    // Profile werden nur nachgeschlagen, ihre Reihenfolge beeinflusst kein Ergebnis.
    private sealed class ProfilesYaml
    {
        public Dictionary<string, Dictionary<int, double>?>? Profiles { get; set; }
    }

    private sealed class CulturesYaml
    {
        public List<CultureYaml>? Cultures { get; set; }
    }

    private sealed class CultureYaml
    {
        public string? Id { get; set; }
        public string? NameKey { get; set; }
        public List<int>? Color { get; set; }
        public List<PeopleYaml>? Peoples { get; set; }
    }

    private sealed class PeopleYaml
    {
        public string? Id { get; set; }
        public string? NameKey { get; set; }
        public List<double>? Center { get; set; }
        public double? Radius { get; set; }
        public List<string>? Profiles { get; set; }
        public List<string>? Bonus { get; set; }
    }
}
