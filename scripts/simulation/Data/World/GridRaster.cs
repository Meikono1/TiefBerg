using System;

namespace TiefBerg.Data.World;

// Zellraster der Weltkarte mit einem Byte je Zelle (Klimazonen, Flüsse; lore/welt_konfiguration.md Abschnitt 5, 7).
// Zeilen von Norden nach Süden, Spalten ab 180° W; die Karte läuft an der Datumsgrenze horizontal um.
public sealed class GridRaster
{
    private readonly byte[] _cells;

    public GridRaster(int width, int height, byte[] cells)
    {
        if (width <= 0 || height <= 0 || cells.Length != width * height)
        {
            throw new ArgumentException($"Raster {width} x {height} passt nicht zu {cells.Length} Zellen.");
        }

        Width = width;
        Height = height;
        _cells = cells;
    }

    public int Width { get; }

    public int Height { get; }

    public byte this[int x, int y] => _cells[y * Width + x];

    // Zelle dieses Rasters an der Mitte einer Zelle eines anderen Rasters gleicher Ausdehnung (nächster Nachbar)
    public byte Sample(int x, int y, int otherWidth, int otherHeight)
    {
        int sx = (int)((x + 0.5) * Width / otherWidth);
        int sy = (int)((y + 0.5) * Height / otherHeight);
        return _cells[Math.Min(sy, Height - 1) * Width + Math.Min(sx, Width - 1)];
    }
}
