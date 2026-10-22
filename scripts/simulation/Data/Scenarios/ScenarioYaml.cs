using System.Collections.Generic;

namespace TiefBerg.Data.Scenarios;

// Rohform einer Szenariodatei, wie YamlDotNet sie befüllt (Schlüssel in snake_case). Alles nullable,
// damit fehlende Pflichtfelder erkannt werden; geprüft und umgewandelt wird im ScenarioDefinitionLoader.
// Unbekannte Schlüssel lässt der Parser nicht durch, so fallen Tippfehler in Moddaten sofort auf.
internal sealed class ScenarioYaml
{
    public string? Id { get; set; }
    public string? TitleKey { get; set; }
    public string? DescriptionKey { get; set; }
    public MapPointYaml? MapPosition { get; set; }
    public string? Icon { get; set; }
    public string? PreviewImage { get; set; }
    public TagsYaml? Tags { get; set; }
    public int? StartYear { get; set; }
    public bool Locked { get; set; }
    public string? UnlockHintKey { get; set; }
    public List<NewsYaml>? News { get; set; }
}

internal sealed class MapPointYaml
{
    public int? X { get; set; }
    public int? Y { get; set; }
}

internal sealed class TagsYaml
{
    public List<string>? Species { get; set; }
    public string? Region { get; set; }
    public string? Difficulty { get; set; }
}

internal sealed class NewsYaml
{
    // Als Text gelesen und selbst geprüft (yyyy-MM-dd), damit die Fehlermeldung verständlich bleibt
    public string? Date { get; set; }
    public string? Version { get; set; }
    public string? Kind { get; set; }
    public string? TextKey { get; set; }
}
