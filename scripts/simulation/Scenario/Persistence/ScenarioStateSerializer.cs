using System;
using System.IO;
using Microsoft.Extensions.Logging;
using TiefBerg.Data.Persistence;
using TiefBerg.Diagnostics;

namespace TiefBerg.Scenario.Persistence;

// Binäres, versioniertes Format des Szenariozustands (Abschnitt 12), eigene Datei neben dem Weltzustand.
//
// Version 1: Kennung "TBSZ", Version, Szenario-ID, Endmarke
public sealed class ScenarioStateSerializer
{
    public const ushort CurrentVersion = 1;
    private const string What = "Szenariozustand";

    private static readonly uint Magic = SaveFormat.Magic("TBSZ");

    private readonly ILogger<ScenarioStateSerializer> _log;

    public ScenarioStateSerializer(ILogger<ScenarioStateSerializer> log)
    {
        _log = log;
    }

    public void Write(Stream stream, ScenarioState state)
    {
        using var writer = new BinaryWriter(stream, SaveFormat.TextEncoding, leaveOpen: true);
        SaveFormat.WriteHeader(writer, Magic, CurrentVersion);
        writer.Write(state.ScenarioId);
        SaveFormat.WriteEnd(writer);
        _log.Debug($"{What} geschrieben: {state.ScenarioId}.");
    }

    public ScenarioState Read(Stream stream)
    {
        try
        {
            using var reader = new BinaryReader(stream, SaveFormat.TextEncoding, leaveOpen: true);
            ushort version = SaveFormat.ReadHeader(reader, Magic, CurrentVersion, What);
            // TODO: Migration älterer Versionen, sobald es Version 2 gibt (Abschnitt 12)
            string scenarioId = reader.ReadString();
            SaveFormat.ReadEnd(reader, What);
            _log.Debug($"{What} gelesen: Version {version}, {scenarioId}.");
            return new ScenarioState(scenarioId);
        }
        catch (Exception e) when (e is EndOfStreamException or IOException or ArgumentException)
        {
            var formatError = new SaveFormatException($"{What} ist abgeschnitten oder defekt.", e);
            _log.Error("Szenariozustand konnte nicht gelesen werden.", formatError);
            throw formatError;
        }
        catch (SaveFormatException e)
        {
            _log.Error("Szenariozustand konnte nicht gelesen werden.", e);
            throw;
        }
    }
}
