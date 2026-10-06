using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Electron2D;
using static Box2D.NET.B2Bodies;
using static Box2D.NET.B2Worlds;

internal static class PhysicsPipelinePerformance
{
    internal static void Run()
    {
        const int warmup = 1600, sampleCount = 1024;
        const double delta = 1d / 144;
        var count = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_PHYSICS_PERFORMANCE_COUNT") ?? "1536");
        using var circle = new CircleShape { Radius = 6 };
        using var surface = new PhysicsMaterial { Friction = .25f, Bounce = .35f };
        using var root = new Node();
        var walls = new List<RectangleShape>();
        foreach (var (x, y, width, height) in new (float, float, float, float)[]
            { (576, 638, 1080, 18), (38, 420, 18, 432), (1114, 420, 18, 432), (576, 190, 1080, 12) })
        {
            var wall = new RectangleShape { Size = new(width, height) };
            walls.Add(wall);
            var floor = new StaticBody { Name = "Wall" + walls.Count, Position = new(x, y), PhysicsMaterialOverride = surface };
            floor.AddChild(new CollisionShape { Shape = wall }); root.AddChild(floor);
        }
        var bodies = new RigidBody[count];
        var entered = 0; var exited = 0;
        ulong eventHash = 14695981039346656037;
        Action<Node> contact = other => { entered++; eventHash = (eventHash ^ 1) * 1099511628211; eventHash = (eventHash ^ other.InstanceID) * 1099511628211; };
        Action<Node> departure = other => { exited++; eventHash = (eventHash ^ 2) * 1099511628211; eventHash = (eventHash ^ other.InstanceID) * 1099511628211; };
        for (var i = 0; i < count; i++)
        {
            var body = new RigidBody
            {
                Name = "Ball" + i,
                Position = new(65 + i % 50 * 20, 216 + i / 50 * 12.5f),
                LinearVelocity = new((i % 7 - 3) * 3, 0),
                CanSleep = false,
                Mass = .2f,
                LinearDamp = .05f,
                AngularDamp = .1f,
                ContactMonitor = true,
                MaxContactsReported = 8,
                PhysicsMaterialOverride = surface
            };
            body.BodyEntered += contact; body.BodyExited += departure;
            body.AddChild(new CollisionShape { Shape = circle }); root.AddChild(body); bodies[i] = body;
        }
        using var tree = new SceneTree(root) { PhysicsInterpolation = true };
        try
        {
            for (var i = 0; i < warmup; i++) tree.PhysicsFrame(delta);
            var world = bodies[0].Space!.WorldID;
            var times = new double[sampleCount];
            double backend = 0;
            var total = GC.GetTotalAllocatedBytes(true);
            var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < sampleCount; i++)
            {
                var start = Stopwatch.GetTimestamp();
                tree.PhysicsFrame(delta);
                times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                backend += b2World_GetProfile(world).step;
            }
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated;
            total = GC.GetTotalAllocatedBytes(true) - total;
            var states = new float[count * 7];
            var reportedContacts = 0;
            var contactValues = new List<float>(count * 8 * 16);
            for (var i = 0; i < count; i++)
            {
                var id = bodies[i].BackendID;
                var t = b2Body_GetTransform(id); var v = b2Body_GetLinearVelocity(id);
                states[i * 7] = t.p.X; states[i * 7 + 1] = t.p.Y;
                states[i * 7 + 2] = t.q.c; states[i * 7 + 3] = t.q.s;
                states[i * 7 + 4] = v.X; states[i * 7 + 5] = v.Y; states[i * 7 + 6] = b2Body_GetAngularVelocity(id);
                reportedContacts += bodies[i].GetContactCount();
            }
            foreach (var body in bodies)
            {
                var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
                contactValues.Add(state.GetContactCount());
                for (var i = 0; i < state.GetContactCount(); i++)
                {
                    contactValues.Add(state.GetContactCollider(i).GetHashCode());
                    contactValues.Add(state.GetContactLocalShape(i)); contactValues.Add(state.GetContactColliderShape(i));
                    Append(state.GetContactLocalPosition(i)); Append(state.GetContactColliderPosition(i));
                    Append(state.GetContactLocalNormal(i)); Append(state.GetContactLocalVelocityAtPosition(i));
                    Append(state.GetContactColliderVelocityAtPosition(i)); Append(state.GetContactImpulse(i));
                }
            }
            var contactSHA256 = Convert.ToHexString(SHA256.HashData(MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(contactValues))));
            void Append(Vector2 value) { contactValues.Add(value.X); contactValues.Add(value.Y); }
            if (allocated != 0 || total != 0 || states.Any(v => !float.IsFinite(v)))
                throw new Exception($"Physics pipeline allocation/finite-state check failed: {allocated}/{total} bytes.");
            Console.WriteLine(JsonSerializer.Serialize(new
            {
                bodies = count,
                hz = 144,
                substeps = 4,
                contactMonitor = true,
                maxContacts = 8,
                interpolation = true,
                warmup,
                samples = sampleCount,
                meanMS = times.Average(),
                p95MS = times.Order().ElementAt((int)(sampleCount * .95)),
                p99MS = times.Order().ElementAt((int)(sampleCount * .99)),
                backendMS = backend / sampleCount,
                ownerBytes = allocated,
                allThreadsBytes = total,
                entered,
                exited,
                eventHash,
                reportedContacts,
                contactSHA256,
                stateSHA256 = Convert.ToHexString(SHA256.HashData(MemoryMarshal.AsBytes(states.AsSpan())))
            }));
        }
        finally { tree.Dispose(); foreach (var wall in walls) wall.Dispose(); }
    }
}
