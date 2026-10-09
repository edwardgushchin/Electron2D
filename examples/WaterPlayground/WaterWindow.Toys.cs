namespace Electron2D.Examples.WaterPlayground;

internal sealed partial class WaterWindow
{
    private readonly List<Button> _tools = [];
    private Button _flow = null!, _drain = null!;
    private bool _movingFaucet;
    private Vector2 FaucetHandle => new((ViewTransform * new Vector2(Simulation.FaucetX, 0)).X, 12);

    private void MakeTools()
    {
        Add("Bucket", WaterToy.Bucket); Add("Wheel", WaterToy.Wheel); Add("Gate", WaterToy.Gate);
        Add("Wood", WaterToy.Wood); Add("Steel", WaterToy.Steel); Add("Ball", WaterToy.Ball);
        _flow = Tool("Flow 100%"); _flow.Pressed += CycleFlow;
        _drain = Tool("Drain closed"); _drain.Pressed += ToggleDrain;
        void Add(string label, WaterToy kind)
        {
            var button = Tool(label); button.ToggleMode = true;
            button.Toggled += enabled =>
            {
                if (enabled)
                {
                    if (!Simulation.AddToy(kind)) button.SetPressedNoSignal(Simulation.KindCount(kind) > 0);
                }
                else Simulation.RemoveToy(kind);
                _drawing.QueueRedraw();
            };
        }
        Button Tool(string label)
        {
            var button = ModeButton(label); button.ToggleMode = false; button.ButtonGroup = null;
            button.AddThemeFontSizeOverride("font_size", 13); _tools.Add(button); return button;
        }
    }
    private void FitTools()
    {
        var columns = Math.Min(_tools.Count, Math.Max(4, (Size.X - 32) / 94));
        var rows = (_tools.Count + columns - 1) / columns;
        var width = Math.Min(96, (Size.X - 32f - (columns - 1) * 6) / columns);
        var start = (Size.X - (columns * width + (columns - 1) * 6)) / 2;
        for (var i = 0; i < _tools.Count; i++)
        { _tools[i].Position = new(start + i % columns * (width + 6), Size.Y - 16 - rows * 36 + i / columns * 36); _tools[i].Size = new(width, 30); }
    }
    private bool OverControls(Vector2 p)
    {
        if (!InterfaceVisible) return false;
        if (new Rect2(_cpu.Position, _cpu.Size).HasPoint(p) || new Rect2(_gpu.Position, _gpu.Size).HasPoint(p)) return true;
        foreach (var button in _tools) if (new Rect2(button.Position, button.Size).HasPoint(p)) return true;
        return false;
    }
    private void CycleFlow() { Simulation.FaucetFlow = Simulation.FaucetFlow <= 0 ? 1 : Math.Max(0, Simulation.FaucetFlow - .25f); UpdateValves(); }
    private void ToggleDrain() { Simulation.DrainOpen = !Simulation.DrainOpen; UpdateValves(); }
    private void UpdateValves()
    { _flow.Text = $"Flow {Simulation.FaucetFlow * 100:0}%"; _drain.Text = Simulation.DrainOpen ? "Drain open" : "Drain closed"; _hud.QueueRedraw(); }
    private bool ToyInput(InputEvent input)
    {
        if (input is InputEventMouseMotion motion && _movingFaucet)
        { Simulation.FaucetX = Math.Clamp(ToWorld(motion.Position).X, 100, Simulation.Size.X - 100); _hud.QueueRedraw(); return true; }
        if (input is InputEventMouseButton button)
        {
            if (button.ButtonIndex == MouseButton.Left && !button.Pressed && _movingFaucet) { _movingFaucet = false; return true; }
            if (button.Pressed && OverControls(button.Position)) return true;
            if (button.ButtonIndex is MouseButton.WheelUp or MouseButton.WheelDown && button.Pressed)
            {
                var direction = button.ButtonIndex == MouseButton.WheelUp ? 1 : -1;
                if (Simulation.Dragged.IsValid()) Simulation.TiltHeld(direction);
                else { Simulation.FaucetFlow = Math.Clamp(Simulation.FaucetFlow + direction * .1f, 0, 1); UpdateValves(); }
                return true;
            }
            if (InterfaceVisible && button.Pressed && button.Position.DistanceTo(FaucetHandle) < 28)
            {
                if (button.ButtonIndex == MouseButton.Left) _movingFaucet = true;
                if (button.ButtonIndex == MouseButton.Right) Simulation.FaucetAngle = Simulation.FaucetAngle >= .5f ? -.5f : Simulation.FaucetAngle + .25f;
                return true;
            }
        }
        if (input is InputEventKey { Pressed: true, Echo: false } key)
        {
            if (key.PhysicalKeycode == Key.F) { CycleFlow(); return true; }
            if (key.PhysicalKeycode == Key.D) { ToggleDrain(); return true; }
            if (key.PhysicalKeycode is Key.Q or Key.E)
            {
                var direction = key.PhysicalKeycode == Key.Q ? -1 : 1;
                if (Simulation.Dragged.IsValid()) Simulation.TiltHeld(direction);
                else { Simulation.FaucetAngle = Math.Clamp(Simulation.FaucetAngle + direction * .1f, -.55f, .55f); _hud.QueueRedraw(); }
                return true;
            }
        }
        return false;
    }
    private void DrawTools(CanvasItem canvas)
    {
        if (Simulation.ActorExists(WaterSimulation.WheelSlot))
        {
            var wheel = Simulation.ActorPose(WaterSimulation.WheelSlot).Origin;
            var platform = Simulation.ActorPose(WaterSimulation.PlatformSlot).Origin;
            canvas.DrawLine(wheel, new(platform.X, wheel.Y), Ink, 2, true);
            canvas.DrawLine(new(platform.X, wheel.Y), platform, Ink, 2, true);
            canvas.DrawLine(new(platform.X, 305), new(platform.X, 660), Light, 4, true);
        }
        for (var slot = WaterSimulation.BucketSlot; slot < WaterSimulation.BodyCount; slot++)
        {
            if (!Simulation.ActorExists(slot)) continue;
            var pose = Simulation.ActorPose(slot); canvas.DrawSetTransformMatrix(pose);
            switch (Simulation.ActorKind(slot))
            {
                case WaterToy.Bucket:
                    canvas.DrawArc(new(0, -40), 49, MathF.PI, MathF.Tau, 24, Ink, 4, true);
                    canvas.DrawRect(new(-57, -45, 10, 90), Peach); canvas.DrawRect(new(47, -45, 10, 90), Peach);
                    canvas.DrawRect(new(-57, 38, 114, 12), Peach);
                    canvas.DrawLine(new(-55, -46), new(-45, -46), Sail, 3); canvas.DrawLine(new(45, -46), new(55, -46), Sail, 3);
                    break;
                case WaterToy.Wheel:
                    for (var i = 0; i < 8; i++)
                    {
                        var angle = pose.Rotation + i * MathF.PI / 4;
                        canvas.DrawSetTransform(pose.Origin, angle);
                        canvas.DrawRect(new(20, -8, 50, 16), i % 2 == 0 ? Peach : Butter);
                    }
                    canvas.DrawSetTransformMatrix(pose); canvas.DrawCircle(Vector2.Zero, 16, Lavender); canvas.DrawCircle(Vector2.Zero, 5, Ink);
                    break;
                case WaterToy.Gate:
                    canvas.DrawRect(new(-6, -180, 12, 360), Lavender);
                    canvas.DrawRect(new(-20, -200, 40, 20), Peach); canvas.DrawLine(new(-9, -190), new(9, -190), Sail, 3);
                    break;
                case WaterToy.Ball:
                    canvas.DrawCircle(Vector2.Zero, 28, Peach); canvas.DrawArc(Vector2.Zero, 27, -.7f, 2.4f, 24, Sail, 6, true); break;
                case WaterToy.Wood:
                    canvas.DrawRect(new(-18, -18, 36, 36), Butter); canvas.DrawLine(new(-12, -7), new(12, -7), Wing, 2); canvas.DrawLine(new(-12, 6), new(7, 6), Wing, 2); break;
                case WaterToy.Steel:
                    canvas.DrawCircle(Vector2.Zero, 14, Ink); canvas.DrawCircle(new(-4, -5), 4, Light); break;
                case WaterToy.Platform:
                    canvas.DrawRect(new(-50, -6, 100, 12), Lavender); canvas.DrawLine(new(-48, -6), new(48, -6), Sail, 2); break;
            }
        }
        canvas.DrawSetTransform(Vector2.Zero);
        var drain = Simulation.DrainPosition;
        canvas.DrawRect(new(drain.X - 28, drain.Y - 2, 56, 10), Simulation.DrainOpen ? Ink : Lavender);
        for (var i = -20; i <= 20; i += 10) canvas.DrawLine(drain + new Vector2(i, 0), drain + new Vector2(i, 6), Light, 2);
    }
    private void DrawFaucet(CanvasItem canvas)
    {
        var handle = FaucetHandle;
        canvas.DrawCircle(handle, 12, Lavender);
        var direction = new Vector2(MathF.Sin(Simulation.FaucetAngle), MathF.Cos(Simulation.FaucetAngle));
        canvas.DrawLine(handle - direction * 6, handle + direction * 9, Ink, 3, true);
        canvas.DrawCircle(handle + direction * 9, 3, Simulation.FaucetFlow > 0 ? Water : Ink);
    }
}
