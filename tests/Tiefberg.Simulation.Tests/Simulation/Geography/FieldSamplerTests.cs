using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Data;
using TiefBerg.Data.World;
using TiefBerg.Simulation.Geography;
using TiefBerg.Tests.Data.World;

namespace TiefBerg.Tests.Simulation.Geography;

public class FieldSamplerTests
{
    private const byte L = 1;
    private const byte S = 20;

    private static readonly ZoneTable Zones = new([
        new ZoneDefinition(L, "ZONE_LAND", ZoneKind.Land),
        new ZoneDefinition(S, "ZONE_SEA", ZoneKind.Sea),
    ]);

    // Zellen 4 x 2: Werte je Zelle für die Mengen-Quelle
    private static readonly byte[] Amounts = [10, 20, 30, 40, 50, 60, 70, 80];

    [Fact]
    public void Sample_InlandField_InterpolatesBilinear()
    {
        FieldSampler sampler = Create([L, L, S, S, L, L, L, S], LandMask(all: true));

        // Feld (1, 1) im 8 x 4 Raster: Mitte bei Zelle (0,25 | 0,25)
        FieldValues? field = sampler.Sample(1, 1);

        Assert.NotNull(field);
        Assert.True(field.IsLand);
        float expected = 0.5625f * 10 + 0.1875f * 20 + 0.1875f * 50 + 0.0625f * 60;
        Assert.Equal(expected, field.Amounts["menge"], 3);
        Assert.Equal(expected, field.Amounts[FieldValues.Key("ebene", "a")], 3);
    }

    [Fact]
    public void Sample_CoastField_IgnoresSeaCells()
    {
        FieldSampler sampler = Create([L, L, S, S, L, L, L, S], LandMask(all: true));

        // Feld (3, 1): Zellen (1,0) L, (2,0) S, (1,1) L, (2,1) L; das Meer zählt nicht, Gewichte neu verteilt
        FieldValues? field = sampler.Sample(3, 1);

        Assert.NotNull(field);
        float expected = (0.5625f * 20 + 0.1875f * 60 + 0.0625f * 70) / 0.8125f;
        Assert.Equal(expected, field.Amounts["menge"], 3);
    }

    [Fact]
    public void Sample_Category_TakesLargestWeight()
    {
        FieldSampler sampler = Create([L, L, S, S, L, L, L, S], LandMask(all: true),
            categories: [1, 5, 9, 9, 3, 7, 7, 9]);

        // Wie oben: 5 mit Gewicht 0,69 gegen 7 mit 0,31
        FieldValues? field = sampler.Sample(3, 1);

        Assert.NotNull(field);
        Assert.Equal(5, field.Categories["kategorie"]);
        Assert.Equal(5, field.Categories[FieldValues.Key("ebene", "haupt")]);
    }

    [Fact]
    public void Sample_NoMatchingCorner_TakesNearestCellAcrossDateLine()
    {
        FieldSampler sampler = Create([L, S, S, S, L, S, S, S], LandMask(all: true));

        // Feld (5, 1): alle vier Zellen Meer; nächste Landzelle ist (0, 0), über die Datumsgrenze näher als von Westen
        FieldValues? field = sampler.Sample(5, 1);

        Assert.NotNull(field);
        Assert.Equal(10, field.Amounts["menge"], 3);
    }

    [Fact]
    public void Sample_WaterField_UsesWaterCells()
    {
        FieldSampler sampler = Create([L, L, S, S, L, L, L, S], LandMask(all: false));

        FieldValues? field = sampler.Sample(3, 1);

        Assert.NotNull(field);
        Assert.False(field.IsLand);
        Assert.Equal(30, field.Amounts["menge"], 3);
    }

    [Theory]
    [InlineData(-1, 0)]
    [InlineData(8, 0)]
    [InlineData(0, 4)]
    public void Sample_Outside_ReturnsNull(int x, int y)
    {
        FieldSampler sampler = Create([L, L, S, S, L, L, L, S], LandMask(all: true));

        Assert.Null(sampler.Sample(x, y));
    }

    [Fact]
    public void TryCreate_SourceOfOtherSize_ReturnsNull()
    {
        var zones = new GridRaster(4, 2, [L, L, S, S, L, L, L, S]);
        var wrong = new FieldRasterSource("menge", new GridRaster(2, 2, new byte[4]), FieldValueKind.Amount);

        Assert.Null(FieldSampler.TryCreate(NullLogger<FieldSampler>.Instance, LandMask(all: true), zones, Zones,
            [wrong], []));
    }

    [Fact]
    public void Sample_ProjectData_ReturnsAllLayers()
    {
        // Die mitgelieferten Weltdaten müssen zusammenpassen; Feld am Mittelpunkt der Menschen (102° W, 45° N)
        FieldSampler sampler = ProjectSampler(out int layerCount);

        FieldValues? field = sampler.Sample(1775, 1024);

        Assert.NotNull(field);
        Assert.True(field.IsLand);
        Assert.Equal(layerCount, field.Categories.Count + field.Amounts.Count);
        Assert.Contains(FieldValues.Key("boeden", "hauptboden"), field.Categories.Keys);
        Assert.Contains(field.Amounts, pair => pair.Key.StartsWith("pflanzen/", StringComparison.Ordinal) && pair.Value > 0);
    }

    private static FieldSampler Create(byte[] zoneCells, LayerFile landMask, byte[]? categories = null)
    {
        var zones = new GridRaster(4, 2, zoneCells);
        byte[] categoryCells = categories ?? new byte[8];
        byte[] file = LayerFileReaderTests.Build("TBLA", 1, 4, 2, ("a", Amounts), ("haupt", categoryCells));
        LayerFile? layers = new LayerFileReader(NullLogger<LayerFileReader>.Instance)
            .TryRead(new BinaryFile("e.bin", file), "TBLA");
        Assert.NotNull(layers);

        FieldSampler? sampler = FieldSampler.TryCreate(NullLogger<FieldSampler>.Instance, landMask, zones, Zones,
            [
                new FieldRasterSource("menge", new GridRaster(4, 2, Amounts), FieldValueKind.Amount),
                new FieldRasterSource("kategorie", new GridRaster(4, 2, categoryCells), FieldValueKind.Category),
            ],
            [new FieldLayerSource("ebene", layers, new HashSet<string> { "haupt" })]);
        Assert.NotNull(sampler);
        return sampler;
    }

    // Spielfeld 8 x 4, doppelt so fein wie die Zellen
    private static LayerFile LandMask(bool all)
    {
        byte[] cells = Enumerable.Repeat(all ? (byte)255 : (byte)0, 32).ToArray();
        LayerFile? mask = new LayerFileReader(NullLogger<LayerFileReader>.Instance)
            .TryRead(new BinaryFile("m.bin", LayerFileReaderTests.Build("TBLM", 1, 8, 4, ("land", cells))),
                LayerFileReader.LandMaskMagic);
        Assert.NotNull(mask);
        return mask;
    }

    private static FieldSampler ProjectSampler(out int layerCount)
    {
        string world = Path.Combine(FindProjectRoot(), "data", "world");
        var rasters = new GridRasterReader(NullLogger<GridRasterReader>.Instance);
        var layerFiles = new LayerFileReader(NullLogger<LayerFileReader>.Instance);
        GridRaster Raster(string name, string magic) =>
            rasters.TryRead(new BinaryFile(name, File.ReadAllBytes(Path.Combine(world, name))), magic)
            ?? throw new InvalidDataException(name);
        LayerFile Layers(string name, string magic) =>
            layerFiles.TryRead(new BinaryFile(name, File.ReadAllBytes(Path.Combine(world, name))), magic)
            ?? throw new InvalidDataException(name);

        ZoneTable? zoneTable = new WorldTableLoader(NullLogger<WorldTableLoader>.Instance)
            .TryLoadZones(new TextFile("klimazonen.yaml", File.ReadAllText(Path.Combine(world, "klimazonen.yaml"))));
        Assert.NotNull(zoneTable);
        GridRaster zones = Raster("klimazonen.bin", GridRasterReader.ZonesMagic);
        FieldRasterSource[] rasterSources =
        [
            new("klimazone", zones, FieldValueKind.Category),
            new("koeppen", Raster("koeppen.bin", GridRasterReader.KoeppenMagic), FieldValueKind.Category),
            new("gesteinsraum", Raster("gesteinsraeume.bin", GridRasterReader.RockRoomsMagic), FieldValueKind.Category),
            new("fluss", Raster("fluesse.bin", GridRasterReader.RiversMagic), FieldValueKind.Category),
            new("gezeiten", Raster("gezeiten.bin", GridRasterReader.TidesMagic), FieldValueKind.Amount),
        ];
        var none = new HashSet<string>();
        FieldLayerSource[] layerSources =
        [
            new("gestein", Layers("gestein.bin", LayerFileReader.RocksMagic), none),
            new("lagerstaetten", Layers("lagerstaetten.bin", LayerFileReader.DepositsMagic), none),
            new("boeden", Layers("boeden.bin", LayerFileReader.SoilsMagic), new HashSet<string> { "hauptboden" }),
            new("bodenfeuchte", Layers("bodenfeuchte.bin", LayerFileReader.SoilMoistureMagic), none),
            new("pflanzen", Layers("pflanzen.bin", LayerFileReader.PlantsMagic), none),
        ];
        layerCount = rasterSources.Length + layerSources.Sum(source => source.File.LayerIds.Count);

        FieldSampler? sampler = FieldSampler.TryCreate(NullLogger<FieldSampler>.Instance,
            Layers("landmaske.bin", LayerFileReader.LandMaskMagic), zones, zoneTable, rasterSources, layerSources);
        Assert.NotNull(sampler);
        return sampler;
    }

    private static string FindProjectRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null && !File.Exists(Path.Combine(directory.FullName, "project.godot")))
        {
            directory = directory.Parent;
        }

        Assert.NotNull(directory);
        return directory.FullName;
    }
}
