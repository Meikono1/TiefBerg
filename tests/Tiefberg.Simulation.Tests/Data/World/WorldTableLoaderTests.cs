using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Data;
using TiefBerg.Data.World;

namespace TiefBerg.Tests.Data.World;

public class WorldTableLoaderTests
{
    private readonly WorldTableLoader _loader = new(NullLogger<WorldTableLoader>.Instance);

    [Fact]
    public void TryLoadZones_Valid_KnowsKinds()
    {
        ZoneTable? zones = _loader.TryLoadZones(new TextFile("k.yaml", """
            raster: k.bin
            zones:
              - { id: 1, name_key: Z_LAND, kind: land, color: [1, 2, 3] }
              - { id: 20, name_key: Z_SEA, kind: sea, color: [1, 2, 3] }
              - { id: 25, name_key: Z_LAKE, kind: inland_water, color: [1, 2, 3] }
            """));

        Assert.NotNull(zones);
        Assert.True(zones.IsLand(1));
        Assert.True(zones.IsSea(20));
        Assert.False(zones.IsLand(25));
        Assert.False(zones.IsSea(25));
        Assert.Null(zones.Find(7));
    }

    [Theory]
    [InlineData("zones:\n  - { id: 1, name_key: A, kind: swamp }")]
    [InlineData("zones:\n  - { id: 1, name_key: A, kind: land }\n  - { id: 1, name_key: B, kind: sea }")]
    [InlineData("zones:\n  - { id: 0, name_key: A, kind: land }")]
    [InlineData("zones:\n  - { id: 1, name_key: A, kind: land, height: 3 }")]
    public void TryLoadZones_Invalid_ReturnsNull(string yaml)
    {
        Assert.Null(_loader.TryLoadZones(new TextFile("k.yaml", yaml)));
    }

    [Fact]
    public void TryLoadRivers_SeasonalClass_DoesNotFlow()
    {
        RiverTable? rivers = _loader.TryLoadRivers(new TextFile("f.yaml", """
            classes:
              - { id: 1, name_key: R, seasonal: false, min_discharge_m3s: 150 }
              - { id: 4, name_key: R_DRY, seasonal: true, min_discharge_m3s: 0 }
            """));

        Assert.NotNull(rivers);
        Assert.True(rivers.IsFlowing(1));
        Assert.False(rivers.IsFlowing(4));
        Assert.False(rivers.IsFlowing(0));
    }
}
