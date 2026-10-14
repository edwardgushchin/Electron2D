using Electron2D;

public static partial class BackendChecks
{
    private static void ResourceCopyFailures(Node root, World world)
    {
        Check((int)DeepDuplicateMode.None == 0 && (int)DeepDuplicateMode.Internal == 1 &&
            (int)DeepDuplicateMode.All == 2 && Enum.GetValues<DeepDuplicateMode>().Length == 3,
            "Deep copy policies retain their complete public numeric identities");
        using var shared = new Resource();
        using var source = new CopyFailureShape { ResourceName = "source", Radius = 6, Shared = shared, Numbers = [3, 7] };
        using var alternate = new CopyFailureShape { ResourceName = "alternate", Radius = 4 };
        using var target = new CopyFailureShape { ResourceName = "target", Radius = 2, Transient = 9 };
        target.ResourcePath = "local://copy-failure-" + target.InstanceID;
        target.ResourceSceneUniqueID = "preserved";
        var id = target.InstanceID; var path = target.ResourcePath; var rid = target.GetRID();
        AddBody(root, "CopyFailureBorrower", 120, target);
        var body = root.GetNode<StaticBody>("CopyFailureBorrower");
        using var query = new PhysicsPointQueryParameters { Position = new(125, 0) }; var hits = new PhysicsPointResult[1];
        var changes = 0; var sourceChanges = 0;
        source.Changed += _ => sourceChanges++;
        target.Changed += resource =>
        {
            changes++;
            Check(ReferenceEquals(resource, target) && target.InstanceID == id && target.ResourcePath == path &&
                target.ResourceSceneUniqueID == "preserved" && ReferenceEquals(ResourceLoader.GetCachedRef<Resource>(path), target),
                "Every copy result preserves committed target object/cache/scene identity");
        };
        var resetError = new InvalidOperationException("Expected reset failure");
        target.ResetHook = () => { target.EmitChanged(); throw resetError; };
        Check(ReferenceEquals(CaptureCopy(() => target.CopyFromResource(source)), resetError) && target.ResetCount == 1 &&
            target.Transient == 0 && target.ResourceName == "target" && target.Radius == 2 && changes == 1 && sourceChanges == 0,
            "Reset failure reports exactly one partial-state change before any stored fields are copied");
        Frame(false);

        target.ResetHook = null;
        var copyError = new ArgumentException("Expected custom copy failure");
        var observerError = new InvalidOperationException("Expected copy observer failure");
        source.CopyHook = _ => throw copyError;
        Action<Resource> fail = _ => throw observerError; var later = 0;
        Action<Resource> skipped = _ => later++;
        target.Changed += fail; target.Changed += skipped;
        var error = CaptureCopy(() => target.CopyFromResource(source));
        Check(error is AggregateException aggregate && aggregate.InnerExceptions.Count == 2 &&
            ReferenceEquals(aggregate.InnerExceptions[0], copyError) && ReferenceEquals(aggregate.InnerExceptions[1], observerError) &&
            target.Radius == 6 && target.ResourceName == "source" && ReferenceEquals(target.Shared, shared) &&
            ReferenceEquals(target.Numbers, source.Numbers) && changes == 2 && later == 0 && sourceChanges == 0,
            "Custom-copy and final observer failures both survive, with shallow partial state and synchronous subscriber order");
        target.Changed -= fail; target.Changed -= skipped; source.CopyHook = null;
        Frame(true);

        var nested = 0;
        target.ResetHook = () =>
        {
            target.ResetHook = null; nested++;
            target.CopyFromResource(alternate); target.EmitChanged();
        };
        target.CopyFromResource(source);
        Check(nested == 1 && changes == 3 && target.Radius == 6 && target.ResourceName == "source",
            "Nested copy batches coalesce at their outer boundary and restore the outer operation's final state");
        var reentered = false;
        void Reenter(Resource _) { if (!reentered) { reentered = true; target.CopyFromResource(alternate); } }
        target.Changed += Reenter; target.CopyFromResource(source); target.Changed -= Reenter;
        Check(reentered && changes == 5 && target.Radius == 4 && target.ResourceName == "alternate",
            "A final Changed callback starts a balanced new copy batch with its own committed event");
        Frame(false); target.CopyFromResource(source); Frame(true);
        root.RemoveChild(body); body.Dispose();

        var created = new List<CopyFailureShape>(); var retired = new List<CopyFailureShape>();
        var childCleanup = new InvalidOperationException("Expected child cleanup failure");
        var rootCleanup = new InvalidOperationException("Expected root cleanup failure");
        source.Factory = () => Acquire(rootCleanup);
        alternate.Factory = () => Acquire(childCleanup);
        source.Shared = alternate; source.CopyHook = _ => throw copyError;
        error = CaptureCopy(() => source.DuplicateDeep(DeepDuplicateMode.All));
        Check(error is AggregateException rollback && rollback.InnerExceptions.Count == 3 &&
            ReferenceEquals(rollback.InnerExceptions[0], copyError) && ReferenceEquals(rollback.InnerExceptions[1], childCleanup) &&
            ReferenceEquals(rollback.InnerExceptions[2], rootCleanup) && created.Count == 2 && retired.SequenceEqual(created.AsEnumerable().Reverse()) &&
            created.All(resource => resource.IsDisposed) && !source.IsDisposed && !alternate.IsDisposed && !shared.IsDisposed,
            "Failed duplication retires every acquired target in reverse order, retains all failures and preserves borrowed sources");
        foreach (var resource in created)
        {
            Check(!resource.CreatedRID.IsValid() || CaptureCopy(() => PhysicsServer.ShapeGetType(resource.CreatedRID)) is ArgumentException,
                "Rollback releases actual query shape identities even when application cleanup throws");
        }
        source.Factory = null; alternate.Factory = null; source.CopyHook = null;
        using var recovered = (CopyFailureShape)source.DuplicateDeep(DeepDuplicateMode.All);
        Check(recovered.Shared is CopyFailureShape copy && !ReferenceEquals(copy, alternate) && !copy.IsDisposed,
            "A failed session cannot contaminate a subsequent independent deep-copy graph");
        recovered.Shared!.Dispose();
        Check(CaptureCopy(() => source.DuplicateDeep((DeepDuplicateMode)(-1))) is ArgumentOutOfRangeException &&
            CaptureCopy(() => source.DuplicateDeep((DeepDuplicateMode)3)) is ArgumentOutOfRangeException,
            "Unknown deep policies reject before factories or graph acquisition");
        Console.WriteLine("Physical resource copy failures: reset, partial geometry, paired exceptions, nested/reentrant batches, reverse throwing cleanup, live queries and recovery passed.");

        CopyFailureShape Acquire(Exception cleanup)
        {
            var resource = new CopyFailureShape { CleanupError = cleanup, Retired = retired };
            resource.CreatedRID = resource.GetRID(); created.Add(resource); return resource;
        }
        void Frame(bool hit)
        {
            root.Tree!.PhysicsFrame(1d / 60);
            Check(target.GetRID() == rid && world.DirectSpaceState.IntersectPoint(query, hits) == (hit ? 1 : 0) &&
                (!hit || hits[0].ColliderRID == body.GetRID()),
                "Failed and reentrant copies preserve live body/shape identity and expose their accepted geometry to real queries");
        }
    }

    private static Exception? CaptureCopy(Action operation)
    {
        try { operation(); return null; }
        catch (Exception error) { return error; }
    }

    private sealed class CopyFailureShape : Shape
    {
        private float _radius = 2;
        public float Radius { get => _radius; set { EnsureGeometryChangeAccess(); _radius = value; EmitGeometryChanged(); } }
        public Resource? Shared { get; set; }
        public int[] Numbers { get; set; } = [];
        public int Transient { get; set; }
        public int ResetCount { get; private set; }
        public Action? ResetHook { get; set; }
        public Action<CopyFailureShape>? CopyHook { get; set; }
        public Func<CopyFailureShape>? Factory { get; set; }
        public Exception? CleanupError { get; init; }
        public List<CopyFailureShape>? Retired { get; init; }
        public RID CreatedRID { get; set; }
        protected override ShapeGeometry GetGeometryCore() => ShapeGeometry.Circle(_radius);
        protected override Resource CreateDuplicateInstance() => Factory?.Invoke() ?? new CopyFailureShape();
        protected override void OnResetState() { Transient = 0; ResetCount++; ResetHook?.Invoke(); }
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode,
            Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
        {
            base.CopyCustomStateTo(target, deep, mode, duplicate, force);
            var copy = (CopyFailureShape)target; copy.Radius = Radius; copy.Shared = duplicate(Shared);
            copy.Numbers = deep ? (int[])Numbers.Clone() : Numbers; CopyHook?.Invoke(copy);
        }
        protected override void Dispose(bool disposing)
        {
            base.Dispose(disposing);
            if (disposing) { Retired?.Add(this); if (CleanupError is { } error) throw error; }
        }
    }
}
