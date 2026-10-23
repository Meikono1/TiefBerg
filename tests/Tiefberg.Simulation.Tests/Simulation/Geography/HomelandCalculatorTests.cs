using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Data.Peoples;
using TiefBerg.Data.World;
using TiefBerg.Simulation.Geography;

namespace TiefBerg.Tests.Simulation.Geography;

public class HomelandCalculatorTests
{
    private const byte Land = 5;
    private const byte Sea = 20;

    // 36 x 18 Zellen à 10°; zwei Inseln, dazwischen drei Zellen Meer
    private static readonly string[] TwoIslands =
    {
        "....................................",
        "....................................",
        "....................................",
        "....................................",
        "..###########...###########.........",
        "..###########...###########.........",
        "..###########...###########.........",
        "..###########...###########.........",
        "..###########...###########.........",
        "..###########...###########.........",
        "..###########...###########.........",
        "..###########...###########.........",
        "..###########...###########.........",
        "..###########...###########.........",
        "....................................",
        "....................................",
        "....................................",
        "....................................",
    };

    // Landstreifen über die Datumsgrenze hinweg
    private static readonly string[] AcrossSeam =
    {
        "....................................",
        "....................................",
        "....................................",
        "....................................",
        "#######......................#######",
        "#######......................#######",
        "#######......................#######",
        "#######......................#######",
        "#######......................#######",
        "#######......................#######",
        "#######......................#######",
        "....................................",
        "....................................",
        "....................................",
        "....................................",
        "....................................",
        "....................................",
        "....................................",
    };

    private static readonly ZoneTable Zones = new(new[]
    {
        new ZoneDefinition(Land, "Z_LAND", ZoneKind.Land),
        new ZoneDefinition(Sea, "Z_SEA", ZoneKind.Sea),
    });

    private static readonly RiverTable Rivers = new(new[] { new RiverClass(1, "R", false, 150) });

    private static readonly HabitatProfile Steppe = new("steppe", new Dictionary<byte, double> { [Land] = 1.0 });

    private readonly HomelandCalculator _calculator = new(NullLogger<HomelandCalculator>.Instance);

    [Fact]
    public void Calculate_CentreOnIsland_InfluenceDoesNotCrossSea()
    {
        // Mittelpunkt auf der westlichen Insel, Radius reicht weit über die Meerenge
        HomelandMap map = Calculate(TwoIslands, People("a", -110, 0, 40));

        Assert.NotEmpty(map.HomelandCells(0));
        for (int y = 0; y < map.Height; y++)
        {
            for (int x = 16; x < 27; x++)
            {
                Assert.Equal(-1, map.CultureAt(x, y));
                Assert.Equal(HomelandArea.Wilderness, map.AreaAt(x, y));
            }
        }
    }

    [Fact]
    public void Calculate_CentreInSea_HasNoHomeland()
    {
        HomelandMap map = Calculate(TwoIslands, People("a", -35, 0, 40));

        Assert.Empty(map.HomelandCells(0));
    }

    [Fact]
    public void Calculate_NeighboursOfOneCulture_HomelandsOverlap()
    {
        HomelandMap map = Calculate(TwoIslands, People("a", -120, 0, 40), People("b", -90, 0, 40));

        IReadOnlyList<int> first = map.HomelandCells(0);
        IReadOnlyList<int> second = map.HomelandCells(1);
        Assert.NotEmpty(first);
        Assert.NotEmpty(second);
        Assert.NotEmpty(first.Intersect(second));
    }

    [Fact]
    public void Calculate_CentreAtSeam_ReachesOtherSide()
    {
        HomelandMap map = Calculate(AcrossSeam, People("a", -175, 15, 40));

        IReadOnlyList<int> cells = map.HomelandCells(0);
        Assert.Contains(cells, cell => cell % map.Width == 0);
        Assert.Contains(cells, cell => cell % map.Width == map.Width - 1);
    }

    [Fact]
    public void Calculate_SameInput_SameResult()
    {
        PeopleDefinition[] peoples = { People("a", -120, 0, 40), People("b", -90, 0, 40) };

        HomelandMap first = Calculate(TwoIslands, peoples);
        HomelandMap second = Calculate(TwoIslands, peoples);

        Assert.Equal(first.HomelandCells(0), second.HomelandCells(0));
        Assert.Equal(first.HomelandCells(1), second.HomelandCells(1));
        for (int y = 0; y < first.Height; y++)
        {
            for (int x = 0; x < first.Width; x++)
            {
                Assert.Equal(first.AreaAt(x, y), second.AreaAt(x, y));
            }
        }
    }

    private HomelandMap Calculate(string[] rows, params PeopleDefinition[] peoples)
    {
        int width = rows[0].Length;
        int height = rows.Length;
        byte[] cells = rows.SelectMany(row => row.Select(c => c == '#' ? Land : Sea)).ToArray();
        var zones = new GridRaster(width, height, cells);
        var rivers = new GridRaster(width, height, new byte[width * height]);
        var culture = new CultureDefinition("c", "C", new RgbColor(1, 2, 3), peoples);
        var data = new PeopleData(new[] { Steppe }, new[] { culture });
        return _calculator.Calculate(zones, Zones, rivers, Rivers, data, width, height);
    }

    private static PeopleDefinition People(string id, double longitude, double latitude, double radius) =>
        new(id, "P", "c", longitude, latitude, radius, new[] { Steppe }, false, false);
}
