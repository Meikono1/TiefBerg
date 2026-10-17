using System;
using System.Collections.Generic;
using System.IO;
using Microsoft.Extensions.Logging;
using TiefBerg.Data.Persistence;
using TiefBerg.Diagnostics;
using TiefBerg.Simulation.Dice;
using TiefBerg.Simulation.Scheduling;

namespace TiefBerg.Simulation.Persistence;

// Binäres, versioniertes Format des Weltzustands (Abschnitt 12). Weiß nichts vom Szenario.
//
// Version 1: Kennung "TBWD", Version, Seed, Weltzeit, RandomState (4 × u64),
//            Anzahl Fälligkeiten, je (System-Id, Fälligkeit), Endmarke
public sealed class WorldStateSerializer
{
    public const ushort CurrentVersion = 1;
    private const string What = "Weltzustand";
    private const int MaxScheduledRuns = 100_000;

    private static readonly uint Magic = SaveFormat.Magic("TBWD");

    private readonly ILogger<WorldStateSerializer> _log;

    public WorldStateSerializer(ILogger<WorldStateSerializer> log)
    {
        _log = log;
    }

    public void Write(Stream stream, WorldState state)
    {
        using var writer = new BinaryWriter(stream, SaveFormat.TextEncoding, leaveOpen: true);
        SaveFormat.WriteHeader(writer, Magic, CurrentVersion);
        writer.Write(state.Seed);
        writer.Write(state.WorldSeconds);
        writer.Write(state.Random.S0);
        writer.Write(state.Random.S1);
        writer.Write(state.Random.S2);
        writer.Write(state.Random.S3);

        writer.Write(state.Schedule.Count);
        foreach (ScheduledRun run in state.Schedule)
        {
            writer.Write(run.SystemId);
            writer.Write(run.DueSeconds);
        }

        SaveFormat.WriteEnd(writer);
        _log.Debug($"{What} geschrieben: Seed {state.Seed}, {state.Schedule.Count} Fälligkeiten.");
    }

    public WorldState Read(Stream stream)
    {
        try
        {
            using var reader = new BinaryReader(stream, SaveFormat.TextEncoding, leaveOpen: true);
            ushort version = SaveFormat.ReadHeader(reader, Magic, CurrentVersion, What);
            // TODO: Migration älterer Versionen, sobald es Version 2 gibt (Abschnitt 12)

            ulong seed = reader.ReadUInt64();
            ulong worldSeconds = reader.ReadUInt64();
            var random = new RandomState(reader.ReadUInt64(), reader.ReadUInt64(), reader.ReadUInt64(),
                reader.ReadUInt64());

            int count = SaveFormat.ReadCount(reader, MaxScheduledRuns, What);
            var schedule = new List<ScheduledRun>(count);
            for (int i = 0; i < count; i++)
            {
                schedule.Add(new ScheduledRun(reader.ReadString(), reader.ReadUInt64()));
            }

            SaveFormat.ReadEnd(reader, What);
            _log.Debug($"{What} gelesen: Version {version}, Seed {seed}, {count} Fälligkeiten.");
            return new WorldState(seed, worldSeconds, random, schedule);
        }
        catch (Exception e) when (e is EndOfStreamException or IOException or ArgumentException)
        {
            // ArgumentException: ungültiges UTF-8 in einem String
            var formatError = new SaveFormatException($"{What} ist abgeschnitten oder defekt.", e);
            _log.Error("Weltzustand konnte nicht gelesen werden.", formatError);
            throw formatError;
        }
        catch (SaveFormatException e)
        {
            _log.Error("Weltzustand konnte nicht gelesen werden.", e);
            throw;
        }
    }
}
