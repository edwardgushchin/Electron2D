using Electron2D;

public static partial class BackendChecks
{
    public static void RunQueryGeometry(PhysicsServer.Backend? backend = null, bool threaded = false, bool poolOwner = false)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("QueryGeometry", () => new Solver());
        try { if (poolOwner) { var task = WorkerThreadPool.AddTask(Run); WorkerThreadPool.WaitForTaskCompletion(task); } else Run(); }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver); }
        void Run()
        {
            using var world = backend is { } selected ? new World(selected) : new World("QueryGeometry");
            using var circle = new CircleShape { Radius = 3 }; using var source = new BorrowedGeometryShape();
            using var replacement = new CircleShape { Radius = .1f };
            using var query = new PhysicsShapeQueryParameters { Shape = source };
            using var nested = new PhysicsShapeQueryParameters { Shape = circle, Transform = new(0, new(-500, 0)) };
            var target = PhysicsServer.BodyCreate(); var moving = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
            try
            {
                PhysicsServer.BodySetMode(target, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(target, circle.GetRID()); PhysicsServer.BodySetSpace(target, world.Space);
                PhysicsServer.AreaAddShape(area, circle.GetRID()); PhysicsServer.AreaSetSpace(area, world.Space);
                PhysicsServer.BodyAddShape(moving, circle.GetRID()); PhysicsServer.BodySetTransform(moving, new(0, new(0, 300)));
                PhysicsServer.BodySetLinearVelocity(moving, new(1, 0)); PhysicsServer.BodySetGravityScale(moving, 0); PhysicsServer.BodySetCanSleep(moving, false);
                PhysicsServer.BodySetLinearDampMode(moving, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(moving, 0); PhysicsServer.BodySetSpace(moving, world.Space);
                var view = world.DirectSpaceState;
                using var custom = new CircleQueries(world.Space, [new(target, circle, false), new(area, circle, true)], circle) { OwnedQueryRID = source.GetRID() };
                var hits = new PhysicsShapeResult[2]; var innerHits = new PhysicsShapeResult[2]; var contacts = new Vector2[4]; var calls = 0; var rejected = 0;
                var marker = new PhysicsShapeResult(moving, 0); var pointMarker = new Vector2(777, 888); var bias = PhysicsServer.SpaceGetConstraintDefaultBias(world.Space);
                for (var route = 0; route < 15; route++)
                {
                    var before = source.Reads; source.Arm(Inspect); Invoke(route); Check(source.Reads == before + 1, "Every query route samples one cold supplier");
                    var error = new IOException("query-geometry-supplier"); source.Arm(() => throw error); Array.Fill(hits, marker); Array.Fill(contacts, pointMarker);
                    Check(ReleaseErrors(() => Invoke(route)).Any(e => ReferenceEquals(e, error)), "Query geometry retains the original supplier failure");
                    Check(hits.All(hit => hit.ColliderRID == moving) && contacts.All(p => p == pointMarker), "Supplier failure preserves caller storage");
                    Check(!world.IsDisposed && PhysicsServer.BodyGetSpace(target) == world.Space && PhysicsServer.SpaceGetConstraintDefaultBias(world.Space) == bias, "Failed geometry retains the live world and common authoring");
                    source.Read = null; Invoke(route); Check(source.Reads == before + 3, "Query failure releases its borrow and can retry");
                }
                Check(calls == 15 && rejected == 90 && PhysicsServer.BodyGetLinearVelocity(moving) == new Vector2(1, 0), "Fifteen cold query routes reject six world reentries each");
                Action[] changes =
                [
                    () =>
                    {
                        query.Shape = replacement; query.Transform = new(0, new(900, 0)); query.Motion = Vector2.Zero;
                        query.Margin = 100; query.CollisionMask = 0; query.CollideWithBodies = false; query.CollideWithAreas = true;
                        query.Exclude = [target];
                    },
                    () => query.Shape = replacement, () => query.Transform = new(0, new(900, 0)), () => query.Motion = Vector2.Zero,
                    () => query.Margin = 100, () => query.CollisionMask = 0, () => query.CollideWithBodies = false,
                    () => query.CollideWithAreas = true, () => query.Exclude = [target]
                ];
                for (var edit = 0; edit < changes.Length; edit++)
                    for (var route = 0; route < 12; route++)
                    {
                        ResetQuery(); source.Arm(changes[edit]); Invoke(route);
                        var expected = edit switch { 1 => route % 6 == 2 ? 1 : 0, 3 => route % 6 == 2 ? 0 : 1, 4 => 1, 7 => 2, _ => 0 };
                        Check(view.IntersectShape(query, hits) == expected, "The next invocation observes the edited query geometry and filters");
                    }
                ResetQuery(); source.Read = null;
                nested.Transform = new(0, new(5, 0));
                var innerContacts = new Vector2[4];
                for (var route = 0; route < 12; route++)
                {
                    source.Arm(NestedHits); Invoke(route);
                    var error = new IOException("query-geometry-after-nested-hits");
                    source.Arm(() => { NestedHits(); throw error; }); Array.Fill(hits, marker); Array.Fill(contacts, pointMarker);
                    Check(ReleaseErrors(() => Invoke(route)).Any(e => ReferenceEquals(e, error)), "Nested hits retain the outer supplier exception");
                    Check(hits.All(hit => hit.ColliderRID == moving) && contacts.All(p => p == pointMarker), "Nested hits cannot leak into failed caller output");
                    source.Read = null; Invoke(route);
                }
                using var root = new SubViewport { World = world }; var anchor = new StaticBody { Position = new(0, 1000) };
                anchor.AddChild(new CollisionShape { Shape = circle }); root.AddChild(anchor); using var tree = new SceneTree(root);
                using var point = new PhysicsPointQueryParameters(); var points = new PhysicsPointResult[2];
                for (var i = 0; i < 128; i++) Frame(); Collect(); for (var i = 0; i < 128; i++) Frame();
                var tick = PhysicsServer.SpaceGetTick(world.Space); var start = PhysicsServer.BodyGetTransform(moving).Origin;
                var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame(); own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && MathF.Abs(PhysicsServer.BodyGetTransform(moving).Origin.X - start.X - 1.28f) < .001f,
                    $"128 complete moving frames and eleven query routes allocate {own}/{all} B");
                Check(world.RequestedPhysicsBackendName == world.PhysicsBackendName && world.PhysicsFallbackReason is null, "Query preparation preserves the selected numerical owner");
                Console.WriteLine($"Query geometry {backend?.ToString() ?? "registered"}/{threaded}/{poolOwner}: 15 cold routes, 90 rejected reentries, 108 input-mutation cases, 12 nonempty nested success/failure/retry routes and 128 moving full frames {own}/{all} B passed.");
                void Inspect()
                {
                    calls++;
                    foreach (var operation in new Action[] { world.Dispose, () => PhysicsServer.FreeRID(target), () => PhysicsServer.BodySetSpace(target, default),
                        () => PhysicsServer.BodySetTransform(target, Transform.Identity), () => PhysicsServer.BodySetLinearVelocity(moving, Vector2.Zero), () => PhysicsServer.SpaceSetConstraintDefaultBias(world.Space, .75f) })
                    { Reject<InvalidOperationException>(operation); rejected++; }
                    Check(view.IntersectShape(nested, innerHits) == 0, "A nested query completes before outer geometry uses shared scratch");
                }
                void ResetQuery()
                {
                    query.Shape = source; query.Margin = 0; query.CollisionMask = uint.MaxValue;
                    query.CollideWithBodies = true; query.CollideWithAreas = false; query.Exclude = [];
                }
                void NestedHits()
                {
                    Check(view.IntersectShape(nested, innerHits) == 1 && innerHits[0].ColliderRID == target, "A nested selected query returns its real nonempty result");
                    Check(view.CollideShape(nested, innerContacts) == 1, "A nested selected query returns its real contact pair");
                    Check(custom.IntersectShape(nested, innerHits) == 1 && custom.CollideShape(nested, innerContacts) == 1,
                        "A nested caller-created view retains separate nonempty output");
                }
                void Invoke(int route)
                {
                    var selectedView = route < 6 ? view : custom; var kind = route % 6;
                    query.Transform = new(0, new(kind == 2 && route < 12 ? -10 : 5, 0)); query.Motion = kind == 2 && route < 12 ? new(20, 0) : Vector2.Zero;
                    if (route >= 12)
                    {
                        var hit = route == 14 ? PhysicsServer.BodyCollideShape(target, 0, source.GetRID(), query.Transform, Vector2.Zero, contacts, out var count)
                            : route == 12 ? PhysicsServer.ShapeCollide(world.Space, source.GetRID(), query.Transform, Vector2.Zero, circle.GetRID(), Transform.Identity, Vector2.Zero, contacts, out count)
                            : PhysicsServer.ShapeCollide(world.Space, circle.GetRID(), Transform.Identity, Vector2.Zero, source.GetRID(), query.Transform, Vector2.Zero, contacts, out count);
                        Check(hit && count > 0, "Selected resource-pair and body-slot queries return actual contacts"); return;
                    }
                    switch (kind)
                    {
                        case 0: Check(selectedView.IntersectShape(query, hits) == 1 && hits[0].ColliderRID == target && hits[0].ShapeIndex == 0, "Span shape query retains exact logical identity"); break;
                        case 1: var array = selectedView.IntersectShape(query); Check(array.Length == 1 && array[0].ColliderRID == target, "Array shape query returns its real collider"); break;
                        // Eight binary refinements bracket the analytical circle contact at 0.2.
                        case 2: var fractions = selectedView.CastMotion(query); if (fractions.SafeFraction > .200001f || fractions.UnsafeFraction < .199999f || fractions.UnsafeFraction - fractions.SafeFraction > 3f / 512 + .000001f) throw new InvalidOperationException($"Circle sweep route {route}: safe={fractions.SafeFraction}, unsafe={fractions.UnsafeFraction}"); break;
                        case 3: Check(selectedView.CollideShape(query, contacts) == 1, "Span query returns an actual contact pair"); break;
                        case 4: Check(selectedView.CollideShape(query).Length == 2, "Array query returns one actual contact pair"); break;
                        case 5: Check(selectedView.GetRestInfo(query)?.ColliderRID == target, "Rest query retains the actual collider"); break;
                    }
                }
                void Frame()
                {
                    tree.PhysicsFrame(.01);
                    for (var route = 0; route < 15; route++) if (route % 6 is not (1 or 4) || route >= 12) Invoke(route);
                    point.Position = PhysicsServer.BodyGetTransform(moving).Origin;
                    Check(view.IntersectPoint(point, points) == 1 && points[0].ColliderRID == moving, "Query preparation retains complete moving physics");
                }
            }
            finally { PhysicsServer.FreeRID(target); PhysicsServer.FreeRID(moving); PhysicsServer.FreeRID(area); }
        }
    }
}
