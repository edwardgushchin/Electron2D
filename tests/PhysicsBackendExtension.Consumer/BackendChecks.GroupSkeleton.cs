using Electron2D;

public static partial class BackendChecks
{
    private sealed class GroupPoseModification : SkeletonModification
    {
        internal Action? Prepare, Apply;
        protected override void OnSetupModification(SkeletonModificationStack modificationStack) => Prepare?.Invoke();
        protected override void OnExecute(double delta) => Apply?.Invoke();
    }
    private sealed class GroupSkeletonScene : IDisposable
    {
        internal readonly Skeleton Rig;
        internal readonly Bone A = new() { Name = "A", Rest = Transform.Identity };
        internal readonly Bone B = new() { Name = "B", Position = new(30, 0), Rest = new(0, new(30, 0)) };
        internal readonly Entity Tip = new() { Name = "Tip", Position = new(30, 0) }, Target;
        internal readonly PhysicalBone Body;
        internal readonly SkeletonModificationStack Stack = new() { Enabled = true };
        internal readonly SkeletonModification? Modification;
        private readonly List<Resource> _resources = [];
        internal GroupSkeletonScene(Node root, Shape shape, string kind, ProcessPhase phase, bool worker, string tag = "", Vector2 origin = default)
        {
            Rig = new() { Name = "Rig" + tag, Position = origin, ProcessThreadGroup = worker ? Node.ProcessThreadGroupMode.SubThread : Node.ProcessThreadGroupMode.MainThread, ProcessThreadGroupOrder = -1 };
            Target = new() { Name = "Target" + tag, Position = origin + new Vector2(30, 30) };
            A.SetAutocalculateLengthAndAngle(false); B.SetAutocalculateLengthAndAngle(false); A.SetLength(30); B.SetLength(30);
            Body = new()
            {
                Name = "P",
                BoneNodePath = "../A",
                SimulatePhysics = true,
                FollowBoneWhenSimulating = kind != "physical",
                AutoConfigureJoint = false,
                GravityScale = 0,
                CanSleep = false,
                CollisionMask = 0,
                LinearVelocity = kind == "physical" ? new(4, 0) : default
            };
            Body.AddChild(new CollisionShape { Position = new(4, 0), Shape = shape }); B.AddChild(Tip); A.AddChild(B); Rig.AddChild(A); Rig.AddChild(Body); root.AddChild(Rig); root.AddChild(Target);
            _resources.Add(Stack); var target = "../" + Target.Name;
            switch (kind)
            {
                case "look-at": Modification = new SkeletonModificationLookAt { BoneIndex = 0, TargetNodePath = target }; break;
                case "two-bone":
                    var two = new SkeletonModificationTwoBoneIK { TargetNodePath = target }; two.SetJointOneBoneNode("A"); two.SetJointTwoBoneNode("A/B"); Modification = two; break;
                case "ccdik":
                    var ccd = new SkeletonModificationCCDIK { CCDIKDataChainLength = 2, TargetNodePath = target, TipNodePath = "A/B/Tip" };
                    ccd.SetCCDIKJointBoneNode(0, "A/B"); ccd.SetCCDIKJointBoneNode(1, "A"); Modification = ccd; break;
                case "fabrik":
                    var fab = new SkeletonModificationFABRIK { FABRIKDataChainLength = 2, TargetNodePath = target };
                    fab.SetFABRIKJointBoneNode(0, "A"); fab.SetFABRIKJointBoneNode(1, "A/B"); Modification = fab; break;
                case "jiggle":
                    var jiggle = new SkeletonModificationJiggle { JiggleDataChainLength = 1, TargetNodePath = target }; jiggle.SetJiggleJointBoneNode(0, "A"); Modification = jiggle; break;
                case "physical":
                    var physical = new SkeletonModificationPhysicalBones { PhysicalBoneChainLength = 1 }; physical.SetPhysicalBoneNode(0, "P"); Stack.Strength = .5f; Modification = physical; break;
                case "holder":
                    var held = new SkeletonModificationStack { Enabled = true, Strength = .5f }; var look = new SkeletonModificationLookAt { BoneIndex = 0, TargetNodePath = target, ExecutionMode = phase };
                    held.AddModification(look); var holder = new SkeletonModificationStackHolder(); holder.SetHeldModificationStack(held); Stack.Strength = .25f;
                    _resources.Add(held); _resources.Add(look); Modification = holder; break;
            }
            if (Modification is not null) { Modification.ExecutionMode = phase; _resources.Add(Modification); Stack.AddModification(Modification); Rig.SetModificationStack(Stack); }
        }
        internal void Own()
        {
            Copy(Rig); Target.Reparent(Rig, keepGlobalTransform: true); Target.Owner = Rig;
            void Copy(Node node) { for (var i = 0; i < node.GetChildCount(); i++) { var child = node.GetChild(i); child.Owner = Rig; Copy(child); } }
        }
        public void Dispose() { foreach (var resource in _resources) resource.Dispose(); }
    }
    public static void RunGroupSkeleton(PhysicsServer.Backend? backend, bool threaded, bool poolOwner = false)
    {
        var oldThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var oldSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("GroupSkeleton", () => new Solver());
        try { if (poolOwner) { var task = WorkerThreadPool.AddTask(Run); WorkerThreadPool.WaitForTaskCompletion(task); } else Run(); }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, oldThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, oldSolver); }
        World Create() => backend is { } selected ? new World(selected) : new World("GroupSkeleton");
        void Run()
        {
            using (var world = Create())
            {
                var cases = 0;
                foreach (var kind in new[] { "empty", "override", "look-at", "two-bone", "ccdik", "fabrik", "jiggle", "physical", "holder" })
                    foreach (var phase in new[] { ProcessPhase.Idle, ProcessPhase.Physics }) foreach (var route in new[] { "scheduled", "propagated", "direct" }) foreach (var worker in new[] { false, true })
                            {
                                using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 1 };
                                using var scene = new GroupSkeletonScene(root, shape, kind, phase, worker); using var tree = new SceneTree(root);
                                var rid = scene.Body.GetRID(); var tick = PhysicsServer.SpaceGetTick(world.Space); var previousSolved = scene.Body.GlobalPosition; var solved = previousSolved;
                                if (route != "scheduled")
                                {
                                    scene.Rig.InternalPhysicsProcessEnabled = scene.Rig.InternalProcessEnabled = false;
                                    scene.Body.InternalPhysicsProcessEnabled = scene.Body.InternalProcessEnabled = false; scene.Rig.ProcessThreadMessages = Node.ProcessThreadMessageFlags.All;
                                }
                                for (var step = 0; step < 2; step++)
                                {
                                    if (kind == "override") scene.Rig.SetBoneLocalPoseOverride(0, new(.2f, new(3, 4)), 1, false);
                                    previousSolved = scene.Body.GlobalPosition;
                                    if (route == "propagated") scene.Rig.CallDeferredThreadGroup<Skeleton>(static n => n.PropagateNotification(Node.NotificationInternalPhysicsProcess));
                                    else if (route == "direct") scene.Rig.CallDeferredThreadGroup<Skeleton>(static n => { if (n.GetModificationStack() is { } stack) stack.Execute(.01, ProcessPhase.Physics); else n.ExecuteModifications(.01, ProcessPhase.Physics); });
                                    tree.PhysicsFrame(.01); solved = scene.Body.GlobalPosition;
                                    if (phase == ProcessPhase.Physics && kind != "override") Check(scene.A.Rotation == 0 && scene.B.Rotation == 0, "Physics modifications stage overrides and restore the authored pose");
                                    if (route == "propagated") scene.Rig.CallDeferredThreadGroup<Skeleton>(static n => n.PropagateNotification(Node.NotificationInternalProcess));
                                    else if (route == "direct") scene.Rig.CallDeferredThreadGroup<Skeleton>(static n => { if (n.GetModificationStack() is { } stack) stack.Execute(.01, ProcessPhase.Idle); else n.ExecuteModifications(.01, ProcessPhase.Idle); });
                                    tree.ProcessFrame(.01);
                                }
                                var correct = kind switch
                                {
                                    "empty" => scene.A.Transform == Transform.Identity,
                                    "override" => scene.A.Position.DistanceTo(new(3, 4)) < .001f && MathF.Abs(scene.A.Rotation - .2f) < .001f,
                                    "look-at" => MathF.Abs(scene.A.Rotation - Mathf.Pi / 4) < .001f,
                                    "two-bone" or "ccdik" => scene.Tip.GlobalPosition.DistanceTo(scene.Target.GlobalPosition) < .002f,
                                    "fabrik" => scene.Tip.GlobalPosition.DistanceTo(scene.Target.GlobalPosition) < .02f,
                                    "jiggle" => scene.A.Transform.IsFinite() && scene.A.Rotation > 0,
                                    "physical" => MathF.Abs(scene.A.GlobalPosition.X - (phase == ProcessPhase.Physics ? previousSolved.X : solved.X) * .5f) < .001f && solved.X > .01f,
                                    "holder" => MathF.Abs(scene.A.Rotation - Mathf.Pi / 8) < .001f,
                                    _ => false
                                };
                                Check(correct, $"Prepared {kind}/{phase}/{route}/worker={worker} has the selected pose result");
                                Check(scene.Body.GetRID() == rid && PhysicsServer.SpaceGetTick(world.Space) == tick + 2 && world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null,
                                    "Skeletal execution retains real selected physics identity and numerical intervals"); cases++;
                            }
                Console.WriteLine($"Group skeleton {backend?.ToString() ?? "registered"}/{threaded}/{poolOwner}: {cases} scheduled/propagated/direct pose cases passed.");
            }
            Parallel(); Failures(); SetupCallbacks(); Packing();
        }
        void Parallel()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 1 };
            using var a = new GroupSkeletonScene(root, shape, "look-at", ProcessPhase.Idle, true, "A"); using var b = new GroupSkeletonScene(root, shape, "look-at", ProcessPhase.Idle, true, "B", new(200, 0));
            using var left = new GroupPoseModification(); using var right = new GroupPoseModification(); a.Stack.AddModification(left); b.Stack.AddModification(right);
            using var tree = new SceneTree(root); using var together = new Barrier(2); var cold = true; var phase = 0; var leftThread = 0; var rightThread = 0;
            left.Apply = () => { leftThread = Environment.CurrentManagedThreadId; Apply(a, b); }; right.Apply = () => { rightThread = Environment.CurrentManagedThreadId; Apply(b, a); };
            tree.ProcessFrame(.01); Check(leftThread != rightThread, "Two actual skeleton worker groups overlap their callbacks"); cold = false;
            using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[2];
            for (var i = 0; i < 128; i++) Frame(); Collect(); for (var i = 0; i < 128; i++) Frame();
            var tick = PhysicsServer.SpaceGetTick(world.Space); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame(); own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128, $"128 grouped skeleton physics/idle/query cycles allocated {own}/{all} B");
            Console.WriteLine($"128 grouped skeleton physics/idle/query cycles: {own}/{all} owner/all-thread managed bytes.");
            Task.Run(() => { Reject<InvalidOperationException>(() => a.Stack.GetSkeleton()); Reject<InvalidOperationException>(() => a.Rig.GetModificationStack()); Reject<InvalidOperationException>(() => a.Stack.Execute(.01, ProcessPhase.Idle)); }).GetAwaiter().GetResult();
            void Apply(GroupSkeletonScene current, GroupSkeletonScene foreign)
            {
                if (cold) Check(together.SignalAndWait(TimeSpan.FromSeconds(5)), "Custom callbacks enter both active groups together");
                Check(foreign.Stack.GetSkeleton() == foreign.Rig && foreign.Stack.GetIsSetup() && foreign.Stack.Strength == 1 && current.Rig.GetModificationStack() == current.Stack,
                    "Foreign active groups read coherent stack metadata outside callback gates");
                if (cold) ColdGuards(current, foreign);
                current.Rig.SetBoneLocalPoseOverride(0, new(.2f + current.Target.Position.Y * .001f, Vector2.Zero), 1, false);
            }
            void ColdGuards(GroupSkeletonScene current, GroupSkeletonScene foreign)
            {
                Reject<InvalidOperationException>(() => foreign.Stack.Execute(.01, ProcessPhase.Idle));
                Reject<InvalidOperationException>(() => current.Stack.Execute(.01, ProcessPhase.Idle));
                Reject<InvalidOperationException>(() => current.Stack.Enabled = true); Reject<InvalidOperationException>(() => current.Stack.Setup());
                Reject<InvalidOperationException>(() => foreign.Rig.SetBoneLocalPoseOverride(0, Transform.Identity, 1, false));
                Reject<InvalidOperationException>(() => world.DirectSpaceState.IntersectPoint(new PhysicsPointQueryParameters(), new PhysicsPointResult[1]));
            }
            void Frame()
            {
                phase++; a.Target.Position = new(30, 25 + (phase & 1) * 5); b.Target.Position = new(230, 25 + (phase & 1) * 5);
                tree.PhysicsFrame(.01); tree.ProcessFrame(.01); Verify(a); Verify(b);
            }
            void Verify(GroupSkeletonScene scene)
            {
                Check(MathF.Abs(scene.A.Rotation - (.2f + scene.Target.Position.Y * .001f)) < .001f && scene.A.Position == Vector2.Zero, "Idle publishes the immediate callback override");
                point.Position = scene.Body.GlobalTransform * new Vector2(4, 0); var count = world.DirectSpaceState.IntersectPoint(point, hits);
                Check(count == 1 && hits[0].ColliderRID == scene.Body.GetRID(), "The selected real collider consumes worker-authored bone following");
            }
        }
        void Failures()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 1 };
            using var scene = new GroupSkeletonScene(root, shape, "look-at", ProcessPhase.Idle, true); using var fail = new GroupPoseModification(); scene.Stack.AddModification(fail);
            using var tree = new SceneTree(root); var original = new ApplicationException("group-pose-request");
            scene.Rig.SetBoneLocalPoseOverride(0, new(.3f, Vector2.Zero), 1, true);
            fail.Apply = () => { scene.Rig.SetBoneLocalPoseOverride(0, new(.8f, Vector2.Zero), 1, false); throw original; };
            Check(ReleaseErrors(() => tree.ProcessFrame(.01)).Any(e => ReferenceEquals(e, original)) && scene.A.Rotation == 0, "A failed custom group callback restores authored poses and original errors");
            scene.Stack.Enabled = false; tree.ProcessFrame(.01); Check(MathF.Abs(scene.A.Rotation - .3f) < .001f, "Failure restores the prior persistent request");
            scene.Stack.Enabled = true; fail.Apply = null; tree.ProcessFrame(.01); Check(MathF.Abs(scene.A.Rotation - Mathf.Pi / 4) < .001f, "A corrected custom stage retries after guard cleanup");
            var foreign = new Bone { Name = "Foreign", Rest = Transform.Identity, ProcessThreadGroup = Node.ProcessThreadGroupMode.MainThread }; foreign.SetAutocalculateLengthAndAngle(false); scene.A.AddChild(foreign);
            var pose = scene.A.Transform; Check(ReleaseErrors(() => tree.ProcessFrame(.01)).Length > 0 && scene.A.Transform == pose, "A foreign bone rejects before any pose transaction state changes");
            scene.A.RemoveChild(foreign); foreign.Dispose(); tree.ProcessFrame(.01);
            if (scene.Modification is not null) scene.Modification.Enabled = false;
            using var jiggle = new SkeletonModificationJiggle { JiggleDataChainLength = 1, TargetNodePath = "../Target", ExecutionMode = ProcessPhase.Physics }; jiggle.SetJiggleJointBoneNode(0, "A"); jiggle.SetUseColliders(true); scene.Stack.AddModification(jiggle);
            Check(ReleaseErrors(() => tree.PhysicsFrame(.01)).Length > 0 && scene.A.Rotation == 0, "A collider Jiggle query retains its numerical owner boundary and pose rollback");
            jiggle.SetUseColliders(false); tree.PhysicsFrame(.01); tree.ProcessFrame(.01); Check(scene.A.Transform.IsFinite(), "Noncolliding Jiggle resumes in the current group");
        }
        void SetupCallbacks()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 1 };
            using var scene = new GroupSkeletonScene(root, shape, "look-at", ProcessPhase.Idle, true); using var tree = new SceneTree(root); using var custom = new GroupPoseModification();
            var original = new ApplicationException("group-pose-setup"); var fail = true; var calls = 0;
            custom.Prepare = () =>
            {
                calls++; var task = Task.Run(() => Reject<InvalidOperationException>(() => scene.Stack.GetSkeleton()));
                Check(task.Wait(TimeSpan.FromSeconds(5)), "Setup callbacks release stack metadata before off-owner readers reject");
                Reject<InvalidOperationException>(() => scene.Rig.SetModificationStack(null));
                Reject<InvalidOperationException>(() => scene.Stack.ModificationCount = scene.Stack.ModificationCount);
                if (fail) throw original;
            };
            var errors = ReleaseErrors(() => scene.Stack.AddModification(custom));
            Check(errors.Any(e => ReferenceEquals(e, original)) && !scene.Stack.GetIsSetup(), "Failed added setup retains committed list and explicit retry state: " + string.Join("; ", errors.Select(e => e.Message)));
            fail = false; scene.Stack.Setup(); scene.Stack.SetModification(1, custom); Check(calls == 3 && scene.Stack.GetIsSetup(), "Full setup and replacement callbacks share unlocked preparation and retry guards");
            tree.PhysicsFrame(.01); tree.ProcessFrame(.01);
        }
        void Packing()
        {
            using var world = Create(); using var root = new SubViewport { World = world }; using var shape = new CircleShape { Radius = 1 };
            using var scene = new GroupSkeletonScene(root, shape, "look-at", ProcessPhase.Idle, true); using var tree = new SceneTree(root);
            scene.Own(); ((SkeletonModificationLookAt)scene.Modification!).TargetNodePath = "Target";
            using var packed = new PackedScene(); packed.Pack(scene.Rig); using var otherWorld = Create(); using var otherRoot = new SubViewport { World = otherWorld };
            var copy = (Skeleton)packed.Instantiate(); otherRoot.AddChild(copy); using var otherTree = new SceneTree(otherRoot);
            otherTree.PhysicsFrame(.01); otherTree.ProcessFrame(.01);
            Check(copy.ProcessThreadGroup == Node.ProcessThreadGroupMode.SubThread && !ReferenceEquals(copy.GetModificationStack(), scene.Stack) &&
                MathF.Abs(copy.GetNode<Bone>("A").Rotation - Mathf.Pi / 4) < .001f && copy.GetNode<PhysicalBone>("P").GetRID() != scene.Body.GetRID(),
                "A restored owned graph executes its copied stack and grouped skeleton with fresh physical identity");
        }
    }
}
