using System.Collections.Generic;
using TiefBerg.Data.World;

namespace TiefBerg.Simulation.Geography;

// Art eines Werts im Spielfeld (Design-Referenz Abschnitt 4): Mengen werden gemischt, Kategorien nach Mehrheit gewählt
public enum FieldValueKind
{
    Amount,
    Category,
}

// Einzelraster als Quelle der Feldwerte (z. B. Köppen-Klassen, Flüsse, Tidenhub); Id ist zugleich der Schlüssel
public sealed record FieldRasterSource(string Id, GridRaster Raster, FieldValueKind Kind);

// Ebenen-Datei als Quelle; alle Layer sind Mengen außer CategoryLayers (z. B. hauptboden).
// Schlüssel der Werte: FieldValues.Key(Id, Layer).
public sealed record FieldLayerSource(string Id, LayerFile File, IReadOnlySet<string> CategoryLayers);

// Werte eines Spielfelds (Pixel des Rasters 8192 × 4096), berechnet von FieldSampler.
// Mengen stehen in der Einheit der Quelle: Anteile 0–255 (255 = ergiebigste Stelle des Planeten), Tidenhub in cm.
public sealed class FieldValues
{
    public FieldValues(int x, int y, bool isLand, IReadOnlyDictionary<string, byte> categories,
        IReadOnlyDictionary<string, float> amounts)
    {
        X = x;
        Y = y;
        IsLand = isLand;
        Categories = categories;
        Amounts = amounts;
    }

    public int X { get; }

    public int Y { get; }

    // Laut Meermaske; Binnengewässer kennt die Maske nicht
    public bool IsLand { get; }

    public IReadOnlyDictionary<string, byte> Categories { get; }

    public IReadOnlyDictionary<string, float> Amounts { get; }

    public static string Key(string sourceId, string layerId) => $"{sourceId}/{layerId}";
}
