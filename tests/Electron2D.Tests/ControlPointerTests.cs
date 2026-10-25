using Electron2D;

internal static partial class ControlPointerTests
{
    private sealed class TestViewport : Viewport { public override Rect2 GetVisibleRect() => new(0, 0, 128, 96); }
    private sealed class Probe : Control
    {
        internal readonly List<(MouseButton Button, bool Pressed, Vector2 Position, int Device)> Events = [];
        internal Action<InputEventMouseButton>? During;
        internal InputEvent? Borrowed;
        protected override void OnGUIInput(InputEvent input)
        {
            if (input is not InputEventMouseButton button) return;
            Events.Add((button.ButtonIndex, button.Pressed, button.Position, button.Device)); Borrowed = input; During?.Invoke(button);
        }
    }
    internal static void Run()
    {
        var root = new TestViewport(); var first = new Probe { Name = "First", FocusMode = FocusMode.Click, Position = new(4, 8), Size = new(20, 20) };
        var second = new Probe { Name = "Second", Position = new(48, 16), Scale = new(2, 2), Size = new(20, 20) };
        var third = new Probe { Name = "Third", Position = new(80, 16), Size = new(20, 20) };
        root.AddChild(first); root.AddChild(second); root.AddChild(third); using var tree = new SceneTree(root);
        second.GrabClickFocus(); tree.FlushDeferred(); Check(second.Events.Count == 0, "No existing capture remains unchanged.");
        Button(root, MouseButton.Left, true, new(10, 12)); Button(root, MouseButton.Right, true, new(12, 14));
        first.Events.Clear(); second.Events.Clear(); third.Events.Clear();
        Check(ReferenceEquals(root.GetGUIFocusOwner(), first), "Pointer press sets keyboard focus.");
        second.GrabClickFocus(); third.GrabClickFocus(); second.GrabClickFocus();
        Check(first.Events.Count == 0, "Request is deferred.");
        first.During = e => { if (!e.Pressed) Check(second.Events.Count == 0 && third.Events.Count == 0, "New presses wait until the release batch completes."); };
        tree.FlushDeferred(); first.During = null;
        Check(first.Events.Count == 2 && first.Events.All(e => !e.Pressed && e.Device == InputEvent.DeviceIdInternal && e.Position == new Vector2(8, 6)), "Held primary buttons release old capture locally.");
        Check(third.Events.Count == 0, "The last request selects the transfer target.");
        Check(second.Events.Count == 2 && second.Events.All(e => e.Pressed && e.Device == InputEvent.DeviceIdInternal && e.Position == new Vector2(-18, -1)), "Target local transformed presses.");
        Check(second.Borrowed!.IsDisposed && ReferenceEquals(root.GetGUIFocusOwner(), first), "Synthetic events are temporary and preserve keyboard focus.");
        second.Events.Clear(); Button(root, MouseButton.Left, false, new(120, 80));
        Check(second.Events.Count == 1 && !second.Events[0].Pressed, "Outside release follows transferred capture.");
        second.Events.Clear(); second.GrabClickFocus(); tree.FlushDeferred(); tree.FlushDeferred(); Check(second.Events.Count == 0, "Same target is a no-op.");
        for (var i = 0; i < 32; i++) { second.GrabClickFocus(); tree.FlushDeferred(); }
        var bytes = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { second.GrabClickFocus(); tree.FlushDeferred(); }
        var allocated = GC.GetAllocatedBytesForCurrentThread() - bytes; Check(allocated == 0, $"Warmed same-target capture requests allocated {allocated} bytes.");
        Button(root, MouseButton.Right, false, new(120, 80));
        Button(root, MouseButton.Left, true, new(10, 12)); root.CanvasTransform = new(Vector2.Zero, new Vector2(0, 1), Vector2.Zero); first.Events.Clear(); second.GrabClickFocus();
        Throws<AggregateException>(tree.FlushDeferred); Check(first.Events.Count == 0, "Singular target rejects before releasing capture."); root.CanvasTransform = Transform.Identity;
        Button(root, MouseButton.Left, false, new(120, 80));
        Button(root, MouseButton.Left, true, new(10, 12)); first.Events.Clear();
        first.During = e => { if (!e.Pressed) throw new ArithmeticException(); };
        second.Events.Clear(); second.GrabClickFocus(); Throws<AggregateException>(() => tree.FlushDeferred()); first.During = null;
        Check(second.Events.Count == 1 && second.Events[0].Pressed, "Release callback failure preserves the later press in the same flush.");
        tree.FlushDeferred(); Check(second.Events.Count == 1, "A later flush does not replay the transferred press.");
        Button(root, MouseButton.Left, false, new(120, 80));
        Button(root, MouseButton.Left, true, new(10, 12)); second.Events.Clear(); second.GrabClickFocus(); tree.FlushDeferred();
        Button(root, MouseButton.Left, false, new(120, 80)); tree.FlushDeferred();
        Check(second.Events.Count == 2 && second.Events[0].Pressed && !second.Events[1].Pressed, "A physical release follows the synthetic press completed by the previous flush.");
        Button(root, MouseButton.Left, true, new(10, 12)); var gone = new Probe { Size = new(10, 10) }; root.AddChild(gone); gone.GrabClickFocus(); gone.Dispose();
        first.Events.Clear(); tree.FlushDeferred(); Check(first.Events.Count == 0, "Disposed pending target cancels before release.");
        Button(root, MouseButton.Left, false, new(120, 80));
        Button(root, MouseButton.Left, true, new(10, 12)); first.During = e => { if (!e.Pressed) first.Dispose(); };
        second.Events.Clear(); second.GrabClickFocus(); tree.FlushDeferred(); tree.FlushDeferred();
        Check(second.Events.Count == 1 && second.Events[0].Pressed, "Disposal of old capture during release retains the new capture.");
        Button(root, MouseButton.Left, false, new(120, 80));
        Throws<InvalidOperationException>(() => Task.Run(second.GrabClickFocus).GetAwaiter().GetResult());
        using var detached = new Control(); Throws<InvalidOperationException>(detached.GrabClickFocus); Throws<InvalidOperationException>(() => detached.GetScreenPosition());
        Throws<ArgumentException>(() => second.WarpMouse(new(float.NaN, 0))); Throws<InvalidOperationException>(() => second.WarpMouse(Vector2.Zero));
        Check(second.GetScreenPosition() == second.GetScreenTransform().Origin, "Managed screen origin composes current transform.");
        var nested = new SubViewport { Name = "Nested", Size = new(64, 64) }; var nestedA = new Probe { Name = "A", Size = new(16, 16) }; var nestedB = new Probe { Name = "B", Position = new(24, 0), Size = new(16, 16) };
        nested.AddChild(nestedA); nested.AddChild(nestedB); root.AddChild(nested);
        Button(nested, MouseButton.Middle, true, new(4, 4)); nestedB.GrabClickFocus(); tree.FlushDeferred(); tree.FlushDeferred();
        Check(nestedB.Events.Count == 1 && nestedB.Events[0].Button == MouseButton.Middle && nestedB.Events[0].Pressed, "Independent viewport transfer includes middle button.");
        Check(nestedB.GetScreenPosition() == nestedB.GetScreenTransform().Origin, "Independent viewport screen query shares canvas transform.");
        nestedA.GrabClickFocus(); nested.Dispose(); tree.FlushDeferred(); tree.FlushDeferred();
        Console.WriteLine("Control click capture deferred transfer, buttons, coordinates, lifetime and errors passed.");
    }
    private static void Button(Viewport root, MouseButton button, bool pressed, Vector2 point)
    { using var input = new InputEventMouseButton { ButtonIndex = button, Pressed = pressed, Position = point, GlobalPosition = point }; root.PushInput(input, inLocalCoordinates: true); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Throws<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
