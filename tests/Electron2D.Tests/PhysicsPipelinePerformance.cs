using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Text.Json;
using Electron2D;
using static PhysicsDebugTests;

internal static class PhysicsPipelinePerformance
{
    internal static void Run()
    {
        var count = Read("COUNT", 1536, 64, 65536);
        var warmup = Read("WARMUP", 1600, 64, 4096);
        var samples = Read("SAMPLES", 1024, 64, 2048);
        var choice = Environment.GetEnvironmentVariable("ELECTRON2D_PHYSICS_PERFORMANCE_BACKEND") ?? "both";
        var backends = choice switch
        {
            "cpu" => new[] { PhysicsServer.Backend.CPU },
            "gpu" => new[] { PhysicsServer.Backend.GPU },
            "both" => new[] { PhysicsServer.Backend.CPU, PhysicsServer.Backend.GPU },
            _ => throw new ArgumentException("Choose cpu, gpu or both.")
        };
        var previous = PhysicsSpace.ProfilingEnabled;
        PhysicsSpace.ProfilingEnabled = true;
        try { foreach (var backend in backends) Measure(backend, count, warmup, samples); }
        finally { PhysicsSpace.ProfilingEnabled = previous; }
    }

    private static int Read(string suffix, int fallback, int min, int max)
    {
        var value = int.Parse(Environment.GetEnvironmentVariable("ELECTRON2D_PHYSICS_PERFORMANCE_" + suffix) ?? fallback.ToString());
        if (value < min || value > max) throw new ArgumentOutOfRangeException(suffix);
        return value;
    }

    private static void Measure(PhysicsServer.Backend backend, int count, int warmup, int samples)
    {
        const double delta = 1d / 144;
        const float radius = 6, mass = .2f;
        var bottom = Math.Max(638, 216 + ((count + 49) / 50) * 12.5f + 28);
        using var world = new World(backend);
        using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = radius };
        using var surface = new PhysicsMaterial { Friction = .25f, Bounce = .35f };
        var walls = new List<RectangleShape>();
        foreach (var (x, y, width, height) in new (float, float, float, float)[]
            { (576, bottom, 1080, 18), (38, (bottom + 190) / 2, 18, bottom - 178),
                (1114, (bottom + 190) / 2, 18, bottom - 178), (576, 190, 1080, 12) })
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
        double initialEnergy = 0;
        for (var i = 0; i < count; i++)
        {
            var position = new Vector2(65 + i % 50 * 20, 216 + i / 50 * 12.5f);
            var velocity = new Vector2((i % 7 - 3) * 3, 0);
            initialEnergy += mass * (980d * (bottom - 9 - radius - position.Y) + .5 * velocity.LengthSquared());
            var body = new RigidBody
            {
                Name = "Ball" + i,
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
        try
        {
            PhysicsServer.AreaSetGravity(world.Space, 980);
            PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
            PhysicsServer.SpaceSetSolverIterations(world.Space, 16);
            Check(world.PhysicsBackend == backend && world.RequestedPhysicsBackend == backend, "Full scene pipeline uses the explicit requested backend");
            var data = PhysicsServer.Service.GetSceneSpace(world.Space); var gpu = data.GPUStore;
            var publicationControl = Environment.GetEnvironmentVariable("ELECTRON2D_PHYSICS_PERFORMANCE_PUBLICATION_CONTROL") == "1";
            data.ForceGPUStatePublication = publicationControl;
            if (gpu is not null) gpu.ForceCountFirstChangeReadback = publicationControl;
            var times = new double[samples]; var waits = new double[samples]; var phases = new double[8]; var phaseBytes = new long[8];
            for (var i = 0; i < warmup; i++) tree.PhysicsFrame(delta);
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: false); GC.WaitForPendingFinalizers();
            var upload = gpu?.UploadBytes ?? 0; var readback = gpu?.ReadbackBytes ?? 0;
            var submissions = gpu?.SubmissionCount ?? 0; var uniform = gpu?.UniformBytes ?? 0;
            var tick = PhysicsServer.SpaceGetTick(world.Space);
            var total = GC.GetTotalAllocatedBytes(true); var allocated = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < samples; i++)
            {
                var wait = gpu?.WaitMS ?? 0; var start = Stopwatch.GetTimestamp();
                tree.PhysicsFrame(delta);
                times[i] = Stopwatch.GetElapsedTime(start).TotalMilliseconds;
                waits[i] = (gpu?.WaitMS ?? 0) - wait;
                for (var phase = 0; phase < phases.Length; phase++) { phases[phase] += data.ProfileMS[phase]; phaseBytes[phase] += data.ProfileBytes[phase]; }
            }
            allocated = GC.GetAllocatedBytesForCurrentThread() - allocated; total = GC.GetTotalAllocatedBytes(true) - total;
            upload = (gpu?.UploadBytes ?? 0) - upload; readback = (gpu?.ReadbackBytes ?? 0) - readback;
            submissions = (gpu?.SubmissionCount ?? 0) - submissions; uniform = (gpu?.UniformBytes ?? 0) - uniform;
            Check(PhysicsServer.SpaceGetTick(world.Space) == tick + (ulong)samples, "Each complete scene frame advances exactly one fixed physical tick");
            Check(PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.ActiveObjects) == count, "Every requested rigid body remains awake and simulated");
            var states = new float[count * 7]; var positions = new Vector2[count];
            var grid = new Dictionary<(int X, int Y), List<int>>();
            var reportedContacts = 0; double maxEscape = 0, maxPenetration = 0, maxSpeed = 0, energy = 0;
            double maxBasisDifference = 0;
            var contactValues = new List<float>(count * 8 * 16);
            for (var index = 0; index < bodies.Length; index++)
            {
                var body = bodies[index]; var pose = body.GlobalTransform;
                var velocity = body.LinearVelocity; var angular = body.AngularVelocity;
                Check(pose.IsFinite() && velocity.IsFinite() && float.IsFinite(angular) && !body.Sleeping, "Full published scene state remains finite and awake");
                var state = PhysicsServer.BodyGetDirectState(body.GetRID())!;
                var directPose = state.Transform;
                var basisDifference = Math.Max((pose.X - directPose.X).Length(), (pose.Y - directPose.Y).Length());
                maxBasisDifference = Math.Max(maxBasisDifference, basisDifference);
                Check(pose.Origin == directPose.Origin && basisDifference <= 1e-6 && velocity == state.LinearVelocity && angular == state.AngularVelocity,
                    "Scene and direct-state publication agree; unit basis allows float angle reconstruction roundoff <=1e-6");
                states[index * 7] = pose.Origin.X; states[index * 7 + 1] = pose.Origin.Y;
                states[index * 7 + 2] = pose.X.X; states[index * 7 + 3] = pose.X.Y;
                states[index * 7 + 4] = velocity.X; states[index * 7 + 5] = velocity.Y; states[index * 7 + 6] = angular;
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
                var contacts = state.GetContactCount(); reportedContacts += contacts;
                Check(contacts == body.GetContactCount() && contacts <= 8, "Real monitor and direct-state contacts agree within the unchanged cap");
                contactValues.Add(contacts);
                for (var i = 0; i < contacts; i++)
                {
                    Check(state.GetContactCollider(i).IsValid() && state.GetContactColliderObject(i) is PhysicsBody,
                        "Completed contacts preserve actual scene collider identity");
                    contactValues.Add(state.GetContactLocalShape(i)); contactValues.Add(state.GetContactColliderShape(i));
                    Append(state.GetContactLocalPosition(i)); Append(state.GetContactColliderPosition(i));
                    Append(state.GetContactLocalNormal(i)); Append(state.GetContactLocalVelocityAtPosition(i));
                    Append(state.GetContactColliderVelocityAtPosition(i)); Append(state.GetContactImpulse(i));
                }
            }
            var penetrationLimit = 2 * PhysicsServer.SpaceGetContactMaxAllowedPenetration(world.Space) + .001f;
            var physicalValid = maxEscape <= penetrationLimit && maxPenetration <= penetrationLimit && maxSpeed < 1000 &&
                double.IsFinite(energy) && energy < initialEnergy * 1.02 && entered > 0 && reportedContacts > 0;
            var valid = physicalValid && allocated == 0 && total == 0;
            var report = JsonSerializer.Serialize(new
            {
                schema = 2,
                backend = backend.ToString(),
                bodies = count,
                walls = 4,
                hz = 144,
                substeps = 4,
                solverIterations = 16,
                contactMonitor = true,
                maxContacts = 8,
                interpolation = true,
                sleeping = false,
                rendered = false,
                publicationControl,
                warmup,
                samples,
                meanMS = times.Average(),
                step = Distribution(times),
                wait = Distribution(waits),
                phaseMeanMS = phases.Select(value => value / samples).ToArray(),
                uploadBytesPerTick = (double)upload / samples,
                readbackBytesPerTick = (double)readback / samples,
                uniformBytesPerTick = (double)uniform / samples,
                submissionsPerTick = (double)submissions / samples,
                finalContactColors = gpu?.ContactColorCount,
                finalContactColorRounds = gpu?.ContactColorRounds,
                contactColorFallbacks = gpu?.ContactColorFallbacks,
                ownerBytes = allocated,
                allThreadsBytes = total,
                phaseOwnerBytes = phaseBytes,
                outsideStepOwnerBytes = allocated - phaseBytes.Sum(),
                entered,
                exited,
                eventHash,
                reportedContacts,
                maxEscape,
                maxPenetration,
                penetrationLimit,
                maxSpeed,
                maxBasisDifference,
                energy,
                initialEnergy,
                physicalValid,
                valid,
                contactSHA256 = Convert.ToHexString(SHA256.HashData(MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(contactValues)))),
                stateSHA256 = Convert.ToHexString(SHA256.HashData(MemoryMarshal.AsBytes(states.AsSpan())))
            });
            Console.WriteLine(report);
            if (Environment.GetEnvironmentVariable("ELECTRON2D_PHYSICS_PERFORMANCE_OUTPUT") is { } output)
            {
                Directory.CreateDirectory(output); File.WriteAllText(System.IO.Path.Combine(output, backend + ".json"), report + Environment.NewLine);
            }
            Check(valid, $"Scene pipeline gates: managed {allocated}/{total} B, escape/penetration {maxEscape}/{maxPenetration} <= {penetrationLimit}, speed {maxSpeed}, energy {energy}/{initialEnergy}");
            void Append(Vector2 value) { Check(value.IsFinite(), "Reported contact values are finite"); contactValues.Add(value.X); contactValues.Add(value.Y); }
        }
        finally { tree.Dispose(); foreach (var wall in walls) wall.Dispose(); }
    }

    private static object Distribution(double[] values)
    {
        Array.Sort(values);
        return new
        {
            p50 = values[values.Length / 2],
            p95 = values[Math.Min(values.Length - 1, (int)(values.Length * .95))],
            p99 = values[Math.Min(values.Length - 1, (int)(values.Length * .99))],
            max = values[^1]
        };
    }
}
