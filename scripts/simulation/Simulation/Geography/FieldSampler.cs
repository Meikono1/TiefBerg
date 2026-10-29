using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using TiefBerg.Data.World;
using TiefBerg.Diagnostics;

namespace TiefBerg.Simulation.Geography;

// Berechnet die Werte eines Spielfelds aus den umliegenden Zellen der Weltdaten (Design-Referenz Abschnitt 4):
// bilinear zwischen den vier Zellen um die Feldmitte, aber nur über Zellen derselben Art wie das Feld (Land bzw.
// Wasser), damit das Meer Küstenfelder nicht verarmt. Kategorien nach dem größten Gewicht. Kein Zufall.
// Ob das Feld Land ist, sagt die Landmaske im Raster des Spielfelds (8192 × 4096).
// Unveränderliche Quellen, keine geteilten Puffer: von mehreren Threads gleichzeitig nutzbar.
public sealed class FieldSampler
{
    // O - Vorschlag (Abschnitt 4): Liegt keine der vier Zellen auf derselben Art, gilt die nächste bis zu diesem
    // Abstand (in Zellen, rund 185 km am Äquator)
    public const int MaxSearchCells = 10;
    public const string LandLayer = "land";

    private const int Corners = 4;

    private readonly ILogger<FieldSampler> _log;
    private readonly LayerFile _landMask;
    private readonly int _landLayer;
    private readonly GridRaster _zones;
    private readonly ZoneTable _zoneTable;
    private readonly IReadOnlyList<FieldRasterSource> _rasters;
    private readonly IReadOnlyList<FieldLayerSource> _layers;

    private FieldSampler(ILogger<FieldSampler> log, LayerFile landMask, int landLayer, GridRaster zones,
        ZoneTable zoneTable, IReadOnlyList<FieldRasterSource> rasters, IReadOnlyList<FieldLayerSource> layers)
    {
        _log = log;
        _landMask = landMask;
        _landLayer = landLayer;
        _zones = zones;
        _zoneTable = zoneTable;
        _rasters = rasters;
        _layers = layers;
    }

    // Breite und Höhe des Spielfeldrasters
    public int Width => _landMask.Width;

    public int Height => _landMask.Height;

    // Prüft, dass alle Quellen auf dem Raster der Klimazonen liegen und die Schlüssel eindeutig sind.
    // Fehler im Aufbau werden geloggt; Ergebnis dann null.
    public static FieldSampler? TryCreate(ILogger<FieldSampler> log, LayerFile landMask, GridRaster zones,
        ZoneTable zoneTable, IReadOnlyList<FieldRasterSource> rasters, IReadOnlyList<FieldLayerSource> layers)
    {
        int landLayer = landMask.IndexOf(LandLayer);
        if (landLayer < 0)
        {
            log.Error($"Landmaske {landMask.Name}: Layer '{LandLayer}' fehlt.");
            return null;
        }

        if (landMask.Width < zones.Width || landMask.Height < zones.Height)
        {
            log.Error($"Landmaske {landMask.Width} x {landMask.Height} ist gröber als die Zellen {zones.Width} x {zones.Height}.");
            return null;
        }

        var ids = new HashSet<string>(StringComparer.Ordinal);
        foreach (FieldRasterSource source in rasters)
        {
            if (!ids.Add(source.Id) || source.Raster.Width != zones.Width || source.Raster.Height != zones.Height)
            {
                log.Error($"Quelle {source.Id}: doppelt oder {source.Raster.Width} x {source.Raster.Height} statt {zones.Width} x {zones.Height}.");
                return null;
            }
        }

        foreach (FieldLayerSource source in layers)
        {
            if (!ids.Add(source.Id) || source.File.Width != zones.Width || source.File.Height != zones.Height)
            {
                log.Error($"Quelle {source.Id}: doppelt oder {source.File.Width} x {source.File.Height} statt {zones.Width} x {zones.Height}.");
                return null;
            }

            foreach (string layer in source.CategoryLayers)
            {
                if (source.File.IndexOf(layer) < 0)
                {
                    log.Error($"Quelle {source.Id}: Kategorie-Layer '{layer}' fehlt in {source.File.Name}.");
                    return null;
                }
            }
        }

        log.Info($"Spielfeld {landMask.Width} x {landMask.Height} über Zellen {zones.Width} x {zones.Height}: {rasters.Count} Raster, {layers.Count} Ebenen-Dateien.");
        return new FieldSampler(log, landMask, landLayer, zones, zoneTable, rasters, layers);
    }

    // Werte des Felds (x, y) im Raster des Spielfelds; null, wenn das Feld außerhalb liegt, keine Bezugszelle hat
    // oder eine Quelle defekt ist (geloggt).
    public FieldValues? Sample(int x, int y)
    {
        using (Perf.Measure())
        {
            if ((uint)x >= (uint)Width || (uint)y >= (uint)Height)
            {
                _log.Error($"Feld ({x}, {y}) liegt außerhalb von {Width} x {Height}.");
                return null;
            }

            try
            {
                return SampleInside(x, y);
            }
            catch (InvalidDataException e)
            {
                _log.Error($"Feld ({x}, {y}): Weltdaten defekt.", e);
                return null;
            }
        }
    }

    private FieldValues? SampleInside(int x, int y)
    {
        Span<byte> land = stackalloc byte[1];
        _landMask.ReadCells(_landLayer, [y * Width + x], land);
        bool isLand = land[0] > 127;

        Span<int> cells = stackalloc int[Corners];
        Span<float> weights = stackalloc float[Corners];
        int count = CornerCells(x, y, isLand, cells, weights);
        if (count == 0)
        {
            count = NearestCell(x, y, isLand, cells, weights);
            if (count == 0)
            {
                _log.Warn($"Feld ({x}, {y}): keine Zelle der Art {(isLand ? "Land" : "Wasser")} bis {MaxSearchCells} Zellen, keine Werte.");
                return null;
            }
        }

        cells = cells[..count];
        weights = weights[..count];
        var categories = new Dictionary<string, byte>();
        var amounts = new Dictionary<string, float>();
        Span<byte> values = stackalloc byte[Corners];
        foreach (FieldRasterSource source in _rasters)
        {
            for (int i = 0; i < count; i++)
            {
                values[i] = source.Raster[cells[i] % _zones.Width, cells[i] / _zones.Width];
            }

            Store(source.Id, source.Kind, values[..count], weights, categories, amounts);
        }

        foreach (FieldLayerSource source in _layers)
        {
            for (int layer = 0; layer < source.File.LayerIds.Count; layer++)
            {
                string layerId = source.File.LayerIds[layer];
                source.File.ReadCells(layer, cells, values);
                FieldValueKind kind = source.CategoryLayers.Contains(layerId) ? FieldValueKind.Category : FieldValueKind.Amount;
                Store(FieldValues.Key(source.Id, layerId), kind, values[..count], weights, categories, amounts);
            }
        }

        _log.Info($"Feld ({x}, {y}) {(isLand ? "Land" : "Wasser")}: {count} Bezugszellen, {categories.Count} Kategorien, {amounts.Count} Mengen.");
        return new FieldValues(x, y, isLand, categories, amounts);
    }

    // Die vier Zellen um die Feldmitte mit bilinearen Gewichten, nur Zellen der passenden Art, Gewichte normiert.
    // Ost-West läuft die Karte um, an den Polen wird die Randzeile wiederholt.
    private int CornerCells(int x, int y, bool isLand, Span<int> cells, Span<float> weights)
    {
        double cx = (x + 0.5) * _zones.Width / Width - 0.5;
        double cy = (y + 0.5) * _zones.Height / Height - 0.5;
        int x0 = (int)Math.Floor(cx);
        int y0 = (int)Math.Floor(cy);
        double fx = cx - x0;
        double fy = cy - y0;
        int xa = Wrap(x0);
        int xb = Wrap(x0 + 1);
        int ya = Math.Clamp(y0, 0, _zones.Height - 1);
        int yb = Math.Clamp(y0 + 1, 0, _zones.Height - 1);

        ReadOnlySpan<int> cornerX = [xa, xb, xa, xb];
        ReadOnlySpan<int> cornerY = [ya, ya, yb, yb];
        ReadOnlySpan<double> cornerWeight = [(1 - fx) * (1 - fy), fx * (1 - fy), (1 - fx) * fy, fx * fy];
        int count = 0;
        double total = 0;
        for (int i = 0; i < Corners; i++)
        {
            if (cornerWeight[i] <= 0 || _zoneTable.IsLand(_zones[cornerX[i], cornerY[i]]) != isLand)
            {
                continue;
            }

            cells[count] = cornerY[i] * _zones.Width + cornerX[i];
            weights[count] = (float)cornerWeight[i];
            total += cornerWeight[i];
            count++;
        }

        for (int i = 0; i < count; i++)
        {
            weights[i] = (float)(weights[i] / total);
        }

        return count;
    }

    // Nächste Zelle der passenden Art um die Feldmitte (Insel oder Bucht kleiner als eine Zelle).
    // Bei gleichem Abstand gewinnt die zuerst gefundene (Norden vor Süden, Westen vor Osten).
    private int NearestCell(int x, int y, bool isLand, Span<int> cells, Span<float> weights)
    {
        double cx = (x + 0.5) * _zones.Width / Width - 0.5;
        double cy = (y + 0.5) * _zones.Height / Height - 0.5;
        int centerX = (int)Math.Round(cx);
        int centerY = (int)Math.Round(cy);
        double best = double.MaxValue;
        int bestCell = -1;
        for (int dy = -MaxSearchCells; dy <= MaxSearchCells; dy++)
        {
            int cellY = centerY + dy;
            if (cellY < 0 || cellY >= _zones.Height)
            {
                continue;
            }

            for (int dx = -MaxSearchCells; dx <= MaxSearchCells; dx++)
            {
                int cellX = Wrap(centerX + dx);
                double distance = Math.Sqrt((centerX + dx - cx) * (centerX + dx - cx) + (cellY - cy) * (cellY - cy));
                if (distance < best && distance <= MaxSearchCells && _zoneTable.IsLand(_zones[cellX, cellY]) == isLand)
                {
                    best = distance;
                    bestCell = cellY * _zones.Width + cellX;
                }
            }
        }

        if (bestCell < 0)
        {
            return 0;
        }

        _log.Debug($"Feld ({x}, {y}): keine der vier Zellen passt, nächste Zelle in {best:F1} Zellen Abstand.");
        cells[0] = bestCell;
        weights[0] = 1f;
        return 1;
    }

    private int Wrap(int cellX) => ((cellX % _zones.Width) + _zones.Width) % _zones.Width;

    private static void Store(string key, FieldValueKind kind, ReadOnlySpan<byte> values, ReadOnlySpan<float> weights,
        Dictionary<string, byte> categories, Dictionary<string, float> amounts)
    {
        if (kind == FieldValueKind.Amount)
        {
            float sum = 0;
            for (int i = 0; i < values.Length; i++)
            {
                sum += weights[i] * values[i];
            }

            amounts[key] = sum;
            return;
        }

        // Größtes Gesamtgewicht je Wert; bei Gleichstand der zuerst vorkommende
        byte chosen = values[0];
        float chosenWeight = -1;
        for (int i = 0; i < values.Length; i++)
        {
            float weight = 0;
            for (int j = 0; j < values.Length; j++)
            {
                if (values[j] == values[i])
                {
                    weight += weights[j];
                }
            }

            if (weight > chosenWeight)
            {
                chosen = values[i];
                chosenWeight = weight;
            }
        }

        categories[key] = chosen;
    }
}
