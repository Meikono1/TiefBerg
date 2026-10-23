using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using TiefBerg.Data.Peoples;
using TiefBerg.Data.World;
using TiefBerg.Diagnostics;

namespace TiefBerg.Simulation.Geography;

// Berechnet die Heimatgebiete der Völker beim Weltstart (Design-Referenz Abschnitt 4, lore/voelker/verbreitung.md):
//   Einfluss je Volk  I = clip(Eignung · exp(−(d / R)²) · 1,25; 0; 1), d = Weg über Land
//   Kultur            1 − Π(1 − I) über ihre Völker
//   Kern > 0,50, Einfluss > 0,22, Misch: zweitstärkste Kultur > 0,22 und > 45 % der stärksten
//   Heimatgebiet eines Volks: Kerngebiet seiner Kultur, in dem sein eigener Einfluss über 0,22 liegt (Annahme;
//   überlappt bei benachbarten Völkern einer Kultur, dort leben sie nebeneinander)
// Kein Volk, keine Zone und kein Profil ist hier verdrahtet; alles kommt aus den geladenen Daten (Abschnitt 2).
// Völker werden parallel gerechnet und in fester Reihenfolge zusammengeführt, das Ergebnis ist damit immer gleich.
public sealed class HomelandCalculator
{
    public const int DefaultWidth = 1440;
    public const int DefaultHeight = 720;

    private const double SmoothingSigmaCells = 2.0;
    private const double InfluenceScale = 1.25;
    private const double CoreThreshold = 0.50;
    private const double InfluenceThreshold = 0.22;
    private const double MixedShare = 0.45;
    private const double BonusBase = 0.55;
    private const double BonusFactor = 0.6;
    private const double CoastReachDegrees = 3.0;

    // Annahme: Flussnähe wirkt bis 1° (verbreitung.md nennt keine Reichweite); nur ganzjährige Flüsse zählen
    private const double RiverReachDegrees = 1.0;

    // Jenseits von 2,2 R ist exp(−(d/R)²) · 1,25 unter 1 %; dort wird nicht mehr gerechnet
    private const double CutoffRadii = 2.2;
    private const float MinInfluence = 0.005f;

    private readonly ILogger<HomelandCalculator> _log;

    public HomelandCalculator(ILogger<HomelandCalculator> log)
    {
        _log = log;
    }

    public HomelandMap Calculate(GridRaster zones, ZoneTable zoneTable, GridRaster rivers, RiverTable riverTable,
        PeopleData peopleData, int width = DefaultWidth, int height = DefaultHeight)
    {
        using (Perf.Measure())
        {
            Terrain terrain = BuildTerrain(zones, zoneTable, rivers, riverTable, width, height);
            List<PeopleDefinition> peoples = peopleData.Cultures.SelectMany(c => c.Peoples).ToList();
            var suitability = new ConcurrentDictionary<string, float[]>();
            var influence = new (int Cell, float Value)[peoples.Count][];

            Parallel.For(0, peoples.Count,
                () => new LandDistance(terrain.Land, width, height),
                (index, _, distance) =>
                {
                    influence[index] = PeopleInfluence(peoples[index], terrain, suitability, distance);
                    return distance;
                },
                _ => { });

            for (int i = 0; i < peoples.Count; i++)
            {
                if (influence[i].Length == 0)
                {
                    _log.Warn($"Volk '{peoples[i].Id}': Mittelpunkt ({peoples[i].CenterLongitude}, {peoples[i].CenterLatitude}) liegt nicht an Land, kein Heimatgebiet.");
                }
            }

            HomelandMap map = Combine(peopleData.Cultures, peoples, influence, width, height);
            LogSummary(map, terrain);
            return map;
        }
    }

    private static Terrain BuildTerrain(GridRaster zones, ZoneTable zoneTable, GridRaster rivers, RiverTable riverTable,
        int width, int height)
    {
        int cells = width * height;
        var zone = new byte[cells];
        var land = new bool[cells];
        var sea = new bool[cells];
        var river = new bool[cells];
        for (int y = 0; y < height; y++)
        {
            for (int x = 0; x < width; x++)
            {
                int index = y * width + x;
                zone[index] = zones.Sample(x, y, width, height);
                land[index] = zoneTable.IsLand(zone[index]);
                sea[index] = zoneTable.IsSea(zone[index]);
                river[index] = riverTable.IsFlowing(rivers.Sample(x, y, width, height));
            }
        }

        double cellDegrees = 180.0 / height;
        float[] coastNear = Nearness(GridOperations.DistanceToNearest(sea, width, height), cellDegrees, CoastReachDegrees);
        float[] riverNear = Nearness(GridOperations.DistanceToNearest(river, width, height), cellDegrees, RiverReachDegrees);
        return new Terrain(width, height, zone, land, coastNear, riverNear);
    }

    // 1 direkt an der Quelle, linear fallend bis 0 an der Reichweite
    private static float[] Nearness(float[] distanceCells, double cellDegrees, double reachDegrees)
    {
        var nearness = new float[distanceCells.Length];
        for (int i = 0; i < nearness.Length; i++)
        {
            nearness[i] = (float)Math.Max(0, 1 - distanceCells[i] * cellDegrees / reachDegrees);
        }

        return nearness;
    }

    private static (int Cell, float Value)[] PeopleInfluence(PeopleDefinition people, Terrain terrain,
        ConcurrentDictionary<string, float[]> suitabilityCache, LandDistance distance)
    {
        string key = string.Join("+", people.Profiles.Select(p => p.Id).Order(StringComparer.Ordinal));
        float[] suitability = suitabilityCache.GetOrAdd(key, _ => Suitability(people.Profiles, terrain));

        int x = Math.Clamp((int)((people.CenterLongitude + 180.0) / 360.0 * terrain.Width), 0, terrain.Width - 1);
        int y = Math.Clamp((int)((90.0 - people.CenterLatitude) / 180.0 * terrain.Height), 0, terrain.Height - 1);
        var result = new List<(int, float)>();
        double radius = people.RadiusDegrees;
        distance.Visit(y * terrain.Width + x, radius * CutoffRadii, (cell, degrees) =>
        {
            double value = suitability[cell] * Bonus(people, terrain, cell)
                           * Math.Exp(-(degrees / radius) * (degrees / radius)) * InfluenceScale;
            float clipped = (float)Math.Clamp(value, 0, 1);
            if (clipped >= MinInfluence)
            {
                result.Add((cell, clipped));
            }
        });

        return result.ToArray();
    }

    // Eignung: je Zone der höchste Wert der Profile, Meer und Binnengewässer 0, danach geglättet
    private static float[] Suitability(IReadOnlyList<HabitatProfile> profiles, Terrain terrain)
    {
        var lookup = new float[256];
        foreach (HabitatProfile profile in profiles)
        {
            foreach ((byte zone, double value) in profile.Suitability)
            {
                lookup[zone] = Math.Max(lookup[zone], (float)value);
            }
        }

        var raw = new float[terrain.Zone.Length];
        for (int i = 0; i < raw.Length; i++)
        {
            raw[i] = terrain.Land[i] ? lookup[terrain.Zone[i]] : 0f;
        }

        return GridOperations.GaussianBlur(raw, terrain.Width, terrain.Height, SmoothingSigmaCells);
    }

    private static double Bonus(PeopleDefinition people, Terrain terrain, int cell)
    {
        double factor = 1;
        if (people.RiverBonus)
        {
            factor *= BonusBase + BonusFactor * terrain.RiverNear[cell];
        }

        if (people.CoastBonus)
        {
            factor *= BonusBase + BonusFactor * terrain.CoastNear[cell];
        }

        return factor;
    }

    private static HomelandMap Combine(IReadOnlyList<CultureDefinition> cultures, List<PeopleDefinition> peoples,
        (int Cell, float Value)[][] influence, int width, int height)
    {
        int cells = width * height;
        var best = new float[cells];
        var second = new float[cells];
        var bestCulture = new short[cells];
        Array.Fill(bestCulture, (short)-1);
        var cultureOfPeople = new short[peoples.Count];

        // Je Kultur der Rest Π(1 − I); das Hilfsfeld wird nach jeder Kultur zurückgesetzt
        var remaining = new float[cells];
        Array.Fill(remaining, 1f);
        var touched = new List<int>();
        int peopleIndex = 0;
        for (short cultureIndex = 0; cultureIndex < cultures.Count; cultureIndex++)
        {
            foreach (PeopleDefinition _ in cultures[cultureIndex].Peoples)
            {
                cultureOfPeople[peopleIndex] = cultureIndex;
                foreach ((int cell, float value) in influence[peopleIndex])
                {
                    if (remaining[cell] == 1f)
                    {
                        touched.Add(cell);
                    }

                    remaining[cell] *= 1 - value;
                }

                peopleIndex++;
            }

            foreach (int cell in touched)
            {
                float culture = 1 - remaining[cell];
                if (culture > best[cell])
                {
                    second[cell] = best[cell];
                    best[cell] = culture;
                    bestCulture[cell] = cultureIndex;
                }
                else if (culture > second[cell])
                {
                    second[cell] = culture;
                }

                remaining[cell] = 1f;
            }

            touched.Clear();
        }

        var area = new HomelandArea[cells];
        for (int cell = 0; cell < cells; cell++)
        {
            if (best[cell] > CoreThreshold)
            {
                area[cell] = HomelandArea.Core;
            }
            else if (second[cell] > InfluenceThreshold && second[cell] > MixedShare * best[cell])
            {
                area[cell] = HomelandArea.Mixed;
            }
            else if (best[cell] > InfluenceThreshold)
            {
                area[cell] = HomelandArea.Influence;
            }
            else
            {
                bestCulture[cell] = -1;
            }
        }

        var homelands = new int[peoples.Count][];
        for (int i = 0; i < peoples.Count; i++)
        {
            short culture = cultureOfPeople[i];
            homelands[i] = influence[i]
                .Where(entry => entry.Value > InfluenceThreshold && area[entry.Cell] == HomelandArea.Core
                                && bestCulture[entry.Cell] == culture)
                .Select(entry => entry.Cell)
                .Order()
                .ToArray();
        }

        return new HomelandMap(width, height, cultures, peoples, area, bestCulture, homelands);
    }

    private void LogSummary(HomelandMap map, Terrain terrain)
    {
        int land = terrain.Land.Count(l => l);
        var counts = new int[4];
        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 0; x < map.Width; x++)
            {
                if (terrain.Land[y * map.Width + x])
                {
                    counts[(int)map.AreaAt(x, y)]++;
                }
            }
        }

        _log.Info($"Heimatgebiete berechnet: {map.Peoples.Count} Völker auf {map.Width} x {map.Height}. Land: " +
                  $"Kern {Percent(counts[1], land)}, Einfluss {Percent(counts[2], land)}, Misch {Percent(counts[3], land)}, " +
                  $"Wildnis {Percent(counts[0], land)}.");
        for (int i = 0; i < map.Peoples.Count; i++)
        {
            int homelandCells = map.HomelandCells(i).Count;
            if (homelandCells == 0)
            {
                _log.Warn($"Volk '{map.Peoples[i].Id}' hat kein Heimatgebiet (eigener Einfluss erreicht kein Kerngebiet seiner Kultur).");
            }
            else
            {
                _log.Debug($"Volk '{map.Peoples[i].Id}': Heimatgebiet {homelandCells} Zellen.");
            }
        }
    }

    private static string Percent(int part, int total) => total == 0 ? "0 %" : $"{100.0 * part / total:0.0} %";

    private sealed record Terrain(int Width, int Height, byte[] Zone, bool[] Land, float[] CoastNear, float[] RiverNear);
}
