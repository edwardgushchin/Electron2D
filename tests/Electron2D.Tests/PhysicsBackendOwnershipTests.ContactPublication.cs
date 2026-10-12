using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    private sealed class PublishedBodyState(RID body, int ownerThread) : FactoryBodyState(body)
    {
        internal int Mode, CountCalls, ContactCalls;
        internal bool Hide, Probe;
        internal PhysicsBodyContact OldContact;
        internal Vector2 NativeImpulse;
        internal float NativeDepth;
        protected override int GetContactCountCore()
        {
            Check(Environment.CurrentManagedThreadId == ownerThread, "Selected contact hooks execute only on the world owner, after native worker capture");
            CountCalls++;
            if (Mode == 1) return Runtime.ContactLimit + 1;
            return Hide ? 0 : base.GetContactCountCore();
        }
        protected override PhysicsBodyContact GetContactCore(int index)
        {
            ContactCalls++;
            var value = CapturedContacts[index]; NativeImpulse = value.Impulse; NativeDepth = value.Depth;
            if (Probe) ProbeGuards(value);
            if (Mode == 2) throw new InvalidOperationException("Expected selected contact projection failure");
            if (Mode == 3) return default;
            if (Mode == 4) return OldContact;
            return new PhysicsBodyContact(Runtime.RID, value.LocalShape, value.Collider, value.ColliderShape, value.LocalPoint, value.ColliderPoint,
                value.Normal, value.LocalVelocity, value.ColliderVelocity, value.Impulse * 2, Mode == 5 ? -1 : value.Depth);
        }
        private void ProbeGuards(PhysicsDirectBodyState.Contact value)
        {
            RequireBorrowRejection(() => PhysicsServer.BodyGetTransform(Runtime.RID));
            RequireBorrowRejection(() => PhysicsServer.BodyApplyCentralImpulse(Runtime.RID, Vector2.One));
            RequireBorrowRejection(() => PhysicsServer.BodyGetDirectState(Runtime.RID));
            if (Runtime.Owners.Server is not null) RequireBorrowRejection(() => PhysicsServer.BodySetSpace(Runtime.RID, default));
            else RequireBorrowRejection(() => Runtime.Owners.Scene!.Parent!.RemoveChild(Runtime.Owners.Scene));
            RequireBorrowRejection(() => new PhysicsPointResult(value.Collider, value.ColliderShape));
            RequireBorrowRejection(Dispose);
        }
    }
    internal static void ContactPublication(PhysicsServer.Backend backend)
    {
        var thread = Environment.CurrentManagedThreadId;
        using var world = new World(new WorldRuntime(false, backend, factory: space => new ViewBackend(space, backend)
        { BodyFactory = body => new PublishedBodyState(body.RID, thread) }));
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 5 };
        using var rectangle = new RectangleShape { Size = new(120, 10) };
        var floor = new StaticBody { Name = "Floor", Position = new(0, 20) };
        floor.AddChild(new CollisionShape { Shape = rectangle }); root.AddChild(floor);
        var scene = new RigidBody { Name = "Receiver", CanSleep = false, MaxContactsReported = 8, ContactMonitor = true };
        scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
        var entered = 0; var exited = 0;
        scene.BodyEntered += node => { Check(ReferenceEquals(node, floor), "Projected monitor preserves the real scene collider"); entered++; };
        scene.BodyExited += node => { Check(ReferenceEquals(node, floor), "Projected departure preserves collider identity"); exited++; };
        for (var i = 0; i < 300; i++)
        {
            var body = new RigidBody
            {
                Name = "Sparse" + i,
                Position = new(i * 20, 1000),
                GravityScale = 0,
                CollisionLayer = 0,
                CollisionMask = 0,
                CanSleep = false,
                LinearVelocity = new(1, 0)
            };
            body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body);
        }
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetTransform(raw, new(0, new(30, 0)));
            PhysicsServer.BodySetCanSleep(raw, false); PhysicsServer.BodySetMaxContactsReported(raw, 8); PhysicsServer.BodySetSpace(raw, world.Space);
            var callbacks = 0;
            PhysicsServer.BodySetStateSyncCallback(raw, _ => callbacks++);
            for (var i = 0; i < 120; i++) tree.PhysicsFrame(1d / 60);
            var left = (PublishedBodyState)PhysicsServer.BodyGetDirectState(scene.GetRID())!;
            var right = (PublishedBodyState)PhysicsServer.BodyGetDirectState(raw)!;
            Check(left.GetContactCount() > 0 && right.GetContactCount() > 0 && scene.GetContactCount() == left.GetContactCount() && entered == 1 && exited == 0,
                "Selected scene/raw contacts reach real monitors and cached views");
            Check(left.GetContactImpulse(0) == left.NativeImpulse * 2 && right.GetContactImpulse(0) == right.NativeImpulse * 2,
                "Published contact hooks change actual reported impulse once");
            var calls = left.ContactCalls; var countCalls = left.CountCalls;
            var colliderID = left.GetContactColliderID(0);
            using var associated = new Node(); PhysicsServer.BodyAttachObject(floor.GetRID(), associated);
            Check(left.GetContactColliderID(0) == colliderID && ReferenceEquals(left.GetContactColliderObject(0), floor) && left.ContactCalls == calls && left.CountCalls == countCalls,
                "Published getter snapshots remain immutable after association changes and never reinvoke contact hooks");
            PhysicsServer.BodyAttachObject(floor.GetRID(), floor);
            left.Probe = right.Probe = true; tree.PhysicsFrame(1d / 60); left.Probe = right.Probe = false;
            Check(!PhysicsServer.Service.GetSceneSpace(world.Space).HasBackendFailure, "Read-only result construction does not reopen solver queries or mutation");
            left.Hide = true; tree.PhysicsFrame(1d / 60);
            Check(scene.GetContactCount() == 0 && left.GetContactCount() == 0 && exited == 1 && right.GetContactCount() > 0,
                "Selected suppression causes real scene contact departure while raw contacts remain present");
            left.Hide = false; tree.PhysicsFrame(1d / 60);
            Check(entered == 2 && scene.GetContactCount() > 0, "Selected reappearance causes one real scene reentry");
            foreach (var mode in new[] { 1, 2, 3 })
            {
                var old = left.GetContactImpulse(0); var beforeCallbacks = callbacks; var beforeTick = PhysicsServer.SpaceGetTick(world.Space);
                left.Mode = mode;
                Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
                Check(callbacks == beforeCallbacks + 1 && PhysicsServer.SpaceGetTick(world.Space) == beforeTick + 1 && !PhysicsServer.Service.GetSceneSpace(world.Space).HasBackendFailure &&
                    left.GetContactImpulse(0) == old && scene.GetContactCount() > 0 && right.GetContactCount() > 0,
                    "Bad selected contact count/value/hook preserves published receiver state, later callbacks and committed healthy solver tick");
                left.Mode = 0; tree.PhysicsFrame(1d / 60);
            }
            var saved = right.ReadContact(0);
            PhysicsServer.BodySetSpace(raw, default); PhysicsServer.BodySetSpace(raw, world.Space);
            right = (PublishedBodyState)PhysicsServer.BodyGetDirectState(raw)!;
            right.OldContact = saved; right.Mode = 4;
            Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
            Check(right.GetContactCount() == 0 && !PhysicsServer.Service.GetSceneSpace(world.Space).HasBackendFailure, "Previous-generation contact cannot publish through a replacement factory view");
            right.Mode = 0;
            Check(left.ReadContact(0).Depth == left.NativeDepth, "Cached projection retains explicit numerical scene-unit depth");
            var projectedCalls = left.ContactCalls;
            PhysicsServer.BodySetContactsReportedDepthThreshold(scene.GetRID(), .000001f); left.Mode = 5;
            scene.Position = new(0, 11); scene.LinearVelocity = Vector2.Zero;
            tree.PhysicsFrame(1d / 60);
            Check(left.ContactCalls > projectedCalls && left.GetContactCount() == 0 && scene.GetContactCount() == 0,
                $"Cached projected shallow depth filters after eligible capture: hooks={left.ContactCalls - projectedCalls}, view={left.GetContactCount()}, scene={scene.GetContactCount()}, nativeDepth={left.NativeDepth}");
            left.Mode = 0; PhysicsServer.BodySetContactsReportedDepthThreshold(scene.GetRID(), 0);
            tree.PhysicsFrame(1d / 60);
            Check(left.GetContactCount() > 0, "Clearing projected threshold restores committed contact reporting");
            for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 60);
            var space = PhysicsServer.Service.GetSceneSpace(world.Space);
            if (backend == PhysicsServer.Backend.CPU)
                Check(Box2D.NET.B2Worlds.b2GetWorldFromId(space.WorldID).workerCount > 1, "CPU fixture exercises real worker contact capture");
            Collect(); for (var i = 0; i < 64; i++) Cycle();
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) Cycle();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0, $"Selected contact publication allocated {own}/{all} owner/all-thread bytes");
            Console.WriteLine($"{backend}: selected scene/raw contact hooks, doubled immutable impulses, real monitor suppression/reentry, failure/stale guards and 301 scene + 1 raw bodies/64 warm full frames: {own}/{all} B.");
            void Cycle()
            {
                tree.PhysicsFrame(1d / 60);
                _ = left.GetContactImpulse(0); _ = right.GetContactImpulse(0);
            }
        }
        finally { PhysicsServer.FreeRID(raw); }
    }
}
