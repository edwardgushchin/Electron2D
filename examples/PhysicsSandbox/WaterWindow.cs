using System.Globalization;

namespace Electron2D.Examples.PhysicsSandbox;

/// <summary>A resizable, full-window water playground with two floating toys.</summary>
internal sealed class WaterWindow : Window
{
    internal static readonly Color Cream = Color.FromHTML("#F7F4EB"), Water = Color.FromHTML("#A7D8DE");
    private static readonly Color Butter = Color.FromHTML("#F5D684"), Wing = Color.FromHTML("#EBC46F"),
        Peach = Color.FromHTML("#EBAA8D"), Ink = Color.FromHTML("#54666F"), Lavender = Color.FromHTML("#BDB4D9"),
        Sail = Color.FromHTML("#FFFDF5"), Light = Color.FromHTML("#D9EDEF");
    private readonly Entity _drawing;
    private readonly Entity _waterDrawing;
    private readonly ArrayMesh _disc = new();
    private readonly MultiMesh _waterMesh;
    private readonly int _previousPhysicsBudget = Engine.MaxPhysicsStepsPerFrame;
    private float[] _instances = [];
    private readonly Font _font;
    private RenderingDevice? _device;
    private readonly Button _cpu, _gpu;
    private readonly StyleBoxFlat _buttonStyle, _activeStyle, _focusStyle;
    private string _deviceError = "";
    private bool _held;
    private Vector2 _pointer;
    internal WaterSimulation Simulation { get; private set; }
    internal bool Paused { get; private set; }

    internal WaterWindow(Font font, bool startOnGPU = true)
    {
        _font = font;
        Engine.MaxPhysicsStepsPerFrame = 1;
        Name = "WaterPlayground"; Title = "Water playground";
        Size = new(1152, 800); MinSize = new(480, 360); Unresizable = false;
        Simulation = new(Size);
        _drawing = new Entity { Name = "WaterAndToys" };
        _drawing.Draw += DrawScene;
        AddChild(_drawing);
        const int sides = 24;
        var vertices = new Vector2[sides + 1]; var indices = new int[sides * 3]; var colors = new Color[sides + 1];
        Array.Fill(colors, Water);
        for (var i = 0; i < sides; i++)
        {
            var angle = i * MathF.Tau / sides;
            vertices[i + 1] = new Vector2(MathF.Cos(angle), MathF.Sin(angle));
            indices[3 * i] = 0; indices[3 * i + 1] = i + 1; indices[3 * i + 2] = (i + 1) % sides + 1;
        }
        _disc.AddSurfaceFromArrays(Mesh.PrimitiveType.Triangles, new MeshSurfaceData { Vertices = vertices, Indices = indices, Colors = colors });
        _waterMesh = new MultiMesh { Mesh = _disc };
        var waterLayer = new CanvasGroup { Name = "TransparentWater", SelfModulate = new(1, 1, 1, .65f), FitMargin = 0, ClearMargin = 0 };
        _waterDrawing = new Entity { Name = "WaterParticles" };
        _waterDrawing.Draw += canvas => canvas.DrawMultiMesh(_waterMesh);
        waterLayer.AddChild(_waterDrawing); AddChild(waterLayer);
        _buttonStyle = new StyleBoxFlat { BGColor = Color.FromHTML("#EAE7EF"), BorderColor = Color.FromHTML("#B1A9C7") };
        _activeStyle = new StyleBoxFlat { BGColor = Lavender, BorderColor = Ink };
        _focusStyle = new StyleBoxFlat { BGColor = new(0, 0, 0, 0), BorderColor = Ink };
        _focusStyle.SetCornerRadiusAll(8); _focusStyle.SetBorderWidthAll(2);
        _buttonStyle.SetCornerRadiusAll(8); _activeStyle.SetCornerRadiusAll(8); _activeStyle.SetBorderWidthAll(1);
        _cpu = ModeButton("CPU"); _gpu = ModeButton("GPU");
        _cpu.Pressed += () => SetMode(false); _gpu.Pressed += () => SetMode(true);
        PublishWater();
        PhysicsProcessEnabled = ProcessEnabled = InputEnabled = true;
        SizeChanged += () => { if (Size.X >= 320 && Size.Y >= 240) Simulation.Resize(Size); PlaceModes(); };
        FocusExited += () => { _held = false; Simulation.EndDrag(); };
        Ready += _ => { RenderingServer.SetDefaultClearColor(Cream); SetMode(startOnGPU); };
        PlaceModes();
    }

    private Button ModeButton(string text)
    {
        var button = new Button { Name = text, Text = text, Size = new(72, 34), ToggleMode = true };
        button.AddThemeFontOverride("font", _font); button.AddThemeFontSizeOverride("font_size", 14);
        foreach (var state in new[] { "normal", "hover" }) button.AddThemeStyleBoxOverride(state, _buttonStyle);
        button.AddThemeStyleBoxOverride("focus", _focusStyle);
        foreach (var state in new[] { "pressed", "hover_pressed" }) button.AddThemeStyleBoxOverride(state, _activeStyle);
        foreach (var state in new[] { "font_color", "font_hover_color", "font_pressed_color", "font_focus_color" }) button.AddThemeColorOverride(state, Ink);
        AddChild(button); return button;
    }
    private void PlaceModes() { _cpu.Position = new(Size.X - 176, 22); _gpu.Position = new(Size.X - 96, 22); }
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
        var gpu = Simulation.UseGPU; Simulation.Dispose(); Simulation = new(Size); if (gpu) Simulation.SetUseGPU(true, _device); Paused = false; _held = false;
    }

    protected override void OnPhysicsProcess(double delta)
    {
        if (Paused) return;
        Simulation.Step(delta);
    }

    protected override void OnProcess(double delta) { PublishWater(); _drawing.QueueRedraw(); }

    private void PublishWater()
    {
        if (_waterMesh.InstanceCount != Simulation.Count)
        {
            _waterMesh.InstanceCount = Simulation.Count;
            _instances = new float[Simulation.Count * 8];
            for (var i = 0; i < Simulation.Count; i++) { _instances[i * 8] = Simulation.Spacing * .84f; _instances[i * 8 + 5] = Simulation.Spacing * .84f; }
        }
        for (var i = 0; i < Simulation.Count; i++)
        {
            _instances[i * 8] = _instances[i * 8 + 5] = Simulation.Spacing * .84f;
            _instances[i * 8 + 3] = Simulation.Positions[i].X;
            _instances[i * 8 + 7] = Simulation.Positions[i].Y;
        }
        _waterMesh.Buffer = _instances; _waterMesh.CustomAABB = new(Vector2.Zero, Size);
        _waterDrawing.QueueRedraw();
    }

    protected override void OnInput(InputEvent input)
    {
        if (input is InputEventMouseMotion motion) { _pointer = motion.Position; Simulation.MovePointer(_pointer); }
        if (input is InputEventMouseButton { ButtonIndex: MouseButton.Left } button) { _pointer = button.Position; _held = button.Pressed; if (_held) Simulation.BeginDrag(_pointer); else Simulation.EndDrag(); }
        if (input is not InputEventKey { Pressed: true, Echo: false } key) return;
        switch (key.PhysicalKeycode)
        {
            case Key.R: Reset(); break;
            case Key.Tab: SetMode(!Simulation.UseGPU); break;
            case Key.Space: Paused = !Paused; break;
            case Key.Escape: Tree!.Quit(); break;
        }
    }

    private void DrawScene(CanvasItem canvas)
    {
        canvas.DrawRect(new(Vector2.Zero, Size), Cream);
        if (Simulation.Duck.IsValid()) DrawDuck(canvas, Simulation.DuckPose);
        if (Simulation.Boat.IsValid()) DrawBoat(canvas, Simulation.BoatPose);
        canvas.DrawSetTransform(Vector2.Zero);
        canvas.DrawString(_font, new(24, 34), "Water playground", fontSize: 18, modulate: Ink);
        canvas.DrawString(_font, new(24, 58), Paused ? "Paused · Space to play" : "Drag toys / water · Space: pause · R: reset", fontSize: 13, modulate: Ink);
        Span<char> text = stackalloc char[100];
        text.TryWrite(CultureInfo.InvariantCulture, $"{Simulation.Count:N0} particles · {(Simulation.UseGPU ? "GPU" : "CPU")} · {Simulation.StepMS:0.0} ms/step · {Engine.FramesPerSecond:0} FPS", out var length);
        var position = new Vector2(24, 82);
        foreach (var character in text[..length]) position.X += _font.DrawChar(canvas, position, character, 13, Ink);
        if (_deviceError.Length > 0) canvas.DrawString(_font, new(24, 105), _deviceError, fontSize: 13, modulate: Ink);
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
        if (disposing) { _device?.Dispose(); _waterMesh.Dispose(); _disc.Dispose(); _buttonStyle.Dispose(); _activeStyle.Dispose(); _focusStyle.Dispose(); Engine.MaxPhysicsStepsPerFrame = _previousPhysicsBudget; }
    }
}
