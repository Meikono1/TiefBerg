using Godot;
using TiefBerg.Core;
using TiefBerg.Core.Logging;

namespace TiefBerg.UI;

public partial class MainMenu : Control
{
    private static readonly GameLogger Log = LogManager.GetLogger<MainMenu>();

    [Export] private Button _startButton;
    [Export] private Button _quitButton;

    public override void _Ready()
    {
        _startButton ??= GetNodeOrNull<Button>("CenterContainer/VBoxContainer/StartButton");
        _quitButton ??= GetNodeOrNull<Button>("CenterContainer/VBoxContainer/QuitButton");

        if (_startButton == null || _quitButton == null)
        {
            Log.Error($"Buttons nicht gefunden (start={_startButton != null}, quit={_quitButton != null})");
            return;
        }

        _startButton.Pressed += OnStartPressed;
        _quitButton.Pressed += OnQuitPressed;
        _startButton.GrabFocus();
        Log.Debug("Menü initialisiert.");
        // TODO: Fortsetzen-Button, wenn ein Spielstand existiert
        // TODO: Optionen (Auflösung, Sprache, Log-Level)
    }

    private void OnStartPressed()
    {
        Log.Info("Start gedrückt.");
        // TODO: Szenario-Auswahl vorschalten statt direkt auf die Map (Abschnitt 11)
        if (GameManager.Instance == null)
        {
            Log.Error("GameManager-Autoload fehlt, Szenenwechsel nicht möglich.");
            return;
        }

        GameManager.Instance.ChangeScene(ScenePaths.Map);
    }

    private void OnQuitPressed()
    {
        Log.Info("Beenden gedrückt.");
        if (GameManager.Instance == null)
        {
            Log.Error("GameManager-Autoload fehlt, beende direkt.");
            GetTree().Quit();
            return;
        }

        GameManager.Instance.QuitGame();
    }
}
