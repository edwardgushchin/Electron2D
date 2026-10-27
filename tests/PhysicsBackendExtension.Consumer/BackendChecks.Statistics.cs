using Electron2D;

public static partial class BackendChecks
{
    public static void RunStatistics(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { Verify(); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }

        void Verify()
        {
            const string name = "StatisticsSolver";
            if (backend is null) PhysicsServerManager.RegisterServer(name, () => new Solver());
            using var world = backend is { } builtIn ? new World(builtIn) : new World(name);
            using var root = new SubViewport { World = world };
            using var circle = new CircleShape { Radius = 2 };
            using var sensorShape = new CircleShape { Radius = 50 };
            var scene = new RigidBody { Name = "StatisticsScene", Position = new(-20, -4), GravityScale = 0, CanSleep = false };
            scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
            using var tree = new SceneTree(root);
            var raw = PhysicsServer.BodyCreate(); var floor = PhysicsServer.BodyCreate(); var kinematic = PhysicsServer.BodyCreate();
            var area = PhysicsServer.AreaCreate(); var joint = PhysicsServer.JointCreate();
            try
            {
                PhysicsServer.AreaSetGravity(world.Space, 0);
                foreach (var selector in new[] { -1, 3, int.MinValue, int.MaxValue })
                    Reject<ArgumentOutOfRangeException>(() => PhysicsServer.GetProcessInfo((PhysicsServer.ProcessInfo)selector));
                Check(Performance.GetMonitor(Performance.Monitor.RenderBufferMemUsed) == 0, "Headless statistics do not initialize a renderer.");
                Reject<ArgumentOutOfRangeException>(() => Performance.GetMonitor((Performance.Monitor)20));
                PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetGravityScale(raw, 0);
                PhysicsServer.BodySetCanSleep(raw, false); PhysicsServer.BodySetTransform(raw, new(0, new Vector2(20, -4)));
                PhysicsServer.BodySetSpace(raw, world.Space);
                Step(); Counts(2, 0, "Unconstrained dynamic bodies have no islands");
                PhysicsServer.BodySetMode(kinematic, PhysicsServer.BodyMode.Kinematic);
                PhysicsServer.BodySetCanSleep(kinematic, false);
                PhysicsServer.BodySetTransform(kinematic, new(0, new Vector2(500, 0))); PhysicsServer.BodySetSpace(kinematic, world.Space);
                Step(); Counts(3, 0, "Kinematic activity does not create a dynamic island");
                PhysicsServer.BodySetMode(floor, PhysicsServer.BodyMode.Static);
                PhysicsServer.BodyAddShape(floor, circle.GetRID(), new(0, new Vector2(-20, 0)));
                PhysicsServer.BodyAddShape(floor, circle.GetRID(), new(0, new Vector2(20, 0))); PhysicsServer.BodySetSpace(floor, world.Space);
                scene.GravityScale = 1; PhysicsServer.BodySetGravityScale(raw, 1); PhysicsServer.AreaSetGravity(world.Space, 10);
                for (var i = 0; i < 90; i++) Step();
                Counts(3, 2, "Shared static support keeps two independent dynamic groups");
                Task.Run(() => Counts(3, 2, "Another thread reads the same completed sample")).GetAwaiter().GetResult();
                var physicalPairs = PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.CollisionPairs);
                Check(physicalPairs >= 2, "Actual floor contacts participate in candidate diagnostics");
                PhysicsServer.AreaAddShape(area, sensorShape.GetRID());
                PhysicsServer.AreaSetSpace(area, world.Space); Step(); Counts(3, 2, "Sensor overlap does not join islands");
                Check(PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.CollisionPairs) > physicalPairs,
                    "Actual sensor pairs participate in candidate diagnostics");
                PhysicsServer.JointMakePin(joint, new(0, -4), scene.GetRID(), raw); Step();
                Counts(3, 1, "Pin constraints join dynamic groups");
                PhysicsServer.BodySetCanSleep(scene.GetRID(), true); PhysicsServer.BodySetCanSleep(raw, true);
                PhysicsServer.BodySetSleeping(scene.GetRID(), true); PhysicsServer.BodySetSleeping(raw, true); Step();
                Counts(1, 0, "Sleeping dynamic groups leave only kinematic activity");
                PhysicsServer.BodyApplyCentralImpulse(raw, new(1, 0)); Step(); Counts(3, 1, "Joint wake restores one active group");
                PhysicsServer.BodySetCanSleep(scene.GetRID(), false); PhysicsServer.BodySetCanSleep(raw, false);
                PhysicsServer.SpaceSetActive(world.Space, false); Counts(0, 0, "Inactive worlds are excluded");
                tree.PhysicsFrame(1d / 60); Counts(0, 0, "Inactive frame retains exclusion");
                PhysicsServer.SpaceSetActive(world.Space, true); Counts(3, 1, "Activation retains the last completed sample");
                using (var history = PhysicsServer.SpaceCreateCheckpoint(world.Space))
                {
                    history.Capture();
                    PhysicsServer.BodySetSleeping(scene.GetRID(), true); PhysicsServer.BodySetSleeping(raw, true); Step();
                    Counts(1, 0, "Changed sleep publishes a new sample");
                    history.Restore(); Counts(3, 1, "Private replay restores diagnostic publication");
                }
                for (var i = 0; i < 128; i++) Frame();
                Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame();
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(owner == 0 && all == 0, $"Full moving statistics frames allocate {owner}/{all} B");
                var secondSpace = backend is { } selected ? PhysicsServer.SpaceCreate(selected) : PhysicsServer.SpaceCreate(name);
                var secondBody = PhysicsServer.BodyCreate();
                try
                {
                    PhysicsServer.BodySetMode(secondBody, PhysicsServer.BodyMode.Kinematic);
                    PhysicsServer.BodySetCanSleep(secondBody, false);
                    PhysicsServer.BodySetSpace(secondBody, secondSpace);
                    PhysicsServer.SpaceSetActive(secondSpace, true);
                    Counts(3, 1, "A new world contributes no unfinished sample");
                    PhysicsServer.SpaceStep(secondSpace, 1d / 60);
                    Counts(4, 1, "Scene and owned server worlds contribute independent completed samples");
                    PhysicsServer.SpaceSetActive(secondSpace, false);
                    Counts(3, 1, "Local inactivity excludes only its own world's sample");
                    PhysicsServer.SpaceSetActive(secondSpace, true);
                    PhysicsServer.SetActive(false);
                    try
                    {
                        PhysicsServer.SpaceStep(secondSpace, 1d / 60); Step();
                        Counts(4, 1, "Global suspension retains both completed samples");
                    }
                    finally { PhysicsServer.SetActive(true); }
                    PhysicsServer.FreeRID(secondSpace); secondSpace = default;
                    Counts(3, 1, "Retirement removes only the retired world's contribution");
                }
                finally
                {
                    PhysicsServer.FreeRID(secondBody);
                    if (secondSpace.IsValid()) PhysicsServer.FreeRID(secondSpace);
                }
                Console.WriteLine($"Statistics {world.PhysicsBackendName}, threaded={threaded}: contacts/sensors/kinematic/joint/sleep/history, 128 warm/128 measured full frames {owner}/{all} B passed.");

                void Frame() { PhysicsServer.BodyApplyCentralImpulse(raw, new(.001f, 0)); Step(); Counts(3, 1, "Measured complete frame retains actual groups"); }
                void Step() => tree.PhysicsFrame(1d / 60);
                static void Counts(int active, int islands, string message)
                {
                    var actualActive = PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.ActiveObjects);
                    var actualIslands = PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.IslandCount);
                    if (actualActive != active || actualIslands != islands ||
                        Performance.GetMonitor(Performance.Monitor.PhysicsActiveObjects) != active ||
                        Performance.GetMonitor(Performance.Monitor.PhysicsIslandCount) != islands ||
                        Performance.GetMonitor(Performance.Monitor.PhysicsCollisionPairs) != PhysicsServer.GetProcessInfo(PhysicsServer.ProcessInfo.CollisionPairs))
                        throw new InvalidOperationException($"{message}: expected {active}/{islands}, got {actualActive}/{actualIslands}.");
                }
            }
            finally
            {
                PhysicsServer.FreeRID(joint); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(kinematic);
                PhysicsServer.FreeRID(floor); PhysicsServer.FreeRID(raw);
            }
        }
    }
}
