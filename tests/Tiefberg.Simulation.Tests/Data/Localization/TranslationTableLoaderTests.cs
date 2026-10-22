using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Data.Localization;

namespace TiefBerg.Tests.Data.Localization;

public class TranslationTableLoaderTests
{
    private readonly TranslationTableLoader _loader = new(NullLogger<TranslationTableLoader>.Instance);

    [Fact]
    public void Load_SkipsInvalidEntriesOnly()
    {
        var table = _loader.Load("en.yaml", "UI_BACK: Back\nlower_key: x\nUI_EMPTY: \"\"\nUI_CONTINUE: \"Continue\"");

        Assert.Equal(2, table.Count);
        Assert.Equal("Back", table["UI_BACK"]);
        Assert.Equal("Continue", table["UI_CONTINUE"]);
    }

    [Theory]
    [InlineData("UI_BACK: [Back")]
    [InlineData("UI_GROUP:\n  NESTED: x")]
    [InlineData("UI_BACK: >\n\tBack")]
    [InlineData("")]
    public void Load_Invalid_ReturnsEmpty(string yaml)
    {
        Assert.Empty(_loader.Load("en.yaml", yaml));
    }

    [Fact]
    public void Load_ProjectData_ContainsAllScenarioKeys()
    {
        // Jeder Schlüssel aus den Szenariodateien muss in der englischen Tabelle stehen
        string root = FindProjectRoot();
        var table = _loader.Load("en.yaml", File.ReadAllText(Path.Combine(root, "data", "lang", "en.yaml")));
        var keys = Directory.GetFiles(Path.Combine(root, "data", "scenarios"), "*.yaml")
            .SelectMany(File.ReadAllLines)
            .SelectMany(line => Regex.Matches(line, "\\b(SCENARIO|TAG|UI)_[A-Z0-9_]+\\b"))
            .Select(match => match.Value)
            .Distinct();

        Assert.All(keys, key => Assert.True(table.ContainsKey(key), $"{key} fehlt in en.yaml"));
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
