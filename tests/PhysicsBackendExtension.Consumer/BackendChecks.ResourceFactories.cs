using Electron2D;

public static partial class BackendChecks
{
    private static void ResourceFactoryOwnership(Node root, World world, Shape shape, PhysicsMaterial material)
    {
        using var existing = new FactoryResource { ResourceName = "existing" };
        using var source = new FactoryResource { ResourceName = "source", Result = existing };
        var changes = 0;
        existing.Changed += _ => changes++;
        foreach (var mode in Enum.GetValues<DeepDuplicateMode>())
            Reject(source, () => source.DuplicateDeep(mode), existing);
        Reject(source, () => source.Duplicate(), existing);
        Check(existing.ResourceName == "existing" && changes == 0 && source.Copies == 0,
            "Rejected existing targets preserve stored state and subscribers before custom copying");

        using var nested = new FactoryResource { Children = [source] };
        Reject(nested, () => nested.DuplicateDeep(DeepDuplicateMode.All), existing);
        Check(nested.LastCreated is { IsDisposed: true } && !source.IsDisposed,
            "Nested rejection retires the acquired parent while preserving source and foreign target");

        using var first = new FactoryResource();
        using var second = new FactoryResource { ResultFactory = () => first.LastCreated! };
        using var repeated = new FactoryResource { Children = [first, second] };
        Reject(repeated, () => repeated.DuplicateDeep(DeepDuplicateMode.All), first);
        Check(repeated.LastCreated is { IsDisposed: true } && first.LastCreated is { IsDisposed: true } && second.Copies == 0,
            "Distinct graph sources cannot reuse an earlier acquired target; original acquisitions roll back");

        using var custom = new FactoryShape { ResourceName = "borrowed", CustomSolverBias = .2f };
        AddBody(root, "BorrowedFactoryShape", 90, custom);
        using var customSource = new FactoryShape { ResourceName = "replacement", CustomSolverBias = .7f, Result = custom };
        var geometry = custom.GetGeometryData(); var rid = custom.GetRID();
        Reject(customSource, () => customSource.Duplicate(), custom);
        Check(custom.ResourceName == "borrowed" && custom.CustomSolverBias == .2f && custom.GetRID() == rid &&
            ReferenceEquals(custom.GetGeometryData(), geometry) && customSource.Copies == 0,
            "Existing exact-type scene geometry cannot be overwritten by a duplication factory");
        foreach (var target in new Resource[] { shape, material, world })
        {
            var id = target.InstanceID; var name = target.ResourceName;
            source.Result = target;
            Reject(source, () => source.Duplicate(), target);
            Check(target.InstanceID == id && target.ResourceName == name,
                "Wrong-type existing physical resources retain their identity and content");
        }
        using var point = new PhysicsPointQueryParameters { Position = new(90, 0) };
        var hits = new PhysicsPointResult[1];
        root.Tree!.PhysicsFrame(1d / 60);
        Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 &&
            hits[0].ColliderRID == root.GetNode<StaticBody>("BorrowedFactoryShape").GetRID(),
            "The preserved custom shape remains attached to the active selected physical world");
        root.GetNode<StaticBody>("BorrowedFactoryShape").Dispose();
        Console.WriteLine("Resource factory ownership: pre-existing exact/wrong types, nested rollback and acquired-target reuse passed.");

        static void Reject(Resource source, Func<Resource> duplicate, Resource preserved)
        {
            Resource? result = null;
            Exception? error = null;
            try { result = duplicate(); }
            catch (Exception caught) { error = caught; }
            Check(error is InvalidOperationException && !preserved.IsDisposed && !source.IsDisposed,
                "A factory must reject an existing target without copying or disposing caller-owned resources");
            result?.Dispose();
        }
    }

    private sealed class FactoryResource : Resource
    {
        public Resource? Result { get; set; }
        public Func<Resource>? ResultFactory { get; init; }
        public Resource[] Children { get; init; } = [];
        public FactoryResource? LastCreated { get; private set; }
        public int Copies { get; private set; }
        protected override Resource CreateDuplicateInstance() => ResultFactory?.Invoke() ?? Result ?? (LastCreated = new FactoryResource());
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode,
            Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
        {
            Copies++;
            foreach (var child in Children) duplicate(child);
        }
    }

    private sealed class FactoryShape : Shape
    {
        public FactoryShape? Result { get; init; }
        public int Copies { get; private set; }
        protected override ShapeGeometry GetGeometryCore() => ShapeGeometry.Circle(3);
        protected override Resource CreateDuplicateInstance() => Result ?? new FactoryShape();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode,
            Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
        {
            Copies++;
            base.CopyCustomStateTo(target, deep, mode, duplicate, force);
        }
    }
}
