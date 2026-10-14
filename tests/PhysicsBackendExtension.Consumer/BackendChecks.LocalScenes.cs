using System.Runtime.CompilerServices;
using Electron2D;

public static partial class BackendChecks
{
    private sealed class LocalSceneRoot : Node
    {
        internal static readonly PropertyDescriptor<LocalSceneRoot, LocalSceneData?> DataProperty =
            new(nameof(Data), n => n.Data, (n, v) => n.Data = v, _ => null, stored: true);
        public LocalSceneData? Data { get; set; }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(DataProperty);
        protected override Func<Node> CreateSceneInstanceFactory() => CreateLocalSceneRoot;
    }
    private static Node CreateLocalSceneRoot() => new LocalSceneRoot();

    private sealed class LocalSceneData : Resource
    {
        internal static readonly PropertyDescriptor<LocalSceneData, Resource[]> ReferencesProperty =
            new(nameof(References), n => n.References, (n, v) => n.References = v, _ => [], stored: true);
        internal static readonly PropertyDescriptor<LocalSceneData, LocalSceneData?> PeerProperty =
            new(nameof(Peer), n => n.Peer, (n, v) => n.Peer = v, _ => null, stored: true);
        public Resource[] References { get; set; } = [];
        public LocalSceneData? Peer { get; set; }
        internal int SetupCount;
        internal bool TranslationReadyAtSetup;
        internal static int Retirements;
        internal bool UnboundAtRetirement;
        internal bool ThrowOnSetup;
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(ReferencesProperty).Append(PeerProperty);
        protected override Resource CreateDuplicateInstance() => new LocalSceneData();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode,
            Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> forceDuplicate)
        {
            var result = (LocalSceneData)target;
            result.References = References.Select(r => duplicate(r)!).ToArray();
            result.Peer = (LocalSceneData?)duplicate(Peer); result.ThrowOnSetup = ThrowOnSetup;
        }
        protected override void Dispose(bool disposing)
        { if (disposing) { UnboundAtRetirement = GetLocalScene() is null; Retirements++; } base.Dispose(disposing); }
        protected override void OnSetupLocalToScene()
        {
            SetupCount++;
            TranslationReadyAtSetup = !CanTranslateMessages && TranslationDomain == StoredResourceDomain &&
                References.OfType<Shape>().All(s => !s.CanTranslateMessages && s.TranslationDomain == StoredResourceDomain);
            Check(GetLocalScene() is LocalSceneRoot root && ReferenceEquals(root.Data, this) && !root.IsInsideTree &&
                ReferenceEquals(Peer, this), "Local setup sees the complete detached root and cyclic resource graph");
            if (ThrowOnSetup) throw new InvalidOperationException("Expected local resource setup failure.");
        }
    }

    public static void RunLocalScenes(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previousThread = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var previousEngine = ProjectSettings.Get(ProjectSettings.Physics2DEngine);
        if (backend is null) PhysicsServerManager.RegisterServer("LocalScenes", () => new Solver());
        ProjectSettings.Set(ProjectSettings.Physics2DEngine, backend?.ToString() ?? "LocalScenes");
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { LocalScenes(backend); }
        finally
        {
            ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previousThread);
            ProjectSettings.Set(ProjectSettings.Physics2DEngine, previousEngine);
        }
    }

    private static void LocalScenes(PhysicsServer.Backend? backend)
    {
        var (retained, retiredRoot) = DetachedLocalScene();
        using (retained)
        {
            Collect();
            Check(!retiredRoot.TryGetTarget(out _) && retained.GetLocalScene() is null,
                "A retained local resource must not keep its unowned scene root alive");
        }
        using var circle = new CircleShape { Radius = 3, ResourceLocalToScene = true, CanTranslateMessages = false, TranslationDomain = StoredResourceDomain };
        using var shared = new PhysicsMaterial { Friction = .4f }; shared.ResourcePath = "local-scenes-material";
        using var builtInShared = new PhysicsMaterial { Friction = .6f };
        using var data = new LocalSceneData { ResourceLocalToScene = true, References = [circle, shared, circle, builtInShared], CanTranslateMessages = false, TranslationDomain = StoredResourceDomain };
        data.Peer = data;
        using var source = new LocalSceneRoot { Name = "Template", Data = data };
        for (var i = 0; i < 2; i++)
        {
            var body = new StaticBody { Name = "Body" + i, Position = new(i * 30, 0), PhysicsMaterialOverride = i == 0 ? shared : builtInShared };
            source.AddChild(body); body.Owner = source;
            var shape = new CollisionShape { Name = "Shape", Shape = circle }; body.AddChild(shape); shape.Owner = source;
        }
        var movingSource = new RigidBody
        {
            Name = "Mover",
            Position = new(0, 100),
            LinearVelocity = new(3, 0),
            GravityScale = 0,
            CanSleep = false,
            LinearDampMode = RigidBody.DampMode.Replace,
            LinearDamp = 0,
            AngularDampMode = RigidBody.DampMode.Replace,
            AngularDamp = 0
        };
        source.AddChild(movingSource); movingSource.Owner = source;
        var movingShape = new CollisionShape { Name = "Shape", Shape = circle }; movingSource.AddChild(movingShape); movingShape.Owner = source;
        using var packed = new PackedScene(); packed.Pack(source);
        var first = (LocalSceneRoot)packed.Instantiate(); var second = (LocalSceneRoot)packed.Instantiate();
        var firstData = first.Data!; var secondData = second.Data!;
        Check(firstData.SetupCount == 1 && secondData.SetupCount == 1 && data.SetupCount == 0 &&
            ReferenceEquals(firstData.GetLocalScene(), first) && ReferenceEquals(secondData.GetLocalScene(), second),
            "Each scene receives one independent local resource setup");
        var firstCircle = (CircleShape)firstData.References[0]; var secondCircle = (CircleShape)secondData.References[0];
        Check(firstData.TranslationReadyAtSetup && secondData.TranslationReadyAtSetup &&
            !firstData.CanTranslateMessages && !secondData.CanTranslateMessages && !firstCircle.CanTranslateMessages && !secondCircle.CanTranslateMessages &&
            firstData.TranslationDomain == StoredResourceDomain && secondData.TranslationDomain == StoredResourceDomain &&
            firstCircle.TranslationDomain == StoredResourceDomain && secondCircle.TranslationDomain == StoredResourceDomain,
            "Local resource graphs preserve inherited stored state before setup and physical scene use");
        Check(firstCircle != circle && firstCircle != secondCircle && firstData.References[2] == firstCircle &&
            firstData.References[1] == shared && secondData.References[1] == shared && shared.GetLocalScene() is null &&
            first.GetNode<CollisionShape>("Body0/Shape").Shape == firstCircle && first.GetNode<CollisionShape>("Body1/Shape").Shape == firstCircle,
            "Local graph aliases agree with actual collision nodes while external non-local resources remain borrowed");
        Check(firstData.References[3] == builtInShared && secondData.References[3] == builtInShared &&
            first.GetNode<StaticBody>("Body1").PhysicsMaterialOverride == builtInShared && builtInShared.GetLocalScene() is null,
            "Non-local built-in material remains one borrowed identity across the local graph and scene nodes");
        var policyChanges = 0; firstData.Changed += _ => policyChanges++;
        firstData.ResourceLocalToScene = false;
        Check(ReferenceEquals(firstData.GetLocalScene(), first) && data.ResourceLocalToScene && secondData.ResourceLocalToScene && policyChanges == 0,
            "Changing local policy does not reassign existing scene resources");
        using var copy = firstData.Duplicate(true);
        var copiedData = (LocalSceneData)copy;
        Check(copy.GetLocalScene() is null && copiedData.Peer == copiedData && copiedData.References[3] != builtInShared && copiedData.References[1] == shared,
            "Ordinary deep duplication retains its independent built-in/external policy without a scene association");
        var firstBody = first.GetNode<StaticBody>("Body0");
        var firstMover = first.GetNode<RigidBody>("Mover"); var secondMover = second.GetNode<RigidBody>("Mover");
        using var firstWorld = backend is { } selectedFirst ? new World(selectedFirst) : new World("LocalScenes");
        using var secondWorld = backend is { } selectedSecond ? new World(selectedSecond) : new World("LocalScenes");
        var firstHost = new SubViewport { World = firstWorld }; firstHost.AddChild(first); var secondHost = new SubViewport { World = secondWorld }; secondHost.AddChild(second);
        using var firstTree = new SceneTree(firstHost); using var secondTree = new SceneTree(secondHost);
        firstTree.PhysicsFrame(1d / 60); secondTree.PhysicsFrame(1d / 60);
        if (backend is { } selected) Check(firstWorld.PhysicsBackend == selected && secondWorld.PhysicsBackend == selected,
            "Both scene instances use the explicitly selected backend");
        else Check(firstWorld.PhysicsBackendName == "LocalScenes", "The selected registered numerical owner executes");
        firstCircle.Radius = 5;
        using var query = new PhysicsPointQueryParameters { Position = new(4, 0) }; var hits = new PhysicsPointResult[2];
        Check(firstWorld.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == firstBody.GetRID() &&
            secondWorld.DirectSpaceState.IntersectPoint(query, hits) == 0 && secondCircle.Radius == 3 && circle.Radius == 3,
            "Editing local geometry changes only the owning instance's real physical queries");
        for (var i = 0; i < 128; i++) Frame();
        Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame();
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"Local resource association/full scene frames allocate {owner}/{all} B");
        var replacement = new LocalSceneRoot { Name = "Replacement", Data = firstData };
        first.ReplaceBy(replacement); first.Dispose();
        Check(!firstData.IsDisposed && !firstCircle.IsDisposed && ReferenceEquals(firstData.GetLocalScene(), replacement) &&
            ReferenceEquals(firstCircle.GetLocalScene(), replacement), "Root replacement transfers resource ownership and borrowed association");
        replacement.Dispose();
        Check(firstData.IsDisposed && firstData.UnboundAtRetirement && firstCircle.IsDisposed && !secondData.IsDisposed && !secondCircle.IsDisposed && !shared.IsDisposed,
            "Replacement retirement releases only its owned local graph");
        Reject<ObjectDisposedException>(() => firstData.GetLocalScene());
        secondTree.Dispose();
        Check(secondData.IsDisposed && secondData.UnboundAtRetirement && secondCircle.IsDisposed && !builtInShared.IsDisposed && !data.IsDisposed && !circle.IsDisposed && !shared.IsDisposed,
            "Scene retirement preserves template resources and borrowed material");
        Reject<ObjectDisposedException>(() => secondData.GetLocalScene());
        var beforeFailure = LocalSceneData.Retirements;
        data.ThrowOnSetup = true; Reject<AggregateException>(() => packed.Instantiate());
        Check(!data.IsDisposed && !circle.IsDisposed && !shared.IsDisposed && data.SetupCount == 0 && LocalSceneData.Retirements == beforeFailure + 1,
            "Setup failure retires duplicates without mutating or disposing source resources");
        Console.WriteLine($"Local scene resources {backend?.ToString() ?? "registered"}, threaded={ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread)}: aliases/cycles, setup, geometry, root replacement, retirement and 128 full frames {owner}/{all} B passed.");
        void Frame()
        {
            var firstX = firstMover.Position.X; var secondX = secondMover.Position.X;
            firstTree.PhysicsFrame(1d / 60); secondTree.PhysicsFrame(1d / 60);
            Check(MathF.Abs(firstMover.Position.X - firstX - .05f) < .00005f && MathF.Abs(secondMover.Position.X - secondX - .05f) < .00005f,
                "Both local scenes execute active numerical integration within float accumulation tolerance");
            Check(ReferenceEquals(firstCircle.GetLocalScene(), first) && ReferenceEquals(secondCircle.GetLocalScene(), second) &&
                firstWorld.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == firstBody.GetRID(),
                "Warm local resource reads preserve owning scene and live query identity");
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (LocalSceneData Resource, WeakReference<Node> Root) DetachedLocalScene()
    {
        using var data = new LocalSceneData { ResourceLocalToScene = true }; data.Peer = data;
        using var source = new LocalSceneRoot { Data = data }; using var packed = new PackedScene(); packed.Pack(source);
        var root = (LocalSceneRoot)packed.Instantiate(); var retained = root.Data!;
        Check(ReferenceEquals(retained.GetLocalScene(), root), "Fresh detached local association is valid");
        return (retained, new(root));
    }
}
