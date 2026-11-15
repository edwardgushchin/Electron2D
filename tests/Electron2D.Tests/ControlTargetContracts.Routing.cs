using Electron2D;

internal static partial class ControlTargetContracts
{
    private static void Routing()
    {
        var root = new SubViewport { Size = new(200, 200) };
        var parent = new Control { Position = new(10, 10), Size = new(60, 60) };
        var child = new Control { Position = new(5, 5), Scale = new(2, 2), Size = new(20, 20) };
        var hits = new List<string>();
        root.AddChild(parent); parent.AddChild(child);
        var observer = new Unhandled(() => hits.Add("unhandled")); root.AddChild(observer);
        using var tree = new SceneTree(root);
        InputEvent? borrowed = null;
        child.GUIInput += input =>
        {
            hits.Add("child"); borrowed = input;
            if (input is InputEventMouse mouse) Check(mouse.Position == new Vector2(2, 3), "Scaled GUI input is control-local.");
        };
        parent.GUIInput += _ => hits.Add("parent");
        using var motion = new InputEventMouseMotion { Position = new(19, 21) };
        using var wheel = new InputEventMouseButton { Position = motion.Position, ButtonIndex = MouseButton.WheelDown, Pressed = true };
        void Expect(InputEvent input, string expected)
        {
            hits.Clear(); root.PushInput(input, true);
            Check(string.Join(",", hits) == expected, $"Routing expected {expected}, received {string.Join(",", hits)}.");
        }
        Expect(motion, "child");
        Check(borrowed!.IsDisposed && !motion.IsDisposed, "Temporary GUI copies retire, caller input stays live.");
        child.MouseFilter = MouseFilter.Pass; Expect(motion, "child,parent");
        parent.MouseFilter = MouseFilter.Pass; Expect(motion, "child,parent,unhandled");
        child.MouseFilter = MouseFilter.Ignore; Expect(motion, "parent,unhandled");
        child.MouseFilter = parent.MouseFilter = MouseFilter.Stop;
        Expect(wheel, "child,parent,unhandled");
        child.MouseForcePassScrollEvents = false; Expect(wheel, "child");
        child.MouseForcePassScrollEvents = true; parent.MouseForcePassScrollEvents = false; Expect(wheel, "child,parent");
        var contacts = new List<(int Index, Vector2 Position)>();
        child.GUIInput += input =>
        {
            if (input is InputEventScreenTouch touch) contacts.Add((touch.Index, touch.Position));
            if (input is InputEventScreenDrag drag) contacts.Add((drag.Index, drag.Position));
        };
        using var press = new InputEventScreenTouch { Position = motion.Position, Index = 42, Pressed = true };
        root.PushInput(press, true);
        using var drag = new InputEventScreenDrag { Position = new(95, 95), Index = 42 };
        root.PushInput(drag, true);
        using var release = new InputEventScreenTouch { Position = drag.Position, Index = 42 };
        root.PushInput(release, true);
        Check(contacts.SequenceEqual(new[] { (42, new Vector2(2, 3)), (42, new Vector2(40, 40)), (42, new Vector2(40, 40)) }), "Touch drag/release retain capture and transform outside coordinates.");
        root.PushInput(release, true); Check(contacts.Count == 3, "Released touch capture cannot receive a second release.");
        root.PushInput(press, true); child.Visible = false; root.PushInput(release, true);
        Check(contacts.Count == 4, "Hiding a touch target retires its contact capture.");
        Console.WriteLine("Control routing: Stop/Pass/Ignore, wheel force-pass matrix, local copies and touch capture/release passed.");
    }

    private sealed class Unhandled(Action action) : Node
    {
        protected override void OnReady() => UnhandledInputEnabled = true;
        protected override void OnUnhandledInput(InputEvent input) => action();
    }

    private static void PreparedFrames()
    {
        var root = new SubViewport { Size = new(100, 100) };
        var child = new Control { Size = new(20, 20) };
        root.AddChild(child);
        using var tree = new SceneTree(root);
        using var motion = new InputEventMouseMotion { Position = new(5, 5) };
        root.PushInput(motion, true);
        void Frames(bool active)
        {
            for (var i = 0; i < 128; i++)
            {
                if (active) child.Position = i % 2 == 0 ? new(30, 30) : Vector2.Zero;
                tree.ProcessFrame(0);
            }
        }
        foreach (var active in new[] { false, true })
        {
            Frames(active); Frames(active);
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            Frames(active);
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner;
            var allBytes = GC.GetTotalAllocatedBytes(true) - all;
            Check(ownerBytes == 0 && allBytes == 0, $"Prepared {(active ? "active" : "idle")} frames allocated {ownerBytes}/{allBytes} owner/all-thread bytes.");
            Console.WriteLine($"Control hover: 128 {(active ? "active" : "idle")} process frames, {ownerBytes}/{allBytes} owner/all-thread bytes.");
        }
    }
}
