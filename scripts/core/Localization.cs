using System.Linq;
using Godot;
using Microsoft.Extensions.Logging;
using TiefBerg.Core.Logging;
using TiefBerg.Data.Localization;

namespace TiefBerg.Core;

// Trägt die Texte aus res://data/lang/<sprache>.yaml in Godots TranslationServer ein (Abschnitt 12).
// Danach übersetzt Godot Label-/Button-Texte selbst, Code nutzt Tr(schlüssel).
// Bewusst YAML statt Godots CSV-Import: gleiche Daten-Pipeline wie die übrigen Inhalte, kein Import-Schritt im Editor.
public static class Localization
{
    // Spiel komplett auf Englisch (Abschnitt 12); fest, damit ein deutsches System nicht die Schlüssel zeigt
    public const string GameLocale = "en";

    private const string LanguageDirectory = "res://data/lang";
    private const string Extension = ".yaml";

    private static readonly GameLogger Log = LogManager.GetLogger(nameof(Localization));

    // TODO: Sprachauswahl in den Optionen (T-016), weitere Sprachen
    public static void Initialize()
    {
        if (!DirAccess.DirExistsAbsolute(LanguageDirectory))
        {
            Log.Error($"Sprachordner fehlt: {LanguageDirectory}, Texte erscheinen als Schlüssel.");
            return;
        }

        var loader = new TranslationTableLoader(
            LogManager.SimulationLoggerFactory.CreateLogger<TranslationTableLoader>());
        foreach (string fileName in DirAccess.GetFilesAt(LanguageDirectory).Where(f => f.EndsWith(Extension)))
        {
            string path = $"{LanguageDirectory}/{fileName}";
            string text = FileAccess.GetFileAsString(path);
            Error error = FileAccess.GetOpenError();
            if (error != Error.Ok)
            {
                Log.Warn($"Übersetzung {path} nicht lesbar ({error}), übersprungen.");
                continue;
            }

            var translation = new Translation { Locale = fileName[..^Extension.Length] };
            foreach (var (key, message) in loader.Load(path, text))
            {
                translation.AddMessage(key, message);
            }

            TranslationServer.AddTranslation(translation);
        }

        TranslationServer.SetLocale(GameLocale);
        Log.Info($"Sprache: {TranslationServer.GetLocale()}");
    }
}
