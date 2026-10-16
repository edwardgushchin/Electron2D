using Electron2D;

public static partial class BackendChecks
{
    public static void RunProcessModes(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("ProcessModes", () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("ProcessModes");
            var log = new List<(string Name, int What)>();
            using (var detached = new ModeProbe { Name = "Detached", Signal = Record })
            {
                Check(!detached.CanProcess() && detached.ProcessMode == ProcessMode.Inherit, "Detached processing defaults");
                detached.ProcessMode = ProcessMode.Disabled; detached.ProcessMode = ProcessMode.Always;
                Check(log.Count == 0 && !detached.CanProcess(), "Detached mode changes never deliver tree transition notifications");
            }
            Check(Enum.GetValues<ProcessMode>().Select(mode => (int)mode).SequenceEqual(new[] { 0, 1, 2, 3, 4 }), "Stable complete mode identities");
            using (var source = new Node())
            using (var packed = new PackedScene())
                foreach (var mode in Enum.GetValues<ProcessMode>())
                {
                    source.ProcessMode = mode; packed.Pack(source); using var copy = packed.Instantiate();
                    Check(copy.ProcessMode == mode && !copy.CanProcess(), "Every mode is stored and reconstructed while detached");
                }
            var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
            Check(root.ProcessMode == ProcessMode.Pausable, "An entering root replaces Inherit with Pausable");
            Reject<InvalidOperationException>(() => root.ProcessMode = ProcessMode.Inherit);
            Check(root.ProcessMode == ProcessMode.Pausable, "Invalid root inheritance preserves policy");
            var parent = new ModeProbe { Name = "Parent", Signal = Record }; root.AddChild(parent);
            var child = new ModeProbe { Name = "Child", Signal = Record }; parent.AddChild(child, internalMode: Node.InternalMode.Front);
            var independent = new ModeProbe { Name = "Independent", ProcessMode = ProcessMode.Always, Signal = Record }; parent.AddChild(independent);
            var independentChild = new ModeProbe { Name = "IndependentChild", Signal = Record }; independent.AddChild(independentChild);
            foreach (var paused in new[] { false, true })
            {
                tree.Paused = paused;
                foreach (var from in Enum.GetValues<ProcessMode>())
                    foreach (var to in Enum.GetValues<ProcessMode>())
                    {
                        parent.ProcessMode = from; log.Clear(); parent.ProcessMode = to;
                        var expected = new List<(string Name, int What)>();
                        if (from != to)
                            foreach (var name in new[] { "Parent", "Child" })
                            {
                                var before = ModeEligible(from, paused); var after = ModeEligible(to, paused);
                                if (before != after) expected.Add((name, after ? Node.NotificationUnpaused : Node.NotificationPaused));
                                if ((from == ProcessMode.Disabled) != (to == ProcessMode.Disabled))
                                    expected.Add((name, to == ProcessMode.Disabled ? Node.NotificationDisabled : Node.NotificationEnabled));
                            }
                        Check(log.SequenceEqual(expected), $"Complete inherited mode transition {from}->{to}, paused={paused}: {string.Join(',', log)}");
                        Check(parent.CanProcess() == ModeEligible(to, paused) && child.CanProcess() == parent.CanProcess() && independentChild.CanProcess(),
                            "Effective eligibility follows inheritance and stops at explicit owners");
                    }
            }
            parent.ProcessMode = ProcessMode.Inherit; tree.Paused = false;
            var branches = Enum.GetValues<ProcessMode>().Select(mode => new ModeProbe { Name = mode.ToString(), ProcessMode = mode, Signal = Record }).ToArray();
            foreach (var node in branches) root.AddChild(node);
            log.Clear(); tree.Paused = true;
            Check(log.SequenceEqual(new[] { ("Parent", Node.NotificationPaused), ("Child", Node.NotificationPaused), ("Inherit", Node.NotificationPaused), ("Pausable", Node.NotificationPaused), ("WhenPaused", Node.NotificationUnpaused) }),
                "Tree pause only reports actual transitions, including reversed WhenPaused and silent Always/Disabled");
            log.Clear(); tree.Paused = true; Check(log.Count == 0, "Repeated pause is silent");
            tree.Paused = false; foreach (var node in branches) node.Dispose();
            parent.Signal = (_, what) => { Record("Parent", what); if (what == Node.NotificationPaused) parent.ProcessMode = ProcessMode.Always; };
            log.Clear(); parent.ProcessMode = ProcessMode.WhenPaused;
            Check(parent.ProcessMode == ProcessMode.Always && child.CanProcess() &&
                log.SequenceEqual(new[] { ("Parent", Node.NotificationPaused), ("Parent", Node.NotificationUnpaused), ("Child", Node.NotificationUnpaused) }),
                "Nested mode changes supersede the stale outer descendant transition");
            parent.Signal = Record; parent.ProcessMode = ProcessMode.Disabled;
            var failure = new InvalidOperationException("Expected mode callback failure");
            parent.Signal = (name, what) => { Record(name, what); if (what == Node.NotificationUnpaused) throw failure; };
            log.Clear(); var error = CaptureCopy(() => parent.ProcessMode = ProcessMode.Always);
            Check(error is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(failure) &&
                log.SequenceEqual(new[] { ("Parent", Node.NotificationUnpaused), ("Parent", Node.NotificationEnabled), ("Child", Node.NotificationUnpaused), ("Child", Node.NotificationEnabled) }),
                "A failed pause handler preserves committed mode and attempts enabled and descendant delivery");
            parent.Signal = null; parent.ProcessMode = ProcessMode.Disabled;
            parent.Signal = (name, what) => { Record(name, what); if (what == Node.NotificationUnpaused) tree.Paused = true; };
            log.Clear(); parent.ProcessMode = ProcessMode.Pausable;
            Check(tree.Paused && !child.CanProcess() && log.Contains(("Parent", Node.NotificationEnabled)) && log.Contains(("Child", Node.NotificationEnabled)) &&
                log.Count(item => item == ("Child", Node.NotificationPaused)) == 1 && !log.Contains(("Child", Node.NotificationUnpaused)),
                "A nested tree pause supersedes stale eligibility without dropping enabled delivery");
            parent.Signal = null; tree.Paused = false; parent.ProcessMode = ProcessMode.Always;
            parent.Signal = null; child.Signal = null; independent.Signal = null; independentChild.Signal = null;
            Reject<ArgumentOutOfRangeException>(() => parent.ProcessMode = (ProcessMode)5);
            Reject<ArgumentOutOfRangeException>(() => parent.ProcessMode = (ProcessMode)(-1));
            var retired = new Node(); retired.Dispose(); Reject<ObjectDisposedException>(() => retired.CanProcess());
            Reject<ObjectDisposedException>(() => _ = retired.ProcessMode); Reject<ObjectDisposedException>(() => retired.ProcessMode = ProcessMode.Always);
            Exception? foreignError = null;
            var foreign = new Thread(() =>
            {
                try
                {
                    Reject<InvalidOperationException>(() => parent.CanProcess()); Reject<InvalidOperationException>(() => _ = parent.ProcessMode);
                    Reject<InvalidOperationException>(() => parent.ProcessMode = ProcessMode.Pausable);
                }
                catch (Exception e) { foreignError = e; }
            });
            foreign.Start(); foreign.Join(); if (foreignError is not null) throw foreignError;
            var worker = new GroupProbe { ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread }; parent.AddChild(worker);
            var owner = Environment.CurrentManagedThreadId; var ran = false;
            worker.Signal = (_, _) =>
            {
                Check(Environment.CurrentManagedThreadId != owner && worker.ProcessMode == ProcessMode.Inherit && worker.CanProcess(), "A worker resolves inherited policy without reading foreign public metadata");
                Reject<InvalidOperationException>(() => parent.CanProcess()); Reject<InvalidOperationException>(() => _ = parent.ProcessMode);
                Reject<InvalidOperationException>(() => worker.ProcessMode = ProcessMode.Disabled); ran = true;
            };
            worker.ProcessEnabled = true; tree.ProcessFrame(.01); Check(ran, "Actual worker access was exercised"); worker.Dispose();
            using var disk = new CircleShape { Radius = 1 };
            var mover = new RigidBody
            {
                GravityScale = 0,
                CanSleep = false,
                LinearVelocity = new(6, 0),
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace,
                DisableMode = CollisionDisableMode.KeepActive
            };
            mover.AddChild(new CollisionShape { Shape = disk }); parent.AddChild(mover);
            mover.DisableMode = CollisionDisableMode.Remove; parent.ProcessMode = ProcessMode.Disabled;
            Check(PhysicsServer.BodyGetDirectState(mover.GetRID()) is null, "Disabled inherited body leaves simulation");
            parent.Signal = (_, what) => { if (what == Node.NotificationUnpaused) mover.ProcessMode = ProcessMode.Always; };
            parent.ProcessMode = ProcessMode.Always; parent.Signal = null;
            using (var query = new PhysicsPointQueryParameters())
            {
                var hits = new PhysicsPointResult[2];
                Check(PhysicsServer.BodyGetDirectState(mover.GetRID()) is not null && world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == mover.GetRID(),
                    "An explicit child mode selected in the parent callback cannot consume pending physical Enabled delivery");
            }
            mover.ProcessMode = ProcessMode.Inherit; mover.DisableMode = CollisionDisableMode.KeepActive;
            var callbacks = 0; PhysicsServer.BodySetForceIntegrationCallback(mover.GetRID(), _ => callbacks++);
            var always = new GroupProbe { ProcessMode = ProcessMode.Always }; var whenPaused = new GroupProbe { ProcessMode = ProcessMode.WhenPaused };
            root.AddChild(always); root.AddChild(whenPaused); var alwaysTicks = 0; var pausedTicks = 0;
            always.Signal = (_, physics) => { if (physics) alwaysTicks++; }; whenPaused.Signal = (_, physics) => { if (physics) pausedTicks++; };
            always.PhysicsProcessEnabled = whenPaused.PhysicsProcessEnabled = true;
            tree.PhysicsFrame(.01); var x = mover.Position.X; var count = callbacks; var ticks = alwaysTicks;
            tree.Paused = true; for (var i = 0; i < 8; i++) tree.PhysicsFrame(.01);
            Check(mover.Position.X == x && callbacks == count && alwaysTicks == ticks + 8 && pausedTicks == 8,
                "Paused tree stops integration while Always/WhenPaused callbacks keep running");
            var rawSpace = backend is { } rawBackend ? PhysicsServer.SpaceCreate(rawBackend) : PhysicsServer.SpaceCreate("ProcessModes");
            var rawBody = PhysicsServer.BodyCreate();
            try
            {
                PhysicsServer.SpaceSetActive(rawSpace, true);
                PhysicsServer.BodySetMode(rawBody, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodyAddShape(rawBody, disk.GetRID());
                PhysicsServer.BodySetSpace(rawBody, rawSpace); PhysicsServer.BodySetLinearVelocity(rawBody, new(6, 0));
                PhysicsServer.SpaceStep(rawSpace, .01);
                Check(PhysicsServer.BodyGetTransform(rawBody).Origin.X > .05f && mover.Position.X == x,
                    "Explicit owned-space steps remain independent of the paused scene tree");
            }
            finally { PhysicsServer.FreeRID(rawBody); PhysicsServer.FreeRID(rawSpace); }
            tree.Paused = false; tree.PhysicsFrame(.01);
            Check(Math.Abs(mover.Position.X - x - .06f) < .0001f && callbacks == count + 1, "Unpause resumes the same body and callback");
            var rid = mover.GetRID(); for (var i = 0; i < 128; i++) Frame();
            Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); x = mover.Position.X; count = callbacks;
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && mover.GetRID() == rid && Math.Abs(mover.Position.X - x - 7.68f) < .01f && callbacks == count + 128,
                $"Warmed mode/pause/full-frame contract allocated {own}/{all} owner/all-thread B");
            Console.WriteLine($"Process modes {backend?.ToString() ?? "registered"}, threaded={threaded}: 50 transitions, pause notifications, nested/failing callbacks, worker access and 128 moving/paused full frames; {own}/{all} B.");
            void Frame()
            {
                parent.ProcessMode = ProcessMode.Disabled; parent.ProcessMode = ProcessMode.Always;
                tree.Paused = true; tree.PhysicsFrame(.01); tree.Paused = false; tree.PhysicsFrame(.01);
            }
            void Record(string name, int what) { if (what is Node.NotificationPaused or Node.NotificationUnpaused or Node.NotificationDisabled or Node.NotificationEnabled) log.Add((name, what)); }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }

    private static bool ModeEligible(ProcessMode mode, bool paused) => mode switch
    {
        ProcessMode.Always => true,
        ProcessMode.Disabled => false,
        ProcessMode.WhenPaused => paused,
        _ => !paused
    };
    private sealed class ModeProbe : Node
    {
        public Action<string, int>? Signal { get; set; }
        protected override void OnNotification(int what) { base.OnNotification(what); Signal?.Invoke(Name, what); }
    }
}
