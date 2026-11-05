using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text.Json;
using Electron2D;

internal static class PublicScenePerformance
{
    internal static void Run(string[] args)
    {
        if (args.Length != 3 || args[0] is not ("cpu" or "gpu") ||
            !int.TryParse(args[1], out var count) || count < 16 || count > 16384)
            throw new ArgumentException("Use scene cpu|gpu body-count output.json; body-count must be 16..16384.");
        var consumer = typeof(PublicScenePerformance).Assembly.GetName().Name;
        if (typeof(PhysicsServer).Assembly.GetCustomAttributes(typeof(InternalsVisibleToAttribute), false)
            .Cast<InternalsVisibleToAttribute>().Any(friend => friend.AssemblyName.Split(',')[0] == consumer))
            throw new InvalidOperationException("Public performance consumers must have no friend access to the runtime.");

        const int warmup = 1600, samples = 256;
        const double delta = 1d / 144;
        const float radius = 6, mass = .2f;
        var backend = args[0] == "cpu" ? PhysicsServer.Backend.CPU : PhysicsServer.Backend.GPU;
        var previousThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, false);
        try
        {
            using var world = new World(backend);
            using var root = new SubViewport { World = world };
            using var circle = new CircleShape { Radius = radius };
            using var surface = new PhysicsMaterial { Friction = .25f, Bounce = .35f };
            var walls = new List<RectangleShape>();
            try
            {
                var bottom = Math.Max(638, 216 + ((count + 49) / 50) * 12.5f + 28);
                foreach (var (x, y, width, height) in new (float, float, float, float)[]
                    { (576, bottom, 1080, 18), (38, (bottom + 190) / 2, 18, bottom - 178),
                        (1114, (bottom + 190) / 2, 18, bottom - 178), (576, 190, 1080, 12) })
                {
                    var wall = new RectangleShape { Size = new(width, height) };
                    walls.Add(wall);
                    var body = new StaticBody { Position = new(x, y), PhysicsMaterialOverride = surface };
                    body.AddChild(new CollisionShape { Shape = wall }); root.AddChild(body);
                }
                var bodies = new RigidBody[count];
                var entered = 0; var exited = 0;
                Action<Node> contact = _ => entered++;
                Action<Node> departure = _ => exited++;
                double initialEnergy = 0;
                for (var i = 0; i < count; i++)
                {
                    var position = new Vector2(65 + i % 50 * 20, 216 + i / 50 * 12.5f);
                    var velocity = new Vector2((i % 7 - 3) * 3, 0);
                    initialEnergy += mass * (980d * (bottom - 9 - radius - position.Y) + .5 * velocity.LengthSquared());
                    var body = new RigidBody
                    {
                        Position = position,
                        LinearVelocity = velocity,
                        CanSleep = false,
                        Mass = mass,
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
                PhysicsServer.AreaSetGravity(world.Space, 980);
                PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
                PhysicsServer.SpaceSetSolverIterations(world.Space, 16);
                if (world.PhysicsBackend != backend || world.RequestedPhysicsBackend != backend)
                    throw new InvalidOperationException("The requested public backend must run without fallback.");
                Console.WriteLine($"Public {backend} scene: {count} bodies; warming {warmup} frames.");
                for (var i = 0; i < warmup; i++) tree.PhysicsFrame(delta);
                var times = new double[samples];
                GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false);
                GC.WaitForPendingFinalizers();
                var tick = PhysicsServer.SpaceGetTick(world.Space);
                var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
                for (var i = 0; i < samples; i++)
                {
                    var start = Stopwatch.GetTimestamp();
                    tree.PhysicsFrame(delta);
                    times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                }
                owner = GC.GetAllocatedBytesForCurrentThread() - owner;
                all = GC.GetTotalAllocatedBytes(true) - all;
                if (PhysicsServer.SpaceGetTick(world.Space) != tick + samples)
                    throw new InvalidOperationException("Every measured public frame must advance one full physical tick.");

                var positions = new Vector2[count];
                var grid = new Dictionary<(int, int), List<int>>();
                double maxEscape = 0, maxPenetration = 0, maxSpeed = 0, energy = 0;
                var contacts = 0;
                foreach (var (body, index) in bodies.Select((body, index) => (body, index)))
                {
                    var pose = body.GlobalTransform; var velocity = body.LinearVelocity; var angular = body.AngularVelocity;
                    var state = PhysicsServer.BodyGetDirectState(body.GetRID())
                        ?? throw new InvalidOperationException("An attached measured body must expose its public direct state.");
                    if (!pose.IsFinite() || !velocity.IsFinite() || !float.IsFinite(angular) || body.Sleeping ||
                        pose.Origin != state.Transform.Origin || velocity != state.LinearVelocity || angular != state.AngularVelocity ||
                        (pose.X - state.Transform.X).Length() > 1e-6 || (pose.Y - state.Transform.Y).Length() > 1e-6)
                        throw new InvalidOperationException("Published scene/direct body values must agree and remain finite and awake.");
                    maxSpeed = Math.Max(maxSpeed, velocity.Length());
                    maxEscape = Math.Max(maxEscape, Math.Max(Math.Max(47 + radius - pose.Origin.X, pose.Origin.X - (1105 - radius)),
                        Math.Max(196 + radius - pose.Origin.Y, pose.Origin.Y - (bottom - 9 - radius))));
                    energy += mass * (.5 * velocity.LengthSquared() + .25 * radius * radius * angular * angular +
                        980 * Math.Max(0, bottom - 9 - radius - pose.Origin.Y));
                    var cell = ((int)MathF.Floor(pose.Origin.X / (2 * radius)), (int)MathF.Floor(pose.Origin.Y / (2 * radius)));
                    for (var y = -1; y <= 1; y++) for (var x = -1; x <= 1; x++)
                            if (grid.TryGetValue((cell.Item1 + x, cell.Item2 + y), out var neighbours))
                                foreach (var other in neighbours) maxPenetration = Math.Max(maxPenetration, 2 * radius - pose.Origin.DistanceTo(positions[other]));
                    if (!grid.TryGetValue(cell, out var bucket)) grid.Add(cell, bucket = []);
                    positions[index] = pose.Origin; bucket.Add(index);
                    contacts += state.GetContactCount();
                }
                var penetrationLimit = 2 * PhysicsServer.SpaceGetContactMaxAllowedPenetration(world.Space) + .001f;
                var valid = owner == 0 && all == 0 && maxEscape <= penetrationLimit && maxPenetration <= penetrationLimit &&
                    maxSpeed < 1000 && double.IsFinite(energy) && energy < initialEnergy * 1.02 && entered > 0 && contacts > 0;
                var mean = times.Average(); var sorted = times.Order().ToArray();
                var output = System.IO.Path.GetFullPath(args[2]);
                Directory.CreateDirectory(System.IO.Path.GetDirectoryName(output)!);
                File.WriteAllText(output, JsonSerializer.Serialize(new
                {
                    schema = 1,
                    api = "SceneTree.PhysicsFrame",
                    publicAPIOnly = true,
                    backend = backend.ToString(),
                    bodies = count,
                    walls = 4,
                    warmup,
                    samples,
                    hz = 144,
                    solverIterations = 16,
                    substeps = "documented engine default",
                    contactMonitor = true,
                    maxContacts = 8,
                    interpolation = true,
                    sleeping = false,
                    threaded = false,
                    meanMS = mean,
                    p50 = sorted[samples / 2],
                    p95 = sorted[(int)(samples * .95)],
                    p99 = sorted[(int)(samples * .99)],
                    max = sorted[^1],
                    perFrameMS = times,
                    ownerBytes = owner,
                    allThreadBytes = all,
                    entered,
                    exited,
                    contacts,
                    maxEscape,
                    maxPenetration,
                    penetrationLimit,
                    maxSpeed,
                    energy,
                    initialEnergy,
                    valid,
                    runtime = System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription,
                    runtimeSHA256 = Convert.ToHexString(SHA256.HashData(File.ReadAllBytes(typeof(PhysicsServer).Assembly.Location))),
                    timing = "complete public frame including required publication/callbacks; no private counters or diagnostic controls",
                    limits = "headless circle scene; no window FPS or private solver-stage timing; validation and disposal outside clock"
                }, new JsonSerializerOptions { WriteIndented = true }));
                if (!valid) throw new InvalidOperationException($"Public scene gates failed: bytes {owner}/{all}, escape/penetration {maxEscape}/{maxPenetration}, contacts {contacts}.");
                Console.WriteLine($"{backend}: public full frame {mean:F4} ms; {owner}/{all} managed bytes, physical/publication checks passed.");
            }
            finally { foreach (var wall in walls) wall.Dispose(); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previousThread); }
    }
}
