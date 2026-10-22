using Godot;
using TiefBerg.Core.Logging;

namespace TiefBerg.UI.Scenario;

// Pan, Zoom und Kartenmodus für die Weltkarte; hängt an MapView.
// Zoom ist relativ zur Gesamtansicht (1 = ganze Karte sichtbar), damit er bei jeder Fenstergröße gleich wirkt.
// Zustand ist Kartenmitte + Zoom statt Pixelversatz, so bleibt der Ausschnitt beim Ändern der Fenstergröße erhalten.
public partial class MapPanZoom : Control
{
    private static readonly GameLogger Log = LogManager.GetLogger<MapPanZoom>();

    // Ausschnitt oder Zoom hat sich geändert, Punkte müssen neu gesetzt werden
    [Signal] public delegate void ViewChangedEventHandler();

    [ExportGroup("Nodes")]
    [Export] private Control _content;
    [Export] private TextureRect _map;
    [Export] private Button _zoomInButton;
    [Export] private Button _zoomOutButton;
    [Export] private Button _resetViewButton;

    [ExportGroup("Karte")]
    // Größe der Originalkarte; Positionen in der Konfiguration beziehen sich darauf, auch wenn die Textur verkleinert importiert wird
    [Export] private Vector2 _sourceMapSize = new(4320, 2160);

    [ExportGroup("Zoom")]
    [Export(PropertyHint.Range, "1,32,0.5")] private float _maxZoom = 8f;
    [Export(PropertyHint.Range, "1.01,2,0.01")] private float _zoomStep = 1.15f;
    [Export(PropertyHint.Range, "1,32,0.5")] private float _focusZoom = 3f;
    [Export(PropertyHint.Range, "0,2,0.05")] private float _focusDuration = 0.4f;

    private bool _initialized;
    private float _zoom = 1f;
    private Vector2 _center;
    private bool _dragging;
    private Tween _focusTween;

    public float Zoom => _zoom;

    private Vector2 MapSize => _map.Texture.GetSize();

    private float FitScale => Mathf.Min(Size.X / MapSize.X, Size.Y / MapSize.Y);

    private float CurrentScale => FitScale * _zoom;

    public override void _Ready()
    {
        var complete = ExportCheck.AllAssigned(Log, this,
            [("Content", _content), ("Map", _map),
            ("ZoomInButton", _zoomInButton), ("ZoomOutButton", _zoomOutButton), ("ResetViewButton", _resetViewButton)]);
        if (!complete)
        {
            return;
        }

        if (_map.Texture == null)
        {
            Log.Error($"WorldMap an '{_map.GetPath()}' hat keine Textur, Kartenansicht inaktiv.");
            return;
        }

        // Der Inhalt wird nur über Position/Scale bewegt; Anker aus dem Editor würden die Größe sonst an MapView binden
        _content.SetAnchorsPreset(LayoutPreset.TopLeft);
        _content.PivotOffset = Vector2.Zero;
        _content.Size = MapSize;
        _center = MapSize / 2f;

        _zoomInButton.Pressed += OnZoomInPressed;
        _zoomOutButton.Pressed += OnZoomOutPressed;
        _resetViewButton.Pressed += OnResetViewPressed;
        Resized += OnResized;

        _initialized = true;
        ApplyView();
        Log.Debug($"Kartenansicht initialisiert (Textur {MapSize}, Original {_sourceMapSize}).");
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (!_initialized)
        {
            return;
        }

        switch (@event)
        {
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelUp, Pressed: true } wheel:
                ZoomAt(wheel.Position, Mathf.Pow(_zoomStep, WheelFactor(wheel)));
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.WheelDown, Pressed: true } wheel:
                ZoomAt(wheel.Position, 1f / Mathf.Pow(_zoomStep, WheelFactor(wheel)));
                AcceptEvent();
                break;
            case InputEventMouseButton { ButtonIndex: MouseButton.Left or MouseButton.Middle } button:
                _dragging = button.Pressed;
                if (_dragging)
                {
                    StopFocusTween();
                }

                AcceptEvent();
                break;
            case InputEventMouseMotion motion when _dragging:
                _center -= motion.Relative / CurrentScale;
                ApplyView();
                AcceptEvent();
                break;
        }
    }

    // Fährt die Ansicht an einen Punkt der Originalkarte; weiter hineingezoomt bleibt es beim aktuellen Zoom
    public void FocusOn(Vector2 sourcePixel)
    {
        if (!_initialized)
        {
            Log.Warn("Fokus angefordert, Kartenansicht ist aber nicht initialisiert.");
            return;
        }

        StopFocusTween();
        var startCenter = _center;
        var startZoom = _zoom;
        var targetCenter = SourceToTexture(sourcePixel);
        var targetZoom = Mathf.Clamp(Mathf.Max(_zoom, _focusZoom), 1f, _maxZoom);

        if (_focusDuration <= 0f)
        {
            _center = targetCenter;
            _zoom = targetZoom;
            ApplyView();
            return;
        }

        _focusTween = CreateTween().SetEase(Tween.EaseType.Out).SetTrans(Tween.TransitionType.Cubic);
        _focusTween.TweenMethod(Callable.From<float>(t =>
        {
            _center = startCenter.Lerp(targetCenter, t);
            // Zoom geometrisch interpolieren, sonst wirkt die Fahrt am Anfang zu schnell
            _zoom = startZoom * Mathf.Pow(targetZoom / startZoom, t);
            ApplyView();
        }), 0f, 1f, _focusDuration);
        Log.Debug($"Fokus auf {sourcePixel}, Zoom {startZoom:0.##} → {targetZoom:0.##}.");
    }

    public void SetMapTexture(Texture2D texture)
    {
        if (texture == null)
        {
            Log.Error("Kartenmodus ohne Textur angefordert, Karte bleibt unverändert.");
            return;
        }

        if (_initialized && texture.GetSize() != MapSize)
        {
            Log.Warn($"Kartentextur '{texture.ResourcePath}' hat {texture.GetSize()} statt {MapSize}; Punkte liegen evtl. falsch (T-037).");
        }

        _map.Texture = texture;
        if (!_initialized)
        {
            return;
        }

        _content.Size = MapSize;
        ApplyView();
        Log.Debug($"Kartentextur gewechselt: {texture.ResourcePath}");
    }

    // Position eines Punkts der Originalkarte in lokalen Koordinaten von MapView (= MarkerLayer)
    public Vector2 SourceToView(Vector2 sourcePixel)
    {
        if (!_initialized)
        {
            return Vector2.Zero;
        }

        return Size / 2f + (SourceToTexture(sourcePixel) - _center) * CurrentScale;
    }

    private Vector2 SourceToTexture(Vector2 sourcePixel) => sourcePixel * MapSize / _sourceMapSize;

    private static float WheelFactor(InputEventMouseButton wheel) => wheel.Factor > 0f ? wheel.Factor : 1f;

    private void ZoomAt(Vector2 viewPosition, float factor)
    {
        StopFocusTween();
        // Der Kartenpunkt unter der Maus bleibt beim Zoomen unter der Maus
        var fromCenter = viewPosition - Size / 2f;
        var mapPoint = _center + fromCenter / CurrentScale;
        _zoom = Mathf.Clamp(_zoom * factor, 1f, _maxZoom);
        _center = mapPoint - fromCenter / CurrentScale;
        ApplyView();
    }

    private void ApplyView()
    {
        // Vor dem ersten Layout ist Size noch 0; Resized holt das nach
        if (!_initialized || Size.X <= 0f || Size.Y <= 0f)
        {
            return;
        }

        _zoom = Mathf.Clamp(_zoom, 1f, _maxZoom);
        var scale = CurrentScale;
        _center = new Vector2(ClampAxis(_center.X, MapSize.X, Size.X / scale), ClampAxis(_center.Y, MapSize.Y, Size.Y / scale));
        _content.Scale = new Vector2(scale, scale);
        _content.Position = Size / 2f - _center * scale;
        EmitSignal(SignalName.ViewChanged);
    }

    // Passt die Karte in eine Richtung ganz hinein, wird zentriert; sonst darf kein Rand ins Bild rutschen
    private static float ClampAxis(float center, float mapLength, float visibleLength)
    {
        if (mapLength <= visibleLength)
        {
            return mapLength / 2f;
        }

        var half = visibleLength / 2f;
        return Mathf.Clamp(center, half, mapLength - half);
    }

    private void StopFocusTween()
    {
        if (_focusTween != null && _focusTween.IsValid())
        {
            _focusTween.Kill();
        }

        _focusTween = null;
    }

    private void OnZoomInPressed() => ZoomAt(Size / 2f, _zoomStep * _zoomStep);

    private void OnZoomOutPressed() => ZoomAt(Size / 2f, 1f / (_zoomStep * _zoomStep));

    private void OnResetViewPressed()
    {
        StopFocusTween();
        _zoom = 1f;
        _center = MapSize / 2f;
        ApplyView();
        Log.Debug("Kartenansicht zurückgesetzt.");
    }

    private void OnResized() => ApplyView();
}
