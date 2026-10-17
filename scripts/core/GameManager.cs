using Godot;
using TiefBerg.Core.Logging;
using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;
using TiefBerg.Simulation;

namespace TiefBerg.Core;

public partial class GameManager : Node
{
    private static readonly GameLogger Log = LogManager.GetLogger<GameManager>();

    private const double PerfSummaryIntervalSeconds = 10.0;

    public static GameManager Instance { get; private set; }

    private double _perfSummaryTimer;
    private SimulationRunner _simulation;

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
        Perf.Initialize(LogManager.SimulationLoggerFactory?.CreateLogger("Perf"));
        LogEnvironmentInfo();

        // TODO: Erst mit StartNewGame()/LoadGame() starten, sobald es eine Welt gibt; vorerst ab Spielstart für die Aufwandsübersicht
        _simulation = new SimulationRunner(LogManager.SimulationLoggerFactory.CreateLogger<SimulationRunner>());
        _simulation.Start();
    }

    public override void _Process(double delta)
    {
        // TODO: Tick vom Frame entkoppeln (eigener Takt/Thread, Godot liest Snapshots), sobald der Scheduler steht (T-010)
        _simulation?.Tick();

        _perfSummaryTimer += delta;
        if (_perfSummaryTimer < PerfSummaryIntervalSeconds)
        {
            return;
        }

        _perfSummaryTimer = 0;
        Perf.LogSummary();
    }

    public override void _ExitTree()
    {
        if (Instance != this)
        {
            return;
        }

        // TODO: Automatisch speichern, sobald SaveService.Save() existiert
        Log.Info("Spiel wird beendet.");
        _simulation?.Stop();
        Instance = null;
        Perf.Shutdown();
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