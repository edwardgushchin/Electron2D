using Electron2D;

internal static class ShapeLifetimeChecks
{
    internal static void Run(World world, SceneTree tree, RID collider, CircleShape shape, CircleQueries view,
        PhysicsShapeQueryParameters overlap, PhysicsShapeQueryParameters sweep)
    {
        using var replacement = new CircleShape { Radius = 8, ResourceName = "replacement" };
        var hits = new PhysicsShapeResult[2]; var contacts = new Vector2[5];
        var owned = PhysicsServer.CircleShapeCreate(); var moving = PhysicsServer.BodyCreate();
        var changed = 0; var calls = 0; var nested = false;
        void Changed(Resource resource) => changed++;
        shape.Changed += Changed;
        try
        {
            PhysicsServer.ShapeSetData(owned, shape); view.OwnedQueryRID = owned;
            using var serverOverlap = new PhysicsShapeQueryParameters { ShapeRID = owned, Transform = overlap.Transform, CollisionMask = 2 };
            using var serverSweep = new PhysicsShapeQueryParameters { ShapeRID = owned, Transform = sweep.Transform, Motion = sweep.Motion, CollisionMask = 2 };
            view.OnShapeQuery = () =>
            {
                calls++;
                Reject(() => shape.Radius = 8); Reject(() => shape.Radius = 4);
                Reject(() => shape.CustomSolverBias = .3f); Reject(() => shape.CopyFromResource(replacement));
                Reject(shape.ResetState); Reject(shape.Dispose);
                if (!nested)
                {
                    nested = true;
                    try { Check(view.CastMotion(sweep).SafeFraction == .375f, "Nested shape query retains its own borrow"); }
                    finally { nested = false; }
                }
                Exception? workerError = null;
                var worker = new Thread(() => { try { Reject(shape.Dispose); Reject(() => shape.Radius = 8); } catch (Exception error) { workerError = error; } });
                worker.Start(); worker.Join(); if (workerError is not null) throw workerError;
                Check(shape.Radius == 4 && shape.ResourceName.Length == 0 && !shape.IsDisposed && changed == 0,
                    "Shape query hook preserves authoring, inherited metadata, lifetime and notifications");
            };
            Queries(overlap, sweep);
            view.OnShapeQuery = () => { calls++; Reject(() => PhysicsServer.ShapeSetData(owned, replacement)); Reject(() => PhysicsServer.FreeRID(owned)); };
            Queries(serverOverlap, serverSweep);
            view.OnShapeQuery = () => throw new InvalidOperationException("Expected shape query hook failure");
            var sentinel = new Vector2(111, 222); Array.Fill(contacts, sentinel);
            Reject(() => view.IntersectShape(overlap, hits)); Reject(() => view.CastMotion(sweep));
            Reject(() => view.GetRestInfo(overlap)); Reject(() => view.CollideShape(overlap, contacts));
            Check(contacts.All(p => p == sentinel), "Failed shape query preserves caller output");
            view.OnShapeQuery = null;
            view.BadFractions = true; Reject(() => view.CastMotion(sweep)); view.BadFractions = false;
            view.BadPoints = true; Reject(() => view.CollideShape(overlap, contacts)); view.BadPoints = false;
            shape.Radius = 5; shape.Radius = 4;
            PhysicsServer.ShapeSetData(owned, replacement); PhysicsServer.ShapeSetData(owned, shape);
            Queries(overlap, sweep); Check(calls == 18 && changed == 2, "All four shape query families release nested, failed and invalid-output borrows");
            using (var procedural = new QueryGeometry())
            using (var parameter = new PhysicsShapeQueryParameters { Shape = procedural, Transform = overlap.Transform, CollisionMask = 2 })
            {
                view.OwnedQueryRID = procedural.GetRID();
                procedural.Fail = true; Reject(() => view.IntersectShape(parameter, hits));
                procedural.Invalidate(); procedural.Fail = false;
                procedural.OnBuild = () =>
                {
                    Reject(procedural.ResetState); Reject(procedural.Dispose);
                    parameter.Transform = new(0, new Vector2(999, 0)); parameter.CollisionMask = 0; parameter.Shape = shape;
                };
                Check(view.IntersectShape(parameter, hits) == 1 && procedural.Builds == 2, "Geometry preparation is borrowed and failure releases frame/resource state");
                Check(view.IntersectShape(parameter, hits) == 0, "Preparation-time parameter edits affect only the next invocation");
                procedural.Invalidate(); procedural.OnBuild = null;
            }
            view.OwnedQueryRID = owned;
            PhysicsServer.AreaSetGravity(world.Space, 0); PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
            PhysicsServer.BodySetCanSleep(moving, false); PhysicsServer.BodySetLinearVelocity(moving, new(60, 0)); PhysicsServer.BodySetSpace(moving, world.Space);
            for (var i = 0; i < 128; i++) { tree.PhysicsFrame(1d / 60); HotQueries(); }
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) { tree.PhysicsFrame(1d / 60); HotQueries(); }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Shape query full frames allocate {owner}/{all} B");
            Check(MathF.Abs(PhysicsServer.BodyGetTransform(moving).Origin.X - 256) < .02f, "Borrowed query guards retain real numerical motion");
            Console.WriteLine($"Shape-query lifetime: all four families, nested/errors/RID guards and 128 full measured frames {owner}/{all} B passed.");
        }
        finally
        {
            shape.Changed -= Changed; view.OnShapeQuery = null; view.OwnedQueryRID = default;
            PhysicsServer.FreeRID(moving); PhysicsServer.FreeRID(owned);
        }
        void HotQueries()
        {
            Check(view.IntersectShape(overlap, hits) == 1 && hits[0].ColliderRID == collider, "Warm shape query");
            Check(MathF.Abs(view.CastMotion(sweep).SafeFraction - .375f) < .0001f, "Warm shape sweep");
            Check(view.CollideShape(overlap, contacts) == 1 && view.GetRestInfo(overlap)!.Value.ColliderRID == collider, "Warm contacts/rest query");
        }
        void Queries(PhysicsShapeQueryParameters o, PhysicsShapeQueryParameters s)
        {
            Check(view.IntersectShape(o, hits) == 1 && hits[0].ColliderRID == collider, "Shape span query");
            Check(view.IntersectShape(o).Single().ColliderRID == collider, "Shape array query");
            Check(MathF.Abs(view.CastMotion(s).SafeFraction - .375f) < .0001f, "Shape motion query");
            Check(view.CollideShape(o, contacts) == 1 && view.CollideShape(o).Length == 2, "Shape contact overloads");
            Check(view.GetRestInfo(o)!.Value.ColliderRID == collider, "Shape rest query");
        }
    }
    private sealed class QueryGeometry : Shape
    {
        internal bool Fail;
        internal int Builds;
        internal Action? OnBuild;
        internal void Invalidate() { EnsureGeometryChangeAccess(); EmitGeometryChanged(); }
        protected override ShapeGeometry GetGeometryCore()
        {
            Builds++; OnBuild?.Invoke();
            return Fail ? throw new InvalidOperationException("Expected geometry preparation failure") : ShapeGeometry.Circle(4);
        }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject(Action action) { try { action(); } catch (InvalidOperationException) { return; } throw new InvalidOperationException("Expected InvalidOperationException during borrowed shape query"); }
}
