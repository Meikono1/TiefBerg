using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Data;
using TiefBerg.Data.Peoples;
using TiefBerg.Data.World;

namespace TiefBerg.Tests.Data.Peoples;

public class PeopleDataLoaderTests
{
    private const string Profiles = """
        profiles:
          mountains: { 13: 1, 9: 0.55 }
          steppe: { 5: 1 }
        """;

    private static readonly ZoneTable Zones = new(new[]
    {
        new ZoneDefinition(5, "Z5", ZoneKind.Land),
        new ZoneDefinition(9, "Z9", ZoneKind.Land),
        new ZoneDefinition(13, "Z13", ZoneKind.Land),
    });

    private readonly PeopleDataLoader _loader = new(NullLogger<PeopleDataLoader>.Instance);

    [Fact]
    public void TryLoad_Valid_ReadsCulturesAndPeoples()
    {
        PeopleData? data = Load("""
            cultures:
              - id: goblin
                name_key: C_GOBLIN
                color: [125, 190, 40]
                peoples:
                  - { id: goblin, name_key: P_GOBLIN, center: [110, -12], radius: 9, profiles: [mountains, steppe], bonus: [river] }
            """);

        Assert.NotNull(data);
        CultureDefinition culture = Assert.Single(data.Cultures);
        PeopleDefinition people = Assert.Single(culture.Peoples);
        Assert.Equal("goblin", people.Id);
        Assert.Equal("goblin", people.CultureId);
        Assert.Equal(110, people.CenterLongitude);
        Assert.Equal(-12, people.CenterLatitude);
        Assert.Equal(new[] { "mountains", "steppe" }, people.Profiles.Select(p => p.Id));
        Assert.True(people.RiverBonus);
        Assert.False(people.CoastBonus);
        Assert.Equal(0.55, people.Profiles[0].Suitability[9]);
    }

    [Theory]
    [InlineData("{ id: a, name_key: P, center: [0, 0], radius: 5, profiles: [jungle] }")]
    [InlineData("{ id: a, name_key: P, center: [200, 0], radius: 5, profiles: [steppe] }")]
    [InlineData("{ id: a, name_key: P, center: [0, 0], radius: 0, profiles: [steppe] }")]
    [InlineData("{ id: a, name_key: P, center: [0, 0], radius: 5, profiles: [steppe], bonus: [lake] }")]
    [InlineData("{ id: A, name_key: P, center: [0, 0], radius: 5, profiles: [steppe] }")]
    public void TryLoad_InvalidPeople_IsSkipped(string people)
    {
        PeopleData? data = Load($$"""
            cultures:
              - id: c
                name_key: C
                color: [1, 2, 3]
                peoples:
                  - {{people}}
                  - { id: ok, name_key: P, center: [0, 0], radius: 5, profiles: [steppe] }
            """);

        Assert.NotNull(data);
        Assert.Equal("ok", Assert.Single(Assert.Single(data.Cultures).Peoples).Id);
    }

    [Fact]
    public void TryLoad_DuplicatePeopleId_SkipsSecond()
    {
        PeopleData? data = Load("""
            cultures:
              - id: c1
                name_key: C
                color: [1, 2, 3]
                peoples:
                  - { id: same, name_key: P, center: [0, 0], radius: 5, profiles: [steppe] }
              - id: c2
                name_key: C
                color: [1, 2, 3]
                peoples:
                  - { id: same, name_key: P, center: [0, 0], radius: 5, profiles: [steppe] }
            """);

        Assert.NotNull(data);
        Assert.Equal("c1", Assert.Single(data.Cultures).Id);
    }

    [Fact]
    public void TryLoad_ProfileWithUnknownZone_RejectsAll()
    {
        PeopleData? data = _loader.TryLoad(new TextFile("l.yaml", "profiles:\n  steppe: { 99: 1 }"),
            new TextFile("k.yaml", "cultures: []"), Zones);

        Assert.Null(data);
    }

    private PeopleData? Load(string cultures) =>
        _loader.TryLoad(new TextFile("l.yaml", Profiles), new TextFile("k.yaml", cultures), Zones);
}
