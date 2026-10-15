using Electron2D;

internal static class PhysicsDenseContactTests
{
    private const int Slots = 32;
    private const double Step = 1d / 60;
    private static readonly Vector2 Load = new(0, 60);

    internal static void Run(bool gpu)
    {
        var display = DisplayServer.IsAvailable; var rendering = RenderingServer.IsAvailable;
        var backends = gpu ? new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU } : new[] { PhysicsServer.Backend.CPU };
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        try
        {
            foreach (var separate in OperatingSystem.IsBrowser() ? new[] { false } : new[] { false, true })
            {
                ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, separate);
                foreach (var raw in new[] { false, true })
                    foreach (var from in backends)
                        foreach (var to in backends) Verify(from, to, raw, separate);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Check(DisplayServer.IsAvailable == display && RenderingServer.IsAvailable == rendering, "Dense CPU/GPU worlds preserve display/rendering service state.");
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_DENSE_CONTACTS") is not null)
            Check(!display && !rendering, "Focused dense CPU/GPU execution remains headless without display or rendering services.");
    }

    private sealed class Rig : IDisposable
    {
        internal readonly World World;
        internal readonly SubViewport Root;
        internal readonly SceneTree Tree;
        internal readonly PhysicsSnapshotMap Map;
        internal readonly CircleShape Circle = new() { Radius = 2 };
        internal readonly RectangleShape Ground = new() { Size = new(10000, 4) };
        internal readonly StaticBody Floor;
        internal readonly RigidBody? SceneBody;
        internal readonly RID Body;
        internal readonly PhysicsDirectBodyState View;
        internal int ObjectEnters, ShapeEnters;

        internal Rig(PhysicsServer.Backend backend, bool raw, bool reverse)
        {
            World = new(backend); Root = new() { World = World }; Tree = new(Root); _ = Root.FindWorld();
            Floor = new() { Name = "Floor", Position = new(0, 2), ConstantLinearVelocity = new(120, 0) };
            Floor.AddChild(new CollisionShape { Shape = Ground });
            if (!reverse) Root.AddChild(Floor);
            if (raw)
            {
                Body = PhysicsServer.BodyCreate();
                for (var i = 0; i < Slots; i++) PhysicsServer.BodyAddShape(Body, Circle.GetRID(), new Transform(0, new(X(i), 0)));
                PhysicsServer.BodySetSpace(Body, World.Space);
            }
            else
            {
                SceneBody = new() { Name = "Receiver", ContactMonitor = true };
                for (var i = 0; i < Slots; i++) SceneBody.AddChild(new CollisionShape { Name = "Slot" + i, Shape = Circle, Position = new(X(i), 0) });
                SceneBody.BodyEntered += _ => ObjectEnters++;
                SceneBody.BodyShapeEntered += (_, _, _, _) => ShapeEnters++;
                Root.AddChild(SceneBody); Body = SceneBody.GetRID();
            }
            if (reverse) Root.AddChild(Floor);
            PhysicsServer.BodySetMode(Body, PhysicsServer.BodyMode.RigidLinear);
            PhysicsServer.BodySetMass(Body, 2); PhysicsServer.BodySetGravityScale(Body, 0); PhysicsServer.BodySetCanSleep(Body, false);
            PhysicsServer.BodySetLinearDampMode(Body, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(Body, 0);
            PhysicsServer.BodySetConstantForce(Body, Load); PhysicsServer.BodySetMaxContactsReported(Body, Slots);
            PhysicsServer.BodySetTransform(Body, new(0, new(0, reverse ? -200 : -1.95f)));
            PhysicsServer.BodySetLinearVelocity(Body, new(0, 40));
            View = PhysicsServer.BodyGetDirectState(Body)!;
            Map = new(World.Space); Map.Bind(10, 1, Floor.GetRID()); Map.Bind(20, 1, Body);
            Check(PhysicsServer.SpaceGetBackend(World.Space) == backend, "Dense fixture uses its selected public backend.");
        }

        internal void Tick()
        {
            var before = PhysicsServer.SpaceGetTick(World.Space); Tree.PhysicsFrame(Step);
            Check(PhysicsServer.SpaceGetTick(World.Space) == before + 1, "Dense interval commits one numbered tick.");
        }

        internal Vector2 Contacts(int count)
        {
            if (View.GetContactCount() != count) throw new InvalidOperationException($"Dense report expected {count} physical points, got {View.GetContactCount()}.");
            uint mask = 0; var impulse = Vector2.Zero;
            var inverse = View.Transform.AffineInverse();
            for (var i = 0; i < count; i++)
            {
                var slot = View.GetContactLocalShape(i); var point = inverse * View.GetContactLocalPosition(i);
                Check((uint)slot < Slots && (mask & (1u << slot)) == 0, "Dense circle contacts retain distinct logical slots.");
                Check(View.GetContactCollider(i) == Floor.GetRID() && View.GetContactColliderShape(i) == 0 &&
                    View.GetContactColliderID(i) == Floor.InstanceID && ReferenceEquals(View.GetContactColliderObject(i), Floor),
                    "Dense contact retains its local floor identity and collider slot.");
                Check(point.IsFinite() && MathF.Abs(point.X - X(slot)) < 2.1f, "Dense contact point agrees with its authored circle slot.");
                var normal = View.GetContactLocalNormal(i); var value = View.GetContactImpulse(i);
                Check(normal.IsFinite() && MathF.Abs(normal.Length() - 1) < .002f && value.IsFinite(), "Dense contact normal and impulse are finite.");
                mask |= 1u << slot; impulse += value;
            }
            if (count == Slots) Check(mask == uint.MaxValue, "Dense report includes every local slot.");
            if (SceneBody is not null) Check(SceneBody.GetCollidingBodies().Length == (count == 0 ? 0 : 1), "Dense monitor deduplicates all slots to one floor object.");
            return impulse;
        }

        public void Dispose()
        {
            Map.Dispose(); View.Dispose(); if (SceneBody is null) PhysicsServer.FreeRID(Body);
            Tree.Dispose(); Root.Dispose(); World.Dispose(); Circle.Dispose(); Ground.Dispose();
        }
    }

    private static void Verify(PhysicsServer.Backend from, PhysicsServer.Backend to, bool raw, bool separate)
    {
        using var a = new Rig(from, raw, false); using var b = new Rig(to, raw, true);
        var before = a.View.LinearVelocity; a.Tick();
        var impulse = a.Contacts(Slots);
        // Scene impulses use kg*scene-unit/s; remove the known constant force over the complete 1/60 s interval.
        Check(impulse.DistanceTo((a.View.LinearVelocity - before) * 2 - Load * (float)Step) < .08f,
            "Dense normal/friction impulse sum matches the complete physical momentum change.");
        Check(impulse.X > 1 && impulse.Y < -1, "Dense contact report contains actual normal and conveyor friction response.");
        if (!raw) Check(a.ObjectEnters == 1 && a.ShapeEnters == Slots, "Dense scene reports one object and every logical shape entry.");
        b.Tick(); Check(b.View.GetContactCount() == 0, "Target begins with no floor history.");
        var sent = new PhysicsSnapshot(); var received = new PhysicsSnapshot(); var packet = new byte[65536];
        Transfer(); b.Contacts(Slots);
        Check(b.ObjectEnters == 0 && b.ShapeEnters == 0, "Dense portable restore is silent.");
        b.Tick(); b.Contacts(Slots);
        Check(b.ObjectEnters == 0 && b.ShapeEnters == 0, "First target step does not repeat imported dense entries.");
        foreach (var cap in new[] { 7, 1, 0, Slots })
        {
            PhysicsServer.BodySetMaxContactsReported(a.Body, cap);
            Check(a.View.GetContactCount() == 0, "Dense cap change clears the preceding point report.");
            a.Tick(); a.Contacts(cap);
        }
        if (!raw) Check(a.ShapeEnters >= Slots && a.SceneBody!.GetContactCount() == Slots, "Dense monitor returns to complete shape coverage after cap changes.");
        for (var i = 0; i < 96; i++) Cycle();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 32; i++) Cycle();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) Cycle();
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"Dense capture/transport/apply/replay allocated owner/all-thread {owner}/{all} bytes.");
        Check(a.Contacts(Slots).Y < -.5f && b.Contacts(Slots).Y < -.5f, "Both dense continuations still carry the real downward load after measurement.");
        Console.WriteLine($"Dense contacts {from}->{to}, raw={raw}, separate={separate}: {Slots} local slots, momentum/caps/identity/imported events; 64 warmed cycles owner/all-thread={owner}/{all}, packet={sent.GetEncodedSize()} bytes.");

        void Transfer()
        {
            a.Map.Capture(sent); var length = sent.WriteTo(packet); received.ReadFrom(packet.AsSpan(0, length)); b.Map.Apply(received);
            Check(PhysicsServer.SpaceGetTick(a.World.Space) == PhysicsServer.SpaceGetTick(b.World.Space), "Dense portable restore carries the authoritative tick.");
            Check(ReferenceEquals(b.View, PhysicsServer.BodyGetDirectState(b.Body)), "Dense restore preserves direct-view identity.");
            Check(a.View.GetContactCount() == Slots && b.View.GetContactCount() == Slots, "Dense portable restore retains all sampled points.");
            for (var i = 0; i < Slots; i++)
                Check(a.View.GetContactLocalShape(i) == b.View.GetContactLocalShape(i) && a.View.GetContactLocalPosition(i) == b.View.GetContactLocalPosition(i) &&
                    a.View.GetContactLocalNormal(i) == b.View.GetContactLocalNormal(i) && a.View.GetContactImpulse(i) == b.View.GetContactImpulse(i),
                    "Dense portable restore preserves point/slot/normal/impulse payloads exactly.");
        }
        void Cycle() { a.Tick(); Transfer(); b.Tick(); }
    }

    private static float X(int slot) => (slot - (Slots - 1) * .5f) * 5;
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
