using Electron2D;

internal static partial class PhysicsPickingTests
{
    private static void SingularCanvases(PhysicsServer.Backend backend)
    {
        using var f = new Fixture(backend); var body = f.Body(new(40, 40));
        var layer = new CanvasLayer { Name = "Layer" }; f.View.AddChild(layer); var overlay = f.Body(new(40, 40), layer); body.PhysicsProcessEnabled = overlay.PhysicsProcessEnabled = false;
        f.Mouse(new(40, 40)); f.Step(); Check(body.Inputs == 1 && overlay.Inputs == 1, "Both canvases initially participate in picking");
        body.Log.Clear(); overlay.Log.Clear(); f.View.CanvasTransform = default;
        f.Mouse(new(40, 40)); f.Step();
        Check(body.Inputs == 1 && overlay.Inputs == 2 && body.Log.SequenceEqual(["exit", "exit-signal", "shape-exit:0", "shape-exit-signal:0"]), "A collapsed default canvas exits hover while an independent layer still receives input: " + body.Inputs + "/" + overlay.Inputs + "; " + string.Join(",", body.Log));
        Check(!overlay.Log.Contains("exit") && !overlay.Log.Contains("enter"), "An unaffected canvas retains hover");
        f.View.CanvasTransform = Transform.Identity; body.Log.Clear(); f.Step();
        Check(body.Inputs == 2 && body.Device == InputEvent.DeviceIdInternal && body.Log.Contains("enter") && overlay.Inputs == 2, "Restoring a canvas reenters a stationary pointer exactly once");
        layer.Transform = new(new(1, 0), new(2, 0), Vector2.Zero); overlay.Log.Clear(); f.Step();
        Check(overlay.Inputs == 2 && overlay.Log.Contains("exit") && overlay.Log.Contains("shape-exit:0"), "A collinear nonzero basis is also unpickable");
        var before = body.Inputs;
        using (var touch = new InputEventScreenTouch { Index = 2, Position = new(40, 40), Pressed = true }) f.View.PushInput(touch, true);
        using (var drag = new InputEventScreenDrag { Index = 2, Position = new(40, 40) }) f.View.PushInput(drag, true);
        using (var button = new InputEventMouseButton { Position = new(40, 40), ButtonIndex = MouseButton.Left, Pressed = true }) f.View.PushInput(button, true);
        f.Step(); Check(body.Inputs == before + 3 && overlay.Inputs == 2, "Queued mouse/touch/drag events continue through valid canvases");
        layer.Transform = new(new(-1, 0), new(0, 1), new(80, 0)); f.Step();
        Check(overlay.Inputs == 3 && overlay.Device == InputEvent.DeviceIdInternal, "An invertible reflected canvas restores passive hover");
        foreach (var transform in new Transform[]
        {
            new(new(float.MaxValue, 0), new(0, float.MaxValue), Vector2.Zero),
            new(new(1e-20f, 0), new(0, 1e-20f), Vector2.Zero),
            new(new(1e-30f, 0), new(0, 1), new(-1e30f, 0))
        })
        {
            before = body.Inputs; var layerBefore = overlay.Inputs; f.View.CanvasTransform = transform;
            f.Mouse(new(40, 40)); f.Step();
            Check(body.Inputs == before && overlay.Inputs == layerBefore + 1, "Unrepresentable canvas query coordinates skip only that canvas");
            f.View.CanvasTransform = Transform.Identity; f.Step();
            Check(body.Inputs == before + 1, "A finite mapping restores picking after numerical overflow");
        }
        f.View.CanvasTransform = new(new(1e-4f, 0), new(0, 1e-4f), new(39.996f, 39.996f));
        before = body.Inputs; f.Mouse(new(40, 40)); f.Step(); Check(body.Inputs == before + 1, "Small invertible determinants are not rejected by an epsilon threshold");
        f.View.CanvasTransform = Transform.Identity; layer.FollowViewportEnabled = true; layer.FollowViewportScale = 0;
        overlay.Log.Clear(); f.Step(); Check(overlay.Log.Contains("exit"), "Zero follow scale removes the layer from passive hover");
        layer.FollowViewportScale = 1; layer.FollowViewportEnabled = false; layer.Transform = Transform.Identity; f.Step();
        var lateLayer = new CanvasLayer { Name = "Later" }; f.View.AddChild(lateLayer); var later = f.Body(new(40, 40), lateLayer); later.PhysicsProcessEnabled = false;
        before = overlay.Inputs; body.Receive = (_, _, _) => layer.Transform = default;
        f.Mouse(new(40, 40)); f.Step();
        Check(overlay.Inputs == before && later.Inputs == 1, "A callback collapsing a later canvas does not abort subsequent independent canvases"); body.Receive = null;
        f.View.CanvasTransform = layer.Transform = lateLayer.Transform = default; f.Step();
        using (var disposing = new DisposingMouse { Position = new(40, 40) })
        {
            f.View.PushInput(disposing, true); Reject<AggregateException>(f.Step);
            Check(disposing.Copies.Count == 1 && disposing.Copies[0].IsDisposed, "Skipped picking still disposes queued event copies and reports disposal observers");
        }
        for (var i = 0; i < 96; i++) f.Step();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var total = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++)
        {
            f.View.CanvasTransform = i % 2 == 0 ? default : new(new(1, 0), new(2, 0), Vector2.Zero); f.Step();
        }
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; total = GC.GetTotalAllocatedBytes(true) - total;
        Check(owner == 0 && total == 0, $"Warmed collapsed-canvas frames allocate {owner}/{total} managed bytes");
        Console.WriteLine($"Singular picking {backend}: default/layer/follow/reentrant maps, hover, event disposal and numerical boundaries; 64 active frames={owner}/{total} owner/all-thread managed bytes.");
    }
}
