using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;
using Kind = Electron2D.PhysicsServer.JointType;

internal static class GPUPhysicsMassStoreTests
{
    internal static void Run()
    {
        VerifyProfilesAndMotion(); VerifyGeometry(); VerifyConstraintCenters(); VerifyRevisionsAndOrder(); VerifyWarmChanges();
        Console.WriteLine("Resident mass: normalized geometry, custom center/inertia, motion/constraint centers, edit order and warmed changes passed.");
    }
    private static Store.BodyHandle Body(Store store, Vector2 position = default, float mass = 1, Mode mode = Mode.Rigid, float angular = 0) =>
        store.Add(new(mode, position, 0, Vector2.Zero, angular, Mass: mass));
    private static void VerifyProfilesAndMotion()
    {
        using var store = new Store(); using var circle = new CircleShape { Radius = 10 };
        var body = Body(store, mass: 2);
        store.AddShape(body, circle, new Transform(0, new(20, 0)));
        var automatic = store.GetMassProperties(body);
        Near(automatic.Center, new(20, 0), 0.001f, "Automatic offset center"); Near(automatic.Inertia, 100, 0.001f, "Circle inertia in scene units");
        Check(store.GetMassProfile(body).Center is null && store.GetMassProfile(body).Inertia == 0, "Resolved values do not overwrite automatic authoring.");
        store.SetMassProfile(body, new(2, 0, new(5, 0)));
        Near(store.GetMassProperties(body).Inertia, 550, 0.001f, "Custom center parallel-axis moment");
        var state = new Store.Snapshot[1]; store.Read([body], state);
        Check(state[0].Position == Vector2.Zero && state[0].Velocity == System.Numerics.Vector4.Zero, "Profile change preserves origin and velocity.");
        store.SetVelocity(body, Vector2.Zero, 1);
        for (var i = 0; i < 120; i++) store.Step(1f / 120, Vector2.Zero);
        store.Read([body], state);
        var center = state[0].Position + new Vector2(5, 0).Rotated(state[0].Rotation);
        Near(center, new(5, 0), 0.001f, "Integration rotates the origin around the fixed center of mass");
        Near(state[0].Rotation, 1, 0.001f, "Angular motion is retained");
        var pose = state[0].Pose; var velocity = state[0].Velocity;
        store.SetMassProfile(body, new(4, 200)); store.Read([body], state);
        Check(state[0].Pose == pose && state[0].Velocity == velocity, "Auto/custom edits preserve solved pose and configured velocity.");
        circle.Radius = 5;
        Near(store.GetMassProperties(body).Inertia, 200, 0, "Explicit inertia survives geometry/mass changes");
        store.SetMassProfile(body, new(4)); Near(store.GetMassProperties(body).Inertia, 50, 0.001f, "Reset restores automatic moment");
        store.SetVelocity(body, Vector2.Zero, 0); store.SetConstantForce(body, new(8, 0), 50);
        store.Step(0.1f, Vector2.Zero); store.Read([body], state);
        Near(state[0].Velocity.X, 0.2f, 0.00001f, "Device force uses updated mass");
        Near(state[0].Velocity.Z, 0.1f, 0.00001f, "Device torque uses updated automatic inertia");
        var configuration = store.GetMassProfile(body);
        Reject<ArgumentOutOfRangeException>(() => store.SetMassProfile(body, new(0)));
        Reject<ArgumentOutOfRangeException>(() => store.SetMassProfile(body, new(2, float.Epsilon)));
        Reject<ArgumentOutOfRangeException>(() => store.SetMassProfile(body, new(2, 10, new(float.MaxValue, 0))));
        Check(store.GetMassProfile(body) == configuration, "Rejected profile keeps authoring and usable state.");
        store.Read([body], state);
        foreach (var mode in new[] { Mode.Static, Mode.Kinematic, Mode.RigidLinear })
        {
            var other = Body(store, new(100, 100), mode: mode);
            store.AddShape(other, circle, new Transform(0, new(3, 4)));
            Near(store.GetMassProperties(other).Center, new(3, 4), 0.0001f, "Nondynamic and locked roles retain resolved geometry");
            store.ApplyImpulse(other, new(10, 0), 100); store.Step(0.01f, Vector2.Zero); store.Read([other], state);
            Near(state[0].Velocity.Z, 0, 0, "Nondynamic and rotation-locked bodies reject torque response");
            Near(state[0].Velocity.X, mode == Mode.RigidLinear ? 10 : 0, 0.00001f, "Mass response follows the body role");
        }
    }
    private static void VerifyGeometry()
    {
        using var store = new Store();
        using var circle = new CircleShape { Radius = 3 };
        using var capsule = new CapsuleShape { Radius = 2, Height = 14 };
        using var shortCapsule = new CapsuleShape { Radius = 2, Height = 4.25f };
        using var rodCapsule = new CapsuleShape { Radius = 0, Height = 12 };
        using var segment = new SegmentShape { A = new(-30, 0), B = new(30, 0) };
        using var point = new SegmentShape { A = new(10, 0), B = new(10.1f, 0) };
        using var rectangle = new RectangleShape { Size = new(8, 12) };
        using var polygon = new ConvexPolygonShape();
        var vertices = new Vector2[14];
        for (var i = 0; i < vertices.Length; i++) vertices[i] = new Vector2(15 * MathF.Cos(i * Mathf.Tau / vertices.Length), 10 * MathF.Sin(i * Mathf.Tau / vertices.Length)) + new Vector2(7, -2);
        polygon.Points = vertices;
        using var hollow = new ConcavePolygonShape { Segments = [new(-10, 0), new(10, 0), new(10, 0), new(10, 30)] };
        using var ray = new SeparationRayShape { Length = 30 };
        Shape[] shapes = [circle, capsule, shortCapsule, rodCapsule, segment, point, rectangle, polygon, hollow, ray];
        var space = PhysicsServer.SpaceCreate();
        try
        {
            foreach (var shape in shapes)
            {
                var pose = new Transform(0.3f, new(8, 11)); var body = Body(store, mass: 3);
                store.AddShape(body, shape, pose);
                var server = PhysicsServer.BodyCreate();
                try
                {
                    PhysicsServer.BodySetMass(server, 3); PhysicsServer.BodyAddShape(server, shape.GetRID(), pose);
                    PhysicsServer.BodyApplyCentralImpulse(server, new(1, 0));
                    var detachedCenter = PhysicsServer.BodyGetCenterOfMass(server); var detachedInertia = PhysicsServer.BodyGetInertia(server);
                    PhysicsServer.BodySetSpace(server, space);
                    var actual = store.GetMassProperties(body);
                    Near(actual.Center, PhysicsServer.BodyGetCenterOfMass(server), 0.005f, "Public CPU and resident geometry centers");
                    var expected = PhysicsServer.BodyGetInertia(server);
                    Near(PhysicsServer.BodyGetCenterOfMass(server), detachedCenter, 0.001f, "Detached and attached CPU geometry use the same rotation");
                    Near(expected, detachedInertia, MathF.Max(0.005f, expected * 0.0005f), "Detached and attached CPU geometry retain the same moment");
                    Near(actual.Inertia, expected, MathF.Max(0.005f, expected * 0.0005f), "Public CPU and resident area/rod moments");
                    store.SetMassProfile(body, new(3, 0, new(2, 4))); PhysicsServer.BodySetCenterOfMass(server, new(2, 4));
                    expected = PhysicsServer.BodyGetInertia(server);
                    Near(store.GetMassProperties(body).Inertia, expected, MathF.Max(0.005f, expected * 0.0005f), "Common custom-center geometry profile");
                }
                finally { PhysicsServer.FreeRID(server); }
                store.Remove(body);
            }
        }
        finally { PhysicsServer.FreeRID(space); }
        var compound = Body(store, mass: 5);
        store.AddShape(compound, circle, new Transform(0, new(-10, 0)));
        store.AddShape(compound, circle, new Transform(0, new(10, 0)));
        store.AddShape(compound, segment, new Transform(0, new(100, 0)));
        store.AddShape(compound, rectangle, new Transform(0, new(1000, 0)), sensor: true);
        store.AddShape(compound, ray, new Transform(0, new(-1000, 0)));
        var result = store.GetMassProperties(compound);
        Near(result.Center, Vector2.Zero, 0.001f, "Area weighting ignores rods, sensors and rays when solid area exists");
        Near(result.Inertia, 5 * (4.5f + 100), 0.01f, "Compound normalized parallel-axis inertia");
    }
    private static void VerifyConstraintCenters()
    {
        using var store = new Store(); using var circle = new CircleShape { Radius = 1 };
        var offset = new Vector2(0, 10); var a = Body(store, -offset); var b = Body(store, new(2, 0), mode: Mode.Static);
        store.AddShape(a, circle, new Transform(0, offset)); store.AddShape(b, circle);
        store.SetVelocity(a, new(10, 0), 0); store.SolveConstraints(0.01f, margin: 0, correctionFactor: 0);
        var state = new Store.Snapshot[1]; store.Read([a], state);
        Near(state[0].Velocity.X, 0, 0.001f, "Centered collision stops translation");
        Near(state[0].Velocity.Z, 0, 0.001f, "Contact lever uses computed center instead of body origin");
        store.SetMassProfile(a, new(2)); store.SolveConstraints(0.01f, margin: 0, correctionFactor: 0);
        Check(store.WarmStartedPointCount == 0, "A mass-only edit invalidates contact impulse history.");
        store.SetMassProfile(a, new(1));
        store.Remove(b); store.SetVelocity(a, Vector2.Zero, 1);
        var pin = store.AddJoint(new(Kind.Pin, a, default, new Transform(0, offset), Transform.Identity));
        for (var i = 0; i < 120; i++) store.Simulate(1f / 120, new(0, 980), iterations: 32);
        store.Read([a], state);
        Near(state[0].Position + offset.Rotated(state[0].Rotation), Vector2.Zero, 0.02f, "Pin retains an anchor at the rotating center");
        Near(state[0].Velocity.Z, 1, 0.001f, "Pin force at the center does not create torque");
        store.RemoveJoint(pin); store.SetPose(a, -offset, 0); store.SetVelocity(a, Vector2.Zero, 0);
        b = Body(store, new(12, -10)); store.AddShape(b, circle, new Transform(0, offset));
        store.AddJoint(new(Kind.DampedSpring, a, b, new Transform(0, offset), new Transform(0, offset)) { RestLength = 10, Stiffness = 20, Damping = 0 });
        store.SolveConstraints(0.01f); store.Read([a], state);
        Near(state[0].Velocity.X, 0.4f, 0.001f, "Centered spring impulse");
        Near(state[0].Velocity.Z, 0, 0.001f, "Spring lever uses computed center");
    }
    private static void VerifyRevisionsAndOrder()
    {
        using var store = new Store(); using var circle = new CircleShape { Radius = 2 };
        var body = Body(store, mass: 2); var shape = store.AddShape(body, circle, new Transform(0, new(4, 0)));
        store.ApplyImpulse(body, new(4, 0), 4);
        circle.Radius = 4;
        store.SetMassProfile(body, new(4));
        store.ApplyImpulse(body, new(4, 0), 32);
        var state = new Store.Snapshot[1]; store.Read([body], state);
        Near(state[0].Velocity.X, 3, 0.0001f, "Impulses capture mass on each side of a profile edit");
        Near(state[0].Velocity.Z, 2, 0.0001f, "Impulses capture automatic inertia on each side of a geometry edit");
        store.SetShapePose(shape, new Transform(0, new(-5, 7)));
        Near(store.GetMassProperties(body).Center, new(-5, 7), 0.001f, "Local shape edits refresh mass geometry");
        store.SetShapeFilter(shape, 1, uint.MaxValue, true);
        Check(store.GetMassProperties(body).Inertia == 0 && store.GetMassProperties(body).Center == Vector2.Zero, "Sensor conversion removes mass contribution.");
        store.SetShapeFilter(shape, 1, uint.MaxValue, false);
        Near(store.GetMassProperties(body).Inertia, 32, 0.001f, "Solid conversion restores automatic inertia");
        Action<Resource> fail = _ => throw new InvalidOperationException("observer"); circle.Changed += fail;
        Reject<InvalidOperationException>(() => circle.Radius = 5); circle.Changed -= fail;
        Near(store.GetMassProperties(body).Inertia, 50, 0.001f, "Revision polling survives a throwing observer");
        circle.Radius = 1e20f;
        Reject<ArgumentOutOfRangeException>(() => store.Step(0.01f, Vector2.Zero));
        circle.Radius = 5; store.Step(0.01f, Vector2.Zero); store.Read([body], state);
        Check(state[0].Position.IsFinite(), "Invalid mass geometry rejects before device work and can be corrected.");
        circle.Dispose(); Check(store.GetMassProperties(body).Inertia == 0, "Disposed borrowed geometry no longer supplies mass.");
        store.Remove(body); var replacement = Body(store, mass: 3);
        Check(store.GetMassProperties(replacement).Center == Vector2.Zero && store.GetMassProperties(replacement).Inertia == 0, "Reused body slots inherit no resolved geometry.");
    }
    private static void VerifyWarmChanges()
    {
        using var store = new Store(); using var circle = new CircleShape { Radius = 3 };
        var body = Body(store, mass: 2); store.AddShape(body, circle, new Transform(0, new(10, 0)));
        var state = new Store.Snapshot[1]; Store.BodyHandle[] bodies = [body];
        void Tick(int i)
        {
            store.SetMassProfile(body, new(2 + (i & 1), 0, new(3 + (i & 1), 0)));
            store.SetConstantForce(body, new(2, 1), 3); store.Step(1f / 120, Vector2.Zero);
            store.Read(bodies, state); store.GetMassProperties(body);
        }
        for (var i = 0; i < 128; i++) Tick(i);
        var upload = store.UploadBytes; var readback = store.ReadbackBytes;
        var before = GC.GetAllocatedBytesForCurrentThread(); var start = Stopwatch.GetTimestamp();
        for (var i = 0; i < 128; i++) Tick(i);
        var elapsed = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
        var allocation = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocation == 0 && state[0].Position.IsFinite(), "Warm active profile edits and physics allocate no managed bytes.");
        Console.WriteLine($"Resident mass changes: 128 warmup/128 edits with step and explicit read, {allocation} managed bytes, mean={elapsed / 128:F4} ms/edit+step+read, upload={(store.UploadBytes - upload) / 128}, readback={(store.ReadbackBytes - readback) / 128} B/iteration.");
    }
    private static void Near(Vector2 value, Vector2 expected, float tolerance, string message) => Check(value.DistanceTo(expected) <= tolerance, $"{message}: {value} vs {expected}");
    private static void Near(float value, float expected, float tolerance, string message) => Check(MathF.Abs(value - expected) <= tolerance, $"{message}: {value} vs {expected}");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
