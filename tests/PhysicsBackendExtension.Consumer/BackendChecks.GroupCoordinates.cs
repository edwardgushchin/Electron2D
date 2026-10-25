using Electron2D;

public static partial class BackendChecks
{
    private sealed class CoordinateCapture : Entity
    {
        private static Node Create() => new CoordinateCapture();
        protected override Func<Node> CreateSceneInstanceFactory()
        { _ = GetViewportRect(); _ = GetCanvasTransform(); _ = GetViewportTransform(); _ = GetScreenTransform(); return Create; }
    }
    public static void RunGroupCoordinates(PhysicsServer.Backend? backend, bool threaded, bool poolOwner = false)
    {
        var oldPool = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("GroupCoordinates", () => new Solver());
        try { if (poolOwner) { var task = WorkerThreadPool.AddTask(Run); WorkerThreadPool.WaitForTaskCompletion(task); } else Run(); }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldPool); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldThread); }
        void Run()
        {
            using var world = backend is { } selected ? new World(selected) : new World("GroupCoordinates");
            using var root = new SubViewport { World = world, Size = new(160, 120), Size2DOverride = new(80, 60), Size2DOverrideStretch = true, GUIEmbedSubwindows = true, CanvasTransform = new(0, new(1, 2)) };
            using var circle = new CircleShape { Radius = 2 }; using var coldWindow = new Window();
            var a = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
            var b = new TransformWriter { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread, ProcessThreadGroupOrder = -1 };
            var layer = new CanvasLayer { Transform = new(.15f, new(1.2f, .8f), .1f, new(2, 3)), FollowViewportEnabled = true, FollowViewportScale = 1.5f };
            var left = Body(new(10, 20)); var right = Body(new(40, 30)); var childShape = new CollisionShape { Shape = circle };
            left.AddChild(childShape); right.AddChild(new CollisionShape { Shape = circle }); layer.AddChild(left); a.AddChild(layer); b.AddChild(right);
            var control = new Control { Position = new(3, 4), Size = new(9, 7), Rotation = .2f, Scale = new(1.2f, .9f) };
            var container = new SubViewportContainer { Position = new(4, 6), Size = new(64, 48), Stretch = true, StretchShrink = 2 };
            var nested = new SubViewport { World = world, GUIEmbedSubwindows = true };
            var inner = new Window { World = world, Position = new(2, 3), Size = new(20, 16), ContentScaleSize = new(10, 8), ContentScaleMode = ContentScaleMode.CanvasItems, Borderless = true, CanvasTransform = new(.1f, new(1.5f, .75f), .2f, new(2, 3)) };
            var innerItem = new Entity { Position = new(1, 1), Rotation = .25f, Scale = new(2, 3) };
            inner.AddChild(innerItem); nested.AddChild(inner); container.AddChild(nested); root.AddChild(container); root.AddChild(control); root.AddChild(a); root.AddChild(b);
            using var tree = new SceneTree(root); using var together = new Barrier(2);
            root.GUIEmbedSubwindows = false;
            Reject<InvalidOperationException>(() => nested.GUIEmbedSubwindows = false);
            using (var affected = new Window { World = world, Visible = false })
            {
                root.AddChild(affected); Reject<InvalidOperationException>(() => root.GUIEmbedSubwindows = true); root.RemoveChild(affected);
            }
            root.GUIEmbedSubwindows = true;
            Check(inner.IsEmbedded(), "Ancestor policy changes preserve windows shielded by a nearer embedding viewport");
            if (backend is { } expected) Check(world.PhysicsBackend == expected && world.PhysicsFallbackReason is null, "Coordinate fixture retains the requested physical backend without fallback");
            var twice = Transform.Identity.Scaled(new(2, 2)); var layerCanvas = root.CanvasTransform * Transform.Identity.Scaled(new(1.5f, 1.5f)) * layer.Transform;
            CanvasItem[] items = [left, childShape, right, control, innerItem];
            Transform[] canvases = [layerCanvas, layerCanvas, root.CanvasTransform, root.CanvasTransform, inner.CanvasTransform];
            var innerScreen = new Transform(0, new(2, 3)) * twice;
            var viewportScreen = twice * root.CanvasTransform * new Transform(0, new(4, 6)) * twice * new Transform(0, new(2, 3)) * twice;
            var coldSize = (Vector2)coldWindow.Size; var cold = true; var reads = 0;
            using var key = new InputEventKey(); using var motion = new InputEventMouseMotion { Position = new(13, 17), GlobalPosition = new(19, 23), Relative = new(2, 3), Velocity = new(4, 5) };
            a.Write = Read; b.Write = Read;
            tree.PhysicsFrame(.01); cold = false;
            Check(a.ThreadID != b.ThreadID && reads == 2, "Independent groups overlap during cold coordinate reads");
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            for (var i = 0; i < 128; i++) Frame(); Collect(); for (var i = 0; i < 128; i++) Frame();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame(); own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Grouped coordinates/full physics/query frames allocated {own}/{all} B");
            using var done = new ManualResetEventSlim(); var task = WorkerThreadPool.AddTask(() =>
            {
                try { Reject<InvalidOperationException>(() => left.GetCanvasTransform()); Reject<InvalidOperationException>(() => left.GetScreenTransform()); Reject<InvalidOperationException>(() => root.GetVisibleRect()); Reject<InvalidOperationException>(() => nested.GetVisibleRect()); Reject<InvalidOperationException>(() => root.GetFinalTransform()); Reject<InvalidOperationException>(() => layer.GetFinalTransform()); }
                finally { done.Set(); }
            }, highPriority: true);
            Check(done.Wait(TimeSpan.FromSeconds(5)), "Ordinary worker guards finish before owner assistance"); WorkerThreadPool.WaitForTaskCompletion(task);
            using var detached = new Entity { Position = new(2, 3) }; Check(detached.GetGlobalTransformWithCanvas().Origin == new Vector2(2, 3), "Detached global-with-canvas preserves the logical pose"); Reject<InvalidOperationException>(() => detached.GetViewportRect());
            using var packed = new PackedScene(); var capturing = new CoordinateCapture(); a.AddChild(capturing); packed.Pack(capturing);
            coldWindow.Dispose(); Reject<ObjectDisposedException>(() => coldWindow.GetVisibleRect()); Reject<ObjectDisposedException>(() => coldWindow.GetFinalTransform());
            left.Dispose(); Reject<ObjectDisposedException>(() => left.GetCanvasTransform()); Reject<ObjectDisposedException>(() => left.MakeInputLocal(key));
            Console.WriteLine($"Group coordinates {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}/{(poolOwner ? "pool" : "ordinary")}: cold window/layer, inherited transforms, nested stretch/shrink/popup coordinates, event projection/guards and 128 full physics/query frames at {own}/{all} B passed.");
            void Read()
            {
                if (cold && !together.SignalAndWait(TimeSpan.FromSeconds(10))) throw new TimeoutException("Coordinate groups did not overlap");
                Check(coldWindow.GetVisibleRect().Size == coldSize && coldWindow.GetFinalTransform() == Transform.Identity, "Concurrent cold Window layout publishes coherent visible/final coordinates");
                Check(root.GetVisibleRect().Size == new Vector2(80, 60) && root.GlobalCanvasTransform == Transform.Identity && root.GetFinalTransform() == twice && layer.GetFinalTransform() == layerCanvas, "Logical root/layer reads preserve scale and following");
                Check((inner.GetScreenTransform() * new Vector2(1, 1)).DistanceTo(viewportScreen * new Vector2(1, 1)) < .002f, "Nested screen chain includes container, shrink and window transforms once");
                for (var i = 0; i < items.Length; i++)
                {
                    var item = items[i]; var canvas = canvases[i]; var global = canvas * item.GetGlobalTransform(); var sample = new Vector2(2, 3);
                    Check(item.GetCanvasTransform() == canvas && (item.GetGlobalTransformWithCanvas() * sample).DistanceTo(global * sample) < .002f, "Canvas/global queries retain the logical transform family");
                    Check((item.GetViewportTransform() * sample).DistanceTo(twice * canvas * sample) < .002f && item.GetViewportRect().Size == (i == 4 ? new Vector2(10, 8) : new Vector2(80, 60)), "Viewport transforms exclude unrelated ancestors");
                    Check((item.GetScreenTransform() * sample).DistanceTo((i == 4 ? innerScreen : Transform.Identity) * global * sample) < .002f && item.MakeCanvasPositionLocal(global * sample).DistanceTo(sample) < .002f, "Popup screen boundaries and inverse localization retain complete basis");
                    if (item is Control gui) Check(gui.GetScreenPosition().DistanceTo(item.GetScreenTransform().Origin) < .002f, "Control screen origin shares the inherited logical projection");
                    Check(ReferenceEquals(item.MakeInputLocal(key), key), "Non-positional event projection returns the borrowed identity");
                    if (cold) CheckCold(item, global);
                }
                Interlocked.Increment(ref reads);
            }
            void CheckCold(CanvasItem item, Transform global)
            {
                using var projected = (InputEventMouseMotion)item.MakeInputLocal(motion);
                Check(projected.Position.DistanceTo(global.AffineInverse() * motion.Position) < .002f && projected.GlobalPosition == motion.GlobalPosition && !ReferenceEquals(projected, motion), "Worker positional projection owns its transformed copy");
                Reject<ArgumentException>(() => item.MakeCanvasPositionLocal(new(float.NaN, 0)));
                Reject<InvalidOperationException>(() => item.GetGlobalMousePosition()); Reject<InvalidOperationException>(() => item.GetLocalMousePosition());
                Reject<InvalidOperationException>(() => root.CanvasTransform = Transform.Identity);
            }
            void Frame()
            {
                var x = left.Position.X; tree.PhysicsFrame(.01); tree.ProcessFrame(.01); point.Position = left.Position; point.CanvasInstanceID = layer.InstanceID;
                Check(MathF.Abs(left.Position.X - x - .06f) < .002f && world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == left.GetRID(), "Coordinate reads preserve selected motion and exact layer-filtered physical identity");
            }
            RigidBody Body(Vector2 position) => new() { Position = position, GravityScale = 0, CanSleep = false, LinearVelocity = new(6, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
        }
    }
}
