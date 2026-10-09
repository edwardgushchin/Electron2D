using System.Diagnostics;
using Electron2D;
using Store = Electron2D.GPUPhysicsBodyStore;
using Mode = Electron2D.PhysicsServer.BodyMode;
using Override = Electron2D.Area.SpaceOverride;
using Fields = Electron2D.GPUPhysicsBodyStore.FieldParameters;

internal static class GPUPhysicsFieldTests
{
    internal static void Run()
    {
        VerifyCPUChannels(); VerifyPointAndModes(); VerifyMembershipAndSleep(); VerifyGeometryAndGrowth(); VerifyDirectedAndConcave(); VerifyFailures(); VerifyResidency();
        Console.WriteLine("Resident fields: public CPU priority/channel parity, point gravity, body damping, membership, sleeping, validation and zero-allocation residency passed.");
    }
    private static Store.Snapshot Read(Store s, Store.BodyHandle b)
    { Span<Store.Snapshot> result = stackalloc Store.Snapshot[1]; s.Read([b], result); return result[0]; }
    private static Store.BodyHandle Body(Store s, Mode mode = Mode.Rigid, Vector2 position = default) =>
        s.Add(new(mode, position, 0, Vector2.Zero, 0, Inertia: 1, CanSleep: false));
    private sealed class Fixture : IDisposable
    {
        internal readonly Store GPU = new();
        internal readonly RID Space = PhysicsServer.SpaceCreate(), Body = PhysicsServer.BodyCreate(), Low = PhysicsServer.AreaCreate(), High = PhysicsServer.AreaCreate();
        internal readonly RectangleShape Region = new() { Size = new(1000, 1000) };
        internal readonly CircleShape Circle = new() { Radius = 1 };
        internal readonly Store.BodyHandle B, L, H;
        internal Fixture()
        {
            PhysicsServer.SpaceSetActive(Space, true); PhysicsServer.BodySetInertia(Body, 1); PhysicsServer.BodySetCanSleep(Body, false);
            PhysicsServer.BodyAddShape(Body, Circle.GetRID()); PhysicsServer.BodySetSpace(Body, Space); PhysicsServer.BodySetCollisionMask(Body, 0);
            PhysicsServer.AreaAddShape(Low, Region.GetRID()); PhysicsServer.AreaAddShape(High, Region.GetRID());
            PhysicsServer.AreaSetSpace(Low, Space); PhysicsServer.AreaSetSpace(High, Space);
            B = GPU.Add(new(Mode.Rigid, Vector2.Zero, 0, Vector2.Zero, 0, Inertia: 1, CanSleep: false)); GPU.AddShape(B, Circle, mask: 0);
            L = GPU.Add(new(Mode.Static, Vector2.Zero, 0, Vector2.Zero, 0)); H = GPU.Add(new(Mode.Static, Vector2.Zero, 0, Vector2.Zero, 0));
            GPU.AddShape(L, Region, sensor: true); GPU.AddShape(H, Region, sensor: true);
            // One Area may contain multiple overlapping fixtures; its field contributes once.
            GPU.AddShape(H, Region, sensor: true); PhysicsServer.AreaAddShape(High, Region.GetRID());
        }
        internal void Set(RID rid, in Fields fields)
        {
            PhysicsServer.AreaSetGravity(rid, fields.Gravity); PhysicsServer.AreaSetGravityVector(rid, fields.GravityVector);
            PhysicsServer.AreaSetGravityPoint(rid, fields.GravityPoint); PhysicsServer.AreaSetGravityPointUnitDistance(rid, fields.PointUnitDistance);
            PhysicsServer.AreaSetLinearDamp(rid, fields.LinearDamp); PhysicsServer.AreaSetAngularDamp(rid, fields.AngularDamp);
            PhysicsServer.AreaSetPriority(rid, fields.Priority); PhysicsServer.AreaSetGravitySpaceOverride(rid, fields.GravityMode);
            PhysicsServer.AreaSetLinearDampSpaceOverride(rid, fields.LinearMode); PhysicsServer.AreaSetAngularDampSpaceOverride(rid, fields.AngularMode);
        }
        internal void Reset()
        {
            PhysicsServer.BodySetTransform(Body, Transform.Identity); PhysicsServer.BodySetLinearVelocity(Body, new(8, 6)); PhysicsServer.BodySetAngularVelocity(Body, 2);
            GPU.SetPose(B, Vector2.Zero, 0); GPU.SetVelocity(B, new(8, 6), 2);
        }
        internal void Compare(in Fields defaults, int substeps = 4)
        {
            GPU.SimulateFields(0.1f, defaults, substeps: substeps); PhysicsServer.SpaceStep(Space, 0.1);
            var actual = Read(GPU, B); var expected = PhysicsServer.BodyGetDirectState(Body)!;
            Near(actual.Gravity, expected.TotalGravity, 0.002f, "Selected gravity matches public CPU");
            Near(actual.TotalLinearDamp, expected.TotalLinearDamp, 0.0001f, "Linear channel has its own stopping rule");
            Near(actual.TotalAngularDamp, expected.TotalAngularDamp, 0.0001f, "Angular channel has its own stopping rule");
            Near(new(actual.Velocity.X, actual.Velocity.Y), expected.LinearVelocity, 0.002f, "Damping then gravity matches public CPU across subdivisions");
            Near(actual.Velocity.Z, expected.AngularVelocity, 0.0002f, "Angular damping matches public CPU");
        }
        public void Dispose()
        { PhysicsServer.FreeRID(Body); PhysicsServer.FreeRID(High); PhysicsServer.FreeRID(Low); PhysicsServer.FreeRID(Space); GPU.Dispose(); Circle.Dispose(); Region.Dispose(); }
    }
    private static void VerifyCPUChannels()
    {
        using var f = new Fixture();
        var defaults = new Fields(new(0, 1), 10, LinearDamp: 0.3f, AngularDamp: 0.5f, GravityMode: Override.Replace, Priority: int.MaxValue);
        var low = new Fields(new(1, 0), 20, LinearDamp: 1, AngularDamp: 2, GravityMode: Override.Combine, LinearMode: Override.Combine, AngularMode: Override.Combine, Priority: -4);
        f.Set(f.Space, defaults); f.Set(f.Low, low); f.GPU.SetAreaFields(f.L, low);
        foreach (var mode in Enum.GetValues<Override>())
            foreach (var substeps in new[] { 1, 4, 8 })
            {
                var high = new Fields(new(0, 2), -30, LinearDamp: -2, AngularDamp: 3, GravityMode: mode,
                    LinearMode: (Override)(((int)mode + 1) % 5), AngularMode: (Override)(((int)mode + 2) % 5), Priority: 7);
                f.Set(f.High, high); f.GPU.SetAreaFields(f.H, high); f.Reset(); f.Compare(defaults, substeps);
            }
        var first = low with { Priority = 0, GravityMode = Override.Combine }; var second = low with { Priority = 0, GravityMode = Override.Replace, Gravity = 4 };
        f.Set(f.Low, first); f.Set(f.High, second); f.GPU.SetAreaFields(f.L, first); f.GPU.SetAreaFields(f.H, second); f.Reset(); f.Compare(defaults);
        foreach (var linear in Enum.GetValues<RigidBody.DampMode>())
            foreach (var angular in Enum.GetValues<RigidBody.DampMode>())
            {
                f.GPU.SetIntegrationPolicy(f.B, new(-2, -0.7f, 0.4f, LinearDampMode: linear, AngularDampMode: angular));
                PhysicsServer.BodySetGravityScale(f.Body, -2); PhysicsServer.BodySetLinearDamp(f.Body, -0.7f); PhysicsServer.BodySetAngularDamp(f.Body, 0.4f);
                PhysicsServer.BodySetLinearDampMode(f.Body, linear); PhysicsServer.BodySetAngularDampMode(f.Body, angular);
                f.Reset(); f.Compare(defaults);
            }
        f.GPU.SetIntegrationPolicy(f.B, new(1, OmitForceIntegration: true)); PhysicsServer.BodySetGravityScale(f.Body, 1);
        PhysicsServer.BodySetLinearDamp(f.Body, 0); PhysicsServer.BodySetAngularDamp(f.Body, 0);
        PhysicsServer.BodySetLinearDampMode(f.Body, RigidBody.DampMode.Combine); PhysicsServer.BodySetAngularDampMode(f.Body, RigidBody.DampMode.Combine);
        PhysicsServer.BodySetOmitForceIntegration(f.Body, true); f.Reset(); f.Compare(defaults);
        Near(Read(f.GPU, f.B).Velocity.X, 8, 0, "Omission reports selected fields without applying them");
    }
    private static void VerifyPointAndModes()
    {
        using var f = new Fixture(); var defaults = new Fields(Vector2.Zero);
        f.Set(f.Space, defaults); f.Set(f.Low, defaults); f.GPU.SetAreaFields(f.L, defaults);
        foreach (var unit in new[] { 10f, 0f, -2f })
        {
            var point = new Fields(new(10, 0), 100, GravityPoint: true, PointUnitDistance: unit, GravityMode: Override.Replace);
            f.Set(f.High, point); PhysicsServer.AreaSetTransform(f.High, new(MathF.PI / 2, new(10, 0)));
            f.GPU.SetAreaFields(f.H, point); f.GPU.SetPose(f.H, new(10, 0), MathF.PI / 2); f.Reset(); f.Compare(defaults);
        }
        var centered = new Fields(Vector2.Zero, 100, GravityPoint: true, PointUnitDistance: 10, GravityMode: Override.Replace);
        f.Set(f.High, centered); PhysicsServer.AreaSetTransform(f.High, Transform.Identity); f.GPU.SetAreaFields(f.H, centered); f.GPU.SetPose(f.H, Vector2.Zero, 0);
        f.Reset(); f.Compare(defaults); Near(Read(f.GPU, f.B).Gravity, Vector2.Zero, 0, "At the attraction center gravity is zero");
        using var s = new Store(); var kinematic = Body(s, Mode.Kinematic); var rigid = Body(s); var stationary = Body(s, Mode.Static);
        var worldPoint = new Fields(new(20, 0), 40, GravityPoint: true, LinearDamp: 0.1f, AngularDamp: 0.2f);
        s.SetKinematicTarget(kinematic, new(40, 0), 0); s.SimulateFields(0.1f, worldPoint);
        Near(Read(s, kinematic).Gravity, new(40, 0), 0.0001f, "Kinematic body reports entry-pose fields without gravity integration");
        Near(Read(s, kinematic).Position, new(40, 0), 0, "Fields do not displace the kinematic target");
        Near(Read(s, rigid).Velocity.X, 4, 0.0001f, "Point world fallback needs no receiver geometry");
        Near(Read(s, stationary).Gravity, Vector2.Zero, 0, "Static body does not resolve dynamic fields");
        s.SimulateFields(0.1f, worldPoint); Near(Read(s, kinematic).Gravity, new(-40, 0), 0.0001f, "Point fields refresh once per outer tick");
    }
    private static void VerifyMembershipAndSleep()
    {
        using var s = new Store(); using var region = new RectangleShape { Size = new(20, 20) }; using var circle = new CircleShape { Radius = 1 };
        var area = Body(s, Mode.Static); var body = Body(s); s.SetCanSleep(body, true);
        var outside = Body(s, position: new(100, 100)); s.SetCanSleep(outside, true); s.SetIntegrationPolicy(outside, new(0));
        var sensor = s.AddShape(area, region, layer: 0, mask: 1u << 31, sensor: true); s.AddShape(body, circle, layer: 1u << 31, mask: 0);
        var field = new Fields(new(2, 0), 10, GravityMode: Override.Replace); s.SetAreaFields(area, field);
        s.Simulate(0.01f, Vector2.Zero); Near(Read(s, body).Gravity, new(20, 0), 0, "Area mask alone filters the body layer");
        s.SetSleeping(body, true); s.SetSleeping(outside, true); s.Simulate(0.01f, Vector2.Zero); Check(Read(s, body).Sleeping, "Unchanged fields do not continuously wake sleepers.");
        s.SetAreaFields(area, field with { Gravity = 20 }); s.Simulate(0, Vector2.Zero); Near(Read(s, body).Gravity, new(20, 0), 0, "Zero-time work keeps the last resolved fields");
        s.Simulate(0.01f, Vector2.Zero); Check(!Read(s, body).Sleeping && Read(s, body).Gravity.X == 40, "A changed field wakes its sleeping receiver.");
        Check(Read(s, outside).Sleeping, "A changed Area does not wake an unaffected receiver.");
        s.Simulate(0.01f, new(0, 100)); Check(Read(s, outside).Sleeping, "Zero body gravity scale prevents an irrelevant default-gravity change from waking it.");
        s.SetSleeping(body, true); s.SetShapeFilter(sensor, 0, 0, true); s.Simulate(0.01f, Vector2.Zero);
        Check(!Read(s, body).Sleeping && Read(s, body).Gravity == Vector2.Zero, "Losing directional membership wakes and restores fallback.");
        s.SetShapeFilter(sensor, 0, 1u << 31, true); s.SetPose(area, new(100, 0), 0); s.SetVelocity(body, Vector2.Zero, 0); s.Simulate(0.01f, Vector2.Zero);
        Near(Read(s, body).Gravity, Vector2.Zero, 0, "Current geometry controls membership before the step");
        s.SetPose(area, Vector2.Zero, 0); s.Simulate(0.01f, Vector2.Zero); s.RemoveAreaFields(area); s.Simulate(0.01f, Vector2.Zero);
        Near(Read(s, body).Gravity, Vector2.Zero, 0, "Removing field behavior retains the sensor but restores defaults");
        s.SetAreaFields(area, field with { GravityMode = Override.Disabled }); var submissions = s.FieldSubmissionCount;
        s.Simulate(0.01f, Vector2.Zero); Check(s.FieldSubmissionCount == submissions, "Areas with every channel disabled do not launch field reduction.");
        s.SetAreaFields(area, field); s.Remove(area); var reused = Body(s, Mode.Static); s.AddShape(reused, region, sensor: true); s.Simulate(0.01f, Vector2.Zero);
        Near(Read(s, body).Gravity, Vector2.Zero, 0, "A reused body slot does not inherit Area fields");
        Reject<ArgumentException>(() => s.SetAreaFields(area, field));
    }
    private static void VerifyGeometryAndGrowth()
    {
        using var s = new Store(); using var circle = new CircleShape { Radius = 1 }; using var box = new RectangleShape { Size = new(2, 2) };
        var a = Body(s, Mode.Static); var b = Body(s, position: new(1.44f, 1.44f));
        var areaShape = s.AddShape(a, circle, sensor: true); var bodyShape = s.AddShape(b, circle, mask: 0);
        var field = new Fields(Vector2.Right, 10, GravityMode: Override.Replace); s.SetAreaFields(a, field);
        s.StepFields(0.01f, new(Vector2.Zero)); Near(Read(s, b).Gravity, new(10, 0), 0, "Diagonal proximity within the accepted sensor tolerance contributes when tight AABBs overlap");
        s.SetPose(b, new(1.5f, 1.5f), 0); s.SetVelocity(b, Vector2.Zero, 0); s.Simulate(0.01f, Vector2.Zero);
        Near(Read(s, b).Gravity, Vector2.Zero, 0, "An AABB overlap alone does not establish field membership");
        s.RemoveShape(areaShape); s.RemoveShape(bodyShape); s.AddShape(a, box, sensor: true); s.AddShape(b, box, mask: 0);
        s.SetPose(b, new(2.02f, 0), 0); s.SetVelocity(b, Vector2.Zero, 0); s.Simulate(0.01f, Vector2.Zero);
        Near(Read(s, b).Gravity, new(10, 0), 0, "Nearby polygons inside the accepted field tolerance contribute despite disjoint tight bounds");
        var space = PhysicsServer.SpaceCreate(); var cpuArea = PhysicsServer.AreaCreate(); var cpuBody = PhysicsServer.BodyCreate();
        try
        {
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.AreaSetGravity(space, 0);
            PhysicsServer.AreaAddShape(cpuArea, box.GetRID()); PhysicsServer.BodyAddShape(cpuBody, box.GetRID());
            PhysicsServer.AreaSetSpace(cpuArea, space); PhysicsServer.BodySetSpace(cpuBody, space);
            PhysicsServer.AreaSetGravitySpaceOverride(cpuArea, Override.Replace); PhysicsServer.AreaSetGravityVector(cpuArea, Vector2.Right); PhysicsServer.AreaSetGravity(cpuArea, 10);
            foreach (var x in new[] { 2.02f, 2.06f })
            {
                PhysicsServer.BodySetTransform(cpuBody, new(0, new(x, 0))); PhysicsServer.BodySetLinearVelocity(cpuBody, Vector2.Zero); PhysicsServer.SpaceStep(space, 0.01);
                s.SetPose(b, new(x, 0), 0); s.SetVelocity(b, Vector2.Zero, 0); s.Simulate(0.01f, Vector2.Zero);
                Near(Read(s, b).Gravity, PhysicsServer.BodyGetDirectState(cpuBody)!.TotalGravity, 0.0001f, "Field boundary tolerance agrees with public CPU polygon overlap");
            }
        }
        finally { PhysicsServer.FreeRID(cpuBody); PhysicsServer.FreeRID(cpuArea); PhysicsServer.FreeRID(space); }
        s.SetPose(b, Vector2.Zero, 0); s.Simulate(0.01f, Vector2.Zero);
        for (var i = 0; i < 130; i++) Body(s, Mode.Static, new(100 + i, 100));
        s.Simulate(0.01f, Vector2.Zero); Near(Read(s, b).Gravity, new(10, 0), 0, "Device growth retains resolved state and Area configuration");
        box.Dispose(); s.Simulate(0.01f, Vector2.Zero); Near(Read(s, b).Gravity, Vector2.Zero, 0, "Disposed geometry cannot retain stale field membership");
    }
    private static void VerifyDirectedAndConcave()
    {
        using var ray = new SeparationRayShape { Length = 5 }; using var circle = new CircleShape { Radius = 1 };
        ComparePair(ray, circle, new(0, 5.98f), true);
        ComparePair(ray, circle, new(0, 6.02f), false);
        ComparePair(circle, ray, new(0, -5.98f), true);
        ComparePair(circle, ray, new(0, -6.02f), false);
        using var horizontal = new ConcavePolygonShape { Segments = [new(-5, 0), new(5, 0)] };
        using var vertical = new ConcavePolygonShape { Segments = [new(0, -5), new(0, 5)] };
        ComparePair(horizontal, vertical, Vector2.Zero, true);
        static void ComparePair(Shape areaShape, Shape bodyShape, Vector2 position, bool overlaps)
        {
            using var s = new Store(); var a = Body(s, Mode.Static); var b = Body(s, position: position);
            s.AddShape(a, areaShape, sensor: true); s.AddShape(b, bodyShape, mask: 0);
            s.SetAreaFields(a, new(Vector2.Right, 10, GravityMode: Override.Replace));
            var space = PhysicsServer.SpaceCreate(); var cpuArea = PhysicsServer.AreaCreate(); var cpuBody = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.SpaceSetActive(space, true); PhysicsServer.AreaSetGravity(space, 0);
                PhysicsServer.AreaAddShape(cpuArea, areaShape.GetRID()); PhysicsServer.BodyAddShape(cpuBody, bodyShape.GetRID());
                PhysicsServer.BodySetCollisionMask(cpuBody, 0); PhysicsServer.BodySetTransform(cpuBody, new(0, position));
                PhysicsServer.AreaSetSpace(cpuArea, space); PhysicsServer.BodySetSpace(cpuBody, space);
                PhysicsServer.AreaSetGravitySpaceOverride(cpuArea, Override.Replace); PhysicsServer.AreaSetGravityVector(cpuArea, Vector2.Right); PhysicsServer.AreaSetGravity(cpuArea, 10);
                PhysicsServer.SpaceStep(space, 0.01); s.Simulate(0.01f, Vector2.Zero);
                var expected = PhysicsServer.BodyGetDirectState(cpuBody)!.TotalGravity;
                Near(expected.X, overlaps ? 10 : 0, 0.0001f, "Public CPU establishes directed/concave field membership");
                Near(Read(s, b).Gravity, expected, 0.0001f, "GPU preserves directed ray extent and sensing between concave pieces");
            }
            finally { PhysicsServer.FreeRID(cpuBody); PhysicsServer.FreeRID(cpuArea); PhysicsServer.FreeRID(space); }
        }
    }
    private static void VerifyFailures()
    {
        using var s = new Store(); using var other = new Store(); using var region = new RectangleShape { Size = new(20, 20) }; using var circle = new CircleShape { Radius = 1 };
        var a = Body(s, Mode.Static); var b = Body(s); var foreign = Body(other, Mode.Static);
        var shape = s.AddShape(a, region, sensor: true); s.AddShape(b, circle); s.SetAreaFields(a, new(Vector2.Right, 1, GravityMode: Override.Replace));
        Reject<ArgumentOutOfRangeException>(() => s.SetAreaFields(a, new(Vector2.Right, float.NaN)));
        Reject<ArgumentOutOfRangeException>(() => s.SetAreaFields(a, new(Vector2.Right, GravityMode: (Override)99)));
        Reject<ArgumentOutOfRangeException>(() => s.SetIntegrationPolicy(b, new(1, LinearDampMode: (RigidBody.DampMode)99)));
        Reject<InvalidOperationException>(() => s.SetAreaFields(b, new(Vector2.Right)));
        Reject<InvalidOperationException>(() => s.SetMode(a, Mode.Rigid));
        Reject<InvalidOperationException>(() => s.SetShapeFilter(shape, 1, 1, false));
        Reject<InvalidOperationException>(() => s.AddShape(a, circle));
        Reject<ArgumentException>(() => s.SetAreaFields(foreign, new(Vector2.Right)));
        Reject<InvalidOperationException>(() => Task.Run(() => s.SetAreaFields(a, new(Vector2.Right))).GetAwaiter().GetResult());
        s.Simulate(0.1f, Vector2.Zero); Near(Read(s, b).Gravity, Vector2.Right, 0, "Rejected edits preserve prior fields");
        Reject<InvalidOperationException>(() => s.SimulateFields(0.1f, new(new(float.MaxValue, 0), float.MaxValue)));
        Near(Read(s, b).Gravity, Vector2.Right, 0, "Invalid uniform defaults reject before GPU work and leave the store usable");
        s.SetAreaFields(a, new(new(float.MaxValue, 0), float.MaxValue, GravityMode: Override.Replace));
        Reject<InvalidOperationException>(() => s.Simulate(0.1f, Vector2.Zero)); Reject<InvalidOperationException>(() => Read(s, b));
        s.Dispose(); Reject<ObjectDisposedException>(() => s.SetAreaFields(a, new(Vector2.Right)));
    }
    private static void VerifyResidency()
    {
        using var s = new Store(); using var region = new RectangleShape { Size = new(10000, 10000) }; using var circle = new CircleShape { Radius = 0.5f };
        var area = Body(s, Mode.Static); s.AddShape(area, region, sensor: true);
        var fields = new Fields(Vector2.Right, GravityMode: Override.Combine); s.SetAreaFields(area, fields);
        var bodies = new Store.BodyHandle[4096];
        for (var i = 0; i < bodies.Length; i++) { bodies[i] = Body(s, position: new(i % 64 * 4, i / 64 * 4)); s.AddShape(bodies[i], circle, mask: 0); }
        void Tick(int tick) { s.SetAreaFields(area, fields with { Gravity = tick % 2 }); s.Simulate(1f / 120, Vector2.Zero, substeps: 1, iterations: 4); }
        for (var i = 0; i < 128; i++) Tick(i);
        var samples = new double[128]; var upload = s.UploadBytes; var readback = s.ReadbackBytes; var wait = s.WaitMS; var fieldMS = s.FieldMS; var fieldWait = s.FieldWaitMS;
        var allocation = GC.GetAllocatedBytesForCurrentThread();
        for (var i = 0; i < samples.Length; i++) { var start = Stopwatch.GetTimestamp(); Tick(i); samples[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds; }
        allocation = GC.GetAllocatedBytesForCurrentThread() - allocation; Check(allocation == 0, "Warmed field edits/reduction/integration allocate zero owner-thread managed bytes.");
        Array.Sort(samples);
        Console.WriteLine($"Resident fields: 4096 bodies + 1 Area, all inside, 128 warmup/128 samples, 1 substep/4 iterations; p50={samples[64]:F4}, p95={samples[121]:F4}, p99={samples[126]:F4} ms, wait={(s.WaitMS - wait) / 128:F4}, fields={(s.FieldMS - fieldMS) / 128:F4}/field wait={(s.FieldWaitMS - fieldWait) / 128:F4} ms; {allocation} managed B, upload={(s.UploadBytes - upload) / 128}, readback={(s.ReadbackBytes - readback) / 128} B/tick; {s.Driver}, {s.DeviceName}.");
        var states = new Store.Snapshot[bodies.Length]; s.Read(bodies, states);
        for (var i = 0; i < states.Length; i++) Near(states[i].Gravity, Vector2.Right, 0, "Every receiver resolves its Area once; explicit full read is outside measurement");
    }
    private static void Near(Vector2 a, Vector2 b, float tolerance, string message) => Check(a.IsFinite() && (a - b).Length() <= tolerance, $"{message}: {a} vs {b}");
    private static void Near(float a, float b, float tolerance, string message) => Check(float.IsFinite(a) && MathF.Abs(a - b) <= tolerance, $"{message}: {a} vs {b}");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}."); }
}
