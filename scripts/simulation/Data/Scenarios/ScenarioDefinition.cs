using System;
using System.Collections.Generic;

namespace TiefBerg.Data.Scenarios;

// Beschreibung eines Szenarios aus data/scenarios/*.yaml (Abschnitt 11, 12). Nur Daten für Auswahl und Start,
// kein Zustand: Was im Spiel passiert, steht im ScenarioState.
// Texte sind Übersetzungsschlüssel, nie Spieltext (Abschnitt 12).
// TODO: Startzeit als Weltzeit statt Anzeigejahr (T-025) und Startort (Abschnitt 4, 11 offen)
public sealed record ScenarioDefinition(
    string Id,
    string TitleKey,
    string DescriptionKey,
    MapPoint MapPosition,
    string Icon,
    string? PreviewImage,
    ScenarioTags Tags,
    int StartYear,
    bool Locked,
    string? UnlockHintKey,
    IReadOnlyList<ScenarioNews> News);

// Pixel der Originalkarte (4320×2160), damit die Position unabhängig von Import-Größe und Zoom bleibt
public readonly record struct MapPoint(int X, int Y);

// Alle Werte sind Übersetzungsschlüssel
public sealed record ScenarioTags(IReadOnlyList<string> SpeciesKeys, string? RegionKey, string? DifficultyKey);

public sealed record ScenarioNews(DateOnly Date, string Version, ScenarioNewsKind Kind, string TextKey);

public enum ScenarioNewsKind
{
    New,
    Updated,
    Planned
}
