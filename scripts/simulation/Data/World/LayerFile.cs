using System;
using System.Buffers;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;

namespace TiefBerg.Data.World;

// Ebenen-Datei der Natur-Ebenen (Gestein, Lagerstätten, Böden, Bodenfeuchte, Pflanzen, Landmaske;
// lore/welt_konfiguration.md Abschnitt 8): viele Layer gleicher Größe, je ein Byte je Zelle, einzeln mit zlib gepackt.
// Die Layer bleiben gepackt im Speicher (entpackt wären es rund 900 MB) und werden nur so weit entpackt wie nötig
// (Design-Referenz Abschnitt 4). Unveränderlich und damit von mehreren Threads lesbar.
public sealed class LayerFile
{
    private readonly byte[] _bytes;
    private readonly int[] _offsets;
    private readonly int[] _lengths;
    private readonly Dictionary<string, int> _indexById;

    internal LayerFile(string name, int width, int height, IReadOnlyList<string> layerIds, byte[] bytes, int[] offsets,
        int[] lengths)
    {
        Name = name;
        Width = width;
        Height = height;
        LayerIds = layerIds;
        _bytes = bytes;
        _offsets = offsets;
        _lengths = lengths;
        _indexById = new Dictionary<string, int>(layerIds.Count);
        for (int i = 0; i < layerIds.Count; i++)
        {
            _indexById.Add(layerIds[i], i);
        }
    }

    public string Name { get; }

    public int Width { get; }

    public int Height { get; }

    // Reihenfolge wie in der Datei
    public IReadOnlyList<string> LayerIds { get; }

    // −1, wenn es den Layer nicht gibt
    public int IndexOf(string layerId) => _indexById.GetValueOrDefault(layerId, -1);

    // Liest einzelne Zellen (Index y * Width + x) eines Layers. Entpackt wird nur bis zur höchsten verlangten Zelle,
    // weil die Daten zeilenweise von Norden nach Süden liegen.
    // Wirft InvalidDataException, wenn die gepackten Daten defekt oder zu kurz sind.
    public void ReadCells(int layer, ReadOnlySpan<int> cells, Span<byte> values)
    {
        if ((uint)layer >= (uint)_offsets.Length)
        {
            throw new ArgumentOutOfRangeException(nameof(layer), $"{Name}: Layer {layer} gibt es nicht.");
        }

        if (values.Length < cells.Length)
        {
            throw new ArgumentException($"{Name}: Platz für {values.Length} statt {cells.Length} Werte.");
        }

        int last = -1;
        foreach (int cell in cells)
        {
            if ((uint)cell >= (uint)(Width * Height))
            {
                throw new ArgumentOutOfRangeException(nameof(cells), $"{Name}: Zelle {cell} liegt außerhalb.");
            }

            last = Math.Max(last, cell);
        }

        if (last < 0)
        {
            return;
        }

        byte[] buffer = ArrayPool<byte>.Shared.Rent(last + 1);
        try
        {
            using (var packed = new MemoryStream(_bytes, _offsets[layer], _lengths[layer], writable: false))
            using (var stream = new ZLibStream(packed, CompressionMode.Decompress))
            {
                stream.ReadExactly(buffer, 0, last + 1);
            }

            for (int i = 0; i < cells.Length; i++)
            {
                values[i] = buffer[cells[i]];
            }
        }
        catch (EndOfStreamException e)
        {
            throw new InvalidDataException($"{Name}: Layer {LayerIds[layer]} ist zu kurz.", e);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }
}
