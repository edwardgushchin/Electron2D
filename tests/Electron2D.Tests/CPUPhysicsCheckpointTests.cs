using System.Diagnostics;
using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Joints;
using static Box2D.NET.B2Geometries;
using static Box2D.NET.B2MathFunction;
using static Box2D.NET.B2Shapes;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;
using static PhysicsDebugTests;

internal static class CPUPhysicsCheckpointTests
{
    internal static void Run()
    {
        Replay(1); Replay(4); SleepAndForce(); SensorsAndChains(); GeometryAndCCD(); Guards(); Warm();
        Console.WriteLine("CPU checkpoints: contact/joint/sleep/sensor/query replay, task lanes, lifetime and zero warm allocation passed.");
    }
    private sealed class Rig : IDisposable
    {
        internal readonly B2WorldId ID;
        private readonly PhysicsTaskScheduler _tasks;
        internal Rig(int workers = 1, float gravity = -10)
        {
            _tasks = new(workers);
            var definition = b2DefaultWorldDef(); definition.gravity = new(0, gravity);
            definition.workerCount = _tasks.WorkerCount; definition.enqueueTask = _tasks.Enqueue; definition.finishTask = _tasks.Finish;
            ID = b2CreateWorld(definition); _tasks.Bind(b2GetWorldFromId(ID));
        }
        internal B2World World => b2GetWorldFromId(ID);
        internal B2BodyId Box(float x, float y, float width = 1, float height = 1, bool dynamic = true, bool sleep = true, bool sensor = false)
        {
            var body = b2DefaultBodyDef(); body.position = new(x, y); body.type = dynamic ? B2BodyType.b2_dynamicBody : B2BodyType.b2_staticBody;
            body.enableSleep = sleep;
            var id = b2CreateBody(ID, body);
            var shape = b2DefaultShapeDef(); shape.isSensor = sensor; shape.enableSensorEvents = true; shape.enableContactEvents = true;
            b2CreatePolygonShape(id, shape, b2MakeBox(width / 2, height / 2)); return id;
        }
        internal void Step(int count = 1) { for (var i = 0; i < count; i++) b2World_Step(ID, 1f / 60, 4); }
        public void Dispose() { if (b2World_IsValid(ID)) b2DestroyWorld(ID); _tasks.Dispose(); }
    }
    private readonly record struct State(B2Vec2 Position, B2Rot Rotation, B2Vec2 Velocity, float Angular, bool Awake);
    private static State Read(B2BodyId body) => new(b2Body_GetPosition(body), b2Body_GetRotation(body), b2Body_GetLinearVelocity(body), b2Body_GetAngularVelocity(body), b2Body_IsAwake(body));
    private static State[] Read(B2BodyId[] bodies) { var states = new State[bodies.Length]; for (var i = 0; i < bodies.Length; i++) states[i] = Read(bodies[i]); return states; }
    private static void Near(State[] expected, B2BodyId[] bodies)
    {
        for (var i = 0; i < bodies.Length; i++)
        {
            var a = expected[i]; var b = Read(bodies[i]);
            Check(b2Distance(a.Position, b.Position) < 1e-4f && b2Distance(a.Velocity, b.Velocity) < 1e-3f &&
                Math.Abs(a.Rotation.c - b.Rotation.c) < 1e-4f && Math.Abs(a.Rotation.s - b.Rotation.s) < 1e-4f &&
                Math.Abs(a.Angular - b.Angular) < 1e-3f && a.Awake == b.Awake, "CPU replay preserves motion and sleep within .0001 m/.001 m/s");
        }
    }
    private static void Replay(int workers)
    {
        using var rig = new Rig(workers);
        var floor = rig.Box(0, -1, 200, 1, false);
        var bodies = new B2BodyId[workers == 1 ? 12 : 288];
        for (var i = 0; i < bodies.Length; i++) bodies[i] = rig.Box((i / 12) * 3 - 30, (i % 12) + .1f, sleep: false);
        var motor = b2DefaultRevoluteJointDef(); motor.@base.bodyIdA = floor; motor.@base.bodyIdB = bodies[0];
        motor.@base.localFrameA = new(new(-30, 1), new B2Rot(1, 0)); motor.@base.localFrameB = new(default, new B2Rot(1, 0));
        motor.enableMotor = true; motor.motorSpeed = .4f; motor.maxMotorTorque = 8;
        b2CreateRevoluteJoint(rig.ID, motor);
        rig.Step(120); var before = Read(bodies); var contacts = b2World_GetCounters(rig.ID).contactCount;
        Check(contacts > 0, "Checkpoint captures physical contacts");
        using var saved = new CPUPhysicsCheckpoint(rig.ID);
        Advance(); var expected = Read(bodies);
        saved.Restore(); Near(before, bodies); Check(b2World_GetCounters(rig.ID).contactCount == contacts, "Contact storage restored");
        Advance(); Near(expected, bodies);
        using var later = new CPUPhysicsCheckpoint(rig.ID); rig.Step(10); saved.Restore(); Near(before, bodies); later.Restore(); Near(expected, bodies);
        Console.WriteLine($"CPU checkpoint replay: {bodies.Length} dynamic bodies/{workers} workers, contact stack + motor, 60 replay ticks passed.");
        void Advance()
        {
            for (var i = 0; i < 60; i++)
            {
                if (i == 1) b2Body_ApplyLinearImpulseToCenter(bodies[10], new(2, 1), true);
                if (i == 10) b2Body_ApplyForceToCenter(bodies[3], new(4, 0), true);
                if (i == 20) b2Body_SetTransform(bodies[^1], new(20, 8), new B2Rot(1, 0));
                rig.Step();
            }
        }
    }
    private static void SleepAndForce()
    {
        using var rig = new Rig(gravity: 0); var body = rig.Box(0, 0); rig.Step(120);
        Check(!b2Body_IsAwake(body), "Checkpoint has real sleeping solver set");
        using var saved = new CPUPhysicsCheckpoint(rig.ID);
        b2Body_ApplyLinearImpulseToCenter(body, new(3, 0), true); rig.Step(20); saved.Restore();
        Check(!b2Body_IsAwake(body), "Sleeping state rewound"); rig.Step(); Check(!b2Body_IsAwake(body), "Restored sleeping body stays asleep");
        b2Body_ApplyForceToCenter(body, new(60, 0), true); saved.Capture(); rig.Step(); var expected = Read(body);
        rig.Step(10); saved.Restore(); rig.Step(); Near([expected], [body]);
        Check(Math.Abs(Read(body).Velocity.X - 1) < 1e-4, "Restored pending force consumed once");
    }
    private static void SensorsAndChains()
    {
        using var rig = new Rig(gravity: 0);
        var owner = rig.Box(0, 0, 8, 8, false, sensor: true); var moving = rig.Box(0, 0);
        var chain = b2DefaultChainDef(); chain.isLoop = true;
        chain.points = [new(-20, -20), new(20, -20), new(20, 20), new(-20, 20)]; chain.count = 4;
        var chainID = b2CreateChain(owner, chain);
        rig.Step(); var events = b2World_GetSensorEvents(rig.ID); Check(events.beginCount == 1, "Sensor overlap begins");
        using var saved = new CPUPhysicsCheckpoint(rig.ID);
        b2Body_SetTransform(moving, new(12, 0), new B2Rot(1, 0)); rig.Step(); Check(b2World_GetSensorEvents(rig.ID).endCount == 1, "Sensor overlap ends");
        saved.Restore(); Check(b2World_GetSensorEvents(rig.ID).beginCount == 1, "Saved completed sensor events restored");
        var query = new List<B2ShapeId>();
        b2World_OverlapAABB(rig.ID, new(new(-1, -1), new(1, 1)), b2DefaultQueryFilter(), static (shape, context) => { ((List<B2ShapeId>)context).Add(shape); return true; }, query);
        Check(query.Count == 2, "Restored spatial tree answers at restored pose");
        rig.Step(); Check(b2World_GetSensorEvents(rig.ID).beginCount == 0, "Sensor history does not repeat begin after replay");
        b2Body_SetTransform(moving, new(12, 0), new B2Rot(1, 0)); rig.Step(); Check(b2World_GetSensorEvents(rig.ID).endCount == 1, "Replayed sensor exit remains singular");
        b2DestroyChain(chainID); saved.Capture(); rig.Step(); saved.Restore();
    }
    private static void GeometryAndCCD()
    {
        using var rig = new Rig(gravity: 0);
        var body = rig.Box(-5, 0, .4f, .4f, sleep: false); rig.Box(0, 0, .1f, 20, false);
        b2Body_SetBullet(body, true); b2Body_SetLinearVelocity(body, new(400, 0));
        using var saved = new CPUPhysicsCheckpoint(rig.ID);
        rig.Step(3); var expected = Read(body);
        Check(expected.Position.X < 0 && expected.Velocity.X < 1, "CPU continuous collision stops at a thin wall");
        saved.Restore(); rig.Step(3); Near([expected], [body]);
        saved.Capture();
        var shapes = new B2ShapeId[1]; b2Body_GetShapes(body, shapes, 1);
        var geometry = b2MakeBox(3, 2); b2Shape_SetPolygon(shapes[0], ref geometry); b2Shape_SetDensity(shapes[0], 4, true);
        b2Body_SetType(body, B2BodyType.b2_staticBody); rig.Step();
        saved.Restore(); Near([expected], [body]);
        Check(b2Body_GetType(body) == B2BodyType.b2_dynamicBody && Math.Abs(b2Body_GetMass(body) - .16f) < 1e-5f,
            "CPU authored shape/mass/role data and solver membership rewind together");
        Check(Math.Abs(b2Shape_GetPolygon(shapes[0]).vertices[0].X) < .21f, "Shape geometry is independent of the discarded future");
    }
    private static void Guards()
    {
        using var rig = new Rig(); var body = rig.Box(0, 1); using var saved = new CPUPhysicsCheckpoint(rig.ID);
        Reject<InvalidOperationException>(() => Task.Run(saved.Restore).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(saved.Dispose).GetAwaiter().GetResult());
        rig.World.locked = true;
        try { Reject<InvalidOperationException>(saved.Capture); Reject<InvalidOperationException>(saved.Restore); }
        finally { rig.World.locked = false; }
        var taskContext = rig.World.userTaskContext; rig.World.userTaskContext = new object();
        Reject<InvalidOperationException>(saved.Restore); rig.World.userTaskContext = taskContext;
        var added = rig.Box(4, 4); var pose = Read(body); Reject<InvalidOperationException>(saved.Restore); Check(Read(body) == pose, "Rejected restore leaves live data unchanged");
        b2DestroyBody(added); saved.Capture();
        b2DestroyBody(body); Reject<InvalidOperationException>(saved.Restore);
        rig.Dispose(); Reject<ObjectDisposedException>(saved.Restore);
        using var reuse = new Rig(); reuse.Box(0, 0); Reject<ObjectDisposedException>(saved.Capture);
    }
    private static void Warm()
    {
        using var rig = new Rig(4); rig.Box(0, -1, 200, 1, false);
        var bodies = new B2BodyId[1024];
        for (var i = 0; i < bodies.Length; i++) bodies[i] = rig.Box((i % 128) * 1.1f - 70, (i / 128) + .1f, sleep: false);
        rig.Step(120); var initialBytes = GC.GetAllocatedBytesForCurrentThread();
        using var saved = new CPUPhysicsCheckpoint(rig.ID); initialBytes = GC.GetAllocatedBytesForCurrentThread() - initialBytes;
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
        for (var i = 0; i < 64; i++) { saved.Capture(); rig.Step(); saved.Restore(); }
        var times = new double[128]; var start = Stopwatch.GetTimestamp();
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < times.Length; i++) { var begin = Stopwatch.GetTimestamp(); saved.Capture(); saved.Restore(); times[i] = Stopwatch.GetElapsedTime(begin).TotalMilliseconds; }
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        var mean = Stopwatch.GetElapsedTime(start).TotalMilliseconds / times.Length; Array.Sort(times);
        Check(owner == 0 && all == 0, "Warm CPU checkpoint capture/restore allocates zero managed bytes");
        Console.WriteLine($"CPU checkpoint: 1024 dynamic boxes + floor, 4 workers, 64 warmups/128 capture+restore samples; mean={mean:F4}, p50/p95/p99={times[64]:F4}/{times[121]:F4}/{times[126]:F4} ms; {owner}/{all} owner/all managed B; initial capture allocated {initialBytes} B.");
    }
}
