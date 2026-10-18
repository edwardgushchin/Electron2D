using Electron2D;

internal static class GroupInterpolationSnapshotTests
{
    private sealed class ObservedCamera : Camera
    {
        internal int Reads;
        public override Transform GetTransform() { Reads++; return base.GetTransform(); }
    }
    private sealed class Writer : Entity
    {
        internal Action? Write;
        protected override void OnPhysicsProcess(double delta) => Write!();
    }
    internal static void Run(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var root = new SubViewport { World = world, Size = new(128, 80) };
        var writer = new Writer { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread };
        var item = new Entity(); var retained = new Entity(); var control = new Control();
        var camera = new Camera { AnchorMode = AnchorMode.FixedTopLeft, LimitEnabled = false, ProcessCallback = ProcessPhase.Physics };
        writer.AddChild(item); writer.AddChild(retained); writer.AddChild(control); writer.AddChild(camera); root.AddChild(writer);
        using var tree = new SceneTree(root); tree.PhysicsInterpolation = true;
        var inactive = new ObservedCamera(); root.AddChild(inactive); inactive.Position = Vector2.One;
        var inactiveReads = inactive.Reads; inactive.Offset = Vector2.One;
        Check(inactive.Reads == inactiveReads, "Inactive tracking returns before reading a virtual scene transform");
        retained.ServerState = new(null!, false) { Transform = new(0, new(600, 0)), Interpolated = false };
        var phase = 0; var immediate = 0;
        writer.Write = () =>
        {
            phase++; var origin = phase * 40;
            writer.PhysicsInterpolationMode = PhysicsInterpolationMode.Off;
            writer.PhysicsInterpolationMode = PhysicsInterpolationMode.On;
            Check(!control.IsPhysicsInterpolatedAndEnabled(), "Control retains its explicit Off default under an On group");
            control.PhysicsInterpolationMode = PhysicsInterpolationMode.Inherit; Check(control.IsPhysicsInterpolatedAndEnabled(), "Control explicitly inherits the group policy");
            control.PhysicsInterpolationMode = PhysicsInterpolationMode.Off;
            item.Position = new(origin + 10, 0); item.ResetPhysicsInterpolation();
            Check(item.GetInterpolatedVisualTransform(0) == item.Transform, "Reset commits the logical previous endpoint before the worker returns"); immediate++;
            item.Position = new(origin + 20, 0);
            camera.GetGlobalTransform(); camera.Position = new(origin + 10, 0); camera.ForceUpdateTransform(); camera.ResetPhysicsInterpolation();
            camera.GetGlobalTransform(); camera.Position = new(origin + 20, 0); camera.ForceUpdateTransform();
        };
        for (var i = 0; i < 3; i++)
        {
            tree.PhysicsFrame(.01); var origin = phase * 40;
            Check(item.GetInterpolatedVisualTransform(0).Origin.X == origin + 10 && item.GetInterpolatedVisualTransform(.5f).Origin.X == origin + 15 && item.GetInterpolatedVisualTransform(1).Origin.X == origin + 20,
                "A later transform preserves the explicit reset endpoint through the full physics frame");
            Check(MathF.Abs(root.GetInterpolatedCanvasTransform(0).Origin.X + origin + 10) < .001f && MathF.Abs(root.GetInterpolatedCanvasTransform(.5f).Origin.X + origin + 15) < .001f && MathF.Abs(root.CanvasTransform.Origin.X + origin + 20) < .001f,
                "Ordered camera scroll/reset/scroll publications preserve both viewport endpoints");
            Check(retained.ServerState.Interpolated is null && retained.ServerState.Interpolation.Previous == retained.ServerState.Transform && retained.ServerState.Interpolation.Current == retained.ServerState.Transform,
                "Worker ancestor policy removes a borrowed renderer override and resets its canonical retained pose");
        }
        Check(immediate == 3, "No owner publication replays immediate worker reset work");
        tree.PhysicsInterpolation = false; var disabled = 0;
        writer.Write = () => { disabled++; writer.ResetPhysicsInterpolation(); };
        tree.PhysicsFrame(.01); Check(disabled == 1, "Scene-wide disabled reset remains legal in a processing group");
        Console.WriteLine($"Group interpolation snapshots {backend}: immediate logical history, ordered camera endpoints, Control defaults/override and retained renderer policy passed.");
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
