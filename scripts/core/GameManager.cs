using System;
using System.Linq;
using Godot;
using TiefBerg.Core.Logging;
using Microsoft.Extensions.Logging;
using TiefBerg.Diagnostics;
using TiefBerg.Scenario;
using TiefBerg.Simulation;
using CryptoRandom = System.Security.Cryptography.RandomNumberGenerator;

namespace TiefBerg.Core;

public partial class GameManager : Node
{
    private static readonly GameLogger Log = LogManager.GetLogger<GameManager>();

    private const double PerfSummaryIntervalSeconds = 10.0;

    // Kommandozeile nach "--": Spiel ohne Menü starten, z. B. für Headless-Läufe
    private const string NewGameArgument = "--new-game";
    private const string ContinueArgument = "--continue";

    public static GameManager Instance { get; private set; }

    private double _perfSummaryTimer;
    private SimulationRunner _simulation;
    private ScenarioState _scenario;

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
    }

    public override void _Ready()
    {
        string[] args = OS.GetCmdlineUserArgs();
        // Verzögert, weil die Hauptszene beim _Ready des Autoloads noch nicht geladen ist
        if (args.Contains(NewGameArgument))
        {
            Log.Info($"{NewGameArgument} auf der Kommandozeile.");
            Callable.From(StartNewGame).CallDeferred();
        }
        else if (args.Contains(ContinueArgument))
        {
            Log.Info($"{ContinueArgument} auf der Kommandozeile.");
            Callable.From(LoadGame).CallDeferred();
        }
    }

    public override void _Process(double delta)
    {
        // TODO: Tick vom Frame entkoppeln (eigener Takt/Thread, Godot liest Snapshots) (Abschnitt 2, T-027)
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

        Log.Info("Spiel wird beendet.");
        if (_simulation != null)
        {
            SaveService.Autosave(_simulation.World.CaptureState(), _scenario);
        }

        StopSimulation();
        Instance = null;
        Perf.Shutdown();
        LogManager.Shutdown();
    }

    public void StartNewGame()
    {
        // Der Seed selbst darf zufällig sein; ab hier ist alles deterministisch und der Seed steht im Spielstand
        ulong seed = BitConverter.ToUInt64(CryptoRandom.GetBytes(sizeof(ulong)));
        Log.Info($"Neues Spiel wird gestartet, Seed {seed}.");

        // TODO: Startzeit aus Welt-/Szenariodaten statt Weltzeit 0 (Abschnitt 3, 11; T-025)
        World world = World.Create(LogManager.SimulationLoggerFactory, seed, 0);
        // TODO: Szenario-Auswahl statt fest Expedition (Abschnitt 11)
        StartSimulation(world, new ScenarioState(ScenarioState.ExpeditionId));
        ChangeScene(ScenePaths.Map);
    }

    public void LoadGame()
    {
        if (!SaveService.TryLoadLatest(out WorldState worldState, out ScenarioState scenario))
        {
            Log.Warn("Laden angefordert, aber kein lesbarer Spielstand vorhanden.");
            return;
        }

        World world = World.Restore(LogManager.SimulationLoggerFactory, worldState);
        StartSimulation(world, scenario);
        ChangeScene(ScenePaths.Map);
    }

    public bool SaveGame(string slotName)
    {
        if (_simulation == null)
        {
            Log.Warn("Speichern angefordert, aber es läuft kein Spiel.");
            return false;
        }

        return SaveService.Save(slotName, _simulation.World.CaptureState(), _scenario);
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

    private void StartSimulation(World world, ScenarioState scenario)
    {
        StopSimulation();
        _scenario = scenario;
        _simulation = new SimulationRunner(LogManager.SimulationLoggerFactory.CreateLogger<SimulationRunner>(), world);
        LogManager.WorldTimeSource = () => world.Clock.Now;
        _simulation.Start();
    }

    private void StopSimulation()
    {
        if (_simulation == null)
        {
            return;
        }

        _simulation.Stop();
        _simulation = null;
        _scenario = null;
        LogManager.WorldTimeSource = null;
    }

    private void LogEnvironmentInfo()
    {
        Log.Info($"Godot {Engine.GetVersionInfo()["string"]}, OS: {OS.GetName()}, Debug-Build: {OS.IsDebugBuild()}");
        Log.Info($"Log-Datei: {LogManager.CurrentLogFilePath ?? "keine (nur Konsole)"}");
    }
}