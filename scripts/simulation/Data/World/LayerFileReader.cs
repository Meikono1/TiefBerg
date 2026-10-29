using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;

namespace TiefBerg.Data.World;

// Liest Ebenen-Dateien aus tools/ebenen.py (lore/welt_konfiguration.md Abschnitt 8):
// 4 Byte Kennung, uint32 Version, Breite, Höhe, Anzahl Layer (Little Endian), dann je Layer die ID (uint16 Länge +
// UTF-8) und die Länge der gepackten Daten (uint32), danach die gepackten Layer in derselben Reihenfolge.
// Geprüft werden Kopf und Inhaltsverzeichnis; die gepackten Daten erst beim Lesen (LayerFile.ReadCells).
// Defekte Dateien werden geloggt und abgelehnt, es gibt keinen Ersatz.
public sealed class LayerFileReader
{
    public const string RocksMagic = "TBGS";
    public const string DepositsMagic = "TBLA";
    public const string SoilsMagic = "TBBO";
    public const string SoilMoistureMagic = "TBBF";
    public const string PlantsMagic = "TBPF";
    public const string LandMaskMagic = "TBLM";

    private const int SupportedVersion = 1;
    private const int HeaderLength = 20;

    private readonly ILogger<LayerFileReader> _log;

    public LayerFileReader(ILogger<LayerFileReader> log)
    {
        _log = log;
    }

    public LayerFile? TryRead(BinaryFile file, string expectedMagic)
    {
        byte[] bytes = file.Bytes;
        if (bytes.Length < HeaderLength)
        {
            _log.Error($"Ebenen-Datei {file.Name}: Datei zu kurz ({bytes.Length} Bytes).");
            return null;
        }

        string magic = Encoding.ASCII.GetString(bytes, 0, 4);
        if (magic != expectedMagic)
        {
            _log.Error($"Ebenen-Datei {file.Name}: Kennung '{magic}' statt '{expectedMagic}'.");
            return null;
        }

        uint version = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(4));
        uint width = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(8));
        uint height = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(12));
        uint count = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(16));
        if (version != SupportedVersion)
        {
            _log.Error($"Ebenen-Datei {file.Name}: Version {version} wird nicht unterstützt (erwartet {SupportedVersion}).");
            return null;
        }

        if (width == 0 || height == 0 || (long)width * height > Array.MaxLength || count == 0 || count > bytes.Length)
        {
            _log.Error($"Ebenen-Datei {file.Name}: {width} x {height} Zellen mit {count} Layern ist ungültig.");
            return null;
        }

        return ReadTable(file, (int)width, (int)height, (int)count);
    }

    private LayerFile? ReadTable(BinaryFile file, int width, int height, int count)
    {
        byte[] bytes = file.Bytes;
        var ids = new List<string>(count);
        var seen = new HashSet<string>(StringComparer.Ordinal);
        var lengths = new int[count];
        int position = HeaderLength;
        try
        {
            for (int i = 0; i < count; i++)
            {
                int idLength = BinaryPrimitives.ReadUInt16LittleEndian(bytes.AsSpan(position));
                string id = new UTF8Encoding(false, true).GetString(bytes, position + 2, idLength);
                uint length = BinaryPrimitives.ReadUInt32LittleEndian(bytes.AsSpan(position + 2 + idLength));
                position += 6 + idLength;
                if (id.Length == 0 || !seen.Add(id) || length == 0 || length > bytes.Length)
                {
                    _log.Error($"Ebenen-Datei {file.Name}: Layer {i} ('{id}') leer, doppelt oder mit Länge {length}.");
                    return null;
                }

                ids.Add(id);
                lengths[i] = (int)length;
            }
        }
        catch (Exception e) when (e is ArgumentOutOfRangeException or ArgumentException)
        {
            // ArgumentOutOfRange: Inhaltsverzeichnis endet mitten im Eintrag; Argument: ungültiges UTF-8
            _log.Error($"Ebenen-Datei {file.Name}: Inhaltsverzeichnis ist abgeschnitten oder defekt.", e);
            return null;
        }

        var offsets = new int[count];
        long offset = position;
        for (int i = 0; i < count; i++)
        {
            offsets[i] = (int)Math.Min(offset, int.MaxValue);
            offset += lengths[i];
        }

        if (offset != bytes.Length)
        {
            _log.Error($"Ebenen-Datei {file.Name}: Layer brauchen {offset} Bytes, Datei hat {bytes.Length}.");
            return null;
        }

        _log.Info($"Ebenen-Datei {file.Name} gelesen: {width} x {height}, {count} Layer.");
        return new LayerFile(file.Name, width, height, ids, bytes, offsets, lengths);
    }
}
