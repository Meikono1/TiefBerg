using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;
using YamlDotNet.Core;
using YamlDotNet.Serialization;

namespace TiefBerg.Data.Localization;

// Liest eine Übersetzungstabelle (data/lang/<sprache>.yaml, flach: SCHLÜSSEL: Text) (Abschnitt 12).
// Den Dateizugriff und das Eintragen in Godots TranslationServer übernimmt der Host.
// Defekte Einträge werden einzeln übersprungen; im Spiel erscheint dann der Schlüssel statt des Texts.
public sealed class TranslationTableLoader
{
    private static readonly Regex ValidKey = new("^[A-Z][A-Z0-9_]*$", RegexOptions.Compiled);
    private static readonly Regex TabIndentedLine = new("^ *\t", RegexOptions.Compiled | RegexOptions.Multiline);

    private readonly ILogger<TranslationTableLoader> _log;
    private readonly IDeserializer _deserializer = new DeserializerBuilder().Build();

    public TranslationTableLoader(ILogger<TranslationTableLoader> log)
    {
        _log = log;
    }

    public IReadOnlyDictionary<string, string> Load(string sourceName, string yaml)
    {
        var result = new Dictionary<string, string>();
        if (TabIndentedLine.IsMatch(yaml))
        {
            _log.Warn($"Übersetzung {sourceName} übersprungen: Einrückung mit Tabs, YAML erlaubt nur Leerzeichen.");
            return result;
        }

        Dictionary<string, string?>? raw;
        try
        {
            raw = _deserializer.Deserialize<Dictionary<string, string?>?>(yaml);
        }
        catch (YamlException e)
        {
            _log.Warn($"Übersetzung {sourceName} übersprungen: kein gültiges YAML oder kein flaches Schlüssel-Text-Verzeichnis.", e);
            return result;
        }

        if (raw == null)
        {
            _log.Warn($"Übersetzung {sourceName} ist leer.");
            return result;
        }

        var invalid = new List<string>();
        foreach (var (key, text) in raw)
        {
            if (!ValidKey.IsMatch(key) || string.IsNullOrWhiteSpace(text))
            {
                invalid.Add(key);
                continue;
            }

            result[key] = text;
        }

        if (invalid.Count > 0)
        {
            _log.Warn($"Übersetzung {sourceName}: {invalid.Count} Einträge übersprungen (Schlüssel nicht GROSS_MIT_UNTERSTRICH oder Text leer): " +
                      string.Join(", ", invalid.Take(10)));
        }

        _log.Info($"Übersetzung {sourceName}: {result.Count} Texte geladen.");
        return result;
    }
}
