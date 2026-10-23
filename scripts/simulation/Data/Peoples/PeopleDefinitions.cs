using System.Collections.Generic;

namespace TiefBerg.Data.Peoples;

public readonly record struct RgbColor(byte R, byte G, byte B);

// Eignung je Klimazone, 0 bis 1 (data/peoples/lebensraum.yaml); nicht genannte Zonen = 0
public sealed record HabitatProfile(string Id, IReadOnlyDictionary<byte, double> Suitability);

// Mittelpunkt in Grad (Länge −180 bis 180, Ost positiv; Breite Nord positiv), Radius in Grad.
// Profile beschreiben das natürliche Habitat, nicht wo das Volk leben kann (Design-Referenz Abschnitt 4).
public sealed record PeopleDefinition(
    string Id,
    string NameKey,
    string CultureId,
    double CenterLongitude,
    double CenterLatitude,
    double RadiusDegrees,
    IReadOnlyList<HabitatProfile> Profiles,
    bool RiverBonus,
    bool CoastBonus);

public sealed record CultureDefinition(string Id, string NameKey, RgbColor Color, IReadOnlyList<PeopleDefinition> Peoples);

// Reihenfolge wie in der Datei; sie bestimmt die Indizes in der Heimatgebietskarte
public sealed record PeopleData(IReadOnlyList<HabitatProfile> Profiles, IReadOnlyList<CultureDefinition> Cultures);
