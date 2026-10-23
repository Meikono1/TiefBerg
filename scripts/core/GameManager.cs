using System;
using System.Linq;
using Godot;
using TiefBerg.Core.Logging;
using TiefBerg.Diagnostics;
using TiefBerg.Scenario;
using TiefBerg.Simulation;
using TiefBerg.Simulation.Control;
using TiefBerg.Simulation.Control.Commands;
using TiefBerg.Simulation.Geography;
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
    private SimulationThread _simulation;
    private ScenarioState _scenario;
    private HomelandMap _homelands;

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
        // Vor der ersten Szene, damit schon das Hauptmenü übersetzt ist
        Localization.Initialize();
    }

    public override void _Ready()
    {
        string[] args = OS.GetCmdlineUserArgs();
        // Verzögert, weil die Hauptszene beim _Ready des Autoloads noch nicht geladen ist
        if (args.Contains(NewGameArgument))
        {
            Log.Info($"{NewGameArgument} auf der Kommandozeile.");
            // Ohne Auswahl startet Szenario 1
            Callable.From(() => StartNewGame(ScenarioState.ExpeditionId)).CallDeferred();
        }
        else if (args.Contains(ContinueArgument))
        {
            Log.Info($"{ContinueArgument} auf der Kommandozeile.");
            Callable.From(LoadGame).CallDeferred();
        }
    }

    // Stand der laufenden Simulation für Ansichten; null ohne Spiel. Ansichten lesen nur diesen (Abschnitt 2).
    public SimulationSnapshot Snapshot => _simulation?.Snapshot;

    // Heimatgebiete der Völker, beim Weltstart aus den Daten berechnet (Abschnitt 4); null ohne Spiel.
    // Nicht im Spielstand: Sie ergeben sich immer gleich aus den Daten.
    // TODO: Startpunkte der Vorsimulation daraus wählen (T-045)
    public HomelandMap Homelands => _homelands;

    // Befehle der Ansichten an die Simulation (Pause, Echtzeit, Abschnitt laufen lassen …)
    public void Send(ISimulationCommand command)
    {
        if (_simulation == null)
        {
            Log.Warn($"Befehl {command.GetType().Name} verworfen: Es läuft kein Spiel.");
            return;
        }

        _simulation.Send(command);
    }

    public override void _Process(double delta)
    {
        // Die Simulation läuft im eigenen Thread; hier nur noch die Aufwandsübersicht
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
            // Erst anhalten, dann lesen: so wird garantiert kein halb gerechneter Stand gespeichert
            _simulation.Stop();
            WorldState state = _simulation.CaptureState();
            if (state != null)
            {
                SaveService.Autosave(state, _scenario);
            }
        }

        StopSimulation();
        Instance = null;
        Perf.Shutdown();
        LogManager.Shutdown();
    }

    // Id aus der Szenarioauswahl; Landet im ScenarioState und damit im Spielstand
    public void StartNewGame(string scenarioId)
    {
        // Der Seed selbst darf zufällig sein; ab hier ist alles deterministisch und der Seed steht im Spielstand
        ulong seed = BitConverter.ToUInt64(CryptoRandom.GetBytes(sizeof(ulong)));
        Log.Info($"Neues Spiel wird gestartet: Szenario '{scenarioId}', Seed {seed}.");
        HomelandMap homelands = WorldDataFiles.LoadHomelands();
        if (homelands == null)
        {
            Log.Error("Neues Spiel abgebrochen: Weltdaten fehlen.");
            return;
        }

        // TODO: Startzeit aus Welt-/Szenariodaten statt Weltzeit 0 (Abschnitt 3, 11; T-025)
        World world = World.Create(LogManager.SimulationLoggerFactory, seed, 0);
        StartSimulation(world, new ScenarioState(scenarioId), homelands);
        ChangeScene(ScenePaths.Map);
    }

    public void LoadGame()
    {
        if (!SaveService.TryLoadLatest(out WorldState worldState, out ScenarioState scenario))
        {
            Log.Warn("Laden angefordert, aber kein lesbarer Spielstand vorhanden.");
            return;
        }

        HomelandMap homelands = WorldDataFiles.LoadHomelands();
        if (homelands == null)
        {
            Log.Error("Laden abgebrochen: Weltdaten fehlen.");
            return;
        }

        World world = World.Restore(LogManager.SimulationLoggerFactory, worldState);
        StartSimulation(world, scenario, homelands);
        ChangeScene(ScenePaths.Map);
    }

    public bool SaveGame(string slotName)
    {
        if (_simulation == null)
        {
            Log.Warn("Speichern angefordert, aber es läuft kein Spiel.");
            return false;
        }

        WorldState state = _simulation.CaptureState();
        return state != null && SaveService.Save(slotName, state, _scenario);
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

    private void StartSimulation(World world, ScenarioState scenario, HomelandMap homelands)
    {
        StopSimulation();
        _scenario = scenario;
        _homelands = homelands;
        _simulation = new SimulationThread(LogManager.SimulationLoggerFactory, world, TimeProvider.System);
        LogManager.WorldTimeSource = () => world.Clock.Now;
        // Startet pausiert (Abschnitt 3); weiter geht es über Befehle der Spielszene (T-014)
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
        _homelands = null;
        LogManager.WorldTimeSource = null;
    }

    private void LogEnvironmentInfo()
    {
        Log.Info($"Godot {Engine.GetVersionInfo()["string"]}, OS: {OS.GetName()}, Debug-Build: {OS.IsDebugBuild()}");
        Log.Info($"Log-Datei: {LogManager.CurrentLogFilePath ?? "keine (nur Konsole)"}");
    }
}