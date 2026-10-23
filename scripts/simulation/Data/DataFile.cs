namespace TiefBerg.Data;

// Eine Datendatei als Text bzw. Bytes; den Dateizugriff (res://, Mods) übernimmt der Host.
// Name dient nur den Logmeldungen (Pfad der Quelle).
public readonly record struct TextFile(string Name, string Text);

public readonly record struct BinaryFile(string Name, byte[] Bytes);
