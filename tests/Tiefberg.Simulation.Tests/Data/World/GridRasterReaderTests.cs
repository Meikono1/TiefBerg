using System;
using System.Buffers.Binary;
using System.Text;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Data;
using TiefBerg.Data.World;

namespace TiefBerg.Tests.Data.World;

public class GridRasterReaderTests
{
    private readonly GridRasterReader _reader = new(NullLogger<GridRasterReader>.Instance);

    [Fact]
    public void TryRead_Valid_ReadsCells()
    {
        byte[] bytes = Raster("TBKZ", 1, 3, 2, new byte[] { 1, 2, 3, 4, 5, 6 });

        GridRaster? raster = _reader.TryRead(new BinaryFile("k.bin", bytes), GridRasterReader.ZonesMagic);

        Assert.NotNull(raster);
        Assert.Equal(3, raster.Width);
        Assert.Equal(2, raster.Height);
        Assert.Equal(6, raster[2, 1]);
    }

    [Theory]
    [InlineData("TBFL", 1, 3, 2, 6)]
    [InlineData("TBKZ", 2, 3, 2, 6)]
    [InlineData("TBKZ", 1, 3, 2, 5)]
    [InlineData("TBKZ", 1, 0, 2, 0)]
    public void TryRead_Invalid_ReturnsNull(string magic, int version, int width, int height, int cells)
    {
        byte[] bytes = Raster(magic, version, width, height, new byte[cells]);

        Assert.Null(_reader.TryRead(new BinaryFile("k.bin", bytes), GridRasterReader.ZonesMagic));
    }

    [Fact]
    public void Sample_OtherSize_TakesCellUnderCentre()
    {
        var raster = new GridRaster(4, 2, new byte[] { 1, 2, 3, 4, 5, 6, 7, 8 });

        // Zelle (1, 0) eines 2 x 1 Rasters liegt mitten über Zelle (3, 1) des 4 x 2 Rasters
        Assert.Equal(8, raster.Sample(1, 0, 2, 1));
    }

    private static byte[] Raster(string magic, int version, int width, int height, byte[] cells)
    {
        var bytes = new byte[16 + cells.Length];
        Encoding.ASCII.GetBytes(magic).CopyTo(bytes, 0);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(4), (uint)version);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(8), (uint)width);
        BinaryPrimitives.WriteUInt32LittleEndian(bytes.AsSpan(12), (uint)height);
        cells.CopyTo(bytes, 16);
        return bytes;
    }
}
