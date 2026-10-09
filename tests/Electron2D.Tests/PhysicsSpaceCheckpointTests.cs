using System.Diagnostics;
using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsSpaceCheckpointTests
{
    internal static void Run(bool gpu = false)
    {
        var backend = gpu ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU;
        Contacts(backend); ForcesAndTargets(backend); ServerState(backend); PendingWorldSettings(backend); JointReplay(backend); OneWayAndCCD(backend); Guards(backend); Warm(backend);
        if (Environment.GetEnvironmentVariable("ELECTRON2D_SPACE_CHECKPOINT_HEADLESS") == "1") Check(!DisplayServer.IsAvailable && !RenderingServer.IsAvailable, "CPU replay never creates graphics services");
        Console.WriteLine($"{backend} common-world checkpoints: scene/server contacts, Area events, pending forces/targets, joints, guards and warm allocation passed.");
    }
    private sealed class Rig : IDisposable
    {
        internal readonly World World;
        internal readonly SubViewport Root;
        internal readonly SceneTree Tree;
        internal readonly CircleShape Circle = new() { Radius = 10 };
        internal readonly RectangleShape Floor = new() { Size = new(600, 20) };
        internal readonly List<RID> ServerObjects = [];
        internal PhysicsSpace Space => PhysicsServer.Service.GetSceneSpace(World.Space);
        internal Rig(PhysicsServer.Backend backend)
        {
            World = new(backend); Root = new() { World = World, Size = new(600, 400) }; Tree = new(Root);
        }
        internal RigidBody Ball(string name, Vector2 position, Node? parent = null)
        {
            var body = new RigidBody { Name = name, Position = position, CanSleep = false, MaxContactsReported = 8, ContactMonitor = true };
            body.AddChild(new CollisionShape { Shape = Circle }); (parent ?? Root).AddChild(body); return body;
        }
        internal StaticBody Ground()
        {
            var floor = new StaticBody { Name = "Ground", Position = new(0, 100) };
            floor.AddChild(new CollisionShape { Shape = Floor }); Root.AddChild(floor); return floor;
        }
        internal RID ServerBall(Vector2 position)
        {
            var shape = PhysicsServer.CircleShapeCreate(); ServerObjects.Add(shape); PhysicsServer.ShapeSetData(shape, Circle);
            var body = PhysicsServer.BodyCreate(); ServerObjects.Add(body); PhysicsServer.BodyAddShape(body, shape);
            PhysicsServer.BodySetMaxContactsReported(body, 8); PhysicsServer.BodySetTransform(body, new Transform(0, position));
            PhysicsServer.BodySetSpace(body, World.Space); return body;
        }
        internal void Step(int count = 1) { for (var i = 0; i < count; i++) Tree.PhysicsFrame(1d / 60); }
        public void Dispose()
        {
            for (var i = ServerObjects.Count - 1; i >= 0; i--) PhysicsServer.FreeRID(ServerObjects[i]);
            Tree.Dispose(); Root.Dispose(); World.Dispose(); Circle.Dispose(); Floor.Dispose();
        }
    }
    private readonly record struct State(Transform Pose, Vector2 Velocity, float Angular, bool Sleeping);
    private static State Read(RigidBody body) => new(body.GlobalTransform, body.LinearVelocity, body.AngularVelocity, body.Sleeping);
    private static State Read(RID body)
    {
        var state = PhysicsServer.BodyGetDirectState(body)!;
        return new(state.Transform, state.LinearVelocity, state.AngularVelocity, state.Sleeping);
    }
    private static void Near(State expected, State actual, float position = .02f, float velocity = .1f)
    {
        // Whole-world replay allows bounded floating-point solver order differences, never a missing impulse or tick.
        Check(expected.Pose.Origin.DistanceTo(actual.Pose.Origin) <= position && Math.Abs(expected.Pose.Rotation - actual.Pose.Rotation) <= .002f &&
            expected.Velocity.DistanceTo(actual.Velocity) <= velocity && Math.Abs(expected.Angular - actual.Angular) <= .02f && expected.Sleeping == actual.Sleeping,
            $"Restored/replayed motion: expected {expected}, actual {actual}");
    }
    private static void Contacts(PhysicsServer.Backend backend)
    {
        using var r = new Rig(backend); r.Ground(); var body = r.Ball("Ball", new(0, 80)); var server = r.ServerBall(new(60, 80));
        using var region = new RectangleShape { Size = new(200, 200) };
        var area = new Area { Name = "Sensor", Position = new(0, 50), Monitoring = true };
        area.AddChild(new CollisionShape { Shape = region }); r.Root.AddChild(area);
        var entered = 0; var exited = 0; var bodyEntered = 0; var serverEntered = 0; var serverExited = 0;
        area.BodyEntered += _ => entered++; area.BodyExited += _ => exited++; body.BodyEntered += _ => bodyEntered++;
        PhysicsServer.AreaSetMonitorCallback(area.GetRID(), (status, _, _, _, _) => { if (status == PhysicsServer.AreaBodyStatus.Added) serverEntered++; else serverExited++; });
        r.Step(90);
        var view = PhysicsServer.BodyGetDirectState(body.GetRID())!; var serverView = PhysicsServer.BodyGetDirectState(server)!;
        var count = view.GetContactCount(); Check(count > 0 && serverView.GetContactCount() > 0 && area.OverlapsBody(body), "Actual scene/server contacts and overlap exist");
        var impulse = view.GetContactImpulse(0); var normal = view.GetContactLocalNormal(0); var before = Read(body); var serverBefore = Read(server);
        using var saved = r.Space.CreateCheckpoint(); var enterBefore = entered; var bodyEnterBefore = bodyEntered; var serverEnterBefore = serverEntered;
        Advance(); var future = Read(body); var serverFuture = Read(server); var exits = exited; var serverExits = serverExited;
        Check(exits > 0 && serverExits > 0, "Discarded future leaves monitored region");
        var notifications = 0; body.NotifyLocalTransformChanges = true; body.NotifyTransformChanges = true;
        body.LocalTransformChanged += _ => notifications++; body.TransformChanged += _ => notifications++;
        saved.Restore();
        Check(notifications == 0 && entered == enterBefore && bodyEntered == bodyEnterBefore && serverEntered == serverEnterBefore && exited == exits && serverExited == serverExits,
            "Restore itself emits no transform, contact or overlap callbacks");
        Check(body.GetInterpolatedGlobalVisualTransform(.5f).Origin.DistanceTo(before.Pose.Origin) < .001f, "Presentation no longer interpolates the discarded future");
        Check(ReferenceEquals(view, PhysicsServer.BodyGetDirectState(body.GetRID())) && area.OverlapsBody(body), "Restored views retain identity and overlap history");
        Near(before, Read(body), .0001f, .0001f); Near(serverBefore, Read(server), .0001f, .0001f);
        Check(view.GetContactCount() == count && view.GetContactImpulse(0) == impulse && view.GetContactLocalNormal(0) == normal, "Direct contact impulse and normal restore immediately");
        r.Step(); Check(entered == enterBefore && bodyEntered == bodyEnterBefore && serverEntered == serverEnterBefore, "A restored touching interval does not emit duplicate enters");
        saved.Restore(); Advance(); Near(future, Read(body)); Near(serverFuture, Read(server));
        Check(exited == 2 * exits && serverExited == 2 * serverExits && entered == enterBefore && bodyEntered == bodyEnterBefore && serverEntered == serverEnterBefore,
            "Replay reproduces exits once without invented enters from discarded history");
        void Advance()
        {
            body.GlobalPosition = new(400, -50); body.LinearVelocity = new(30, -20);
            PhysicsServer.BodySetTransform(server, new Transform(0, new(460, -50))); r.Step(12);
        }
    }
    private static void ForcesAndTargets(PhysicsServer.Backend backend)
    {
        using var r = new Rig(backend);
        var parent = new Entity { Name = "Parent", Position = new(50, 10) }; r.Root.AddChild(parent);
        var body = r.Ball("Ball", new(0, 0), parent); body.GravityScale = 0; body.CanSleep = true;
        var moving = new AnimatableBody { Name = "Platform", Position = new(-100, 0) };
        moving.AddChild(new CollisionShape { Shape = r.Circle }); r.Root.AddChild(moving);
        var character = new CharacterBody { Name = "Character", Position = new(200, 0), Velocity = new(30, 5) };
        character.AddChild(new CollisionShape { Shape = r.Circle }); r.Root.AddChild(character);
        r.Step(90); Check(body.Sleeping, "Scene body sleeps before checkpoint");
        body.ApplyCentralForce(new(120, 30)); body.ConstantForce = new(6, -3); body.ConstantTorque = 2;
        moving.Position = new(-60, 20); character.MoveAndSlide();
        using var saved = r.Space.CreateCheckpoint(); var start = Read(body); var target = moving.Position; var characterStart = character.Position;
        Advance(); var expected = Read(body); var expectedPlatform = moving.Position; var expectedCharacter = character.Position;
        body.ConstantForce = new(400, 200); body.ConstantTorque = 9; character.Velocity = new(-500, 0); parent.Position = new(150, 30);
        saved.Restore(); Near(start, Read(body), .0001f, .0001f);
        Check(moving.Position == target && character.Position == characterStart && body.ConstantForce == new Vector2(6, -3) && body.ConstantTorque == 2,
            "Pending scene targets, forces and character motion restored under a moved parent");
        Advance(); Near(expected, Read(body));
        Check(moving.Position.DistanceTo(expectedPlatform) < .001f && character.Position.DistanceTo(expectedCharacter) < .001f, "Replay consumes each pending target and force once");
        void Advance() { for (var i = 0; i < 12; i++) { r.Step(); character.MoveAndSlide(); } }
    }
    private static void ServerState(PhysicsServer.Backend backend)
    {
        using var r = new Rig(backend); var body = r.ServerBall(default); var platform = r.ServerBall(new(100, 0)); var surface = r.ServerBall(new(-100, 0));
        PhysicsServer.BodySetMode(platform, PhysicsServer.BodyMode.Kinematic); PhysicsServer.BodySetMode(surface, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodySetLinearVelocity(surface, new(12, 3)); PhysicsServer.BodySetAngularVelocity(surface, .4f);
        r.Step(); PhysicsServer.BodyApplyCentralForce(body, new(80, -30)); PhysicsServer.BodySetConstantForce(body, new(4, -1));
        PhysicsServer.BodySetTransform(platform, new(.3f, new(130, 20))); using var saved = r.Space.CreateCheckpoint();
        r.Step(15); var expected = Read(body); var platformExpected = Read(platform);
        PhysicsServer.BodySetConstantForce(body, new(999, 0)); PhysicsServer.BodySetLinearVelocity(surface, new(-200, 0)); PhysicsServer.BodySetAngularVelocity(surface, 9);
        saved.Restore();
        Check(PhysicsServer.BodyGetLinearVelocity(surface) == new Vector2(12, 3) && Math.Abs(PhysicsServer.BodyGetAngularVelocity(surface) - .4f) < .00001f,
            "Stationary server surface velocities rewind");
        r.Step(15); Near(expected, Read(body)); Near(platformExpected, Read(platform));
    }
    private static void PendingWorldSettings(PhysicsServer.Backend backend)
    {
        using var r = new Rig(backend); var body = r.Ball("Ball", default); r.Step();
        PhysicsServer.AreaSetGravity(r.World.Space, 2400); using var saved = r.Space.CreateCheckpoint();
        r.Step(6); var expected = Read(body); saved.Restore(); r.Step(6); Near(expected, Read(body));
    }
    private static void JointReplay(PhysicsServer.Backend backend)
    {
        using var r = new Rig(backend); var a = r.Ball("A", new(-20, 0)); var b = r.Ball("B", new(20, 0)); a.GravityScale = b.GravityScale = 0;
        var joint = new PinJoint { Name = "Pin", NodeA = "../A", NodeB = "../B", MotorEnabled = true, MotorTargetVelocity = 1, MotorMaxTorque = 100 };
        r.Root.AddChild(joint); r.Step(30); using var saved = r.Space.CreateCheckpoint();
        a.ApplyCentralImpulse(new(8, 2)); r.Step(24); var expectedA = Read(a); var expectedB = Read(b);
        saved.Restore(); a.ApplyCentralImpulse(new(8, 2)); r.Step(24); Near(expectedA, Read(a)); Near(expectedB, Read(b));
        joint.MotorTargetVelocity = 2; Reject<InvalidOperationException>(saved.Restore); Check(!r.Space.HasBackendFailure, "Configuration rejection leaves world usable");
    }
    private static void OneWayAndCCD(PhysicsServer.Backend backend)
    {
        using var r = new Rig(backend); var floor = r.Ground(); ((CollisionShape)floor.GetChild(0)).OneWayCollision = true;
        var body = r.Ball("OneWay", new(0, 80)); r.Step(90); using var saved = r.Space.CreateCheckpoint();
        body.ApplyCentralImpulse(new(20, -100)); r.Step(24); var expected = Read(body);
        saved.Restore(); body.ApplyCentralImpulse(new(20, -100)); r.Step(24); Near(expected, Read(body));
        body.GlobalPosition = new(0, 0); body.LinearVelocity = new(0, 12000); body.ContinuousCD = CCDMode.CastShape; saved.Capture();
        r.Step(); expected = Read(body); Check(body.Position.Y < 95, "Real CCD hit survives the scene path");
        saved.Restore(); r.Step(); Near(expected, Read(body));
        body.PhysicsMaterialOverride = new PhysicsMaterial { Bounce = .4f }; saved.Capture();
        body.PhysicsMaterialOverride.Bounce = .8f; Reject<InvalidOperationException>(saved.Restore); body.PhysicsMaterialOverride.Dispose(); body.PhysicsMaterialOverride = null;
    }
    private static void Guards(PhysicsServer.Backend backend)
    {
        using var r = new Rig(backend); var body = r.Ball("Ball", default); r.Step(); using var saved = r.Space.CreateCheckpoint();
        var captureNodes = r.Root.BeginSceneCapture();
        try { Reject<InvalidOperationException>(saved.Restore); } finally { Node.EndSceneCapture(captureNodes); }
        var future = Read(body); body.CollisionMask = 2; Reject<InvalidOperationException>(saved.Restore); Near(future, Read(body)); body.CollisionMask = 1;
        body.ContinuousCD = CCDMode.CastShape; Reject<InvalidOperationException>(saved.Restore); body.ContinuousCD = CCDMode.Disabled;
        using var material = new PhysicsMaterial(); body.PhysicsMaterialOverride = material; Reject<InvalidOperationException>(saved.Restore); body.PhysicsMaterialOverride = null;
        areaGuard();
        var threadError = Task.Run(() => { try { saved.Restore(); return false; } catch (InvalidOperationException) { return true; } }).Result;
        Check(threadError, "Foreign thread cannot restore");
        var callbackRejected = false;
        PhysicsServer.BodySetStateSyncCallback(body.GetRID(), _ => { try { r.Space.CreateCheckpoint(); } catch (InvalidOperationException) { callbackRejected = true; } });
        r.Step(); Check(callbackRejected, "Capture from body callback is rejected"); PhysicsServer.BodySetStateSyncCallback(body.GetRID(), null);
        saved.Capture(); r.Root.RemoveChild(body); r.Root.AddChild(body); Reject<InvalidOperationException>(saved.Restore);
        saved.Capture(); saved.Dispose(); Reject<ObjectDisposedException>(saved.Restore);
        var orphan = r.Space.CreateCheckpoint(); r.Tree.Dispose(); r.Root.Dispose(); r.World.Dispose(); Reject<ObjectDisposedException>(orphan.Restore); orphan.Dispose();
        void areaGuard()
        {
            var sensor = new Area { Name = "Area" }; r.Root.AddChild(sensor); saved.Capture(); sensor.Monitoring = !sensor.Monitoring;
            Reject<InvalidOperationException>(saved.Restore); r.Root.RemoveChild(sensor); sensor.Dispose(); saved.Capture();
        }
    }
    private static void Warm(PhysicsServer.Backend backend)
    {
        using var r = new Rig(backend); r.Ground();
        for (var i = 0; i < 64; i++) r.Ball("Ball" + i, new((i % 16) * 24 - 180, 70 - (i / 16) * 22));
        r.Step(90); var initial = GC.GetAllocatedBytesForCurrentThread(); using var saved = r.Space.CreateCheckpoint(); initial = GC.GetAllocatedBytesForCurrentThread() - initial;
        var samples = new double[64]; var gpu = r.Space.GPUStore;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
        for (var i = 0; i < 64; i++) { saved.Capture(); saved.Restore(); }
        var uploads = gpu?.UploadBytes ?? 0; var readbacks = gpu?.ReadbackBytes ?? 0; var copies = gpu?.DeviceCopyBytes ?? 0; var wait = gpu?.WaitMS ?? 0;
        var total = GC.GetTotalAllocatedBytes(true); var own = GC.GetAllocatedBytesForCurrentThread(); var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < 64; i++) { var mark = Stopwatch.GetTimestamp(); saved.Capture(); saved.Restore(); samples[i] = Stopwatch.GetElapsedTime(mark).TotalMilliseconds; }
        var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds / 64; var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - own; var allBytes = GC.GetTotalAllocatedBytes(true) - total;
        Array.Sort(samples);
        Console.WriteLine($"{backend} common checkpoint 64 bodies capture+restore: mean {elapsed:F4} ms, p50/p95/p99 {samples[31]:F4}/{samples[60]:F4}/{samples[63]:F4} ms, owner/all {ownerBytes}/{allBytes} B over 64 samples; initial managed allocation {initial} B.");
        Console.WriteLine($"{backend} common checkpoint transfers over 64 pairs: upload/readback/device-copy {(gpu?.UploadBytes ?? 0) - uploads}/{(gpu?.ReadbackBytes ?? 0) - readbacks}/{(gpu?.DeviceCopyBytes ?? 0) - copies} B, wait {(gpu?.WaitMS ?? 0) - wait:F4} ms, device capacity {saved.DeviceCapacityBytes} B.");
        Check(gpu is null || gpu.ReadbackBytes == readbacks, "Warmed common snapshots need no extra body-state readback");
        Check(ownerBytes == 0 && allBytes == 0, "Warm common-world capture/restore allocates zero managed bytes");
    }
}
