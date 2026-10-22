using System;
using System.IO;
using System.Linq;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Data.Scenarios;

namespace TiefBerg.Tests.Data.Scenarios;

public class ScenarioDefinitionLoaderTests
{
    private const string Valid = """
        id: expedition
        title_key: T
        description_key: D
        map_position: { x: 3200, y: 900 }
        icon: Flag
        tags:
          species: [S_HUMAN]
          region: R_EAST
        start_year: 1000
        news:
          - date: 2026-10-20
            version: 0.1.0
            kind: new
            text_key: N
        """;

    private readonly ScenarioDefinitionLoader _loader = new(NullLogger<ScenarioDefinitionLoader>.Instance);

    [Fact]
    public void TryLoad_Valid_ReadsAllFields()
    {
        ScenarioDefinition? definition = _loader.TryLoad(new ScenarioSource("a.yaml", Valid));

        Assert.NotNull(definition);
        Assert.Equal("expedition", definition.Id);
        Assert.Equal("T", definition.TitleKey);
        Assert.Equal(new MapPoint(3200, 900), definition.MapPosition);
        Assert.Equal("Flag", definition.Icon);
        Assert.Null(definition.PreviewImage);
        Assert.Equal(["S_HUMAN"], definition.Tags.SpeciesKeys);
        Assert.Equal("R_EAST", definition.Tags.RegionKey);
        Assert.Null(definition.Tags.DifficultyKey);
        Assert.Equal(1000, definition.StartYear);
        Assert.False(definition.Locked);
        ScenarioNews news = Assert.Single(definition.News);
        Assert.Equal(new ScenarioNews(new DateOnly(2026, 10, 20), "0.1.0", ScenarioNewsKind.New, "N"), news);
    }

    [Theory]
    [InlineData("id: expedition", "")]
    [InlineData("id: expedition", "id: Expedition")]
    [InlineData("icon: Flag", "icon: ../Flag.png")]
    [InlineData("map_position: { x: 3200, y: 900 }", "map_position: { x: -1, y: 900 }")]
    [InlineData("map_position: { x: 3200, y: 900 }", "map_position: { x: 3200 }")]
    [InlineData("start_year: 1000", "")]
    [InlineData("title_key: T", "title_key: \"  \"")]
    [InlineData("icon: Flag", "icon: Flag\nicn: Tower")]
    [InlineData("icon: Flag", "icon: [Flag")]
    [InlineData("    version: 0.1.0", "\tversion: 0.1.0")]
    public void TryLoad_Invalid_ReturnsNull(string from, string to)
    {
        Assert.Null(_loader.TryLoad(new ScenarioSource("a.yaml", Valid.Replace(from, to))));
    }

    [Fact]
    public void TryLoad_Empty_ReturnsNull()
    {
        Assert.Null(_loader.TryLoad(new ScenarioSource("a.yaml", "")));
    }

    [Theory]
    [InlineData("kind: new", "kind: old")]
    [InlineData("date: 2026-10-20", "date: 20.10.2026")]
    [InlineData("text_key: N", "")]
    public void TryLoad_InvalidNews_SkipsOnlyNewsEntry(string from, string to)
    {
        ScenarioDefinition? definition = _loader.TryLoad(new ScenarioSource("a.yaml", Valid.Replace(from, to)));

        Assert.NotNull(definition);
        Assert.Empty(definition.News);
    }

    [Fact]
    public void TryLoad_LockedWithoutHint_StaysLocked()
    {
        ScenarioDefinition? definition = _loader.TryLoad(new ScenarioSource("a.yaml", Valid + "\nlocked: true"));

        Assert.NotNull(definition);
        Assert.True(definition.Locked);
        Assert.Null(definition.UnlockHintKey);
    }

    [Fact]
    public void LoadAll_SortsBySourceName_SkipsInvalidAndDuplicates()
    {
        ScenarioSource[] sources =
        [
            new("c.yaml", Valid.Replace("id: expedition", "id: naga")),
            new("b.yaml", "kaputt: ["),
            new("a.yaml", Valid),
            new("d.yaml", Valid.Replace("title_key: T", "title_key: Doppelt"))
        ];

        var result = _loader.LoadAll(sources);

        Assert.Equal(["expedition", "naga"], result.Select(d => d.Id));
        Assert.Equal("T", result[0].TitleKey);
    }

    [Fact]
    public void LoadAll_ProjectData_AllValid()
    {
        // Die mitgelieferten Dateien unter data/scenarios müssen vollständig ladbar sein
        string directory = Path.Combine(FindProjectRoot(), "data", "scenarios");
        ScenarioSource[] sources = Directory.GetFiles(directory, "*.yaml")
            .Select(path => new ScenarioSource(Path.GetFileName(path), File.ReadAllText(path)))
            .ToArray();

        Assert.NotEmpty(sources);
        Assert.Equal(sources.Length, _loader.LoadAll(sources).Count);
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
