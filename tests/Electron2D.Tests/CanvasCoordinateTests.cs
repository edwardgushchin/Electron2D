using Electron2D;

internal static class CanvasCoordinateTests
{
    internal static void Run()
    {
        using var window = new Window();
        Check(window.CanvasTransform == Transform.Identity && window.GlobalCanvasTransform == Transform.Identity && window.GetFinalTransform() == Transform.Identity && window.GetScreenTransform() == Transform.Identity, "Default viewport transforms.");
        window.CanvasTransform = new(0, new(2, 3)); window.GlobalCanvasTransform = new(0, new(4, 5));
        var descriptors = window.GetPropertyList().Where(p => p.Name is "CanvasTransform" or "GlobalCanvasTransform").ToArray();
        Check(descriptors.Length == 2 && descriptors.All(p => !p.IsStored), "Canvas transforms are runtime metadata, not stored scene properties.");
        using var scene = new PackedScene(); scene.Pack(window); using var copy = (Window)scene.Instantiate();
        Check(copy.CanvasTransform == Transform.Identity && copy.GlobalCanvasTransform == Transform.Identity, "PackedScene does not persist viewport runtime transforms.");
        Reject<ArgumentException>(() => window.CanvasTransform = new(float.NaN, Vector2.Zero));
        Reject<ArgumentException>(() => window.GlobalCanvasTransform = new(0, new(float.PositiveInfinity, 0)));
        Reject<InvalidOperationException>(() => window.GetMousePosition()); Reject<InvalidOperationException>(() => window.WarpMouse(Vector2.Zero));
        Reject<ArgumentException>(() => window.WarpMouse(new(float.NaN, 0)));
        var root = new TestViewport { CanvasTransform = new(new(2, 0), new(0, 3), new(10, 20)), GlobalCanvasTransform = new(0, new(100, 200)) };
        var parent = new Entity { Position = new(4, 5), Scale = new(2, 2) }; var item = new Probe { InputEnabled = true, Position = new(1, 2) };
        root.AddChild(parent); parent.AddChild(item);
        Check(item.GetGlobalTransformWithCanvas() == item.GetGlobalTransform(), "Detached item ignores even a viewport ancestor's canvas transform.");
        Reject<InvalidOperationException>(() => item.GetCanvasTransform()); Reject<InvalidOperationException>(() => item.GetViewportTransform());
        using var tree = new SceneTree(root);
        Check(item.GetViewportRect() == new Rect(0, 0, 96, 80), "Viewport bounds ignore canvas transforms.");
        Near(item.GetGlobalTransformWithCanvas() * Vector2.Zero, new(22, 47));
        Near(item.GetViewportTransform() * item.GetGlobalTransform() * Vector2.Zero, new(122, 247));
        Near(item.GetScreenTransform() * Vector2.Zero, new(122, 247));
        Near(item.MakeCanvasPositionLocal(new(26, 53)), Vector2.One);
        var notices = 0; item.TransformChanged += _ => notices++;
        root.CanvasTransform = new(0, new(30, 40)); root.GlobalCanvasTransform = new(new(2, 0), new(0, 4), new(10, 20));
        Check(notices == 0 && item.Position == new Vector2(1, 2), "Viewport transforms leave logical transforms and events intact.");
        item.TopLevel = true; Near(item.GetGlobalTransformWithCanvas() * Vector2.Zero, new(31, 42)); item.TopLevel = false;
        var neutral = new Node(); root.AddChild(neutral); parent.RemoveChild(item); neutral.AddChild(item);
        Near(item.GetGlobalTransformWithCanvas() * Vector2.Zero, new(31, 42));
        using var input = new InputEventMouseMotion { Position = new(74, 196), GlobalPosition = new(999, 888), Relative = new(8, 12), Velocity = new(16, 24), ScreenRelative = new(5, 6), ScreenVelocity = new(7, 8) };
        InputEvent? received = null; var calls = 0;
        item.InputAction = e =>
        {
            calls++; received = e; var motion = (InputEventMouseMotion)e;
            Near(motion.Position, new(32, 44)); Near(motion.GlobalPosition, new(32, 44)); Near(motion.Relative, new(4, 3)); Near(motion.Velocity, new(8, 6));
            Near(motion.ScreenRelative, input.ScreenRelative); Near(motion.ScreenVelocity, input.ScreenVelocity);
            using var local = (InputEventMouseMotion)item.MakeInputLocal(motion); Near(local.Position, new(1, 2)); Near(local.GlobalPosition, new(32, 44));
            Check(!ReferenceEquals(local, motion), "Canvas conversion owns a separate positional copy.");
            Reject<InvalidOperationException>(() => root.PushInput(input)); root.SetInputAsHandled();
        };
        root.PushInput(input); Check(calls == 1 && received!.IsDisposed && !input.IsDisposed && input.Position == new Vector2(74, 196), "Dispatch owns only the localized copy.");
        item.InputAction = e => { Check(ReferenceEquals(e, input), "Local coordinates borrow the original."); root.SetInputAsHandled(); };
        root.PushInput(input, inLocalCoordinates: true);
        using var key = new InputEventKey { Keycode = Key.A };
        Check(ReferenceEquals(item.MakeInputLocal(key), key), "Non-positional canvas conversion returns its input.");
        item.InputAction = e => { received = e; throw new ApplicationException("input callback"); };
        Reject<AggregateException>(() => root.PushInput(input)); Check(received!.IsDisposed && !input.IsDisposed, "Failure disposes temporary input, not the caller event.");
        item.InputAction = e => { received = e; root.SetInputAsHandled(); }; tree.DispatchInputEvent(input); Check(received!.IsDisposed, "Main-loop dispatch uses the same viewport localization.");
        item.InputAction = null;
        var singular = new Transform(Vector2.Zero, Vector2.Zero, Vector2.Zero); root.CanvasTransform = singular;
        Reject<InvalidOperationException>(() => item.MakeCanvasPositionLocal(Vector2.Zero)); Reject<InvalidOperationException>(() => item.MakeInputLocal(input));
        root.GlobalCanvasTransform = singular; Reject<InvalidOperationException>(() => root.PushInput(input)); root.PushInput(input, inLocalCoordinates: true);
        root.CanvasTransform = Transform.Identity; root.GlobalCanvasTransform = Transform.Identity;
        Reject<ArgumentException>(() => item.MakeCanvasPositionLocal(new(float.NaN, 0)));
        Reject<ArgumentNullException>(() => item.MakeInputLocal(null!)); Reject<ArgumentNullException>(() => root.PushInput(null!));
        using var dead = new InputEventScreenTouch(); dead.Dispose(); Reject<ObjectDisposedException>(() => root.PushInput(dead));
        Reject<InvalidOperationException>(() => Task.Run(() => root.CanvasTransform = Transform.Identity).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => item.GetViewportTransform()).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => root.PushInput(input)).GetAwaiter().GetResult());
        VerifyInputSiblings(item, root);
        for (var i = 0; i < 1000; i++) item.MakeCanvasPositionLocal(Vector2.One);
        var allocated = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1000; i++) item.MakeCanvasPositionLocal(Vector2.One);
        Check(GC.GetAllocatedBytesForCurrentThread() == allocated, "Warm point queries allocate nothing.");
        tree.Dispose(); Reject<ObjectDisposedException>(() => item.GetCanvasTransform()); Reject<ObjectDisposedException>(() => root.GetFinalTransform());
        Console.WriteLine("Canvas coordinate, viewport input and ownership checks passed.");
    }

    private static void VerifyInputSiblings(Probe item, TestViewport root)
    {
        root.CanvasTransform = new(0, new(10, 20)); item.Position = Vector2.Zero;
        foreach (InputEvent source in new InputEvent[]
        {
            new InputEventMouseMotion { Position = new(12, 24), GlobalPosition = new(99, 98), Relative = new(2, 3), Velocity = new(4, 5) },
            new InputEventMouseButton { Position = new(12, 24), GlobalPosition = new(99, 98), Pressed = true },
            new InputEventScreenTouch { Position = new(12, 24), Index = 2, Pressed = true },
            new InputEventScreenDrag { Position = new(12, 24), Relative = new(2, 3), ScreenRelative = new(6, 7) },
            new InputEventMagnifyGesture { Position = new(12, 24), Factor = 1.5f },
            new InputEventPanGesture { Position = new(12, 24), Delta = new(3, 4) },
        })
        {
            using (source)
            using (var local = item.MakeInputLocal(source))
            {
                var position = local switch { InputEventMouse mouse => mouse.Position, InputEventScreenTouch touch => touch.Position, InputEventScreenDrag dragEvent => dragEvent.Position, InputEventGesture gesture => gesture.Position, _ => throw new InvalidOperationException() };
                Near(position, new(2, 4));
                if (local is InputEventMouse button) Near(button.GlobalPosition, new(99, 98));
                if (local is InputEventPanGesture pan) Near(pan.Delta, new(3, 4));
                if (local is InputEventScreenDrag drag) { Near(drag.Relative, new(2, 3)); Near(drag.ScreenRelative, new(6, 7)); }
                var huge = new Transform(new(float.MaxValue, 0), new(0, float.MaxValue), Vector2.Zero);
                using var before = new InputEventKey(); var id = before.InstanceID;
                Reject<ArgumentOutOfRangeException>(() => source.XformedBy(huge));
                using var after = new InputEventKey(); Check(after.InstanceID == id + 1, "Overflow rejects before allocating an event copy.");
            }
        }
    }

    private sealed class TestViewport : Viewport { public override Rect GetVisibleRect() => new(0, 0, 96, 80); }
    private sealed class Probe : Entity { internal Action<InputEvent>? InputAction; protected override void OnInput(InputEvent e) => InputAction?.Invoke(e); }
    private static void Near(Vector2 a, Vector2 b) => Check(a.IsEqualApprox(b), $"Expected {b}, got {a}.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
