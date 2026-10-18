using Electron2D;

public static partial class BackendChecks
{
    private sealed class GroupBoneProbe : PhysicalBone
    {
        internal Vector2 BeforeSolver;
        internal int Calls, ThreadID;
        internal Action? Before;
        internal bool Reconfigure;
        protected override void OnNotification(int what)
        {
            if (what != NotificationInternalPhysicsProcess) { base.OnNotification(what); return; }
            Before?.Invoke();
            if (Reconfigure) Notify(NotificationChildOrderChanged);
            base.OnNotification(what);
            BeforeSolver = GlobalPosition; Calls++; ThreadID = Environment.CurrentManagedThreadId;
        }
        protected override Func<Node> CreateSceneInstanceFactory() => CreateGroupBone;
        private static Node CreateGroupBone() => new GroupBoneProbe();
    }
    private sealed class GroupBoneScene
    {
        internal readonly Skeleton Rig = new();
        internal readonly Bone A = new() { Name = "A", Rest = Transform.Identity };
        internal readonly Bone B = new() { Name = "B", Position = new(40, 0), Rest = new(0, new(40, 0)) };
        internal readonly GroupBoneProbe Parent, Child;
        internal readonly PinJoint Joint = new() { Name = "Joint", NodeA = "../..", NodeB = ".." };
        internal GroupBoneScene(Node root, Shape shape, bool worker, bool automatic, bool initiallyMissing)
        {
            if (initiallyMissing) A.Name = "Missing";
            A.SetAutocalculateLengthAndAngle(false); B.SetAutocalculateLengthAndAngle(false);
            Parent = new()
            {
                Name = "P",
                BoneNodePath = "../A",
                SimulatePhysics = true,
                FollowBoneWhenSimulating = true,
                GravityScale = 0,
                CanSleep = false,
                CollisionMask = 0,
                AutoConfigureJoint = automatic,
                Reconfigure = automatic,
                ProcessThreadGroup = worker ? Node.ProcessThreadGroupMode.SubThread : Node.ProcessThreadGroupMode.MainThread,
                ProcessThreadGroupOrder = -1
            };
            Child = new()
            {
                Name = "Q",
                BoneNodePath = "../../A/B",
                SimulatePhysics = true,
                FollowBoneWhenSimulating = true,
                GravityScale = 0,
                CanSleep = false,
                CollisionMask = 0,
                AutoConfigureJoint = automatic,
                Reconfigure = automatic
            };
            A.AddChild(B); Rig.AddChild(A); Parent.AddChild(new CollisionShape { Shape = shape }); Child.AddChild(new CollisionShape { Shape = shape });
            Child.AddChild(Joint); Parent.AddChild(Child); Rig.AddChild(Parent); root.AddChild(Rig);
            Own(Rig);
            void Own(Node parent) { for (var i = 0; i < parent.GetChildCount(); i++) { var child = parent.GetChild(i); child.Owner = Rig; Own(child); } }
        }
        internal void Aligned()
        {
            Check(Parent.BeforeSolver.DistanceTo(A.GlobalPosition) < .001f && Child.BeforeSolver.DistanceTo(B.GlobalPosition) < .001f,
                "Physical bodies follow their bones before the numerical interval");
        }
    }
    public static void RunGroupBones(PhysicsServer.Backend? backend, bool threaded, bool poolOwner = false)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("GroupBones", () => solver = new());
        try
        {
            if (poolOwner) { var job = WorkerThreadPool.AddTask(Run); WorkerThreadPool.WaitForTaskCompletion(job); } else Run();
        }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver); }
        World Create() => backend is { } choice ? new World(choice) : new World("GroupBones");
        void Run()
        {
            var cases = 0;
            foreach (var binding in new[] { "valid", "lost", "repaired" }) foreach (var automatic in new[] { false, true }) foreach (var worker in new[] { false, true })
                    {
                        using var world = Create(); using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 2 };
                        var scene = new GroupBoneScene(root, shape, worker, automatic, binding == "repaired"); using var tree = new SceneTree(root);
                        var parent = scene.Parent.GetRID(); var child = scene.Child.GetRID(); var joint = scene.Joint.GetRID(); var owner = Environment.CurrentManagedThreadId;
                        Check(scene.Parent.IsSimulatingPhysics() == (binding != "repaired"), "The initial actual simulation matches the binding");
                        if (binding == "lost") scene.A.Name = "Missing"; else if (binding == "repaired") scene.A.Name = "A";
                        scene.A.Position = new(24, 10); tree.PhysicsFrame(.01);
                        if (binding != "lost") scene.Aligned();
                        Check(scene.Parent.Calls == 1 && scene.Child.Calls == 1 && (worker ? poolOwner || scene.Parent.ThreadID != owner : scene.Parent.ThreadID == owner),
                            "The real scheduled notification runs on its configured group");
                        using var point = new PhysicsPointQueryParameters { Position = scene.Parent.GlobalPosition }; var hits = new PhysicsPointResult[2];
                        var count = world.DirectSpaceState.IntersectPoint(point, hits); var found = false; for (var i = 0; i < count; i++) found |= hits[i].ColliderRID == parent;
                        Check(found == (binding != "lost") && scene.Parent.IsSimulatingPhysics() == found &&
                            PhysicsServer.BodyGetMode(parent) == (found ? PhysicsServer.BodyMode.Rigid : PhysicsServer.BodyMode.Static), "Binding repair/loss publishes actual mode and query filters");
                        Check(scene.Parent.GetRID() == parent && scene.Child.GetRID() == child && scene.Joint.GetRID() == joint && PhysicsServer.JointGetType(joint) == PhysicsServer.JointType.Pin &&
                            world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null && PhysicsServer.SpaceGetTick(world.Space) == 1,
                            "Both selected backends retain actual body/joint identities and one completed interval"); cases++;
                    }
            Propagated(); Parallel();
            SetupFailure();
            if (backend is null)
            {
                foreach (var repaired in new[] { false, true }) for (var hook = 0; hook < 3; hook++) for (var route = 0; route < 4; route++) Failure(repaired, hook, route);
                TerminalFailure();
            }
            Console.WriteLine($"Group bones {backend?.ToString() ?? "registered"}/{threaded}/{poolOwner}: {cases} binding/joint cases, propagated notification, parallel setup and 128 moving full frames 0/0 B.");
        }
        void Propagated()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 2 };
            var scene = new GroupBoneScene(root, shape, true, true, true);
            scene.Parent.InternalPhysicsProcessEnabled = scene.Child.InternalPhysicsProcessEnabled = false; scene.Parent.ProcessThreadMessages = Node.ProcessThreadMessageFlags.Physics;
            using var tree = new SceneTree(root); scene.A.Name = "A"; scene.A.Position = new(24, 10);
            scene.Parent.CallDeferredThreadGroup<PhysicalBone>(static node => node.PropagateNotification(Node.NotificationInternalPhysicsProcess));
            tree.PhysicsFrame(.01); scene.Aligned();
            Check(scene.Parent.Calls == 1 && scene.Child.Calls == 1 && scene.Parent.IsSimulatingPhysics() && scene.Child.IsSimulatingPhysics(),
                "A propagated reserved notification runs once in the owning group and publishes activation");
        }
        void Parallel()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 2 };
            var scene = new GroupBoneScene(root, shape, true, true, false); scene.Parent.Reconfigure = scene.Child.Reconfigure = false;
            var other = new GroupBoneProbe
            {
                BoneNodePath = "../A",
                SimulatePhysics = true,
                FollowBoneWhenSimulating = true,
                AutoConfigureJoint = false,
                GravityScale = 0,
                CanSleep = false,
                CollisionMask = 0,
                ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread,
                ProcessThreadGroupOrder = -1
            };
            other.AddChild(new CollisionShape { Shape = shape }); scene.Rig.AddChild(other);
            using var tree = new SceneTree(root); using var together = new Barrier(2); using var read = new ManualResetEventSlim();
            var owner = Environment.CurrentManagedThreadId; var setups = 0;
            scene.Rig.BoneSetupChanged += Setup;
            scene.Rig.Notify(Node.NotificationChildOrderChanged);
            scene.Parent.Before = () => Cold(scene.Parent, other); other.Before = () => Cold(other, scene.Parent);
            tree.PhysicsFrame(.01); Check(setups == 1 && scene.Parent.ThreadID != other.ThreadID, "Two actual worker groups share one committed cold bone setup");
            scene.Parent.Before = other.Before = null; scene.Rig.BoneSetupChanged -= Setup;
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[4]; var phase = 0;
            for (var i = 0; i < 128; i++) Frame(); Collect(); for (var i = 0; i < 128; i++) Frame();
            var tick = PhysicsServer.SpaceGetTick(world.Space); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128, $"Moving grouped bone frames allocated {own}/{all} B");
            Task.Run(() => { Reject<InvalidOperationException>(() => scene.Rig.GetBoneCount()); Reject<InvalidOperationException>(() => scene.A.GetLength()); Reject<InvalidOperationException>(() => scene.Parent.GetJoint()); }).GetAwaiter().GetResult();
            using (var packed = new PackedScene()) { packed.Pack(scene.Rig); using var copy = packed.Instantiate(); Check(copy.GetNode<PhysicalBone>("P").BoneNodePath == "../A", "Packing retains binding authoring after grouped execution"); }
            void Setup()
            {
                setups++; Check((poolOwner || Environment.CurrentManagedThreadId != owner) && scene.Rig.GetBone(0) == scene.A && scene.B.GetIndexInSkeleton() == 1,
                    "Cold setup callbacks observe the committed list and indices");
                Check(read.Wait(TimeSpan.FromSeconds(5)), "Another active group can read setup while its subscriber runs");
            }
            void Cold(GroupBoneProbe current, GroupBoneProbe foreign)
            {
                Check(together.SignalAndWait(TimeSpan.FromSeconds(5)), "Cold logical bone readers actually overlap");
                Check(scene.Rig.GetBoneCount() == 2 && scene.A.Rest == Transform.Identity && scene.B.GetLength() == 16 && current.BoneIndex == 0 && current.GetJoint() is null,
                    "Current and foreign group logical bone metadata is readable"); read.Set();
                Reject<InvalidOperationException>(() => foreign.Notify(Node.NotificationChildOrderChanged));
                Reject<InvalidOperationException>(() => scene.A.Rest = Transform.Identity);
                Reject<InvalidOperationException>(() => current.SimulatePhysics = true);
                Reject<InvalidOperationException>(() => PhysicsServer.BodyGetTransform(current.GetRID()));
                if (ReferenceEquals(current, scene.Parent)) { scene.Joint.NodeA = string.Empty; scene.Joint.NodeA = "../.."; scene.Joint.NodeB = ".."; }
                else Reject<InvalidOperationException>(() => scene.Joint.NodeA = "../..");
            }
            void Frame()
            {
                scene.A.Position = new(24 + (++phase & 1), 10); tree.PhysicsFrame(.01); scene.Aligned();
                Check(other.BeforeSolver.DistanceTo(scene.A.GlobalPosition) < .001f, "Both groups follow the shared rig");
                point.Position = scene.Parent.GlobalPosition; Check(world.DirectSpaceState.IntersectPoint(point, hits) == 2, "Both selected colliders follow the moving pose");
            }
        }
        void Failure(bool repaired, int hook, int route)
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 2 };
            var scene = new GroupBoneScene(root, shape, true, true, repaired); using var tree = new SceneTree(root);
            var rid = scene.Parent.GetRID(); var implementation = solver!; var original = new ApplicationException("group-bone-role");
            scene.A.Name = repaired ? "A" : "Missing"; scene.A.Position = new(24, 10);
            var owner = Environment.CurrentManagedThreadId;
            Action<RID> fail = id => { if (id == rid) { Check(Environment.CurrentManagedThreadId == owner, "Selected activation publishes on the owner"); Reject<InvalidOperationException>(() => scene.Parent.SimulatePhysics = true); if (hook == 1) implementation.Bodies[id].Mode = repaired ? PhysicsServer.BodyMode.Rigid : PhysicsServer.BodyMode.Static; throw original; } };
            try
            {
                if (hook < 2) implementation.ModeHook = fail; else implementation.ShapeHook = fail;
                var errors = ReleaseErrors(() => tree.PhysicsFrame(.01)); Check(errors.Any(e => ReferenceEquals(e, original)), "The original selected role error is retained");
            }
            finally { implementation.ModeHook = implementation.ShapeHook = null; }
            if (route == 0)
            {
                using var point = new PhysicsPointQueryParameters { Position = scene.Parent.GlobalPosition }; var hits = new PhysicsPointResult[2]; var count = world.DirectSpaceState.IntersectPoint(point, hits);
                Check((count != 0) == repaired && implementation.Bodies[rid].Mode == (repaired ? PhysicsServer.BodyMode.Rigid : PhysicsServer.BodyMode.Static), "A query retries the complete selected activation/filter publication");
            }
            else if (route == 1)
            {
                PhysicsServer.BodySetMode(rid, PhysicsServer.BodyMode.RigidLinear); tree.PhysicsFrame(.01);
                Check(PhysicsServer.BodyGetMode(rid) == PhysicsServer.BodyMode.RigidLinear, "A later direct mode survives old queued role work");
            }
            else if (route == 2)
            {
                PhysicsServer.BodySetSpace(rid, default); tree.PhysicsFrame(.01);
                Check(!PhysicsServer.BodyGetSpace(rid).IsValid(), "A retired physical attachment is not recreated by a pending role");
            }
            else
            {
                scene.Rig.RemoveChild(scene.Parent); scene.Rig.AddChild(scene.Parent); tree.PhysicsFrame(.01);
                Check(scene.Parent.IsSimulatingPhysics() == repaired && scene.Parent.GetRID() == rid, "Tree reentry retires old pending work and preserves logical identity");
            }
        }
        void SetupFailure()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 2 };
            var scene = new GroupBoneScene(root, shape, true, true, false); using var tree = new SceneTree(root);
            var count = 0; var original = new ApplicationException("group-bone-setup");
            scene.Rig.BoneSetupChanged += () => { count++; Check(scene.Rig.GetBoneCount() == 2 && scene.B.GetIndexInSkeleton() == 1, "A throwing setup observer sees committed bone metadata"); throw original; };
            scene.Rig.Notify(Node.NotificationChildOrderChanged); scene.A.Position = new(24, 10);
            var errors = ReleaseErrors(() => tree.PhysicsFrame(.01)); Check(errors.Any(e => ReferenceEquals(e, original)) && count == 1, "Original setup subscriber failure remains visible");
            tree.PhysicsFrame(.01); scene.Aligned(); Check(count == 1, "Committed setup is not replayed after subscriber failure");
        }
        void TerminalFailure()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 2 };
            var scene = new GroupBoneScene(root, shape, true, true, false); using var tree = new SceneTree(root);
            var original = new ApplicationException("group-bone-solver"); solver!.StepHook = () => throw original;
            try { Check(ReleaseErrors(() => tree.PhysicsFrame(.01)).Any(e => ReferenceEquals(e, original)), "The selected numerical failure remains visible"); }
            finally { solver.StepHook = null; }
            var calls = scene.Parent.Calls; var position = scene.Parent.GlobalPosition; scene.A.Position = new(24, 10);
            Check(ReleaseErrors(() => tree.PhysicsFrame(.01)).Length > 0 && scene.Parent.Calls == calls && scene.Parent.GlobalPosition == position,
                "A failed world rejects the owner preflight before any worker pose or activation edit");
        }
    }
}
