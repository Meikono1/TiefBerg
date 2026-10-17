using Godot;
using TiefBerg.Core.Logging;

namespace TiefBerg.Core;

public partial class GameManager : Node
{
    private static readonly GameLogger Log = LogManager.GetLogger<GameManager>();

    public static GameManager Instance { get; private set; }

    public override void _EnterTree()
    {
        if (Instance != null && Instance != this)
        {
            Log.Warn("Zweite GameManager-Instanz erkannt, wird entfernt.");
            QueueFree();
            return;
        }

        Instance = this;
        LogManager.Initialize();
        LogEnvironmentInfo();
    }

    public override void _ExitTree()
    {
        if (Instance != this)
        {
            return;
        }

        // TODO: Automatisch speichern, sobald SaveService.Save() existiert
        Log.Info("Spiel wird beendet.");
        Instance = null;
        LogManager.Shutdown();
    }

    public void StartNewGame()
    {
        Log.Info("Neues Spiel wird gestartet.");

    }

    public void LoadGame()
    {
        if (!SaveService.HasSave())
        {
            Log.Warn("Laden angefordert, aber kein Spielstand vorhanden.");
            return;
        }

        // TODO: SaveService.Load() -> Welt- und Szenariozustand getrennt laden, dann ChangeScene(ScenePaths.Map) (Abschnitt 12)
        Log.Warn("Laden ist noch nicht implementiert.");
    }

    public void QuitGame()
    {
        Log.Info("Beenden angefordert.");
        GetTree().Quit();
    }

    public void ChangeScene(string scenePath)
    {
        if (!ResourceLoader.Exists(scenePath))
        {
            Log.Error($"Szene nicht gefunden: {scenePath}");
            return;
        }

        Log.Info($"Wechsle zu Szene: {scenePath}");
        Error result = GetTree().ChangeSceneToFile(scenePath);
        if (result != Error.Ok)
        {
            Log.Error($"Szenenwechsel fehlgeschlagen: {scenePath} ({result})");
        }
    }

    private void LogEnvironmentInfo()
    {
        Log.Info($"Godot {Engine.GetVersionInfo()["string"]}, OS: {OS.GetName()}, Debug-Build: {OS.IsDebugBuild()}");
        Log.Info($"Log-Datei: {LogManager.CurrentLogFilePath ?? "keine (nur Konsole)"}");
    }
}