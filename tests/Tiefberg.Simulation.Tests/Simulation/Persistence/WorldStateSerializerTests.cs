using System.IO;
using Microsoft.Extensions.Logging.Abstractions;
using TiefBerg.Data.Persistence;
using TiefBerg.Simulation;
using TiefBerg.Simulation.Dice;
using TiefBerg.Simulation.Persistence;
using TiefBerg.Simulation.Scheduling;

namespace TiefBerg.Tests.Simulation.Persistence;

public class WorldStateSerializerTests
{
    private readonly WorldStateSerializer _serializer = new(NullLogger<WorldStateSerializer>.Instance);

    private static WorldState Sample() => new(
        123456789UL,
        29_788_128_000UL,
        new RandomState(1, 2, 3, ulong.MaxValue),
        new[] { new ScheduledRun("needs", 3_600), new ScheduledRun("economy", 72_000) });

    private byte[] WriteSample()
    {
        using var stream = new MemoryStream();
        _serializer.Write(stream, Sample());
        return stream.ToArray();
    }

    [Fact]
    public void WriteRead_RoundTrip()
    {
        WorldState read = _serializer.Read(new MemoryStream(WriteSample()));

        WorldState expected = Sample();
        Assert.Equal(expected.Seed, read.Seed);
        Assert.Equal(expected.WorldSeconds, read.WorldSeconds);
        Assert.Equal(expected.Random, read.Random);
        Assert.Equal(expected.Schedule, read.Schedule);
    }

    [Fact]
    public void Write_StartsWithReadableMagic()
    {
        byte[] bytes = WriteSample();

        Assert.Equal("TBWD"u8.ToArray(), bytes[..4]);
    }

    [Fact]
    public void Read_Truncated_ThrowsSaveFormatException()
    {
        byte[] bytes = WriteSample();

        for (int length = 0; length < bytes.Length; length++)
        {
            Assert.Throws<SaveFormatException>(() => _serializer.Read(new MemoryStream(bytes[..length])));
        }
    }

    [Fact]
    public void Read_WrongMagic_Throws()
    {
        byte[] bytes = WriteSample();
        bytes[0] = (byte)'X';

        Assert.Throws<SaveFormatException>(() => _serializer.Read(new MemoryStream(bytes)));
    }

    [Fact]
    public void Read_NewerVersion_Throws()
    {
        byte[] bytes = WriteSample();
        bytes[4] = WorldStateSerializer.CurrentVersion + 1;

        Assert.Throws<SaveFormatException>(() => _serializer.Read(new MemoryStream(bytes)));
    }

    [Fact]
    public void Read_TrailingData_Throws()
    {
        byte[] bytes = [.. WriteSample(), 0];

        Assert.Throws<SaveFormatException>(() => _serializer.Read(new MemoryStream(bytes)));
    }
}
