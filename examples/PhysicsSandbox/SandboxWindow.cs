using System.Globalization;

namespace Electron2D.Examples.PhysicsSandbox;

/// <summary>Hosts eleven interactive physics stories in the editor splash's 1152 by 800 client area.</summary>
internal sealed partial class SandboxWindow : Window
{
    internal static readonly Vector2i ClientSize = new(1152, 800);
    internal static readonly string[] SceneNames = ["Collision warehouse", "Marble delivery", "Clockwork playground", "Gravity garden", "Rooftop courier", "Radar rescue", "Orbital tug", "Shape atelier", "Physics stress test", "Gravity Defied", "Angry birds"];
    internal static readonly string[] Stories =
    [
        "A delivery went wrong. Pull crates out of the towers, fire a marble, then rebuild the warehouse.",
        "Deliver marbles through the pastel machine. Compare rubber, ice and clay on the same track.",
        "Wake the toy workshop: a pendulum, a powered wheel, a slider and a spring share one world.",
        "Plant a miniature solar system. Move the stars, reverse their gravity and release new seeds.",
        "Collect every parcel. Jump through the shelves, ride the lift and cross the sloping rooftops.",
        "Guide the rescue probe through the maze. A thin ray and a wide sweep reveal different clearances.",
        "Pilot a custom-integrated tug. Tow the cargo into the dock with thrust, torque and a spring cable.",
        "Build a strange rolling creature. Edit its live shape slots, centre of mass and independent RID world.",
        "Load up to 1,024 active particles. Stir the tank and compare simulation, contacts and drawing cost.",
        "Ride a real motorcycle over ramps and gaps. Balance the chassis and drive both wheels to reach the finish.",
        "Pull the bird back in the sling. Knock the mint targets off their towers and try another shot."
    ];
    private readonly List<Resource> _styles = [];
    private readonly Font _regular;
    private readonly Label _heading;
    private readonly Label _story;
    private readonly Label _help;
    private readonly Entity _telemetry;
    private readonly OptionButton _selector;
    private readonly Button _debug;
    private readonly Button _pause;
    private readonly Button[] _actions;
    private readonly PopupMenu _menu;
    private bool _debugEnabled;
    private bool _paused;
    private double _readoutTime;
    internal PhysicsScene Scene { get; private set; } = null!;
    internal int SceneIndex { get; private set; }

    internal SandboxWindow(Font regular, Font semibold)
    {
        _regular = regular;
        Name = "PhysicsSandbox";
        Title = "PhysicsSandbox — Electron2D";
        Size = MinSize = MaxSize = ClientSize;
        Unresizable = true;
        GUIEmbedSubwindows = true;
        _worldLayer = new CanvasLayer { Name = "SimulationCanvas", Layer = 1, Transform = new Transform(0, new Vector2(.76f, .76f), 0, new Vector2(5.76f, 44.16f)) };
        _ui = new CanvasLayer { Name = "Interface", Layer = 10 };
        AddChild(_worldLayer); AddChild(_ui);
        SnapTransformsToPixel = true;
        Ready += _ => { if (RenderingServer.IsAvailable) RenderingServer.SetDefaultClearColor(PhysicsScene.Paper); };
        var background = new Entity { Name = "Background" };
        background.Draw += c =>
        {
            c.DrawRect(new(0, 0, 1152, 800), PhysicsScene.Paper);
            c.DrawLine(new(24, 86), new(1128, 86), PhysicsScene.Border);
            c.DrawString(semibold, new(893, 50), "PhysicsSandbox", fontSize: 26, modulate: PhysicsScene.Ink);
            c.DrawString(regular, new(995, 71), "ELECTRON2D", fontSize: 11, modulate: PhysicsScene.Muted);
        };
        AddChild(background);
        _selector = new OptionButton { Name = "SceneSelector", Position = new(24, 24), Size = new(292, 44), FitToLongestItem = false };
        StyleButton(_selector, semibold);
        _selector.TooltipText = "Choose a physics story. Arrow keys and Enter work in the menu.";
        for (var i = 0; i < SceneNames.Length; i++) _selector.AddItem($"{i + 1:00}   {SceneNames[i]}", i);
        _ui.AddChild(_selector);
        _menu = _selector.GetPopup();
        _menu.AllowSearch = true;
        _menu.AddThemeFontOverride("font", regular);
        _menu.AddThemeFontSizeOverride("font_size", 17);
        _menu.AddThemeColorOverride("font_color", PhysicsScene.Ink);
        _menu.AddThemeColorOverride("font_hover_color", PhysicsScene.Ink);
        _menu.AddThemeStyleBoxOverride("panel", Style(PhysicsScene.Surface));
        _menu.AddThemeStyleBoxOverride("hover", Style(PhysicsScene.Hover));
        _selector.ItemSelected += SwitchScene;
        _debug = MakeButton("Debug [F3]", new(328, 24), 142, regular);
        _debug.ToggleMode = true;
        _debug.Toggled += enabled => { _debugEnabled = enabled; Scene.DebugEnabled = enabled; };
        _pause = MakeButton("Pause [P]", new(482, 24), 122, regular);
        _pause.ToggleMode = true;
        _pause.Toggled += paused => SetPaused(paused);
        MakeButton("Step [.]", new(616, 24), 90, regular).Pressed += () => { SetPaused(true); Scene.StepOnce(); };
        MakeButton("Reset [R]", new(718, 24), 120, regular).Pressed += () => SwitchScene(SceneIndex);
        _heading = MakeLabel("", new(24, 100), 28, semibold, PhysicsScene.Ink);
        _story = MakeLabel("", new(24, 142), 16, regular, PhysicsScene.Muted);
        _help = MakeLabel("", new(24, 772), 14, regular, PhysicsScene.Muted);
        _actions = [MakeButton("", new(24, 716), 190, regular), MakeButton("", new(226, 716), 210, regular), MakeButton("", new(448, 716), 220, regular)];
        for (var i = 0; i < _actions.Length; i++)
        {
            var action = i;
            _actions[i].Pressed += () => Scene.Act(action);
        }
        var stats = _telemetry = new Entity { Name = "Telemetry" };
        ProcessEnabled = true;
        stats.Draw += c =>
        {
            Span<char> text = stackalloc char[128];
            text.TryWrite(CultureInfo.InvariantCulture, $"{Engine.FramesPerSecond:0} FPS · {Scene.BodyCount} bodies · {Scene.ContactEvents} impacts · {Scene.Score} delivered", out var count);
            PhysicsScene.DrawReadout(c, regular, new(704, 738), text[..count], 15, PhysicsScene.Ink);
            c.DrawString(regular, new(704, 760), _paused ? "Simulation paused · one step = 1/60 s" : "Drag to grab · right click to kick · B / N / F: actions", fontSize: 13, modulate: PhysicsScene.Muted);
        };
        _ui.AddChild(stats);
        BuildParameters(regular);
        InputEnabled = UnhandledInputEnabled = true;
        FocusExited += () => Scene.ReleaseGrab(false);
        SwitchScene(0);
    }

    internal void SwitchScene(int index)
    {
        if ((uint)index >= SceneNames.Length) throw new ArgumentOutOfRangeException(nameof(index));
        if (Scene is not null) { _worldLayer.RemoveChild(Scene); Scene.Dispose(); }
        SceneIndex = index;
        Scene = new PhysicsScene(index, _regular) { DebugEnabled = _debugEnabled, Running = !_paused };
        _worldLayer.AddChild(Scene);
        Engine.TimeScale = 1;
        SyncParameters();
        _heading.Text = SceneNames[index];
        _story.Text = Stories[index];
        _help.Text = Scene.Help;
        _selector.Select(index);
        _selector.ReleaseFocus();
        for (var i = 0; i < _actions.Length; i++) _actions[i].Text = Scene.Actions[i];

    }

    protected override void OnProcess(double delta)
    {
        _readoutTime += delta;
        if (_readoutTime >= .1) { _readoutTime = 0; _telemetry.QueueRedraw(); SyncParameters(); }
        for (var i = 0; i < _actions.Length; i++)
            if (_actions[i].Text != Scene.Actions[i]) _actions[i].Text = Scene.Actions[i];
        RefreshSelection();
    }

    private void SetPaused(bool paused)
    {
        _paused = paused;
        _pause.SetPressedNoSignal(paused);
        _pause.Text = paused ? "Play [P]" : "Pause [P]";
        Scene.Running = !paused;
        if (paused) Scene.ReleaseGrab(false);
    }

    protected override void OnInput(InputEvent @event)
    {
        if (@event is InputEventMouseMotion motion) Scene.SetPointer(motion.Position);
        if (@event is InputEventMouseButton { ButtonIndex: MouseButton.Left, Pressed: false }) Scene.ReleaseGrab();
    }

    protected override void OnUnhandledInput(InputEvent @event)
    {
        if (@event is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.Keycode)
        {
            case Key.Escape: Tree!.Quit(); break;
            case Key.F3: _debug.ButtonPressed = !_debug.ButtonPressed; break;
            case Key.P: SetPaused(!_paused); break;
            case Key.R: SwitchScene(SceneIndex); break;
            case Key.Period: SetPaused(true); Scene.StepOnce(); break;
            default: return;
        }
        SetInputAsHandled();
    }

    private Button MakeButton(string text, Vector2 position, float width, Font font)
    {
        var button = new Button(text) { Name = "Action" + _ui.GetChildCount(), Position = position, Size = new(width, 44) };
        StyleButton(button, font);
        button.Pressed += button.ReleaseFocus;
        _ui.AddChild(button);
        return button;
    }

    private void StyleButton(Button button, Font font)
    {
        button.AddThemeFontOverride("font", font);
        button.AddThemeFontSizeOverride("font_size", 16);
        foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_hover_pressed_color", "font_focus_color" }) button.AddThemeColorOverride(state, PhysicsScene.Ink);
        button.AddThemeColorOverride("font_disabled_color", PhysicsScene.Muted);
        button.AddThemeStyleBoxOverride("normal", Style(PhysicsScene.Surface));
        button.AddThemeStyleBoxOverride("hover", Style(PhysicsScene.Hover));
        button.AddThemeStyleBoxOverride("pressed", Style(PhysicsScene.Pressed));
        button.AddThemeStyleBoxOverride("hover_pressed", Style(PhysicsScene.Pressed));
        button.AddThemeStyleBoxOverride("focus", Style(new Color(0, 0, 0, 0), PhysicsScene.Peach));
    }

    private StyleBoxFlat Style(Color color, Color? border = null)
    {
        var style = new StyleBoxFlat { BGColor = color, BorderColor = border ?? PhysicsScene.Border, ContentMarginLeft = 16, ContentMarginRight = 16, ContentMarginTop = 8, ContentMarginBottom = 8 };
        style.SetCornerRadiusAll(10);
        style.SetBorderWidthAll(border.HasValue ? 2 : 1);
        _styles.Add(style);
        return style;
    }

    private Label MakeLabel(string text, Vector2 position, int size, Font font, Color color)
    {
        var label = new Label(text) { Name = "Text" + _ui.GetChildCount(), Position = position, MouseFilter = MouseFilter.Ignore };
        label.AddThemeFontOverride("font", font);
        label.AddThemeFontSizeOverride("font_size", size);
        label.AddThemeColorOverride("font_color", color);
        _ui.AddChild(label);
        return label;
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing) { foreach (var style in _styles) style.Dispose(); _styles.Clear(); }
    }
}
