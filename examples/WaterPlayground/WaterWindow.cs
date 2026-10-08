using System.Globalization;

namespace Electron2D.Examples.WaterPlayground;

/// <summary>A fixed-world water playground with independent window presentation.</summary>
internal sealed class WaterWindow : Window
{
    internal static readonly Color Cream = Color.FromHTML("#F7F4EB"), Water = Color.FromHTML("#A7D8DE");
    private static readonly Color Butter = Color.FromHTML("#F5D684"), Wing = Color.FromHTML("#EBC46F"),
        Peach = Color.FromHTML("#EBAA8D"), Ink = Color.FromHTML("#54666F"), Lavender = Color.FromHTML("#BDB4D9"),
        Sail = Color.FromHTML("#FFFDF5"), Light = Color.FromHTML("#D9EDEF");
    private readonly Entity _world, _drawing, _waterDrawing, _hud;
    internal WaterSurface Surface { get; } = new();
    private readonly int _previousPhysicsBudget = Engine.MaxPhysicsStepsPerFrame;
    private readonly Font _font;
    private RenderingDevice? _device;
    private readonly Button _cpu, _gpu;
    private readonly StyleBoxFlat _buttonStyle, _activeStyle, _focusStyle;
    private string _deviceError = "";
    private string _presentation = "";
    private bool _held, _fullscreen;
    private Vector2 _pointer;
    private Vector2i _windowedSize;
    private readonly Fish[] _fish = new Fish[6];
    private struct Fish { internal Vector2 Position; internal float Direction; internal bool Active; }
    internal WaterSimulation Simulation { get; private set; }
    internal bool Paused { get; private set; }
    internal Transform ViewTransform { get; private set; } = Transform.Identity;
    internal int SwimmingFishCount => _fish.Count(f => f.Active && Surface.Submerged(f.Position));

    internal static void ConfigurePresentation()
    {
        // This example requests immediate presentation. Prefer XWayland where native Wayland exposes FIFO only.
        // Respect an explicit user-selected video driver; this changes only the example process environment.
        if (OperatingSystem.IsLinux() && !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("WAYLAND_DISPLAY")) &&
            !string.IsNullOrEmpty(Environment.GetEnvironmentVariable("DISPLAY")) && string.IsNullOrEmpty(Environment.GetEnvironmentVariable("SDL_VIDEO_DRIVER")))
            Environment.SetEnvironmentVariable("SDL_VIDEO_DRIVER", "x11");
    }

    internal WaterWindow(Font font, bool startOnGPU = true)
    {
        _font = font; Engine.MaxPhysicsStepsPerFrame = 1; Engine.MaxFPS = 144;
        Name = "WaterPlayground"; Title = "WaterPlayground";
        Size = new(1152, 800); MinSize = new(480, 360); Unresizable = false;
        Simulation = new();
        _world = new Entity { Name = "World" }; AddChild(_world);
        _drawing = new Entity { Name = "ToysAndFish" }; _drawing.Draw += DrawScene; _world.AddChild(_drawing);
        _waterDrawing = new Entity { Name = "WaterSurface" };
        _waterDrawing.Draw += canvas =>
        {
            if (Surface.VertexCount > 0) RenderingServer.CanvasItemAddTriangleArray(canvas.GetCanvasItem(), [], Surface.Vertices, Surface.Colors);
        };
        _world.AddChild(_waterDrawing);
        _hud = new Entity { Name = "HUD" }; _hud.Draw += DrawHUD; AddChild(_hud);
        _buttonStyle = new StyleBoxFlat { BGColor = Color.FromHTML("#EAE7EF"), BorderColor = Color.FromHTML("#B1A9C7") };
        _activeStyle = new StyleBoxFlat { BGColor = Lavender, BorderColor = Ink };
        _focusStyle = new StyleBoxFlat { BGColor = new(0, 0, 0, 0), BorderColor = Ink };
        _focusStyle.SetCornerRadiusAll(8); _focusStyle.SetBorderWidthAll(2);
        _buttonStyle.SetCornerRadiusAll(8); _activeStyle.SetCornerRadiusAll(8); _activeStyle.SetBorderWidthAll(1);
        _cpu = ModeButton("CPU"); _gpu = ModeButton("GPU");
        _cpu.Pressed += () => SetMode(false); _gpu.Pressed += () => SetMode(true);
        PhysicsProcessEnabled = ProcessEnabled = InputEnabled = true;
        SizeChanged += FitWorld;
        FocusExited += () => { _held = false; Simulation.EndDrag(); };
        Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Cream);
            DisplayServer.WindowSetVSyncMode(DisplayServer.VSyncMode.Disabled);
            _presentation = DisplayServer.WindowGetVSyncMode() == DisplayServer.VSyncMode.Disabled ? "VSync off" : "Display synchronization enforced by the platform";
            SetMode(startOnGPU);
        };
        FitWorld();
    }
    private Button ModeButton(string text)
    {
        var button = new Button { Name = text, Text = text, Size = new(72, 36), ToggleMode = true };
        button.AddThemeFontOverride("font", _font); button.AddThemeFontSizeOverride("font_size", 14);
        foreach (var state in new[] { "normal", "hover" }) button.AddThemeStyleBoxOverride(state, _buttonStyle);
        button.AddThemeStyleBoxOverride("focus", _focusStyle);
        foreach (var state in new[] { "pressed", "hover_pressed" }) button.AddThemeStyleBoxOverride(state, _activeStyle);
        foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" }) button.AddThemeColorOverride(state, Ink);
        AddChild(button); return button;
    }
    private void FitWorld()
    {
        var size = (Vector2)Size; var scale = Math.Min(size.X / Simulation.Size.X, size.Y / Simulation.Size.Y);
        ViewTransform = new Transform(0, new Vector2(scale, scale), 0, (size - Simulation.Size * scale) * .5f);
        _world.Transform = ViewTransform;
        _cpu.Position = new(Size.X - 176, 20); _gpu.Position = new(Size.X - 96, 20);
    }
    internal Vector2 ToWorld(Vector2 screen) => ViewTransform.AffineInverse() * screen;
    internal void SetMode(bool gpu)
    {
        if (gpu && _device is null)
        {
            try { _device = RenderingServer.CreateLocalRenderingDevice(); }
            catch (InvalidOperationException) { _deviceError = "GPU compute unavailable · CPU mode"; gpu = false; }
        }
        Simulation.SetUseGPU(gpu, _device);
        _cpu.SetPressedNoSignal(!gpu); _gpu.SetPressedNoSignal(gpu);
    }
    internal void Reset()
    {
        var gpu = Simulation.UseGPU; Simulation.Dispose(); Simulation = new(); if (gpu) Simulation.SetUseGPU(true, _device);
        Paused = false; _held = false; Array.Clear(_fish); Surface.Update(Simulation); _waterDrawing.QueueRedraw();
    }
    protected override void OnPhysicsProcess(double delta)
    {
        if (Paused) return;
        Simulation.Step(delta); Surface.Update(Simulation); Swim((float)delta); _waterDrawing.QueueRedraw();
    }
    protected override void OnProcess(double delta) { _drawing.QueueRedraw(); _hud.QueueRedraw(); }
    protected override void OnInput(InputEvent input)
    {
        if (input is InputEventMouseMotion motion) { _pointer = ToWorld(motion.Position); Simulation.MovePointer(_pointer); }
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } button)
        {
            if (button.Pressed && (new Rect2(_cpu.Position, _cpu.Size).HasPoint(button.Position) || new Rect2(_gpu.Position, _gpu.Size).HasPoint(button.Position))) return;
            _pointer = ToWorld(button.Position); _held = button.Pressed;
            if (_held) Simulation.BeginDrag(_pointer); else Simulation.EndDrag();
        }
        if (input is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.PhysicalKeycode)
        {
            case Key.R: Reset(); break;
            case Key.Tab: SetMode(!Simulation.UseGPU); break;
            case Key.Space: Paused = !Paused; break;
            case Key.F11:
                _fullscreen = !_fullscreen;
                if (_fullscreen) { _windowedSize = Size; Borderless = true; Mode = WindowMode.Fullscreen; }
                else { Mode = WindowMode.Windowed; Borderless = false; Size = _windowedSize; }
                break;
            case Key.Escape: Tree!.Quit(); break;
        }
    }
    private void Swim(float delta)
    {
        for (var i = 0; i < _fish.Length; i++)
        {
            ref var fish = ref _fish[i];
            if (!fish.Active)
            {
                var spawn = new Vector2(Simulation.Size.X * (.16f + .13f * i), Simulation.Size.Y - 65 - 30 * (i % 3));
                if (Simulation.Time < 4 || !Surface.Submerged(spawn, 26)) continue;
                fish.Position = spawn; fish.Direction = i % 2 == 0 ? 1 : -1; fish.Active = true;
            }
            var velocity = new Vector2(fish.Direction * (34 + 4 * i), MathF.Sin((float)Simulation.Time * .8f + i) * 15);
            var next = fish.Position + velocity * delta;
            if (Surface.Submerged(next, 26)) fish.Position = next;
            else fish.Direction = -fish.Direction;
        }
    }
    private void DrawScene(CanvasItem canvas)
    {
        canvas.DrawRect(new(Vector2.Zero, Simulation.Size), Cream);
        canvas.DrawLine(new(0, Simulation.Size.Y - 1), new(Simulation.Size.X, Simulation.Size.Y - 1), Light, 2);
        if (Simulation.Duck.IsValid()) DrawDuck(canvas, Simulation.DuckPose);
        if (Simulation.Boat.IsValid()) DrawBoat(canvas, Simulation.BoatPose);
        for (var i = 0; i < _fish.Length; i++)
        {
            var fish = _fish[i]; if (!fish.Active || !Surface.Submerged(fish.Position)) continue;
            canvas.DrawSetTransform(fish.Position, 0, new(fish.Direction, 1));
            var tint = i % 3 == 0 ? Peach : i % 3 == 1 ? Lavender : Butter;
            var tail = MathF.Sin((float)Simulation.Time * 7 + i) * 4;
            canvas.DrawColoredPolygon([new(-12, 0), new(-28, -10 + tail), new(-26, 10 + tail)], tint);
            canvas.DrawSetTransform(fish.Position, 0, new(fish.Direction * 1.9f, 1));
            canvas.DrawCircle(Vector2.Zero, 10, tint);
            canvas.DrawSetTransform(fish.Position, 0, new(fish.Direction, 1));
            canvas.DrawCircle(new(10, -2), 2, Ink); canvas.DrawLine(new(-2, -2), new(-7, 4), Sail, 1.5f, true);
        }
        canvas.DrawSetTransform(Vector2.Zero);
        if (Simulation.Dragged.IsValid()) canvas.DrawCircle(_pointer, 5, new Color(Ink.R, Ink.G, Ink.B, .6f), false, 1.5f, true);
    }
    private void DrawHUD(CanvasItem canvas)
    {
        canvas.DrawString(_font, new(24, 33), "WaterPlayground", fontSize: 18, modulate: Ink);
        canvas.DrawString(_font, new(24, 57), Paused ? "Paused · Space to play" : (Size.X < 700 ? "Drag toys · Space: pause · R: refill · F11: fullscreen" : "Drag toys / water · Space: pause · R: refill · F11: fullscreen"), fontSize: 13, modulate: Ink);
        Span<char> text = stackalloc char[120];
        text.TryWrite(CultureInfo.InvariantCulture, $"{Simulation.ActiveCount:N0} / {Simulation.Count:N0} particles · {(Simulation.UseGPU ? "GPU" : "CPU")} {Simulation.StepMS:0.0} ms · {Engine.FramesPerSecond:0} / 144 FPS", out var length);
        var position = new Vector2(24, 81);
        foreach (var character in text[..length]) position.X += _font.DrawChar(canvas, position, character, 13, Ink);
        canvas.DrawString(_font, new(24, 104), _deviceError.Length > 0 ? _deviceError : _presentation, fontSize: 12, modulate: Ink);
    }
    private static void DrawDuck(CanvasItem canvas, Transform pose)
    {
        canvas.DrawSetTransform(pose.Origin, pose.Rotation, new(1, .82f));
        canvas.DrawCircle(new(-5, 0), 52, Butter);
        canvas.DrawSetTransformMatrix(pose);
        canvas.DrawColoredPolygon([new(-40, -3), new(-70, -26), new(-60, 16), new(-27, 26)], Butter);
        canvas.DrawCircle(new(31, -32), 27, Butter);
        canvas.DrawColoredPolygon([new(51, -35), new(76, -24), new(51, -17)], Peach);
        canvas.DrawCircle(new(39, -39), 3.5f, Ink);
        canvas.DrawCircle(new(40, -40), 1, Sail);
        canvas.DrawSetTransform(pose.Origin, pose.Rotation, new(1, .65f));
        canvas.DrawCircle(new(-12, 2), 26, Wing);
    }

    private static void DrawBoat(CanvasItem canvas, Transform pose)
    {
        canvas.DrawSetTransformMatrix(pose);
        canvas.DrawLine(new(-8, 0), new(-8, -112), Ink, 4, true);
        canvas.DrawColoredPolygon([new(-14, -106), new(-14, -22), new(-75, -22)], Sail);
        canvas.DrawColoredPolygon([new(-2, -100), new(67, -22), new(-2, -22)], Lavender);
        canvas.DrawColoredPolygon([new(-8, -115), new(26, -107), new(-8, -98)], Peach);
        canvas.DrawColoredPolygon(WaterSimulation.Hull, Peach);
        canvas.DrawColoredPolygon([new(-76, -1), new(76, -1), new(62, 18), new(-58, 18)], Sail);
        canvas.DrawCircle(new(-29, 7), 5, Light); canvas.DrawCircle(new(0, 7), 5, Light); canvas.DrawCircle(new(29, 7), 5, Light);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing) Simulation.Dispose();
        base.Dispose(disposing);
        if (disposing) { _device?.Dispose(); _buttonStyle.Dispose(); _activeStyle.Dispose(); _focusStyle.Dispose(); Engine.MaxPhysicsStepsPerFrame = _previousPhysicsBudget; }
    }
}
