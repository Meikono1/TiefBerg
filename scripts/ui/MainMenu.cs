using Godot;

public partial class MainMenu : Control {
	private const string Cat = "MainMenu";

	[Export] private Button _startButton;
	[Export] private Button _quitButton;

	private GameManager Manager => GetNodeOrNull<GameManager>("/root/GameManager");

	public override void _Ready() {
		_startButton ??= GetNodeOrNull<Button>("CenterContainer/VBoxContainer/StartButton");
		_quitButton  ??= GetNodeOrNull<Button>("CenterContainer/VBoxContainer/QuitButton");

		if (_startButton == null || _quitButton == null) {
			Log.Error(Cat, $"Buttons nicht gefunden (start={_startButton != null}, quit={_quitButton != null})");
			return;
		}

		_startButton.Pressed += OnStartPressed;
		_quitButton.Pressed += OnQuitPressed;
		_startButton.GrabFocus();
		Log.Debug(Cat, "Menü initialisiert");
		// TODO: Fortsetzen-Button wenn Spielstand existiert
		// TODO: Optionen (Auflösung, Sprache, Log-Level)
	}

	private void OnStartPressed() {
		Log.Info(Cat, "Start gedrückt");
		// TODO: Insel-/Rassenauswahl vorschalten statt direkt auf die Map
		var mgr = Manager;
		if (mgr != null) { mgr.LoadScene(GameScene.Map); return; }

		Log.Warn(Cat, "GameManager-Autoload fehlt, direkter Szenenwechsel");
		GetTree().ChangeSceneToFile("res://scenes/world/Map.tscn");
	}

	private void OnQuitPressed() {
		Log.Info(Cat, "Beenden gedrückt");
		var mgr = Manager;
		if (mgr != null) { mgr.QuitGame(); return; }

		Log.Warn(Cat, "GameManager-Autoload fehlt, direktes Quit");
		GetTree().Quit();
	}
}
