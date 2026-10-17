using System;
using System.Buffers.Binary;
using System.IO;
using System.Text;

namespace TiefBerg.Data.Persistence;

// Spielstand ist defekt, abgeschnitten oder stammt aus einer neueren Spielversion
public sealed class SaveFormatException : Exception
{
    public SaveFormatException(string message, Exception? inner = null) : base(message, inner)
    {
    }
}

// Gemeinsamer Rahmen aller Spielstand-Dateien (Abschnitt 12): Kennung, Version, Inhalt, Endmarke.
// BinaryWriter schreibt immer Little Endian, Dateien sind also plattformunabhängig.
// Reine Formathilfe ohne Zustand, daher kein Logger: Fehler werfen SaveFormatException, der Serializer loggt.
public static class SaveFormat
{
    // Endmarke erkennt abgeschnittene Dateien auch dann, wenn der Schnitt genau zwischen zwei Feldern liegt
    private const uint EndMarker = 0x444E4554; // "TEND"

    public static readonly Encoding TextEncoding = new UTF8Encoding(false, true);

    public static uint Magic(string fourCharacters)
    {
        if (fourCharacters.Length != 4)
        {
            throw new ArgumentException("Kennung braucht genau 4 Zeichen.", nameof(fourCharacters));
        }

        // Little Endian wie BinaryWriter, damit die Kennung im Hex-Editor lesbar am Dateianfang steht
        return BinaryPrimitives.ReadUInt32LittleEndian(Encoding.ASCII.GetBytes(fourCharacters));
    }

    public static void WriteHeader(BinaryWriter writer, uint magic, ushort version)
    {
        writer.Write(magic);
        writer.Write(version);
    }

    // Liefert die Version der Datei; ältere Versionen muss der Aufrufer migrieren
    public static ushort ReadHeader(BinaryReader reader, uint magic, ushort currentVersion, string what)
    {
        uint actualMagic = reader.ReadUInt32();
        if (actualMagic != magic)
        {
            throw new SaveFormatException($"{what}: falsche Kennung 0x{actualMagic:X8}, erwartet 0x{magic:X8}.");
        }

        ushort version = reader.ReadUInt16();
        if (version == 0 || version > currentVersion)
        {
            throw new SaveFormatException(
                $"{what}: Version {version} wird nicht unterstützt (aktuell {currentVersion}).");
        }

        return version;
    }

    public static void WriteEnd(BinaryWriter writer) => writer.Write(EndMarker);

    public static void ReadEnd(BinaryReader reader, string what)
    {
        if (reader.ReadUInt32() != EndMarker)
        {
            throw new SaveFormatException($"{what}: Endmarke fehlt, Datei ist defekt.");
        }

        if (reader.BaseStream.CanSeek && reader.BaseStream.Position != reader.BaseStream.Length)
        {
            throw new SaveFormatException($"{what}: unerwartete Daten nach der Endmarke.");
        }
    }

    // Obergrenze für gelesene Anzahlen, damit eine defekte Datei keine Riesen-Allokation auslöst
    public static int ReadCount(BinaryReader reader, int max, string what)
    {
        int count = reader.ReadInt32();
        if (count < 0 || count > max)
        {
            throw new SaveFormatException($"{what}: unplausible Anzahl {count} (erlaubt 0..{max}).");
        }

        return count;
    }
}
