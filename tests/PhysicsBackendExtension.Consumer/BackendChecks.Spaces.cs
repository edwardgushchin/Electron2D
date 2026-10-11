using Electron2D;

public static partial class BackendChecks
{
    public static void RunSpaces(PhysicsServer.Backend? backend = null) => SceneSpaceChecks(backend);
    private static void SceneSpaceChecks(PhysicsServer.Backend? backend = null)
    {
        var solvers = new List<Solver>();
        if (backend is null) PhysicsServerManager.RegisterServer("SceneSpaces", () => { var s = new Solver(); solvers.Add(s); return s; });
        using var first = backend is { } selected ? new World(selected) : new World("SceneSpaces");
        using var second = backend is { } other ? new World(other) : new World("SceneSpaces");
        var firstSpace = first.Space; var secondSpace = second.Space;
        using var circle = new CircleShape { Radius = 3 };
        using var root = new SubViewport { World = first };
        PhysicsBody[] bodies = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone()];
        var area = new Area { Position = new(500, 0) };
        area.AddChild(new CollisionShape { Shape = circle });
        using var point = new PhysicsPointQueryParameters { CollideWithAreas = true };
        var hits = new PhysicsPointResult[4];
        var raw = PhysicsServer.BodyCreate(); var rawArea = PhysicsServer.AreaCreate();
        try
        {
            for (var i = 0; i < bodies.Length; i++)
            {
                var body = bodies[i]; body.Position = new(i * 100, 0);
                body.AddChild(new CollisionShape { Shape = circle });
                Check(!PhysicsServer.BodyGetSpace(body.GetRID()).IsValid(), "Detached scene bodies expose an empty space");
                PhysicsServer.BodySetMode(body.GetRID(), PhysicsServer.BodyMode.Rigid);
                PhysicsServer.BodySetMass(body.GetRID(), 2); PhysicsServer.BodySetGravityScale(body.GetRID(), 0);
                PhysicsServer.BodySetCanSleep(body.GetRID(), false); PhysicsServer.BodySetLinearVelocity(body.GetRID(), new(6, 0));
                PhysicsServer.BodySetSpace(body.GetRID(), secondSpace);
                Check(PhysicsServer.BodyGetSpace(body.GetRID()) == secondSpace, "Every detached scene sibling accepts direct attachment");
                Query(second, body.GetRID(), body.Position);
            }
            Check(!PhysicsServer.AreaGetSpace(area.GetRID()).IsValid(), "Detached scene Areas expose an empty space");
            PhysicsServer.AreaSetSpace(area.GetRID(), secondSpace); Query(second, area.GetRID(), area.Position);
            foreach (var body in bodies) root.AddChild(body); root.AddChild(area);
            using var tree = new SceneTree(root);
            var secondary = new SubViewport { World = second }; root.AddChild(secondary); _ = secondary.FindWorld();
            foreach (var body in bodies) Check(PhysicsServer.BodyGetSpace(body.GetRID()) == firstSpace, "Scene entry reclaims the node's world binding");
            Check(PhysicsServer.AreaGetSpace(area.GetRID()) == firstSpace, "Area scene entry reclaims its world binding");
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetTransform(raw, new(0, new(600, 0)));
            PhysicsServer.AreaAddShape(rawArea, circle.GetRID()); PhysicsServer.AreaSetTransform(rawArea, new(0, new(700, 0)));
            PhysicsServer.BodySetSpace(raw, firstSpace); PhysicsServer.AreaSetSpace(rawArea, firstSpace);
            var stale = PhysicsServer.BodyGetDirectState(bodies[0].GetRID())!;
            foreach (var body in bodies) PhysicsServer.BodySetSpace(body.GetRID(), secondSpace);
            PhysicsServer.AreaSetSpace(area.GetRID(), secondSpace);
            PhysicsServer.BodySetSpace(raw, secondSpace); PhysicsServer.AreaSetSpace(rawArea, secondSpace);
            Reject<ObjectDisposedException>(() => _ = stale.Transform);
            foreach (var body in bodies)
            {
                Check(PhysicsServer.BodyGetSpace(body.GetRID()) == secondSpace && ReferenceEquals(body.GetWorld(), first),
                    "Direct attachment changes physics membership without rewriting the scene world descriptor");
                Query(second, body.GetRID(), body.Position);
                point.Position = body.Position; Check(first.DirectSpaceState.IntersectPoint(point, hits) == 0, "The previous space loses physical query membership");
            }
            Check(PhysicsServer.AreaGetSpace(area.GetRID()) == secondSpace && PhysicsServer.BodyGetSpace(raw) == secondSpace &&
                PhysicsServer.AreaGetSpace(rawArea) == secondSpace, "Scene/raw Body and Area getters report the actual attachment");
            Reject<ArgumentException>(() => PhysicsServer.BodySetSpace(bodies[0].GetRID(), raw));
            Reject<ArgumentException>(() => PhysicsServer.BodyGetSpace(area.GetRID()));
            Reject<ArgumentException>(() => PhysicsServer.AreaGetSpace(bodies[0].GetRID()));
            var before = bodies[0].Position;
            tree.PhysicsFrame(1d / 60);
            Check(bodies[0].Position.X > before.X && PhysicsServer.BodyGetDirectState(bodies[0].GetRID())!.GetSpaceState() == second.DirectSpaceState,
                "The target world advances actual scene bodies and direct-state queries");
            PhysicsServer.BodySetSpace(bodies[0].GetRID(), default); PhysicsServer.AreaSetSpace(area.GetRID(), default);
            Check(!PhysicsServer.BodyGetSpace(bodies[0].GetRID()).IsValid() && !PhysicsServer.AreaGetSpace(area.GetRID()).IsValid(), "Explicit detach reports empty scene attachments");
            root.World = second;
            foreach (var body in bodies) Check(PhysicsServer.BodyGetSpace(body.GetRID()) == secondSpace, "A world replacement reclaims even explicitly detached scene bodies");
            Check(PhysicsServer.AreaGetSpace(area.GetRID()) == secondSpace, "A world replacement reclaims an explicitly detached Area");
            root.RemoveChild(bodies[0]); Check(!PhysicsServer.BodyGetSpace(bodies[0].GetRID()).IsValid(), "Scene exit detaches the actual physical space");
            root.AddChild(bodies[0]); tree.PhysicsFrame(1d / 60);
            if (backend is null)
            {
                solvers[0].IdentityHook = _ => throw new ApplicationException("Target attachment failure");
                Reject<ApplicationException>(() => PhysicsServer.BodySetSpace(bodies[0].GetRID(), firstSpace));
                Reject<ApplicationException>(() => PhysicsServer.AreaSetSpace(area.GetRID(), firstSpace));
                Check(PhysicsServer.BodyGetSpace(bodies[0].GetRID()) == secondSpace && PhysicsServer.AreaGetSpace(area.GetRID()) == secondSpace,
                    "Failed scene attachments restore their previous physical space");
                solvers[1].IdentityHook = _ => throw new InvalidOperationException("Restoration failure");
                Reject<AggregateException>(() => PhysicsServer.BodySetSpace(bodies[0].GetRID(), firstSpace));
                Check(!PhysicsServer.BodyGetSpace(bodies[0].GetRID()).IsValid(), "Failed target and restoration leave an observable detached body");
                solvers[0].IdentityHook = null; solvers[1].IdentityHook = null;
                PhysicsServer.BodySetSpace(bodies[0].GetRID(), secondSpace);
                solvers[0].CapacityHook = () => throw new ApplicationException("Capacity failure after attachment");
                Reject<ApplicationException>(() => PhysicsServer.BodySetSpace(bodies[0].GetRID(), firstSpace));
                Reject<ApplicationException>(() => PhysicsServer.AreaSetSpace(area.GetRID(), firstSpace));
                Reject<ApplicationException>(() => PhysicsServer.BodySetSpace(raw, firstSpace));
                Reject<ApplicationException>(() => PhysicsServer.AreaSetSpace(rawArea, firstSpace));
                Check(PhysicsServer.BodyGetSpace(bodies[0].GetRID()) == secondSpace && PhysicsServer.AreaGetSpace(area.GetRID()) == secondSpace &&
                    PhysicsServer.BodyGetSpace(raw) == secondSpace && PhysicsServer.AreaGetSpace(rawArea) == secondSpace,
                    "Late attachment failures retire the target and restore every scene/raw sibling");
                solvers[0].CapacityHook = null;
                solvers[1].IdentityHook = id =>
                {
                    var space = id == area.GetRID() ? PhysicsServer.AreaGetSpace(id) : PhysicsServer.BodyGetSpace(id);
                    Reject<InvalidOperationException>(() => PhysicsServer.BodySetSpace(bodies[0].GetRID(), firstSpace));
                    Reject<InvalidOperationException>(() => PhysicsServer.AreaSetSpace(area.GetRID(), firstSpace));
                    Check(space == secondSpace, "Borrowed reads retain current scene/raw binding identity");
                };
                PhysicsServer.BodyAttachCanvasInstanceID(bodies[0].GetRID(), 17); solvers[1].IdentityHook = null;
            }
            var owned = backend is { } selectedOwned ? PhysicsServer.SpaceCreate(selectedOwned) : PhysicsServer.SpaceCreate("SceneSpaces");
            using var detachedParent = new Node();
            var detached = new RigidBody { GravityScale = 0, CanSleep = false };
            var childShape = new CollisionShape { Shape = circle };
            detached.AddChild(childShape); detachedParent.AddChild(detached);
            try
            {
                PhysicsServer.BodySetSpace(bodies[0].GetRID(), owned); PhysicsServer.AreaSetSpace(area.GetRID(), owned);
                PhysicsServer.BodySetSpace(detached.GetRID(), owned);
                PhysicsServer.SpaceSetActive(owned, true); before = bodies[0].Position;
                PhysicsServer.SpaceStep(owned, 1d / 60);
                Check(bodies[0].Position.X > before.X && PhysicsServer.BodyGetSpace(detached.GetRID()) == owned,
                    "Caller-owned spaces simulate attached and detached scene bodies independently of scene descriptors");
                if (backend is null)
                {
                    solvers[2].IdentityHook = id =>
                    {
                        var pose = detached.Position; var areaPose = area.Position;
                        Reject<InvalidOperationException>(() => detached.Position = Vector2.Zero);
                        Reject<InvalidOperationException>(() => area.Position = Vector2.Zero);
                        Reject<InvalidOperationException>(() => childShape.Shape = null);
                        Reject<InvalidOperationException>(() => detached.RemoveChild(childShape));
                        Reject<InvalidOperationException>(() => root.RemoveChild(bodies[0]));
                        Reject<InvalidOperationException>(() => detachedParent.Name = "Changed");
                        Reject<InvalidOperationException>(detached.Dispose);
                        Check(detached.Position == pose && area.Position == areaPose && childShape.Shape == circle && childShape.Parent == detached,
                            "Detached/imported scene authoring and hierarchy guards precede mutation during numerical borrowing");
                    };
                    PhysicsServer.BodyAttachCanvasInstanceID(detached.GetRID(), 17); solvers[2].IdentityHook = null;
                    using var procedural = new SpaceCircle();
                    using var cpuTarget = new World(PhysicsServer.Backend.CPU);
                    childShape.Shape = procedural;
                    var reads = 0;
                    procedural.Read = () =>
                    {
                        reads++;
                        Reject<InvalidOperationException>(detached.Dispose);
                        Reject<InvalidOperationException>(() => detachedParent.RemoveChild(detached));
                        Reject<InvalidOperationException>(() => detached.Position = Vector2.One);
                        Reject<InvalidOperationException>(() => childShape.Shape = null);
                        Reject<InvalidOperationException>(() => PhysicsServer.BodySetSpace(detached.GetRID(), default));
                    };
                    procedural.Invalidate();
                    PhysicsServer.BodySetSpace(detached.GetRID(), cpuTarget.Space); procedural.Read = null;
                    Check(reads > 0 && PhysicsServer.BodyGetSpace(detached.GetRID()) == cpuTarget.Space && detached.Parent == detachedParent,
                        "Source preflight guards detached scene geometry and lifetime before a cross-backend transfer");
                    childShape.Shape = circle; PhysicsServer.BodySetSpace(detached.GetRID(), owned);
                }
                Task.Run(() => Reject<InvalidOperationException>(() => detached.Position = Vector2.One)).GetAwaiter().GetResult();
                var expired = PhysicsServer.BodyGetDirectState(bodies[0].GetRID())!;
                PhysicsServer.FreeRID(owned); owned = default;
                Check(!PhysicsServer.BodyGetSpace(bodies[0].GetRID()).IsValid() && !PhysicsServer.BodyGetSpace(detached.GetRID()).IsValid() &&
                    !PhysicsServer.AreaGetSpace(area.GetRID()).IsValid(), "Freeing an owned space detaches actual scene resources without disposing their nodes");
                Reject<ObjectDisposedException>(() => _ = expired.Transform);
                root.World = first;
                foreach (var body in bodies) Check(PhysicsServer.BodyGetSpace(body.GetRID()) == firstSpace, "Scene world control can reclaim released direct attachments");
                Check(PhysicsServer.AreaGetSpace(area.GetRID()) == firstSpace && PhysicsServer.BodyGetSpace(raw) == secondSpace,
                    "Scene reclaim moves scene Areas while raw resources retain their explicit world");
                root.World = second;
            }
            finally { if (backend is null) solvers[2].IdentityHook = null; if (owned.IsValid()) PhysicsServer.FreeRID(owned); }
            Task.Run(() => Reject<InvalidOperationException>(() => PhysicsServer.BodyGetSpace(bodies[0].GetRID()))).GetAwaiter().GetResult();
            Collect(); _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) ReadAndStep();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) ReadAndStep();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Scene/raw space reads and full frames allocated {own}/{all} owner/all-thread B");
            Console.WriteLine($"Scene/raw space family on {backend?.ToString() ?? "registered"}: all five body siblings, Area, detached/live transfer, scene reclaim and 0/0 B full frames passed.");
            void ReadAndStep()
            {
                foreach (var body in bodies) Check(PhysicsServer.BodyGetSpace(body.GetRID()) == secondSpace, "Retained scene membership");
                Check(PhysicsServer.AreaGetSpace(area.GetRID()) == secondSpace && PhysicsServer.BodyGetSpace(raw) == secondSpace && PhysicsServer.AreaGetSpace(rawArea) == secondSpace, "Retained scene/raw membership");
                tree.PhysicsFrame(1d / 60);
            }
        }
        finally
        {
            foreach (var solver in solvers) { solver.IdentityHook = null; solver.CapacityHook = null; }
            PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(rawArea);
            foreach (var body in bodies) if (!body.IsDisposed) body.Dispose();
            if (!area.IsDisposed) area.Dispose();
        }
        void Query(World world, RID id, Vector2 position)
        {
            point.Position = position; var count = world.DirectSpaceState.IntersectPoint(point, hits);
            Check(count == 1 && hits[0].ColliderRID == id, "Direct scene/raw attachment participates in actual selected queries");
        }
    }

    private sealed class SpaceCircle : Shape
    {
        internal Action? Read;
        internal void Invalidate() => EmitGeometryChanged();
        protected override ShapeGeometry GetGeometryCore() { Read?.Invoke(); return ShapeGeometry.Circle(3); }
    }
}
