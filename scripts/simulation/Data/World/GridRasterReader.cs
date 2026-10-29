using System;
using System.Buffers.Binary;
using System.Text;
using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;

namespace TiefBerg.Data.World;

// Liest Zellraster aus tools/klimazonen.py, tools/fluesse.py, tools/gesteinsraeume.py und tools/gezeiten.py
// (lore/welt_konfiguration.md Abschnitt 5, 7, 9, 11):
// 4 Byte Kennung, uint32 Version, Breite, Höhe (Little Endian), danach ein Byte je Zelle.
// Defekte Dateien werden geloggt und abgelehnt; es gibt keinen Ersatz, weil die Welt ohne sie nicht entstehen kann.
public sealed class GridRasterReader
{
    public const string ZonesMagic = "TBKZ";
    public const string RiversMagic = "TBFL";
    public const string KoeppenMagic = "TBKP";
    public const string RockRoomsMagic = "TBGR";
    public const string TidesMagic = "TBGZ";

    private const int SupportedVersion = 1;
    private const int HeaderLength = 16;

    private readonly ILogger<GridRasterReader> _log;

    public GridRasterReader(ILogger<GridRasterReader> log)
    {
        _log = log;
    }

    public GridRaster? TryRead(BinaryFile file, string expectedMagic)
    {
        byte[] bytes = file.Bytes;
        if (bytes.Length < HeaderLength)
        {
            _log.Error($"Raster {file.Name}: Datei zu kurz ({bytes.Length} Bytes).");
            return null;
        }

        string magic = Encoding.ASCII.GetString(bytes, 0, 4);
        if (magic != expectedMagic)
        {
            _log.Error($"Raster {file.Name}: Kennung '{magic}' statt '{expectedMagic}'.");
            return null;
        }

        uint version = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4));
        uint width = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8));
        uint height = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12));
        if (version != SupportedVersion)
        {
            _log.Error($"Raster {file.Name}: Version {version} wird nicht unterstützt (erwartet {SupportedVersion}).");
            return null;
        }

        long expected = HeaderLength + (long)width * height;
        if (width == 0 || height == 0 || bytes.Length != expected)
        {
            _log.Error($"Raster {file.Name}: {width} x {height} Zellen erwarten {expected} Bytes, Datei hat {bytes.Length}.");
            return null;
        }

        var cells = new byte[width * height];
        Array.Copy(bytes, HeaderLength, cells, 0, cells.Length);
        _log.Info($"Raster {file.Name} gelesen: {width} x {height}.");
        return new GridRaster((int)width, (int)height, cells);
    }
}
