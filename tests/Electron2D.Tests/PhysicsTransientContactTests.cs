using Electron2D;

internal static class PhysicsTransientContactTests
{
    private const int Slots = 32, Points = Slots * 2;
    private const double Delta = 1d / 60;

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
                foreach (var from in backends)
                    foreach (var raw in new[] { false, true })
                        foreach (var mode in new[] { CCDMode.CastRay, CCDMode.CastShape })
                        {
                            for (var disabled = 0; disabled < 2; disabled++)
                            {
                                using var control = new Rig(from, raw, mode, false);
                                PhysicsServer.BodySetShapeDisabled(control.Corridor.GetRID(), disabled, true); control.Reset(); control.Tick();
                                Check(control.View.GetContactCount() < Points, "Both active walls are necessary for complete transient history.");
                            }
                            foreach (var to in backends) Verify(from, to, raw, mode, separate);
                        }
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Check(DisplayServer.IsAvailable == display && RenderingServer.IsAvailable == rendering, "Transient worlds preserve display/rendering service state.");
        if (Environment.GetEnvironmentVariable("ELECTRON2D_TEST_TRANSIENT_CONTACTS") is not null)
            Check(!display && !rendering, "Focused transient physics runs without display or rendering services.");
    }

    private static void Verify(PhysicsServer.Backend from, PhysicsServer.Backend to, bool raw, CCDMode mode, bool separate)
    {
        using var a = new Rig(from, raw, mode, false); using var b = new Rig(to, raw, mode, true);
        a.Reset(); var before = a.View.LinearVelocity; a.Tick(); a.Contacts(Points);
        var impulse = a.Impulse();
        Check(impulse.DistanceTo((a.View.LinearVelocity - before) * 2) < 2, "Transient complete-frame impulse balances actual momentum.");
        Check(impulse.Y > 20, "Transient contacts transfer real conveyor friction.");
        Check(MathF.Abs(a.Impulse(0).X - 24000) < 2 && MathF.Abs(a.Impulse(1).X + 48000) < 2,
            "Per-wall physical totals prove one left and two right impacts in the complete frame.");
        Check(a.View.LinearVelocity.X < -5900 && MathF.Abs(a.View.Transform.Origin.X) < 16, "Three CCD bounces consume the remaining frame and end away from both walls.");
        if (!raw) Check(a.ObjectEnters == 1 && a.ShapeEnters == Points && a.EnteredSlots[0] == uint.MaxValue && a.EnteredSlots[1] == uint.MaxValue,
            "One transient object entry retains all 64 logical shape-event pairs.");
        var sent = new PhysicsSnapshot(); var received = new PhysicsSnapshot(); var packet = new byte[65536];
        Transfer(); b.Contacts(Points);
        Check(b.ObjectEnters == 0 && b.ShapeEnters == 0, "Importing departed contacts is silent.");
        before = b.View.LinearVelocity; b.Tick(); b.Contacts(Points);
        Check(b.Impulse().DistanceTo((b.View.LinearVelocity - before) * 2) < 2, "The corrected continuation retains its complete physical impulse.");
        Check(b.ObjectEnters == 0 && b.ShapeEnters == 0, "The first corrected frame does not repeat imported contact entries.");
        foreach (var cap in new[] { 63, 7, 1, 0, Points })
        {
            PhysicsServer.BodySetMaxContactsReported(a.Body, cap); Check(a.View.GetContactCount() == 0, "Transient cap change clears the preceding report.");
            a.Reset(); a.Tick(); a.Contacts(cap);
        }
        for (var i = 0; i < 96; i++) Cycle();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 32; i++) Cycle();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) Cycle();
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"Transient CCD/capture/transport/apply/replay allocated {owner}/{all} owner/all-thread bytes.");
        a.Contacts(Points); b.Contacts(Points);
        Check(a.View.LinearVelocity.X < -5900 && MathF.Abs(a.View.Transform.Origin.X) < 16, "Measured source frames retain three real impacts and departed contacts.");
        Check(a.Impulse().DistanceTo((a.View.LinearVelocity - new Vector2(6000, 0)) * 2) < 2 &&
            b.Impulse().DistanceTo((b.View.LinearVelocity - a.View.LinearVelocity) * 2) < 2,
            "Measured source and replay reports retain their own complete physical impulse totals.");
        Check(MathF.Abs(a.Impulse(0).X - 24000) < 2 && MathF.Abs(a.Impulse(1).X + 48000) < 2, "Measured frames retain all three per-wall impact totals.");
        Console.WriteLine($"Transient CCD {from}->{to}, raw={raw}, mode={mode}, separate={separate}: 64 point histories, momentum/caps/events; 64 warmed cycles owner/all-thread={owner}/{all}, packet={sent.GetEncodedSize()} bytes.");
        void Transfer()
        {
            a.Map.Capture(sent); var length = sent.WriteTo(packet); received.ReadFrom(packet.AsSpan(0, length)); b.Map.Apply(received);
            Check(PhysicsServer.SpaceGetTick(a.World.Space) == PhysicsServer.SpaceGetTick(b.World.Space) && ReferenceEquals(b.View, PhysicsServer.BodyGetDirectState(b.Body)), "Transient restore preserves its tick and direct-view identity.");
            Check(a.View.GetContactCount() == Points && b.View.GetContactCount() == Points, "Transient restore retains all sampled points.");
            for (var i = 0; i < Points; i++)
                Check(a.View.GetContactLocalShape(i) == b.View.GetContactLocalShape(i) && a.View.GetContactColliderShape(i) == b.View.GetContactColliderShape(i) &&
                    a.View.GetContactLocalPosition(i) == b.View.GetContactLocalPosition(i) && a.View.GetContactLocalNormal(i) == b.View.GetContactLocalNormal(i) &&
                    a.View.GetContactImpulse(i) == b.View.GetContactImpulse(i), "Transient restore preserves the sampled logical feature payload exactly.");
        }
        void Cycle() { a.Reset(); a.Tick(); Transfer(); b.Tick(); }
    }

    private sealed class Rig : IDisposable
    {
        internal readonly World World;
        internal readonly SubViewport Root;
        internal readonly SceneTree Tree;
        internal readonly PhysicsSnapshotMap Map;
        internal readonly CircleShape Circle = new() { Radius = 2 };
        internal readonly RectangleShape Wall = new() { Size = new(4, 1000) };
        internal readonly StaticBody Corridor;
        internal readonly RigidBody? SceneBody;
        internal readonly RID Body;
        internal readonly PhysicsDirectBodyState View;
        internal int ObjectEnters, ShapeEnters;
        internal readonly uint[] EnteredSlots = new uint[2];

        internal Rig(PhysicsServer.Backend backend, bool raw, CCDMode mode, bool reverse)
        {
            World = new(backend); Root = new() { World = World }; Tree = new(Root); _ = Root.FindWorld();
            Corridor = new() { Name = "Corridor", ConstantLinearVelocity = new(0, 120) };
            Corridor.AddChild(new CollisionShape { Name = "Left", Shape = Wall, Position = new(-22, 0) });
            Corridor.AddChild(new CollisionShape { Name = "Right", Shape = Wall, Position = new(22, 0) });
            if (!reverse) Root.AddChild(Corridor);
            if (raw)
            {
                Body = PhysicsServer.BodyCreate();
                for (var i = 0; i < Slots; i++) PhysicsServer.BodyAddShape(Body, Circle.GetRID(), new Transform(0, new(0, Y(i))));
                PhysicsServer.BodySetSpace(Body, World.Space);
            }
            else
            {
                SceneBody = new() { Name = "Receiver", ContactMonitor = true };
                for (var i = 0; i < Slots; i++) SceneBody.AddChild(new CollisionShape { Name = "Slot" + i, Shape = Circle, Position = new(0, Y(i)) });
                SceneBody.BodyEntered += node => { Check(ReferenceEquals(node, Corridor), "Transient object entry names its local corridor."); ObjectEnters++; };
                SceneBody.BodyShapeEntered += (rid, node, remote, local) =>
                {
                    Check(rid == Corridor.GetRID() && ReferenceEquals(node, Corridor) && (uint)remote < 2 && (uint)local < Slots, "Transient shape event preserves peer and both logical indices.");
                    EnteredSlots[remote] |= 1u << local; ShapeEnters++;
                };
                Root.AddChild(SceneBody); Body = SceneBody.GetRID();
            }
            if (reverse) Root.AddChild(Corridor);
            PhysicsServer.BodySetMode(Body, PhysicsServer.BodyMode.RigidLinear);
            PhysicsServer.BodySetMass(Body, 2); PhysicsServer.BodySetGravityScale(Body, 0); PhysicsServer.BodySetCanSleep(Body, false);
            PhysicsServer.BodySetLinearDampMode(Body, RigidBody.DampMode.Replace); PhysicsServer.BodySetLinearDamp(Body, 0);
            PhysicsServer.BodySetBounce(Body, .5f); PhysicsServer.BodySetBounce(Corridor.GetRID(), .5f);
            PhysicsServer.BodySetFriction(Body, .5f); PhysicsServer.BodySetFriction(Corridor.GetRID(), .5f);
            PhysicsServer.BodySetContinuousCollisionDetectionMode(Body, mode); PhysicsServer.BodySetMaxContactsReported(Body, Points);
            View = PhysicsServer.BodyGetDirectState(Body)!;
            Map = new(World.Space); Map.Bind(10, 1, Corridor.GetRID()); Map.Bind(20, 1, Body);
            Check(PhysicsServer.SpaceGetBackend(World.Space) == backend, "Transient fixture uses its selected public backend.");
        }

        internal void Reset() { PhysicsServer.BodySetTransform(Body, Transform.Identity); PhysicsServer.BodySetLinearVelocity(Body, new(6000, 0)); }
        internal void Tick()
        {
            var before = PhysicsServer.SpaceGetTick(World.Space); Tree.PhysicsFrame(Delta);
            Check(PhysicsServer.SpaceGetTick(World.Space) == before + 1, "Transient interval commits one numbered tick.");
            Check(Corridor.GlobalTransform == Transform.Identity, "Surface velocity never moves the corridor pose.");
        }
        internal Vector2 Impulse(int remote = -1)
        {
            var sum = Vector2.Zero;
            for (var i = 0; i < View.GetContactCount(); i++) if (remote < 0 || View.GetContactColliderShape(i) == remote) sum += View.GetContactImpulse(i);
            return sum;
        }
        internal void Contacts(int count)
        {
            if (View.GetContactCount() != count) throw new InvalidOperationException($"Transient report expected {count} points, got {View.GetContactCount()}, pose={View.Transform.Origin}, velocity={View.LinearVelocity}, impulse={Impulse()}.");
            uint left = 0, right = 0;
            for (var i = 0; i < count; i++)
            {
                var local = View.GetContactLocalShape(i); var remote = View.GetContactColliderShape(i);
                Check((uint)local < Slots && (uint)remote < 2, "Transient reports retain both logical shape indices.");
                var mask = remote == 0 ? left : right; Check((mask & (1u << local)) == 0, "Transient feature pair is not duplicated.");
                if (remote == 0) left |= 1u << local; else right |= 1u << local;
                Check(View.GetContactCollider(i) == Corridor.GetRID() && View.GetContactColliderID(i) == Corridor.InstanceID && ReferenceEquals(View.GetContactColliderObject(i), Corridor), "Transient peer resolves to its local RID/instance/object.");
                var point = View.GetContactLocalPosition(i); var normal = View.GetContactLocalNormal(i);
                Check(point.IsFinite() && normal.IsFinite() && View.GetContactImpulse(i).IsFinite() && MathF.Abs(normal.Length() - 1) < .002f, "Transient points, normals and impulses remain finite.");
                Check(MathF.Abs(normal.X - (remote == 0 ? 1 : -1)) < .002f && MathF.Abs(point.Y - View.Transform.Origin.Y - Y(local)) < 2.1f, "Transient point and normal match their authored slot and wall.");
            }
            if (count == Points) Check(left == uint.MaxValue && right == uint.MaxValue, "The full frame retains every slot against both walls.");
        }
        public void Dispose()
        {
            Map.Dispose(); View.Dispose(); if (SceneBody is null) PhysicsServer.FreeRID(Body);
            Tree.Dispose(); Root.Dispose(); World.Dispose(); Circle.Dispose(); Wall.Dispose();
        }
    }
    private static float Y(int slot) => (slot - (Slots - 1) * .5f) * 5;
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
}
