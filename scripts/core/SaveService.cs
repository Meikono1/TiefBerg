using Godot;

namespace TiefBerg.Core;

public static class SaveService
{
    public const string SavePath = "user://savegame.json";

    public static bool HasSave() => FileAccess.FileExists(SavePath);

    // TODO: Save(PetData data) – als JSON schreiben, Save-Version und Timestamp mitspeichern, Fehler loggen
    // TODO: Load() – JSON lesen, Save-Version prüfen, bei defekter Datei loggen und Backup anlegen
}