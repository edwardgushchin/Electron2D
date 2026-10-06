using System.Globalization;

namespace Electron2D.Examples.PhysicsSandbox;

/// <summary>Hosts eleven interactive physics stories in the editor splash's 1152 by 800 client area.</summary>
internal sealed partial class SandboxWindow : Window
{
    internal static readonly Vector2i ClientSize = new(1152, 800);
    internal static readonly Rect2 Playfield = new(24, 156, 836, 536);
    internal static readonly string[] SceneNames = ["Collision warehouse", "Marble delivery", "Clockwork playground", "Gravity garden", "Rooftop courier", "Radar rescue", "Orbital tug", "Shape atelier", "Physics stress test", "Gravity Defied", "Angry birds"];
    internal static readonly string[] Stories =
    [
        "Pull the towers apart, launch a heavy ball, then rebuild the warehouse.",
        "Deliver marbles into the blush bowl. Compare rubber, ice and clay.",
        "Pull a pendulum, power the wheel and tune the spring.",
        "Move the stars, reverse attraction and release orbiting seeds.",
        "Collect four parcels, ride the lift and cross the rooftops.",
        "Guide the probe to the beacon. Inspect ray and wide-sweep clearance.",
        "Tow the cargo into the blush dock with thrust and a spring cable.",
        "Morph a live compound shape and explore the independent physics world.",
        "Stir up to 1,024 real particles and tune the physical world.",
        "Ride over ramps and gaps. Balance the motorcycle and reach the flag.",
        "Pull the bird back, aim and release. Knock three blush targets off the towers."
    ];
    private readonly List<Resource> _styles = [];
    private readonly Font _regular;
    private readonly Label _story;
    private readonly Label _help;
    private readonly Entity _telemetry;
    private readonly OptionButton _selector;
    private readonly Button _pause;
    private readonly Button[] _actions;
    private readonly string[] _actionCaptions = ["", "", ""];
    private readonly Control _stageClip;
    private bool _paused;
    private double _readoutTime;
    internal PhysicsScene Scene { get; private set; } = null!;
    internal int SceneIndex { get; private set; }

    internal SandboxWindow(Font regular, Font semibold)
    {
        _regular = regular;
        Name = "PhysicsSandbox"; Title = "PhysicsSandbox — Electron2D";
        Size = MinSize = MaxSize = ClientSize; Unresizable = true; GUIEmbedSubwindows = true;
        _worldLayer = new CanvasLayer { Name = "SimulationCanvas", Layer = 1 };
        _ui = new CanvasLayer { Name = "Interface", Layer = 10 };
        _stageClip = new Control { Name = "StageClip", ClipContents = true, MouseFilter = MouseFilter.Ignore };
        _worldLayer.AddChild(_stageClip); AddChild(_worldLayer); AddChild(_ui);
        SnapTransformsToPixel = true;
        Ready += _ => { if (RenderingServer.IsAvailable) RenderingServer.SetDefaultClearColor(PhysicsScene.Paper); };
        var background = new Entity { Name = "Background" };
        background.Draw += c =>
        {
            c.DrawRect(new(0, 0, 1152, 800), PhysicsScene.Paper);
            c.DrawString(semibold, new(24, 36), "Electron2D", fontSize: 21, modulate: PhysicsScene.Ink);
            c.DrawLine(new(156, 18), new(156, 38), PhysicsScene.Border);
            c.DrawString(regular, new(174, 35), "PhysicsSandbox", fontSize: 18, modulate: PhysicsScene.Muted);
            c.DrawLine(new(24, 48), new(1128, 48), PhysicsScene.Border);
            c.DrawRect(new(Playfield.Position - Vector2.One, Playfield.Size + Vector2.One * 2), PhysicsScene.Border, false);
        };
        AddChild(background);
        _selector = new OptionButton { Name = "SceneSelector", Position = new(24, 64), Size = new(302, 44), FitToLongestItem = false };
        StyleButton(_selector, semibold);
        _selector.TooltipText = "Choose an experiment · arrows and Enter select a scene";
        for (var i = 0; i < SceneNames.Length; i++) _selector.AddItem(SceneNames[i], i);
        _ui.AddChild(_selector);
        var menu = _selector.GetPopup(); menu.AllowSearch = true;
        menu.AddThemeFontOverride("font", regular); menu.AddThemeFontSizeOverride("font_size", 16);
        menu.AddThemeColorOverride("font_color", PhysicsScene.Ink); menu.AddThemeColorOverride("font_hover_color", PhysicsScene.Ink);
        menu.AddThemeStyleBoxOverride("panel", Style(PhysicsScene.Surface)); menu.AddThemeStyleBoxOverride("hover", Style(PhysicsScene.Hover));
        _selector.ItemSelected += SwitchScene;
        _pause = MakeButton("Pause", new(338, 64), 106, regular, "Pause"); _pause.ToggleMode = true;
        _pause.TooltipText = "Pause / play · P"; _pause.Toggled += SetPaused;
        var step = MakeButton("Step", new(456, 64), 90, regular, "Step"); step.TooltipText = "Advance one fixed tick · period key";
        step.Pressed += () => { SetPaused(true); Scene.StepOnce(); };
        var reset = MakeButton("Reset", new(558, 64), 94, regular, "Reset"); reset.TooltipText = "Restore this experiment · R";
        reset.Pressed += () => SwitchScene(SceneIndex);
        _story = MakeLabel("", new(24, 124), 15, regular, PhysicsScene.Muted);
        _help = MakeLabel("", new(24, 776), 13, regular, PhysicsScene.Muted);
        _actions = [MakeButton("", new(24, 712), 266, semibold), MakeButton("", new(306, 712), 266, regular), MakeButton("", new(588, 712), 272, regular)];
        _actions[0].AddThemeStyleBoxOverride("normal", Style(PhysicsScene.Berry, PhysicsScene.Pink));
        _actions[0].AddThemeStyleBoxOverride("hover", Style(Color.FromHTML("#BA4C83"), PhysicsScene.Pink));
        for (var i = 0; i < 3; i++) { var slot = i; _actions[i].Pressed += () => Scene.Act(ActionIndex(slot)); }
        _telemetry = new Entity { Name = "Telemetry" };
        _telemetry.Draw += c =>
        {
            PhysicsScene.DrawReadout(c, semibold, new(-300, -300), "0123456789.-—", 28, new Color(0, 0, 0, 0));
            Span<char> text = stackalloc char[80];
            var count = 1;
            if (Engine.FramesPerSecond < 1) text[0] = '—';
            else text.TryWrite(CultureInfo.InvariantCulture, $"{Engine.FramesPerSecond:0}", out count);
            PhysicsScene.DrawReadout(c, semibold, new(894, 92), text[..count], 28, PhysicsScene.Ink);
            c.DrawString(regular, new(949, 92), "FPS", fontSize: 13, modulate: PhysicsScene.Muted);
            text.TryWrite(CultureInfo.InvariantCulture, $"{Scene.BodyCount} bodies", out count);
            PhysicsScene.DrawReadout(c, regular, new(1012, 90), text[..count], 14, PhysicsScene.Ink);
            c.DrawString(regular, new(894, 111), _paused ? "PAUSED · step = 1/60 s" : "LIVE · fixed simulation", fontSize: 12, modulate: _paused ? PhysicsScene.Pink : PhysicsScene.Blush);
            text.TryWrite(CultureInfo.InvariantCulture, $"{Scene.ContactEvents} contact events", out count);
            PhysicsScene.DrawReadout(c, regular, new(894, 733), text[..count], 14, PhysicsScene.Muted);
            if (SceneIndex is 1 or 4 or 6 or 9 or 10)
            {
                text.TryWrite(CultureInfo.InvariantCulture, $"{Scene.Score} {SceneIndex switch { 9 => "finish reached", 10 => "targets down / 3", 4 => "parcels / 4", 6 => "cargo docked", _ => "marbles delivered" }}", out count);
                PhysicsScene.DrawReadout(c, regular, new(894, 753), text[..count], 14, PhysicsScene.Blush);
            }
        };
        _ui.AddChild(_telemetry); BuildParameters(regular, semibold);
        ProcessEnabled = InputEnabled = UnhandledInputEnabled = true;
        FocusExited += () => Scene.ReleaseGrab(false);
        SwitchScene(0);
    }

    private int ActionIndex(int slot) => SceneIndex switch { 0 => slot switch { 0 => 1, 1 => 0, _ => 2 }, 9 => slot switch { 0 => 1, 1 => 2, _ => 0 }, _ => slot };

    internal void SwitchScene(int index)
    {
        if ((uint)index >= SceneNames.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if (Scene is not null) { _stageClip.RemoveChild(Scene); Scene.Dispose(); }
        SceneIndex = index; Scene = new PhysicsScene(index, _regular) { Running = !_paused, PhysicsInterpolationMode = PhysicsInterpolationMode.Off, InputBounds = Playfield };
        _stageClip.AddChild(Scene); FrameScene(); Engine.TimeScale = 1;
        _selectionRevision = Scene.SelectionRevision; CaptureDefaults(); SyncParameters(); ShowParameters(0);
        _story.Text = Stories[index]; _help.Text = Scene.Help;
        _selector.Select(index); _selector.ReleaseFocus(); UpdateActions();
    }

    private void FrameScene()
    {
        var zoom = SceneIndex switch { 0 => 1.05f, 1 => .85f, 8 => 1, 9 => 3f, 10 => .95f, _ => .9f };
        var width = Playfield.Size.X / zoom; var height = Playfield.Size.Y / zoom;
        var center = SceneIndex switch { 0 => 582f, 8 => 576f, 10 => 480f, 4 or 5 or 6 or 9 => Scene.CameraTarget.X, _ => 576 };
        var x = Math.Clamp(center - width / 2, 36, 1116 - width);
        var bottom = SceneIndex == 9 ? Math.Min(629, Scene.CameraTarget.Y + 100) : 629;
        var view = new Rect2(x, bottom - height, width, height);
        _worldLayer.Transform = new Transform(0, new Vector2(zoom, zoom), 0, Playfield.Position - view.Position * zoom);
        _stageClip.Position = view.Position; _stageClip.Size = view.Size; Scene.Position = -view.Position; Scene.PresentationZoom = zoom;
    }

    private void UpdateActions()
    {
        for (var i = 0; i < 3; i++)
        {
            var caption = Scene.Actions[ActionIndex(i)];
            if (_actionCaptions[i] == caption) continue;
            _actionCaptions[i] = caption;
            var end = caption.IndexOf(" [", StringComparison.Ordinal);
            _actions[i].Text = end < 0 ? caption : caption[..end];
            _actions[i].TooltipText = caption;
        }
    }

    protected override void OnProcess(double delta)
    {
        if (SceneIndex is 4 or 5 or 6 or 9) FrameScene();
        RefreshSelection();
        _readoutTime += delta;
        if (_readoutTime >= .1) { _readoutTime = 0; _telemetry.QueueRedraw(); SyncParameters(); }
        UpdateActions();
    }
    private void SetPaused(bool paused)
    {
        _paused = paused; _pause.SetPressedNoSignal(paused); _pause.Text = paused ? "Play" : "Pause";
        Scene.Running = !paused; if (paused) Scene.ReleaseGrab(false); _telemetry.QueueRedraw();
    }
    protected override void OnInput(InputEvent input)
    {
        if (input is InputEventMouseMotion motion && Playfield.HasPoint(motion.Position)) Scene.SetPointer(motion.Position);
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }) Scene.ReleaseGrab();
    }
    protected override void OnUnhandledInput(InputEvent input)
    {
        if (input is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.Escape: Tree!.Quit(); break;
            case Key.P: SetPaused(!_paused); break;
            case Key.R: SwitchScene(SceneIndex); break;
            case Key.Period: SetPaused(true); Scene.StepOnce(); break;
            default: return;
        }
        SetInputAsHandled();
    }
    private Button MakeButton(string text, Vector2 position, float width, Font font, string? name = null)
    {
        var button = new Button(text) { Name = name ?? "Action" + _ui.GetChildCount(), Position = position, Size = new(width, 44) };
        StyleButton(button, font); button.Pressed += button.ReleaseFocus; _ui.AddChild(button); return button;
    }
    private void StyleButton(Button button, Font font)
    {
        button.AddThemeFontOverride("font", font); button.AddThemeFontSizeOverride("font_size", 15);
        foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color", "font_focus_color" }) button.AddThemeColorOverride(state, PhysicsScene.Ink);
        button.AddThemeColorOverride("font_disabled_color", PhysicsScene.Muted);
        button.AddThemeStyleBoxOverride("normal", Style(PhysicsScene.Surface)); button.AddThemeStyleBoxOverride("hover", Style(PhysicsScene.Hover));
        button.AddThemeStyleBoxOverride("pressed", Style(PhysicsScene.Pressed, PhysicsScene.Pink)); button.AddThemeStyleBoxOverride("hover_pressed", Style(PhysicsScene.Pressed, PhysicsScene.Pink));
        button.AddThemeStyleBoxOverride("focus", Style(new Color(0, 0, 0, 0), PhysicsScene.Pink));
    }
    private StyleBoxFlat Style(Color color, Color? border = null)
    {
        var style = new StyleBoxFlat { BGColor = color, BorderColor = border ?? PhysicsScene.Border, ContentMarginLeft = 12, ContentMarginRight = 12, ContentMarginTop = 8, ContentMarginBottom = 8 };
        style.SetCornerRadiusAll(8); style.SetBorderWidthAll(border.HasValue ? 2 : 1); _styles.Add(style); return style;
    }
    private Label MakeLabel(string text, Vector2 position, int size, Font font, Color color)
    {
        var label = new Label(text) { Name = "Text" + _ui.GetChildCount(), Position = position, MouseFilter = MouseFilter.Ignore };
        label.AddThemeFontOverride("font", font); label.AddThemeFontSizeOverride("font_size", size); label.AddThemeColorOverride("font_color", color); _ui.AddChild(label); return label;
    }
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing); if (disposing) { foreach (var style in _styles) style.Dispose(); _styles.Clear(); }
    }
}
