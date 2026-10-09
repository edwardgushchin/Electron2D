using System.Diagnostics;
using Electron2D;

internal static class PhysicsGPUSpaceTests
{
    internal static void Run()
    {
        if (Environment.GetEnvironmentVariable("ELECTRON2D_GPU_SPACE_NO_DEVICE") == "1") { VerifyUnavailable(); return; }
        Reject<ArgumentOutOfRangeException>(() => PhysicsServer.SpaceCreate((PhysicsServer.Backend)99));
        Reject<ArgumentOutOfRangeException>(() => new World((PhysicsServer.Backend)99));
        VerifyServerWorld(false); VerifyServerWorld(true);
        VerifySceneWorld(false); VerifySceneWorld(true);
        VerifyJoints(false); VerifyJoints(true);
        foreach (var count in new[] { 1024, 4096 }) { MeasureWorld(false, count); MeasureWorld(true, count); }
        VerifyGPUFailure();
        VerifyUnavailableProcess();
    }
    private static void VerifyGPUFailure()
    {
        var space = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU, allowCPUFallback: true);
        var body = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.BodySetSpace(body, space);
            Reject<AggregateException>(() => PhysicsServer.SpaceStep(space, float.MaxValue));
            Check(PhysicsServer.Service.GetSceneSpace(space).HasBackendFailure && PhysicsServer.SpaceGetBackend(space) == PhysicsServer.Backend.GPU,
                "Started GPU failure remains failed GPU even when startup fallback was allowed");
            Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(space, 1d / 60));
            Console.WriteLine("GPU step failure is terminal without CPU replay; failed bodies and world release normally.");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
    }
    private static void VerifyUnavailableProcess()
    {
        var start = new ProcessStartInfo(Environment.ProcessPath!) { RedirectStandardOutput = true, RedirectStandardError = true, UseShellExecute = false };
        start.ArgumentList.Add(typeof(PhysicsGPUSpaceTests).Assembly.Location);
        start.Environment["ELECTRON2D_TEST_GPU_SPACE"] = "1";
        start.Environment["ELECTRON2D_GPU_SPACE_NO_DEVICE"] = "1";
        start.Environment["SDL_GPU_DRIVER"] = "electron2d-no-such-driver";
        start.Environment["SDL_VIDEODRIVER"] = "dummy";
        start.Environment["XDG_SESSION_TYPE"] = "unspecified";
        start.Environment.Remove("DISPLAY"); start.Environment.Remove("WAYLAND_DISPLAY");
        using var child = Process.Start(start)!;
        var stdout = child.StandardOutput.ReadToEndAsync(); var stderr = child.StandardError.ReadToEndAsync();
        if (!child.WaitForExit(30_000)) { child.Kill(entireProcessTree: true); throw new InvalidOperationException("No-device physics child timed out."); }
        Check(child.ExitCode == 0, stdout.GetAwaiter().GetResult() + stderr.GetAwaiter().GetResult());
        Console.Write(stdout.GetAwaiter().GetResult());
    }
    private static void VerifyUnavailable()
    {
        VerifyServerWorld(false);
        Reject<InvalidOperationException>(() => PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU));
        var fallback = PhysicsServer.SpaceCreate(PhysicsServer.Backend.GPU, allowCPUFallback: true);
        try
        {
            Check(PhysicsServer.SpaceGetRequestedBackend(fallback) == PhysicsServer.Backend.GPU &&
                PhysicsServer.SpaceGetBackend(fallback) == PhysicsServer.Backend.CPU &&
                !string.IsNullOrEmpty(PhysicsServer.SpaceGetBackendFallbackReason(fallback)), "Observable startup fallback");
            PhysicsServer.SpaceSetActive(fallback, true); PhysicsServer.SpaceStep(fallback, 1d / 60);
        }
        finally { PhysicsServer.FreeRID(fallback); }
        using var world = new World(PhysicsServer.Backend.GPU, allowCPUFallback: true);
        Check(world.RequestedPhysicsBackend == PhysicsServer.Backend.GPU && world.PhysicsBackend == PhysicsServer.Backend.CPU &&
            world.PhysicsFallbackReason is not null, "Lazy world startup fallback");
        Console.WriteLine("CPU world without graphics, explicit GPU failure and observable startup fallback passed in separate process.");
    }
    private static Transform Pose(float x, float y) => new(0, Vector2.One, 0, new(x, y));
    private static void MeasureWorld(bool gpu, int count)
    {
        const int warmup = 768, samples = 128;
        var columns = (int)MathF.Sqrt(count);
        var space = PhysicsServer.SpaceCreate(gpu ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU);
        var bodies = new RID[count]; var floor = PhysicsServer.BodyCreate();
        using var circle = new CircleShape { Radius = 2 };
        using var floorShape = new RectangleShape { Size = new(MathF.Max(240, columns * 4.2f + 100), 10) };
        var times = new double[samples];
        try
        {
            PhysicsServer.SpaceSetActive(space, true);
            PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetTransform(floor, Pose(0, 100));
            PhysicsServer.BodyAddShape(floor, floorShape.GetRID()); PhysicsServer.BodySetSpace(floor, space);
            for (var i = 0; i < count; i++)
            {
                var body = bodies[i] = PhysicsServer.BodyCreate();
                PhysicsServer.BodySetCanSleep(body, false);
                PhysicsServer.BodySetTransform(body, Pose((i % columns - (columns - 1) * .5f) * 4.2f, -i / columns * 4.2f));
                PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetSpace(body, space);
            }
            for (var i = 0; i < warmup; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            var store = PhysicsServer.Service.GetSceneSpace(space).GPUStore;
            var up = store?.UploadBytes ?? 0; var down = store?.ReadbackBytes ?? 0; var wait = store?.WaitMS ?? 0;
            var uniforms = store?.UniformBytes ?? 0;
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread(); var totalBytes = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < samples; i++)
            {
                var start = Stopwatch.GetTimestamp(); PhysicsServer.SpaceStep(space, 1d / 60);
                times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
            }
            totalBytes = GC.GetTotalAllocatedBytes(true) - totalBytes; ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBytes;
            Check(ownerBytes == 0 && totalBytes == 0, $"Warm whole-world allocation {ownerBytes}/{totalBytes} B");
            Array.Sort(times);
            Console.WriteLine($"Public {(gpu ? "GPU" : "CPU")} whole step: {count} active circles + floor, 4 substeps, {warmup} warmup/{samples} samples; p50/p95/p99={times[64]:F4}/{times[121]:F4}/{times[126]:F4} ms; {ownerBytes}/{totalBytes} owner/all-thread B; GPU up/down={((store?.UploadBytes ?? 0) - up) / samples}/{((store?.ReadbackBytes ?? 0) - down) / samples} B, uniforms={((store?.UniformBytes ?? 0) - uniforms) / samples} B, wait={((store?.WaitMS ?? 0) - wait) / samples:F4} ms.");
            foreach (var body in bodies)
                Check(PhysicsServer.BodyGetTransform(body).IsFinite() && PhysicsServer.BodyGetLinearVelocity(body).IsFinite(), "Finite complete population");
        }
        finally
        {
            foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body);
            PhysicsServer.FreeRID(floor); PhysicsServer.FreeRID(space);
        }
    }
    private static void VerifySceneWorld(bool gpu)
    {
        using var world = new World(gpu ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU);
        using var root = new SubViewport { World = world, Size = new(200, 200) };
        using var circle = new CircleShape { Radius = 5 };
        using var rectangle = new RectangleShape { Size = new(200, 10) };
        var body = new RigidBody { Name = "Body", ContactMonitor = true, MaxContactsReported = 8 };
        var floor = new StaticBody { Name = "Floor", Position = new(0, 50) };
        var area = new Area { Name = "Area", Position = new(0, 15) };
        using var areaShape = new RectangleShape { Size = new(100, 100) };
        body.AddChild(new CollisionShape { Shape = circle }); floor.AddChild(new CollisionShape { Shape = rectangle });
        area.AddChild(new CollisionShape { Shape = areaShape }); root.AddChild(body); root.AddChild(floor); root.AddChild(area);
        var contacts = 0; var enters = 0;
        body.BodyEntered += other => { if (other == floor) contacts++; };
        area.BodyEntered += other => { if (other == body) enters++; };
        using var tree = new SceneTree(root);
        area.AudioBusOverride = true; area.AudioBusName = "Physics test";
        Check(world.Runtime.Space.FindAudioBusOverride(new(0, 15), uint.MaxValue) == "Physics test" &&
            world.Runtime.Space.FindAudioBusOverride(new(200, 15), uint.MaxValue) is null, "Area audio uses selected geometry");
        PhysicsServer.AreaSetGravity(world.Space, 100); PhysicsServer.AreaSetLinearDamp(world.Space, 0);
        for (var i = 0; i < 180; i++) tree.PhysicsFrame(1d / 60);
        Check(body.Position.Y is > 39 and < 42 && contacts == 1 && enters == 1, "Scene pose, contact and Area transitions");
        body.Sleeping = true;
        PhysicsServer.BodySetTransform(floor.GetRID(), Pose(300, 50));
        Check(!body.Sleeping, "Static support movement wakes its touching body before another tick");
        body.Sleeping = true;
        Check(body.Sleeping, "An explicit sleep after the support edit retains caller ordering");
        PhysicsServer.BodySetTransform(floor.GetRID(), Pose(0, 50));
        using var collision = new KinematicCollision();
        Check(body.TestMove(Pose(30, 0), new(0, 100), collision) && collision.GetColliderRID() == floor.GetRID(), "Shared body motion query");
        using var ray = new RayCast { TargetPosition = new(0, 100), ExcludeParent = false };
        using var cast = new ShapeCast { Shape = circle, Name = "Cast" };
        root.AddChild(cast);
        root.AddChild(ray); ray.ForceRaycastUpdate();
        Check(ray.IsColliding() && ReferenceEquals(ray.GetCollider(), body), "Scene ray cache reports the physical object");
        root.RemoveChild(body); root.AddChild(body); tree.PhysicsFrame(1d / 60);
        Check(body.GetWorld()!.Space == world.Space, "Scene reentry retains selected physics space");
        body.LockRotation = true; body.FreezeMode = RigidFreezeMode.Kinematic; body.Freeze = true;
        body.Position = new(200, 0); body.Rotation = .4f; tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.Rotation - .4f) < .001f, "Frozen kinematic target remains rotatable with authored rotation lock");
        body.Freeze = false; body.AngularVelocity = 2; tree.PhysicsFrame(1d / 60);
        Check(MathF.Abs(body.AngularVelocity) < .001f, "Unfreezing restores dynamic rotation lock");
        cast.Dispose(); ray.Dispose();
        Reject<ObjectDisposedException>(() => cast.SetInternalProcessing(false, false));
        Reject<ObjectDisposedException>(() => ray.SetInternalProcessing(true, true));
        Console.WriteLine($"Shared {(gpu ? "resident GPU" : "CPU")} scene passed: transforms, contact/Area events, ray/motion queries and reentry.");
    }

    private static void VerifyJoints(bool gpu)
    {
        var space = PhysicsServer.SpaceCreate(gpu ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU);
        var a = PhysicsServer.BodyCreate(); var b = PhysicsServer.BodyCreate(); var joint = PhysicsServer.JointCreate();
        using var circle = new CircleShape { Radius = 3 };
        try
        {
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.AreaSetGravity(space, 0);
            PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            PhysicsServer.BodySetMode(a, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodyAddShape(a, circle.GetRID()); PhysicsServer.BodyAddShape(b, circle.GetRID());
            PhysicsServer.BodySetSpace(a, space); PhysicsServer.BodySetSpace(b, space);
            PhysicsServer.JointMakePin(joint, default, a, b);
            PhysicsServer.PinJointSetMotorEnabled(joint, true); PhysicsServer.PinJointSetMotorTargetVelocity(joint, 2);
            for (var i = 0; i < 30; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            Check(PhysicsServer.BodyGetTransform(b).Origin.Length() < .1f && MathF.Abs(PhysicsServer.BodyGetAngularVelocity(b)) > 1, "Pin anchor and motor");
            PhysicsServer.PinJointSetMotorMaxTorque(joint, float.MaxValue);
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(float.IsFinite(PhysicsServer.BodyGetAngularVelocity(b)), "Large finite motor cap does not overflow unit conversion");
            PhysicsServer.JointMakeGroove(joint, new(0, 0), new(0, 30), default, a, b);
            PhysicsServer.BodySetLinearVelocity(b, new(20, 20));
            for (var i = 0; i < 60; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            var pose = PhysicsServer.BodyGetTransform(b).Origin;
            Check(MathF.Abs(pose.X) < .5f && pose.Y is > 1 and < 31, "Groove constrains transverse movement");
            PhysicsServer.JointMakeDampedSpring(joint, default, pose, a, b);
            PhysicsServer.DampedSpringJointSetRestLength(joint, 5);
            PhysicsServer.DampedSpringJointSetStiffness(joint, 20);
            PhysicsServer.DampedSpringJointSetDamping(joint, 5);
            PhysicsServer.BodySetLinearVelocity(b, default);
            for (var i = 0; i < 120; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            Check(PhysicsServer.BodyGetTransform(b).Origin.Length() < pose.Length() - 1, "Spring contracts toward authored rest length");
            PhysicsServer.BodySetSpace(b, default); PhysicsServer.BodySetSpace(b, space);
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.DampedSpring, "Joint survives body reattachment");
            Console.WriteLine($"Shared {(gpu ? "resident GPU" : "CPU")} joints passed: pin/motor, groove, spring and reattachment.");
        }
        finally { PhysicsServer.FreeRID(joint); PhysicsServer.FreeRID(a); PhysicsServer.FreeRID(b); PhysicsServer.FreeRID(space); }
    }
    private static void VerifyServerWorld(bool gpu)
    {
        var space = PhysicsServer.SpaceCreate(gpu ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU);
        using var circle = new CircleShape { Radius = 5 };
        using var rectangle = new RectangleShape { Size = new(200, 10) };
        using var areaShape = new RectangleShape { Size = new(100, 100) };
        using var owner = new Node();
        var body = PhysicsServer.BodyCreate(); var floor = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        try
        {
            Check(PhysicsServer.SpaceGetBackend(space) == (gpu ? PhysicsServer.Backend.GPU : PhysicsServer.Backend.CPU) &&
                PhysicsServer.SpaceGetRequestedBackend(space) == PhysicsServer.SpaceGetBackend(space) && PhysicsServer.SpaceGetBackendFallbackReason(space) is null,
                "Requested and actual implementation agree without fallback");
            if (gpu) Reject<InvalidOperationException>(() => _ = PhysicsServer.Service.GetSceneSpace(space).WorldID);
            PhysicsServer.SpaceSetActive(space, true);
            PhysicsServer.AreaSetGravity(space, 100); PhysicsServer.AreaSetGravityVector(space, Vector2.Down);
            PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Static);
            PhysicsServer.BodySetTransform(floor, Pose(0, 50)); PhysicsServer.BodyAddShape(floor, rectangle.GetRID());
            PhysicsServer.BodySetSpace(floor, space);
            PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetMass(body, 2);
            PhysicsServer.BodySetMaxContactsReported(body, 8); PhysicsServer.BodyAttachObject(body, owner);
            PhysicsServer.BodySetSpace(body, space);
            PhysicsServer.AreaAddShape(area, areaShape.GetRID()); PhysicsServer.AreaSetSpace(area, space);
            var enters = 0; var exits = 0; var calls = 0;
            PhysicsServer.AreaSetMonitorCallback(area, (status, id, instance, other, local) =>
            {
                if (id != body) return;
                Check(instance == owner.InstanceID && other == 0 && local == 0, "Area retains public identity and shape slots");
                if (status == PhysicsServer.AreaBodyStatus.Added) enters++; else exits++;
            });
            PhysicsServer.BodySetStateSyncCallback(body, state => { calls++; Check(state.Step == 1f / 60, "Fixed callback duration"); });
            var view = PhysicsServer.BodyGetDirectState(body)!;
            Check(MathF.Abs(view.InverseMass - .5f) < 1e-6f, "Shared public mass units");
            PhysicsServer.BodyApplyCentralImpulse(body, new(4, 0));
            Check(MathF.Abs(view.LinearVelocity.X - 2) < 1e-5f, "Immediate impulse state");
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(calls == 1 && enters == 1 && exits == 0, "Post-step callback and overlap entry");
            var direct = PhysicsServer.SpaceGetDirectState(space);
            using var point = new PhysicsPointQueryParameters { Position = view.Transform.Origin, CollideWithAreas = false };
            var points = direct.IntersectPoint(point);
            Check(points.Length == 1 && points[0].ColliderRID == body && points[0].ColliderObject == owner, "Direct point query uses physical RID and assigned object");
            using var ray = PhysicsRayQueryParameters.Create(new(0, -10), new(0, 100));
            Check(direct.IntersectRay(ray)?.ColliderRID == body, "Nearest ray query");
            using var shape = new PhysicsShapeQueryParameters { Shape = circle, Transform = view.Transform };
            Check(direct.IntersectShape(shape).Any(hit => hit.ColliderRID == body), "Shape intersection query");
            Check(direct.CollideShape(shape).Length > 0 && direct.GetRestInfo(shape)?.ColliderRID == body, "Contact and rest queries");
            shape.Transform = Pose(30, 0); shape.Motion = new(0, 100);
            var fractions = direct.CastMotion(shape);
            Check(fractions.SafeFraction > .3f && fractions.SafeFraction < .5f, "Shape cast reaches floor in scene units");
            for (var i = 0; i < 180; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            Check(view.Transform.Origin.Y is > 39 and < 42 && MathF.Abs(view.LinearVelocity.Y) < 2, "Body rests on floor with solver tolerance below two scene units");
            Check(view.GetContactCount() > 0 && view.GetContactCollider(0) == floor, "Solved contacts expose physical RID");
            PhysicsServer.BodyAddCollisionException(body, floor);
            for (var i = 0; i < 90; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            Check(view.Transform.Origin.Y > 100 && view.GetContactCount() == 0, "Live exception releases an existing contact");
            PhysicsServer.BodyRemoveCollisionException(body, floor);
            PhysicsServer.BodySetTransform(body, Pose(0, 0)); PhysicsServer.BodySetLinearVelocity(body, default);
            for (var i = 0; i < 180; i++) PhysicsServer.SpaceStep(space, 1d / 60);
            Check(view.Transform.Origin.Y is > 39 and < 42 && view.GetContactCount() > 0, "Removing exception restores collision response");
            var previousExits = exits;
            PhysicsServer.BodySetTransform(body, Pose(400, 0));
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(exits == previousExits + 1, "Area exit after teleport");
            PhysicsServer.BodySetSpace(body, default);
            Reject<ObjectDisposedException>(() => _ = view.LinearVelocity);
            PhysicsServer.BodySetSpace(body, space);
            Check(!ReferenceEquals(view, PhysicsServer.BodyGetDirectState(body)), "Reattachment creates fresh direct view");
            Console.WriteLine($"Shared {(gpu ? "resident GPU" : "CPU")} space passed: no second solver, body state, force, collision, Area, callbacks, queries and reattachment.");
        }
        finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(floor); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(space); }
    }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
}
