using System.Buffers.Binary;
using System.Diagnostics;
using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsSnapshotTests
{
    internal static void Run(bool gpu = false)
    {
        var backends = gpu ? new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU } : new[] { PhysicsServer.Backend.CPU };
        foreach (var from in backends)
            foreach (var to in backends) { Contacts(from, to); Forces(from, to); Server(from, to); SceneMotion(from, to); for (var kind = 0; kind < 3; kind++) { Joints(from, to, kind); OneWay(from, to, kind); } }
        Guards();
        if (gpu) ApplyFailure();
        foreach (var backend in backends) Warm(backend, backend);
        if (gpu) Warm(PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_SNAPSHOT_HEADLESS") == "1") Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "CPU snapshots never create graphics services");
        Console.WriteLine("Portable physics snapshots: identities, observers, forces, server state, validation and warmed transport passed.");
    }
    private sealed class Rig : IDisposable
    {
        internal readonly World World;
        internal readonly SubViewport Root;
        internal readonly SceneTree Tree;
        internal readonly PhysicsSnapshotMap Map;
        internal readonly CircleShape Circle = new() { Radius = 10 };
        internal readonly CircleShape Region = new() { Radius = 100 };
        internal readonly RectangleShape Floor = new() { Size = new(600, 20) };
        internal readonly List<RID> ServerObjects = [];
        internal Rig(PhysicsServer.Backend backend)
        {
            World = new(backend); Root = new() { World = World, Size = new(600, 400) }; Tree = new(Root); _ = Root.FindWorld(); Map = new(World.Space);
        }
        internal RigidBody Ball(ulong id, Vector2 position, bool sleep = false)
        {
            var body = new RigidBody { Name = "Ball" + id, Position = position, CanSleep = sleep, MaxContactsReported = 8, ContactMonitor = true };
            body.AddChild(new CollisionShape { Shape = Circle }); Root.AddChild(body); Map.Bind(id, 1, body.GetRID()); return body;
        }
        internal StaticBody Ground(ulong id = 1, bool oneWay = false)
        {
            var floor = new StaticBody { Name = "Ground" + id, Position = new(0, 100) };
            floor.AddChild(new CollisionShape { Shape = Floor, OneWayCollision = oneWay }); Root.AddChild(floor); Map.Bind(id, 1, floor.GetRID()); return floor;
        }
        internal Area Sensor(ulong id = 3)
        {
            var area = new Area { Name = "Sensor" + id, Position = new(0, 70), Monitoring = true };
            area.AddChild(new CollisionShape { Shape = Region }); Root.AddChild(area); Map.Bind(id, 1, area.GetRID()); return area;
        }
        internal RID ServerBall(ulong id, Vector2 position)
        {
            var shape = PhysicsServer.CircleShapeCreate(); ServerObjects.Add(shape); PhysicsServer.ShapeSetData(shape, Circle);
            var body = PhysicsServer.BodyCreate(); ServerObjects.Add(body); PhysicsServer.BodyAddShape(body, shape);
            PhysicsServer.BodySetMaxContactsReported(body, 8); PhysicsServer.BodySetTransform(body, new Transform(0, position));
            PhysicsServer.BodySetSpace(body, World.Space); Map.Bind(id, 1, body); return body;
        }
        internal RID ServerSensor(ulong id)
        {
            var shape = PhysicsServer.CircleShapeCreate(); ServerObjects.Add(shape); PhysicsServer.ShapeSetData(shape, Region);
            var area = PhysicsServer.AreaCreate(); ServerObjects.Add(area); PhysicsServer.AreaAddShape(area, shape);
            PhysicsServer.AreaSetSpace(area, World.Space); Map.Bind(id, 1, area); return area;
        }
        internal void Step(int count = 1)
        {
            for (var i = 0; i < count; i++)
            {
                var before = PhysicsServer.SpaceGetTick(World.Space); Tree.PhysicsFrame(1d / 60);
                Check(PhysicsServer.SpaceGetTick(World.Space) == before + 1, "The fixture advances a real world tick");
            }
        }
        public void Dispose()
        {
            Map.Dispose(); for (var i = ServerObjects.Count - 1; i >= 0; i--) PhysicsServer.FreeRID(ServerObjects[i]);
            Tree.Dispose(); Root.Dispose(); World.Dispose(); Circle.Dispose(); Region.Dispose(); Floor.Dispose();
        }
    }
    private static PhysicsSnapshot Transfer(Rig source, Rig target)
    {
        var sent = new PhysicsSnapshot(); source.Map.Capture(sent);
        var packet = new byte[sent.GetEncodedSize()]; Check(sent.WriteTo(packet) == packet.Length, "Exact packet size");
        var received = new PhysicsSnapshot(); received.ReadFrom(packet); target.Map.Apply(received);
        Check(PhysicsServer.SpaceGetTick(target.World.Space) == sent.Tick && received.ObjectCount == sent.ObjectCount, "Portable tick/count restored");
        return received;
    }
    private static void Near(RID source, RID target, float position = .001f, float velocity = .001f, float angle = .001f, float spin = .001f)
    {
        var a = PhysicsServer.BodyGetDirectState(source)!; var b = PhysicsServer.BodyGetDirectState(target)!;
        Check(a.Transform.Origin.DistanceTo(b.Transform.Origin) <= position && Math.Abs(a.Transform.Rotation - b.Transform.Rotation) <= angle &&
            a.LinearVelocity.DistanceTo(b.LinearVelocity) <= velocity && Math.Abs(a.AngularVelocity - b.AngularVelocity) <= spin && a.Sleeping == b.Sleeping,
            $"Transferred state differs: {a.Transform.Origin}/{a.Transform.Rotation}/{a.LinearVelocity}/{a.AngularVelocity}/{a.Sleeping} vs {b.Transform.Origin}/{b.Transform.Rotation}/{b.LinearVelocity}/{b.AngularVelocity}/{b.Sleeping}");
    }
    private static void Contacts(PhysicsServer.Backend from, PhysicsServer.Backend to)
    {
        using var a = new Rig(from); a.Ground(oneWay: true); var body = a.Ball(2, new(0, 80)); var area = a.Sensor(); a.Step(90);
        using var b = new Rig(to); var otherArea = b.Sensor(); var other = b.Ball(2, new(400, -50)); b.Ground(oneWay: true); b.Step(4);
        var view = PhysicsServer.BodyGetDirectState(body.GetRID())!; var otherView = PhysicsServer.BodyGetDirectState(other.GetRID())!;
        Check(view.GetContactCount() > 0 && area.OverlapsBody(body), "Source has real contacts and Area history");
        var enters = 0; var exits = 0; var transforms = 0; var bodyEnters = 0;
        otherArea.BodyEntered += _ => enters++; otherArea.BodyExited += _ => exits++; other.BodyEntered += _ => bodyEnters++;
        other.NotifyTransformChanges = true; other.TransformChanged += _ => transforms++;
        Transfer(a, b); Near(body.GetRID(), other.GetRID());
        Check(enters == 0 && exits == 0 && transforms == 0 && bodyEnters == 0, "Apply is silent");
        Check(ReferenceEquals(otherView, PhysicsServer.BodyGetDirectState(other.GetRID())) && otherArea.OverlapsBody(other), "Local observer identities and history survive");
        Check(otherView.GetContactCount() == view.GetContactCount() && otherView.GetContactImpulse(0) == view.GetContactImpulse(0), "Contact reports copied immediately");
        Check(otherView.GetContactCollider(0) != view.GetContactCollider(0), "Contact RIDs remapped into the target world");
        using (var fresh = new Rig(to))
        {
            fresh.Ground(oneWay: true); var newborn = fresh.Ball(2, new(400, -50)); fresh.Sensor(); Transfer(a, fresh);
            Check(PhysicsServer.BodyGetDirectState(newborn.GetRID())!.GetContactCount() == view.GetContactCount(), "A new client exposes imported contacts before its first tick");
        }
        b.Step(); Check(enters == 0 && bodyEnters == 0, "Next tick does not repeat contact or overlap enters");
        other.GlobalPosition = new(400, -50); b.Step(2); Check(exits == 1, "A restored overlap exits once");
        Console.WriteLine($"Portable contacts {from}->{to}: passed");
    }
    private static void Forces(PhysicsServer.Backend from, PhysicsServer.Backend to)
    {
        using var a = new Rig(from); var body = a.Ball(1, default, true); body.GravityScale = 0; a.Step(20); Check(!body.Sleeping, "Source is partway through its quiet interval");
        using var b = new Rig(to); var other = b.Ball(1, new(100, 0), true); other.GravityScale = 0; b.Step();
        Transfer(a, b); a.Step(16); b.Step(16);
        Check(body.Sleeping && other.Sleeping, "Correction preserves the already elapsed quiet sleep interval");
        Transfer(a, b); Near(body.GetRID(), other.GetRID()); b.Step(); Check(other.Sleeping, "Imported sleeper remains asleep on the next tick");
        body.ConstantForce = new(6, 0); body.ApplyCentralForce(new(120, 30));
        Transfer(a, b); Check(other.ConstantForce == body.ConstantForce, "Constant force transfers");
        a.Step(12); b.Step(12); Near(body.GetRID(), other.GetRID(), .05f, .05f);
        Console.WriteLine($"Portable sleeping/forces {from}->{to}: passed");
    }
    private static void Server(PhysicsServer.Backend from, PhysicsServer.Backend to)
    {
        using var a = new Rig(from); var first = a.ServerBall(1, default); var moving = a.ServerBall(2, new(100, 0)); PhysicsServer.BodySetMode(moving, PhysicsServer.BodyMode.Kinematic);
        using var b = new Rig(to); var otherMoving = b.ServerBall(2, new(100, 0)); var other = b.ServerBall(1, default); PhysicsServer.BodySetMode(otherMoving, PhysicsServer.BodyMode.Kinematic);
        var sensor = a.ServerSensor(3); var otherSensor = b.ServerSensor(3); var enters = 0; var exits = 0;
        PhysicsServer.AreaSetMonitorCallback(sensor, (_, _, _, _, _) => { });
        PhysicsServer.AreaSetMonitorCallback(otherSensor, (status, _, _, _, _) => { if (status == PhysicsServer.AreaBodyStatus.Added) enters++; else exits++; });
        a.Step(3); b.Step(); Check(enters == 2, $"Server sensor observes both starting bodies: {enters}"); enters = exits = 0; PhysicsServer.BodyApplyCentralForce(first, new(120, -30)); PhysicsServer.BodySetTransform(moving, new(.3f, new(130, 20)));
        Transfer(a, b); Check(enters == 0 && exits == 0, "Server Area apply is silent"); Near(first, other); a.Step(12); b.Step(12); Near(first, other, .1f, .1f);
        // CPU angular conversion uses approximate trigonometry; this .3-rad target differs by .0015 rad.
        Near(moving, otherMoving, angle: .002f);
        Check(enters == 0 && exits == 1, $"Server Area history continues without duplicate enters: {enters} enters, {exits} exits");
        Console.WriteLine($"Portable server state {from}->{to}: passed");
    }
    private static void SceneMotion(PhysicsServer.Backend from, PhysicsServer.Backend to)
    {
        using var a = new Rig(from); using var b = new Rig(to);
        var (platform, rider, character) = Build(a); var (otherPlatform, otherRider, otherCharacter) = Build(b);
        a.Step(30); for (var i = 0; i < 8; i++) { character.Velocity = new(15, 40); character.MoveAndSlide(); a.Step(); }
        b.Step(2);
        platform.Position += new Vector2(20, -5); character.MoveAndSlide();
        Transfer(a, b);
        Check(otherCharacter.Velocity == character.Velocity && otherCharacter.IsOnFloor() == character.IsOnFloor() &&
            otherCharacter.GetSlideCollisionCount() == character.GetSlideCollisionCount(), "Character floor/slide state transfers");
        Check(otherPlatform.Position.DistanceTo(platform.Position) < .001f, "Pending animatable target transfers");
        // Different contact solvers permit 1% velocity/spin error during this fast kinematic push.
        a.Step(); b.Step(); Near(rider.GetRID(), otherRider.GetRID(), 1, 4, .025f, .6f);
        Check(otherPlatform.Position.DistanceTo(platform.Position) < .05f, "Animatable target consumed once");
        Console.WriteLine($"Portable scene targets/character {from}->{to}: passed");
        static (AnimatableBody, RigidBody, CharacterBody) Build(Rig r)
        {
            r.Ground(); var platform = new AnimatableBody { Name = "Platform", Position = new(0, 40) };
            platform.AddChild(new CollisionShape { Shape = r.Floor }); r.Root.AddChild(platform); r.Map.Bind(2, 1, platform.GetRID());
            var rider = r.Ball(3, new(-50, 20));
            var character = new CharacterBody { Name = "Walker", Position = new(50, 20) };
            character.AddChild(new CollisionShape { Shape = r.Circle }); r.Root.AddChild(character); r.Map.Bind(4, 1, character.GetRID());
            return (platform, rider, character);
        }
    }
    private static void Joints(PhysicsServer.Backend from, PhysicsServer.Backend to, int kind)
    {
        using var a = new Rig(from); using var b = new Rig(to);
        var first = Build(a, false); var second = Build(b, true); a.Step(30); b.Step(3);
        Transfer(a, b); Near(first.GetRID(), second.GetRID());
        using var local = PhysicsServer.SpaceCreateCheckpoint(b.World.Space);
        second.ApplyCentralImpulse(new(6, 2)); b.Step(12); var expected = second.Position;
        local.Restore(); second.ApplyCentralImpulse(new(6, 2)); b.Step(12);
        Check(second.Position.DistanceTo(expected) < .05f, "Local prediction checkpoint replays after portable joint correction");
        Check(float.IsFinite(second.Position.X) && second.Position.Length() < 100, "Imported joint remains bounded");
        Console.WriteLine($"Portable joints {from}->{to}, kind={kind}: passed");
        RigidBody Build(Rig r, bool reverse)
        {
            RigidBody x, y;
            if (reverse) { y = r.Ball(2, new(20, 0)); x = r.Ball(1, new(-20, 0)); }
            else { x = r.Ball(1, new(-20, 0)); y = r.Ball(2, new(20, 0)); }
            x.GravityScale = y.GravityScale = 0; x.Rotation = .3f; y.Rotation = -.4f; r.Step();
            Joint joint = kind switch
            {
                0 => new PinJoint { MotorEnabled = true, MotorTargetVelocity = 1, MotorMaxTorque = 100 },
                1 => new GrooveJoint { Length = 40, InitialOffset = 20 },
                _ => new DampedSpringJoint { Length = 40, Stiffness = 20, Damping = 2 }
            };
            joint.Name = "Link"; joint.NodeA = "../Ball1"; joint.NodeB = "../Ball2";
            r.Root.AddChild(joint); r.Map.Bind(3, 1, joint.GetRID()); return x;
        }
    }
    private static void OneWay(PhysicsServer.Backend from, PhysicsServer.Backend to, int geometry)
    {
        using var a = new Rig(from); using var b = new Rig(to);
        var vertices = new Vector2[16]; for (var i = 0; i < vertices.Length; i++) { var angle = i * MathF.Tau / vertices.Length; vertices[i] = new(300 * MathF.Cos(angle), 40 * MathF.Sin(angle)); }
        using Shape floor = geometry switch
        {
            0 => new RectangleShape { Size = new(600, 80) },
            1 => new ConvexPolygonShape { Points = vertices },
            _ => new ConcavePolygonShape { Segments = [new(-300, 200), new(300, 200), new(-300, 40), new(300, 40)] }
        };
        var body = Build(a); var other = Build(b); body.LinearVelocity = new(0, -120);
        for (var i = 0; i < 20 && body.Position.Y > 137; i++) a.Step();
        Check(body.Position.Y is > 130 and < 140, "Body enters the forbidden side without receiving an impulse");
        body.LinearVelocity = new(0, 90);
        var saved = Transfer(a, b); var bytes = new byte[saved.GetEncodedSize()]; saved.WriteTo(bytes);
        Check(BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(44)) > 0, "Snapshot contains an actual one-way episode");
        var start = other.Position.Y; a.Step(8); b.Step(8);
        Check(body.Position.Y > start + 8 && other.Position.Y > start + 8, "Reversing while embedded preserves the denied episode across correction");
        var corrupt = new byte[bytes.Length + 36]; bytes.CopyTo(corrupt, 0); bytes.AsSpan(bytes.Length - 36).CopyTo(corrupt.AsSpan(bytes.Length));
        BinaryPrimitives.WriteInt32LittleEndian(corrupt.AsSpan(44), BinaryPrimitives.ReadInt32LittleEndian(bytes.AsSpan(44)) + 1);
        var bad = new PhysicsSnapshot(); bad.ReadFrom(corrupt); var position = other.Position;
        Reject<InvalidDataException>(() => b.Map.Apply(bad)); Check(other.Position == position, "Duplicate one-way history rejects before apply");
        Console.WriteLine($"Portable one-way episode {from}->{to}, geometry={geometry}: passed");
        RigidBody Build(Rig r)
        {
            var wall = new StaticBody { Name = "Wall", Position = new(0, 100) }; wall.AddChild(new CollisionShape { Shape = floor, OneWayCollision = true }); r.Root.AddChild(wall); r.Map.Bind(1, 1, wall.GetRID());
            var result = r.Ball(2, new(0, 160)); result.GravityScale = 0; result.LinearDampMode = RigidBody.DampMode.Replace; result.LinearDamp = 0; result.ContinuousCD = CCDMode.CastShape; return result;
        }
    }
    private static void ApplyFailure()
    {
        using var r = new Rig(PhysicsServer.Backend.GPU); var body = r.Ball(1, default); r.Step();
        var snapshot = new PhysicsSnapshot(); r.Map.Capture(snapshot); var bytes = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(bytes);
        // Finite fields can still overflow when the device composes solver and surface velocity.
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(116), float.MaxValue);
        BinaryPrimitives.WriteSingleLittleEndian(bytes.AsSpan(296), float.MaxValue);
        var bad = new PhysicsSnapshot(); bad.ReadFrom(bytes);
        Reject<InvalidOperationException>(() => r.Map.Apply(bad));
        Check(PhysicsServer.Service.GetSceneSpace(r.World.Space).HasBackendFailure, "An apply execution failure poisons the world");
        Reject<AggregateException>(() => r.Tree.PhysicsFrame(1d / 60)); Reject<InvalidOperationException>(() => r.Map.Apply(snapshot));
        r.Dispose(); Check(r.Map.IsDisposed, "Failed world still releases the borrowed map");
    }
    private static void Guards()
    {
        using var a = new Rig(PhysicsServer.Backend.CPU); var body = a.Ball(1, default); a.Step(); var snap = new PhysicsSnapshot(); a.Map.Capture(snap);
        var bytes = new byte[snap.GetEncodedSize()]; snap.WriteTo(bytes); var decoded = new PhysicsSnapshot(); decoded.ReadFrom(bytes); var tick = decoded.Tick;
        for (var length = 0; length < bytes.Length; length++) { var truncated = bytes.AsSpan(0, length).ToArray(); Reject<InvalidDataException>(() => decoded.ReadFrom(truncated)); }
        Check(decoded.Tick == tick, "Malformed input preserves prior snapshot");
        var bad = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(4), 99); Reject<InvalidDataException>(() => decoded.ReadFrom(bad));
        BinaryPrimitives.WriteUInt32LittleEndian(bad.AsSpan(4), 1); BinaryPrimitives.WriteSingleLittleEndian(bad.AsSpan(24), float.NaN); Reject<InvalidDataException>(() => decoded.ReadFrom(bad));
        var foreign = (byte[])bytes.Clone(); BinaryPrimitives.WriteUInt64LittleEndian(foreign.AsSpan(48), 2); decoded.ReadFrom(foreign);
        Reject<InvalidDataException>(() => a.Map.Apply(decoded)); decoded.ReadFrom(bytes);
        var tiny = new PhysicsSnapshot(48); Reject<InvalidDataException>(() => tiny.ReadFrom(bytes)); Reject<InvalidOperationException>(() => a.Map.Capture(tiny));
        body.CollisionMask = 2; var position = body.Position; Reject<InvalidDataException>(() => a.Map.Apply(snap)); Check(body.Position == position, "Schema rejection preserves physical state"); body.CollisionMask = 1;
        a.Map.Unbind(1); a.Map.Bind(1, 2, body.GetRID()); Reject<InvalidDataException>(() => a.Map.Apply(snap)); a.Map.Unbind(1); a.Map.Bind(1, 1, body.GetRID());
        var rejected = false; var thread = new Thread(() => { try { a.Map.Apply(snap); } catch (InvalidOperationException) { rejected = true; } }); thread.Start(); thread.Join(); Check(rejected, "Apply rejects foreign thread");
        var orphan = new PhysicsSnapshotMap(a.World.Space); a.Dispose(); Check(orphan.IsDisposed, "World invalidates borrowed maps"); Reject<ObjectDisposedException>(() => orphan.Apply(snap));
    }
    private static void Warm(PhysicsServer.Backend backend, PhysicsServer.Backend receiver)
    {
        using var source = new Rig(backend); using var target = new Rig(receiver); source.Ground(); target.Ground();
        var count = int.TryParse(Environment.GetEnvironmentVariable("ELECTRON2D_SNAPSHOT_BODIES"), out var requested) ? requested : 16;
        Check(count is >= 16 and <= 1024, "Snapshot benchmark body count is bounded");
        for (var i = 0; i < count; i++)
        {
            var position = new Vector2((i % 16 - 8) * 24, 70 - (i / 16) * 24);
            source.Ball((ulong)i + 2, position); target.Ball((ulong)i + 2, position);
        }
        source.Step(90); target.Step(90); var sent = new PhysicsSnapshot(); var received = new PhysicsSnapshot(); source.Map.Capture(sent); var packet = new byte[sent.GetEncodedSize()];
        var samples = new double[64];
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false); GC.WaitForPendingFinalizers();
        _ = GC.GetAllocatedBytesForCurrentThread(); _ = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 96; i++) Cycle();
        var firstGPU = PhysicsServer.Service.GetSceneSpace(source.World.Space).GPUStore;
        var secondGPU = PhysicsServer.Service.GetSceneSpace(target.World.Space).GPUStore;
        var upload = (firstGPU?.UploadBytes ?? 0) + (secondGPU?.UploadBytes ?? 0);
        var download = (firstGPU?.ReadbackBytes ?? 0) + (secondGPU?.ReadbackBytes ?? 0);
        var wait = (firstGPU?.WaitMS ?? 0) + (secondGPU?.WaitMS ?? 0);
        var submissions = (firstGPU?.SubmissionCount ?? 0) + (secondGPU?.SubmissionCount ?? 0);
        var all = GC.GetTotalAllocatedBytes(true); var own = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < 64; i++) { var start = Stopwatch.GetTimestamp(); Cycle(); samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        var bytes = GC.GetAllocatedBytesForCurrentThread() - own; var total = GC.GetTotalAllocatedBytes(true) - all; Array.Sort(samples);
        Console.WriteLine($"Portable {backend}->{receiver} {count + 1} objects capture/encode/decode/apply: {bytes}/{total} owner/all B; payload {packet.Length} B; p50/p95/p99 {samples[31]:F4}/{samples[60]:F4}/{samples[63]:F4} ms");
        Console.WriteLine($"Portable {backend}->{receiver} per cycle: upload/readback {((firstGPU?.UploadBytes ?? 0) + (secondGPU?.UploadBytes ?? 0) - upload) / 64}/{((firstGPU?.ReadbackBytes ?? 0) + (secondGPU?.ReadbackBytes ?? 0) - download) / 64} B; wait {((firstGPU?.WaitMS ?? 0) + (secondGPU?.WaitMS ?? 0) - wait) / 64:F4} ms; submissions {((firstGPU?.SubmissionCount ?? 0) + (secondGPU?.SubmissionCount ?? 0) - submissions) / 64}.");
        Check(bytes == 0 && total == 0, "Warmed snapshot transport allocates zero bytes");
        void Cycle() { source.Map.Capture(sent); sent.WriteTo(packet); received.ReadFrom(packet); target.Map.Apply(received); }
    }
}
