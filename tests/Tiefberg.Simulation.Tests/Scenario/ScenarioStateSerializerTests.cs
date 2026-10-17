using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Data.Persistence;
using TiefBerg.Scenario;
using TiefBerg.Scenario.Persistence;

namespace TiefBerg.Tests.Scenario;

public class ScenarioStateSerializerTests
{
    private readonly ScenarioStateSerializer _serializer = new(NullLogger<ScenarioStateSerializer>.Instance);

    [Fact]
    public void WriteRead_RoundTrip()
    {
        using var stream = new MemoryStream();
        _serializer.Write(stream, new ScenarioState(ScenarioState.ExpeditionId));
        stream.Position = 0;

        Assert.Equal(new ScenarioState(ScenarioState.ExpeditionId), _serializer.Read(stream));
    }

    [Fact]
    public void Read_WorldFile_Throws()
    {
        // Welt- und Szenariodatei haben verschiedene Kennungen und lassen sich nicht verwechseln
        byte[] bytes = [(byte)'T', (byte)'B', (byte)'W', (byte)'D', 1, 0];

        Assert.Throws<SaveFormatException>(() => _serializer.Read(new MemoryStream(bytes)));
    }
}
