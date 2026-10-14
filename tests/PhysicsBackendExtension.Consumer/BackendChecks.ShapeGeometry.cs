using Electron2D;

public static partial class BackendChecks
{
    private sealed record GeometryEdit(Shape Shape, Action Change, Action Equal, Func<string> Read);

    public static void RunShapeGeometry(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { RunShapeGeometryCore(backend); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void RunShapeGeometryCore(PhysicsServer.Backend? backend)
    {
        using var circle = new CircleShape { Radius = 3 };
        using var capsule = new CapsuleShape { Radius = 2, Height = 8 };
        using var rectangle = new RectangleShape { Size = new(4, 6) };
        using var segment = new SegmentShape { A = Vector2.Zero, B = Vector2.Right };
        using var boundary = new WorldBoundaryShape(); using var ray = new SeparationRayShape();
        Vector2[] points = [new(0, 0), new(4, 0), new(0, 4)];
        Vector2[] changedPoints = [new(0, 0), new(6, 0), new(0, 6)];
        Vector2[] edges = [Vector2.Zero, Vector2.Right];
        using var convex = new ConvexPolygonShape { Points = points };
        using var concave = new ConcavePolygonShape { Segments = edges };
        var customData = Annulus.Create(2, 6); var changedCustomData = Annulus.Create(3, 8);
        using var custom = new CustomShape(customData); using var probe = new GeometryProbe();
        GeometryEdit[] edits = [
            new(circle, () => circle.Radius = 4, () => circle.Radius = circle.Radius, () => circle.Radius.ToString()),
            new(capsule, () => capsule.Radius = 3, () => capsule.Radius = capsule.Radius, () => $"{capsule.Radius}/{capsule.Height}"),
            new(capsule, () => capsule.Height = 10, () => capsule.Height = capsule.Height, () => $"{capsule.Radius}/{capsule.Height}"),
            new(capsule, () => capsule.MidHeight = 3, () => capsule.MidHeight = capsule.MidHeight, () => $"{capsule.Radius}/{capsule.Height}"),
            new(rectangle, () => rectangle.Size = new(8, 10), () => rectangle.Size = rectangle.Size, () => rectangle.Size.ToString()),
            new(segment, () => segment.A = Vector2.Left, () => segment.A = segment.A, () => $"{segment.A}/{segment.B}"),
            new(segment, () => segment.B = new(2, 0), () => segment.B = segment.B, () => $"{segment.A}/{segment.B}"),
            new(boundary, () => boundary.Normal = Vector2.Left, () => boundary.Normal = boundary.Normal, () => $"{boundary.Normal}/{boundary.Distance}"),
            new(boundary, () => boundary.Distance = 3, () => boundary.Distance = boundary.Distance, () => $"{boundary.Normal}/{boundary.Distance}"),
            new(ray, () => ray.Length += 5, () => ray.Length = ray.Length, () => $"{ray.Length}/{ray.SlideOnSlope}"),
            new(ray, () => ray.SlideOnSlope = true, () => ray.SlideOnSlope = ray.SlideOnSlope, () => $"{ray.Length}/{ray.SlideOnSlope}"),
            new(convex, () => convex.Points = changedPoints, () => convex.Points = points, () => string.Join('|', convex.Points)),
            new(convex, () => convex.SetPointCloud(changedPoints), () => convex.SetPointCloud(points), () => string.Join('|', convex.Points)),
            new(concave, () => concave.Segments = [Vector2.Zero, new(2, 0)], () => concave.Segments = edges, () => string.Join('|', concave.Segments)),
            new(custom, () => custom.Geometry = changedCustomData, () => custom.Geometry = customData, () => Convert.ToHexString(custom.Geometry.GetCustomData())),
            new(probe, () => probe.Radius = 4, () => probe.Radius = probe.Radius, () => probe.Radius.ToString())
        ];
        var shapes = edits.Select(edit => edit.Shape).Distinct().ToArray();
        var copySources = shapes.Select(shape => (Shape)shape.Duplicate()).ToArray();
        foreach (var source in copySources) { source.ResourceName = "replacement"; source.ResourceLocalToScene = true; source.CanTranslateMessages = false; source.TranslationDomain = StoredResourceDomain; }
        var data = shapes.Select(shape => shape.GetGeometryData()).ToArray();
        var values = edits.Select(edit => edit.Read()).ToArray();
        var ids = shapes.Select(shape => shape.GetRID()).ToArray();
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ShapeGeometry", () => solver = new Solver());
        using var world = backend is { } builtin ? new World(builtin) : new World("ShapeGeometry");
        using var root = new SubViewport { World = world };
        var body = new StaticBody { Name = "Primary" }; body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
        var disabled = new StaticBody { Name = "Disabled", Position = new(1000, 0) }; root.AddChild(disabled);
        foreach (var shape in shapes) PhysicsServer.BodyAddShape(disabled.GetRID(), shape.GetRID(), disabled: true);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        var changes = 0;
        foreach (var shape in shapes) shape.Changed += _ => changes++;
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetTransform(raw, new(0, new(100, 0)));
            PhysicsServer.BodySetGravityScale(raw, 0); PhysicsServer.BodySetCanSleep(raw, false); PhysicsServer.BodySetSpace(raw, world.Space);
            foreach (var shape in shapes) PhysicsServer.AreaAddShape(area, shape.GetRID(), disabled: true);
            PhysicsServer.AreaSetSpace(area, world.Space);
            Exception? failure = null;
            var thread = new Thread(() => { try { circle.Radius = 4; } catch (Exception error) { failure = error; } });
            thread.Start(); thread.Join(); if (failure is not null) throw failure;
            Check(circle.Radius == 4, "Coordinated worker geometry edits remain available while the world is quiescent");
            circle.Radius = 3;
            data[Array.IndexOf(shapes, circle)] = circle.GetGeometryData();
            if (solver is not null)
            {
                solver.BodyHook = solver.ShapeHook = solver.FilterHook = _ => Guard();
                try
                {
                    PhysicsServer.BodySetLinearVelocity(raw, Vector2.Right);
                    PhysicsServer.BodySetShapeDisabled(raw, 0, true); PhysicsServer.BodySetShapeDisabled(raw, 0, false);
                    PhysicsServer.BodySetCollisionMask(raw, 2);
                }
                finally { solver.BodyHook = solver.ShapeHook = solver.FilterHook = null; }
                solver.StepHook = Guard;
                try { tree.PhysicsFrame(1d / 120); }
                finally { solver.StepHook = null; }
                var reads = 0;
                solver.StatusHook = () =>
                {
                    reads++; Check(circle.Radius == 3, "Every selected access hook runs before geometric authoring commits");
                    Reject<InvalidOperationException>(() => circle.Radius = 4);
                    Reject<InvalidOperationException>(() => circle.CopyFromResource(copySources[Array.IndexOf(shapes, circle)]));
                    Reject<InvalidOperationException>(circle.ResetState);
                    Check(circle.ResourceName.Length == 0 && !circle.ResourceLocalToScene, "Reentrant copy rejection preserves base state");
                    Reject<InvalidOperationException>(() => circle.Dispose());
                };
                try { circle.Radius = 4; }
                finally { solver.StatusHook = null; }
                Check(reads > 0 && circle.Radius == 4, "Publication does not repeat fallible selected status hooks after commit");
                circle.Radius = 3;
            }
            var before = changes; var callbacks = 0;
            var circleSource = (CircleShape)copySources[Array.IndexOf(shapes, circle)]; circleSource.Radius = 4;
            PhysicsServer.BodySetStateSyncCallback(raw, _ => { callbacks++; circle.CopyFromResource(circleSource); });
            tree.PhysicsFrame(1d / 120); PhysicsServer.BodySetStateSyncCallback(raw, null);
            Check(callbacks == 1 && circle.Radius == 4 && changes == before + 1 && circle.ResourceName == "replacement" && circle.ResourceLocalToScene,
                "Ordinary post-solver copying commits base/geometry state with one coalesced change");
            using var point = new PhysicsPointQueryParameters { Position = new(3.5f, 0) };
            var hits = new PhysicsPointResult[1];
            Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == body.GetRID(), "Lazy geometry preparation changes real selected point-query results");
            circle.Radius = 3;
            circle.ResourceName = string.Empty; circle.ResourceLocalToScene = false; circle.CanTranslateMessages = true; circle.TranslationDomain = string.Empty;
            foreach (var edit in edits)
            {
                var previous = edit.Shape.GetGeometryData(); edit.Change();
                Check(!ReferenceEquals(previous, edit.Shape.GetGeometryData()) && edit.Shape.GetRID().IsValid(), $"Accepted {edit.Shape.GetType().Name} edit invalidates exact data while retaining resource identity");
            }
            Action<Resource> fail = _ => throw new InvalidOperationException("Geometry observer failure");
            circle.Changed += fail; Reject<InvalidOperationException>(() => circle.Radius = 5); circle.Changed -= fail;
            Check(circle.Radius == 5 && circle.GetGeometryData().GetCircleRadius() == 5, "Observer failure preserves committed geometry and cache invalidation");
            var currentValues = edits.Select(edit => edit.Read()).ToArray();
            var currentData = shapes.Select(shape => shape.GetGeometryData()).ToArray();
            var builds = 0;
            PhysicsServer.BodySetShapeDisabled(disabled.GetRID(), Array.IndexOf(shapes, probe), false);
            probe.Invalidate();
            probe.OnBuild = () =>
            {
                builds++; var count = changes;
                Exception? workerFailure = null;
                var worker = new Thread(() =>
                {
                    try { foreach (var edit in edits) { Reject<InvalidOperationException>(edit.Change); Reject<InvalidOperationException>(edit.Equal); } }
                    catch (Exception error) { workerFailure = error; }
                });
                worker.Start(); worker.Join(); if (workerFailure is not null) throw workerFailure;
                for (var i = 0; i < edits.Length; i++)
                {
                    Reject<InvalidOperationException>(edits[i].Change); Reject<InvalidOperationException>(edits[i].Equal);
                    Check(edits[i].Read() == currentValues[i], "Numerical geometry preparation rejects every edit before private mutation");
                }
                for (var i = 0; i < shapes.Length; i++)
                {
                    Reject<InvalidOperationException>(shapes[i].ResetState);
                    Reject<InvalidOperationException>(() => shapes[i].CopyFromResource(copySources[i]));
                    Check(shapes[i].ResourceName.Length == 0 && !shapes[i].ResourceLocalToScene && shapes[i].CanTranslateMessages && shapes[i].TranslationDomain.Length == 0,
                        "Geometry supplier rejection preserves inherited copy metadata");
                    if (!ReferenceEquals(shapes[i], probe)) Check(ReferenceEquals(shapes[i].GetGeometryData(), currentData[i]), "Other exact data remains unchanged during a geometry supplier");
                }
                Check(changes == count, "Numerical geometry rejection emits no Changed");
            };
            try { tree.PhysicsFrame(1d / 120); }
            finally { probe.OnBuild = null; }
            Check(builds == 1, "A real selected preparation invokes the guarded geometry supplier exactly once");
            for (var i = 0; i < 256; i++) { probe.Preflight(); tree.PhysicsFrame(1d / 120); }
            Collect(); GC.WaitForPendingFinalizers(); _ = GC.GetTotalAllocatedBytes(true);
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) { probe.Preflight(); tree.PhysicsFrame(1d / 120); }
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Geometry preflight and warmed full frames allocated {own}/{all} bytes");
            for (var i = 0; i < shapes.Length; i++)
            {
                var target = shapes[i]; var id = target.GetRID(); var changed = changes;
                target.ResourceSceneUniqueID = "copy1"; target.SetPathCache($"local://copy-target-{i}");
                target.CopyFromResource(copySources[i]);
                Check(target.ResourceName == "replacement" && target.ResourceLocalToScene && !target.CanTranslateMessages && target.TranslationDomain == StoredResourceDomain && target.GetRID() == id &&
                    target.ResourceSceneUniqueID == "copy1" && target.ResourcePath == $"local://copy-target-{i}" && changes == changed + 1,
                    "Allowed copying preserves target identity/path/scene ID and coalesces every shape family");
                Check(target.GetGeometryData().Type == copySources[i].GetGeometryData().Type, "Allowed copying retains represented geometry kind");
            }
            Check(probe.Resets == 1, "Allowed copy resets custom non-stored state once after preflight");
            var resetChanges = changes; foreach (var shape in shapes) shape.ResetState();
            Check(probe.Resets == 2 && changes == resetChanges, "Allowed standalone reset reaches the custom hook without manufacturing Changed");
            Console.WriteLine($"Geometry authoring {backend?.ToString() ?? "registered"}, threaded={ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread)}: {edits.Length} paths, {shapes.Length} inherited copy guards, callback/query/observer checks and 128 full frames {own}/{all} B passed.");
        }
        finally { PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(area); foreach (var source in copySources) source.Dispose(); }
        using var constructing = new GeometryProbe();
        using var constructingSource = new GeometryProbe { Radius = 4, ResourceName = "replacement", ResourceLocalToScene = true };
        constructing.OnBuild = () =>
        {
            Reject<InvalidOperationException>(() => constructing.Radius = 4);
            Reject<InvalidOperationException>(constructing.ResetState);
            Reject<InvalidOperationException>(() => constructing.CopyFromResource(constructingSource));
            Check(constructing.ResourceName.Length == 0 && !constructing.ResourceLocalToScene && constructing.Radius == 2 && constructing.Resets == 0,
                "Rejected copying during geometry construction preserves all base and custom state");
        };
        _ = constructing.GetGeometryData();
        Check(constructing.Radius == 2, "A resource geometry supplier cannot change its private authored data");

        void Guard()
        {
            var count = changes;
            for (var i = 0; i < edits.Length; i++)
            {
                Reject<InvalidOperationException>(edits[i].Change); Reject<InvalidOperationException>(edits[i].Equal);
                Check(edits[i].Read() == values[i], "Rejected geometric write preserves private authoring, including coupled capsule fields");
            }
            for (var i = 0; i < shapes.Length; i++)
            {
                Reject<InvalidOperationException>(shapes[i].ResetState);
                Reject<InvalidOperationException>(() => shapes[i].CopyFromResource(copySources[i]));
                Check(shapes[i].ResourceName.Length == 0 && !shapes[i].ResourceLocalToScene && shapes[i].CanTranslateMessages && shapes[i].TranslationDomain.Length == 0,
                    "Rejected inherited resource copying preserves shape base state");
                Check(shapes[i].GetRID() == ids[i] && ReferenceEquals(shapes[i].GetGeometryData(), data[i]), "Rejected geometric write preserves RID and immutable cache");
            }
            Check(changes == count, "Rejected geometric writes emit no Changed event");
            Check(probe.Resets == 0, "Rejected copying never enters the custom reset hook");
        }
    }

    private sealed class GeometryProbe : Shape
    {
        private float _radius = 2;
        internal Action? OnBuild;
        internal int Resets;
        internal float Radius
        {
            get => _radius;
            set { EnsureGeometryChangeAccess(); _radius = value; EmitGeometryChanged(); }
        }
        internal void Preflight() => EnsureGeometryChangeAccess();
        internal void Invalidate() { EnsureGeometryChangeAccess(); EmitGeometryChanged(); }
        protected override ShapeGeometry GetGeometryCore() { OnBuild?.Invoke(); return ShapeGeometry.Circle(_radius); }
        protected override Resource CreateDuplicateInstance() => new GeometryProbe();
        protected override void OnResetState() => Resets++;
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
            Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
        {
            base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
            ((GeometryProbe)target)._radius = _radius; ((GeometryProbe)target).EmitGeometryChanged();
        }
    }
}
