using Electron2D;

public static partial class BackendChecks
{
    public static void RunColliderInputs(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        var solvers = new List<Solver>();
        if (backend is null) PhysicsServerManager.RegisterServer("ColliderInputs", () => { var solver = new Solver(); solvers.Add(solver); return solver; });
        try
        {
            using var first = backend is { } selected ? new World(selected) : new World("ColliderInputs");
            using var second = backend is { } other ? new World(other) : new World("ColliderInputs");
            using var root = new SubViewport { World = first };
            using var circle = new CircleShape { Radius = 2 }; using var identity = new Resource();
            var objectID = identity.InstanceID; var ownerThread = Environment.CurrentManagedThreadId;
            var moving = new RigidBody
            {
                GravityScale = 0,
                CanSleep = false,
                LinearVelocity = new(3, 0),
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            CollisionObject[] scenes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), moving, new PhysicalBone(), new Area()];
            for (var i = 0; i < scenes.Length; i++) { scenes[i].Position = new(i * 100, 0); root.AddChild(scenes[i]); }
            var rawBody = PhysicsServer.BodyCreate(); var rawArea = PhysicsServer.AreaCreate();
            (RID RID, bool Area)[] owners = [.. scenes.Select(scene => (scene.GetRID(), scene is Area)), (rawBody, false), (rawArea, true)];
            var reads = owners.SelectMany(owner => new Action[] {
                () => { if (owner.Area) PhysicsServer.AreaGetSpace(owner.RID); else PhysicsServer.BodyGetSpace(owner.RID); },
                () => { if (owner.Area) PhysicsServer.AreaGetCollisionLayer(owner.RID); else PhysicsServer.BodyGetCollisionLayer(owner.RID); },
                () => { if (owner.Area) PhysicsServer.AreaGetCollisionMask(owner.RID); else PhysicsServer.BodyGetCollisionMask(owner.RID); },
                () => { if (owner.Area) PhysicsServer.AreaGetObjectInstanceID(owner.RID); else PhysicsServer.BodyGetObjectInstanceID(owner.RID); },
                () => { if (owner.Area) PhysicsServer.AreaGetCanvasInstanceID(owner.RID); else PhysicsServer.BodyGetCanvasInstanceID(owner.RID); }
            }.Concat(owner.Area ? [] : new Action[] { () => PhysicsServer.BodyGetCollisionPriority(owner.RID) })).ToArray();
            var space = default(RID); uint layer = 0x80000001, mask = 0x40000000; ulong canvas = 0xFEDCBA9876543210; var priority = 3.25f; var hooks = 0;
            using var query = new PhysicsPointQueryParameters { CollisionMask = 0x80000000 };
            var hits = new PhysicsPointResult[1];
            try
            {
                PhysicsServer.BodySetGravityScale(rawBody, 0); PhysicsServer.BodySetTransform(rawBody, new(0, new(700, 0)));
                PhysicsServer.AreaSetTransform(rawArea, new(0, new(800, 0)));
                foreach (var owner in owners)
                    if (owner.Area) PhysicsServer.AreaAddShape(owner.RID, circle.GetRID()); else PhysicsServer.BodyAddShape(owner.RID, circle.GetRID());
                Configure(); ReadAll();
                using var tree = new SceneTree(root);
                using var otherRoot = new SubViewport { World = second };
                using var otherTree = new SceneTree(otherRoot);
                var secondSpace = second.Space;
                space = first.Space; PhysicsServer.BodySetSpace(rawBody, space); PhysicsServer.AreaSetSpace(rawArea, space);
                Configure(); ReadAll();
                Task.Run(RejectReads).GetAwaiter().GetResult();
                if (solvers.Count != 0)
                {
                    solvers[0].StepHook = () =>
                    {
                        ReadHook();
                        foreach (var owner in owners)
                        {
                            if (owner.Area)
                            {
                                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetSpace(owner.RID, secondSpace));
                                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetCollisionLayer(owner.RID, layer));
                                Reject<InvalidOperationException>(() => PhysicsServer.AreaSetCollisionMask(owner.RID, mask));
                                Reject<InvalidOperationException>(() => PhysicsServer.AreaAttachObject(owner.RID, identity));
                                Reject<InvalidOperationException>(() => PhysicsServer.AreaAttachCanvasInstanceID(owner.RID, canvas));
                            }
                            else
                            {
                                Reject<InvalidOperationException>(() => PhysicsServer.BodySetSpace(owner.RID, secondSpace));
                                Reject<InvalidOperationException>(() => PhysicsServer.BodySetCollisionLayer(owner.RID, layer));
                                Reject<InvalidOperationException>(() => PhysicsServer.BodySetCollisionMask(owner.RID, mask));
                                Reject<InvalidOperationException>(() => PhysicsServer.BodyAttachObject(owner.RID, identity));
                                Reject<InvalidOperationException>(() => PhysicsServer.BodyAttachCanvasInstanceID(owner.RID, canvas));
                                Reject<InvalidOperationException>(() => PhysicsServer.BodySetCollisionPriority(owner.RID, priority));
                            }
                        }
                        Reject<InvalidOperationException>(() => first.DirectSpaceState.IntersectPoint(query, hits));
                        Task.Run(RejectReads).GetAwaiter().GetResult(); ReadAll();
                    };
                    tree.PhysicsFrame(.01); solvers[0].StepHook = null;
                }
                identity.Dispose(); ReadAll();
                Transfer(secondSpace);
                if (solvers.Count != 0) solvers[1].StepHook = ReadHook;
                otherTree.PhysicsFrame(.01); ReadAll();
                if (solvers.Count != 0) solvers[1].StepHook = null;
                Transfer(first.Space);
                if (solvers.Count != 0)
                {
                    solvers[0].StatusHook = ReadAll; ReadAll(); solvers[0].StatusHook = null;
                    solvers[0].StepHook = ReadHook;
                }
                for (var i = 0; i < 128; i++) Frame(i);
                var x = moving.Position.X; var before = hooks; Collect();
                var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame(i);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0 && Math.Abs(moving.Position.X - x - 3.84f) < .005f && (solvers.Count == 0 || hooks == before + 128),
                    $"128 edited collider-input full frames: {own}/{all} B, motion={moving.Position.X - x}, hooks={hooks - before}");
                if (solvers.Count != 0)
                {
                    solvers[0].StepHook = null; solvers[0].FailSolve = true;
                    Reject<AggregateException>(() => tree.PhysicsFrame(.01)); RejectReads();
                }
                Console.WriteLine($"Collider inputs {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: eleven getters, six scene roles/raw Body/Area, detached/transfer/disposed association, guards and 128 edited full frames {own}/{all} B passed.");

                void Frame(int i)
                {
                    layer = (i & 1) == 0 ? 0x80000001 : uint.MaxValue; mask = (i & 1) == 0 ? 0u : uint.MaxValue;
                    canvas = (i & 1) == 0 ? 0xFEDCBA9876543210 : 0; priority = (i & 1) == 0 ? 3.25f : 7.5f;
                    Configure(); tree.PhysicsFrame(.01); ReadAll(); query.CanvasInstanceID = canvas;
                    Check(first.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == owners[0].RID && hits[0].ColliderID == objectID &&
                        ReferenceEquals(hits[0].Collider, scenes[0]) && hits[0].ColliderObject is null,
                        "Real point queries preserve high-bit filtering, exact canvas identity and disposed weak association");
                }
            }
            finally
            {
                foreach (var solver in solvers) { solver.StepHook = null; solver.StatusHook = null; }
                PhysicsServer.FreeRID(rawBody); PhysicsServer.FreeRID(rawArea);
            }
            void Configure()
            {
                foreach (var owner in owners)
                    if (owner.Area)
                    {
                        PhysicsServer.AreaSetCollisionLayer(owner.RID, layer); PhysicsServer.AreaSetCollisionMask(owner.RID, mask);
                        PhysicsServer.AreaAttachCanvasInstanceID(owner.RID, canvas); if (!identity.IsDisposed) PhysicsServer.AreaAttachObject(owner.RID, identity);
                    }
                    else
                    {
                        PhysicsServer.BodySetCollisionLayer(owner.RID, layer); PhysicsServer.BodySetCollisionMask(owner.RID, mask);
                        PhysicsServer.BodyAttachCanvasInstanceID(owner.RID, canvas); PhysicsServer.BodySetCollisionPriority(owner.RID, priority);
                        if (!identity.IsDisposed) PhysicsServer.BodyAttachObject(owner.RID, identity);
                    }
            }
            void ReadAll()
            {
                foreach (var owner in owners)
                {
                    Check((owner.Area ? PhysicsServer.AreaGetSpace(owner.RID) : PhysicsServer.BodyGetSpace(owner.RID)) == space, "The exact current world association is prepared input");
                    Check((owner.Area ? PhysicsServer.AreaGetCollisionLayer(owner.RID) : PhysicsServer.BodyGetCollisionLayer(owner.RID)) == layer &&
                        (owner.Area ? PhysicsServer.AreaGetCollisionMask(owner.RID) : PhysicsServer.BodyGetCollisionMask(owner.RID)) == mask, "Filters retain every authored bit");
                    Check((owner.Area ? PhysicsServer.AreaGetCanvasInstanceID(owner.RID) : PhysicsServer.BodyGetCanvasInstanceID(owner.RID)) == canvas &&
                        (owner.Area ? PhysicsServer.AreaGetObjectInstanceID(owner.RID) : PhysicsServer.BodyGetObjectInstanceID(owner.RID)) == objectID, "Numeric associations survive target disposal and attachment transfer");
                    if (!owner.Area) Check(PhysicsServer.BodyGetCollisionPriority(owner.RID) == priority, "Body priority remains exact prepared authoring");
                }
            }
            void RejectReads() { foreach (var read in reads) Reject<InvalidOperationException>(read); }
            void ReadHook()
            {
                Check((Environment.CurrentManagedThreadId != ownerThread) == threaded, "Prepared metadata belongs to the selected numerical lane");
                ReadAll(); hooks++;
            }
            void Transfer(RID target)
            {
                foreach (var owner in owners)
                    if (owner.Area) PhysicsServer.AreaSetSpace(owner.RID, target); else PhysicsServer.BodySetSpace(owner.RID, target);
                space = target; ReadAll();
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }
}
