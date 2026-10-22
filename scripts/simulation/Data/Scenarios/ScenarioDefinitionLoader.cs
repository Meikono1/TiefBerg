using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.Serialization;
using YamlDotNet.Serialization.NamingConventions;

namespace TiefBerg.Data.Scenarios;

// Eine Szenariodatei als Text; den Dateizugriff (res://, Mods) übernimmt der Host
public readonly record struct ScenarioSource(string Name, string Yaml);

// Liest Szenariodateien (data/scenarios/*.yaml, eine Datei pro Szenario) und prüft sie (Abschnitt 11, 12).
// Ungültige Szenarien werden mit allen gefundenen Fehlern geloggt und übersprungen, damit eine defekte (Mod-)Datei
// nicht die ganze Auswahl verhindert. Reihenfolge der Ausgabe = Reihenfolge der Quellen, nach Namen sortiert.
public sealed class ScenarioDefinitionLoader
{
    private const string DateFormat = "yyyy-MM-dd";

    // Ids landen im Spielstand und in Pfaden; Icons sind Dateinamen ohne Endung unter resources/MapIcons/
    private static readonly Regex ValidId = new("^[a-z0-9_]{1,64}$", RegexOptions.Compiled);
    private static readonly Regex ValidIcon = new("^[A-Za-z0-9_]{1,64}$", RegexOptions.Compiled);
    private static readonly Regex TabIndentedLine = new("^ *\t", RegexOptions.Compiled | RegexOptions.Multiline);

    private readonly ILogger<ScenarioDefinitionLoader> _log;
    private readonly IDeserializer _deserializer = new DeserializerBuilder()
        .WithNamingConvention(UnderscoredNamingConvention.Instance)
        .Build();

    public ScenarioDefinitionLoader(ILogger<ScenarioDefinitionLoader> log)
    {
        _log = log;
    }

    public IReadOnlyList<ScenarioDefinition> LoadAll(IEnumerable<ScenarioSource> sources)
    {
        var result = new List<ScenarioDefinition>();
        var seenIds = new HashSet<string>(StringComparer.Ordinal);
        int skipped = 0;

        foreach (ScenarioSource source in sources.OrderBy(s => s.Name, StringComparer.Ordinal))
        {
            ScenarioDefinition? definition = TryLoad(source);
            if (definition == null)
            {
                skipped++;
                continue;
            }

            if (!seenIds.Add(definition.Id))
            {
                _log.Warn($"Szenario '{definition.Id}' aus {source.Name} übersprungen: Id ist schon vergeben.");
                skipped++;
                continue;
            }

            result.Add(definition);
        }

        _log.Info($"{result.Count} Szenarien geladen, {skipped} übersprungen.");
        return result;
    }

    public ScenarioDefinition? TryLoad(ScenarioSource source)
    {
        // YAML verbietet Tabs zur Einrückung; Editoren wandeln Leerzeichen gern beim Speichern um.
        // Eigene Meldung, weil die des Parsers darauf nicht hinweist.
        if (TabIndentedLine.IsMatch(source.Yaml))
        {
            _log.Warn($"Szenariodatei {source.Name} übersprungen: Einrückung mit Tabs, YAML erlaubt nur Leerzeichen.");
            return null;
        }

        ScenarioYaml? raw;
        try
        {
            raw = _deserializer.Deserialize<ScenarioYaml?>(source.Yaml);
        }
        catch (YamlException e)
        {
            _log.Warn($"Szenariodatei {source.Name} übersprungen: kein gültiges YAML oder unbekannter Schlüssel.", e);
            return null;
        }

        if (raw == null)
        {
            _log.Warn($"Szenariodatei {source.Name} übersprungen: Datei ist leer.");
            return null;
        }

        var errors = new List<string>();
        ScenarioDefinition? definition = Validate(raw, source.Name, errors);
        if (definition == null)
        {
            _log.Warn($"Szenariodatei {source.Name} übersprungen: {string.Join("; ", errors)}");
            return null;
        }

        _log.Debug($"Szenario '{definition.Id}' aus {source.Name} gelesen.");
        return definition;
    }

    private ScenarioDefinition? Validate(ScenarioYaml raw, string sourceName, List<string> errors)
    {
        string id = Required(raw.Id, "id", errors);
        if (id.Length > 0 && !ValidId.IsMatch(id))
        {
            errors.Add($"id '{id}' darf nur a-z, 0-9 und _ enthalten");
        }

        string titleKey = Required(raw.TitleKey, "title_key", errors);
        string descriptionKey = Required(raw.DescriptionKey, "description_key", errors);
        string icon = Required(raw.Icon, "icon", errors);
        if (icon.Length > 0 && !ValidIcon.IsMatch(icon))
        {
            errors.Add($"icon '{icon}' ist kein Dateiname ohne Endung");
        }

        MapPoint position = ValidatePosition(raw.MapPosition, errors);

        if (raw.StartYear == null)
        {
            errors.Add("start_year fehlt");
        }

        string? unlockHintKey = Optional(raw.UnlockHintKey);
        if (raw.Locked && unlockHintKey == null)
        {
            // Kein Abbruch: Das Szenario bleibt gesperrt sichtbar, nur ohne Hinweis
            _log.Warn($"Szenario '{id}' aus {sourceName} ist gesperrt, aber ohne unlock_hint_key.");
        }

        ScenarioTags tags = ValidateTags(raw.Tags, errors);

        if (errors.Count > 0)
        {
            return null;
        }

        IReadOnlyList<ScenarioNews> news = ValidateNews(raw.News, id, sourceName);

        // StartYear ist hier nie null (sonst stünde ein Fehler in errors)
        return new ScenarioDefinition(id, titleKey, descriptionKey, position, icon, Optional(raw.PreviewImage),
            tags, raw.StartYear ?? 0, raw.Locked, unlockHintKey, news);
    }

    private static MapPoint ValidatePosition(MapPointYaml? raw, List<string> errors)
    {
        if (raw?.X == null || raw.Y == null)
        {
            errors.Add("map_position braucht x und y");
            return default;
        }

        // Die Obergrenze kennt nur die Karte (UI), der Kern prüft nur das Vorzeichen
        if (raw.X < 0 || raw.Y < 0)
        {
            errors.Add($"map_position ({raw.X}, {raw.Y}) ist negativ");
        }

        return new MapPoint(raw.X.Value, raw.Y.Value);
    }

    private static ScenarioTags ValidateTags(TagsYaml? raw, List<string> errors)
    {
        List<string> species = raw?.Species ?? new List<string>();
        if (species.Any(string.IsNullOrWhiteSpace))
        {
            errors.Add("tags.species enthält einen leeren Eintrag");
        }

        return new ScenarioTags(species.ToArray(), Optional(raw?.Region), Optional(raw?.Difficulty));
    }

    // Defekte Neuigkeiten kosten nicht das ganze Szenario, nur den einzelnen Eintrag
    private IReadOnlyList<ScenarioNews> ValidateNews(List<NewsYaml>? raw, string id, string sourceName)
    {
        var result = new List<ScenarioNews>();
        if (raw == null)
        {
            return result;
        }

        for (int i = 0; i < raw.Count; i++)
        {
            NewsYaml entry = raw[i];
            var errors = new List<string>();
            string version = Required(entry.Version, "version", errors);
            string textKey = Required(entry.TextKey, "text_key", errors);

            if (!DateOnly.TryParseExact(entry.Date, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None,
                    out DateOnly date))
            {
                errors.Add($"date '{entry.Date}' ist nicht im Format {DateFormat}");
            }

            if (!TryParseKind(entry.Kind, out ScenarioNewsKind kind))
            {
                errors.Add($"kind '{entry.Kind}' ist nicht new, updated oder planned");
            }

            if (errors.Count > 0)
            {
                _log.Warn($"Neuigkeit {i + 1} von Szenario '{id}' aus {sourceName} übersprungen: " +
                          string.Join("; ", errors));
                continue;
            }

            result.Add(new ScenarioNews(date, version, kind, textKey));
        }

        return result;
    }

    private static bool TryParseKind(string? value, out ScenarioNewsKind kind)
    {
        switch (value)
        {
            case "new":
                kind = ScenarioNewsKind.New;
                return true;
            case "updated":
                kind = ScenarioNewsKind.Updated;
                return true;
            case "planned":
                kind = ScenarioNewsKind.Planned;
                return true;
            default:
                kind = default;
                return false;
        }
    }

    private static string Required(string? value, string key, List<string> errors)
    {
        string? trimmed = Optional(value);
        if (trimmed == null)
        {
            errors.Add($"{key} fehlt");
            return "";
        }

        return trimmed;
    }

    private static string? Optional(string? value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
