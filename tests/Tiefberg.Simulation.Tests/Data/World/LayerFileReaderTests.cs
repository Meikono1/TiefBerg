using System;
using System.Buffers.Binary;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Data;
using TiefBerg.Data.World;

namespace TiefBerg.Tests.Data.World;

public class LayerFileReaderTests
{
    private readonly LayerFileReader _reader = new(NullLogger<LayerFileReader>.Instance);

    [Fact]
    public void TryRead_Valid_ReadsTableAndCells()
    {
        byte[] bytes = Build("TBPF", 1, 3, 2, ("eiche", [1, 2, 3, 4, 5, 6]), ("kiefer", [7, 8, 9, 10, 11, 12]));

        LayerFile? file = _reader.TryRead(new BinaryFile("p.bin", bytes), LayerFileReader.PlantsMagic);

        Assert.NotNull(file);
        Assert.Equal(3, file.Width);
        Assert.Equal(2, file.Height);
        Assert.Equal(["eiche", "kiefer"], file.LayerIds);
        Assert.Equal(1, file.IndexOf("kiefer"));
        Assert.Equal(-1, file.IndexOf("buche"));
        var values = new byte[2];
        file.ReadCells(1, [5, 0], values);
        Assert.Equal([12, 7], values);
    }

    [Theory]
    [InlineData("TBBO", 1, 3, 2)]
    [InlineData("TBPF", 2, 3, 2)]
    [InlineData("TBPF", 1, 0, 2)]
    public void TryRead_InvalidHeader_ReturnsNull(string magic, int version, int width, int height)
    {
        byte[] bytes = Build(magic, version, width, height, ("eiche", new byte[Math.Max(width * height, 1)]));

        Assert.Null(_reader.TryRead(new BinaryFile("p.bin", bytes), LayerFileReader.PlantsMagic));
    }

    [Fact]
    public void TryRead_DuplicateLayer_ReturnsNull()
    {
        byte[] bytes = Build("TBPF", 1, 2, 1, ("eiche", [1, 2]), ("eiche", [3, 4]));

        Assert.Null(_reader.TryRead(new BinaryFile("p.bin", bytes), LayerFileReader.PlantsMagic));
    }

    [Fact]
    public void TryRead_TruncatedOrTrailingBytes_ReturnsNull()
    {
        byte[] bytes = Build("TBPF", 1, 2, 1, ("eiche", [1, 2]));

        Assert.Null(_reader.TryRead(new BinaryFile("p.bin", bytes[..^1]), LayerFileReader.PlantsMagic));
        Assert.Null(_reader.TryRead(new BinaryFile("p.bin", [.. bytes, 0]), LayerFileReader.PlantsMagic));
        Assert.Null(_reader.TryRead(new BinaryFile("p.bin", bytes[..22]), LayerFileReader.PlantsMagic));
    }

    [Fact]
    public void ReadCells_LayerShorterThanGrid_Throws()
    {
        // Inhaltsverzeichnis stimmt, aber der gepackte Layer enthält nur 2 von 4 Zellen
        byte[] bytes = Build("TBPF", 1, 2, 2, ("eiche", [1, 2]));
        LayerFile? file = _reader.TryRead(new BinaryFile("p.bin", bytes), LayerFileReader.PlantsMagic);
        Assert.NotNull(file);

        var values = new byte[1];
        file.ReadCells(0, [1], values);
        Assert.Equal(2, values[0]);
        Assert.Throws<InvalidDataException>(() => file.ReadCells(0, [3], values));
    }

    // Ebenen-Datei wie tools/ebenen.py write_layer_file
    internal static byte[] Build(string magic, int version, int width, int height, params (string Id, byte[] Cells)[] layers)
    {
        var packed = new List<(byte[] Id, byte[] Data)>();
        foreach ((string id, byte[] cells) in layers)
        {
            using var target = new MemoryStream();
            using (var zlib = new ZLibStream(target, CompressionLevel.SmallestSize))
            {
                zlib.Write(cells);
            }

            packed.Add((Encoding.UTF8.GetBytes(id), target.ToArray()));
        }

        using var output = new MemoryStream();
        var number = new byte[4];
        output.Write(Encoding.ASCII.GetBytes(magic));
        foreach (int value in new[] { version, width, height, layers.Length })
        {
            BinaryPrimitives.WriteUInt32LittleEndian(number, (uint)value);
            output.Write(number);
        }

        foreach ((byte[] id, byte[] data) in packed)
        {
            BinaryPrimitives.WriteUInt16LittleEndian(number, (ushort)id.Length);
            output.Write(number, 0, 2);
            output.Write(id);
            BinaryPrimitives.WriteUInt32LittleEndian(number, (uint)data.Length);
            output.Write(number);
        }

        foreach ((_, byte[] data) in packed)
        {
            output.Write(data);
        }

        return output.ToArray();
    }
}
