using Electron2D;

public static partial class BackendChecks
{
    private sealed class EdgeGraph : Resource
    {
        internal static readonly PropertyDescriptor<EdgeGraph, Resource?> OrdinaryProperty = new(nameof(Ordinary), g => g.Ordinary, (g, v) => g.Ordinary = v, _ => null, stored: true);
        public Resource? Ordinary { get; set; }
        public Resource? Forced { get; private set; }
        public Resource? ForcedAlias { get; private set; }
        public Resource? Never { get; set; }
        public Resource[] Items { get; set; } = [];
        public Dictionary<Resource, Resource> Map { get; set; } = [];
        public bool ForceFirst { get; set; }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(OrdinaryProperty);
        protected override Resource CreateDuplicateInstance() => new EdgeGraph();
        protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode,
            Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
        {
            var g = (EdgeGraph)target; g.ForceFirst = ForceFirst;
            if (ForceFirst) g.Forced = force(Ordinary);
            g.Ordinary = duplicate(Ordinary);
            g.Items = deep ? Items.Select(r => duplicate(r)!).ToArray() : Items;
            g.Map = deep ? Map.ToDictionary(p => duplicate(p.Key)!, p => duplicate(p.Value)!) : Map;
            if (!ForceFirst) g.Forced = force(Ordinary);
            g.ForcedAlias = force(Ordinary); g.Never = Never;
        }
    }

    private static void ResourceEdgePolicy(Resource[] resources)
    {
        foreach (var resource in resources)
        {
            var originalPath = resource.ResourcePath;
            try
            {
                foreach (var path in new[] { "", "scene.e2dres::edge", "external.e2dres" })
                {
                    resource.SetPathCache(path);
                    foreach (var first in new[] { false, true })
                    {
                        using var graph = new EdgeGraph { Ordinary = resource, Never = resource, Items = [resource, resource], Map = new() { [resource] = resource }, ForceFirst = first };
                        for (var mode = 0; mode < 5; mode++)
                        {
                            using var copy = CopyEdges(graph, mode); using var acquired = copy.Forced!;
                            var duplicated = OrdinaryDuplicates(resource, mode);
                            var expected = duplicated ? acquired : resource;
                            Check(copy.Ordinary == expected && copy.Never == resource && copy.ForcedAlias == acquired && acquired != resource &&
                                acquired.GetType() == resource.GetType() && acquired.ResourcePath.Length == 0,
                                "Per-edge ordinary/forced/never resource policies remain independent of cache and traversal order");
                            var collectionExpected = mode == 0 ? resource : expected;
                            Check(copy.Items.All(r => r == collectionExpected) && copy.Map.Single().Key == collectionExpected && copy.Map.Single().Value == collectionExpected,
                                "Scalar and collection references preserve their edge policy and aliases");
                            Check(mode == 0 ? ReferenceEquals(copy.Items, graph.Items) && ReferenceEquals(copy.Map, graph.Map) :
                                !ReferenceEquals(copy.Items, graph.Items) && !ReferenceEquals(copy.Map, graph.Map),
                                "Container cloning and nested-resource policies remain separate");
                            if (resource is Shape shape) Check(((Shape)acquired).GetRect() == shape.GetRect(), "Acquired physical shape keeps its geometry");
                            if (resource is World world) Check(((World)acquired).Space == world.Space, "Acquired world wrapper retains its shared physical runtime");
                        }
                    }
                }
            }
            finally { resource.SetPathCache(originalPath); }
        }
        using var root = new EdgeGraph { ResourceLocalToScene = true };
        root.Ordinary = root; root.Never = root; root.Items = [root, root]; root.Map[root] = root;
        foreach (var path in new[] { "", "scene.e2dres::cycle", "external.e2dres" })
        {
            root.SetPathCache(path);
            foreach (var first in new[] { false, true })
            {
                root.ForceFirst = first;
                for (var mode = 0; mode < 5; mode++)
                {
                    using var copy = CopyEdges(root, mode);
                    var expected = OrdinaryDuplicates(root, mode) ? copy : root;
                    Check(copy.Ordinary == expected && copy.Never == root && copy.Forced == copy && copy.ForcedAlias == copy &&
                        copy.Items.All(r => r == (mode == 0 ? root : expected)) && copy.Map.Single().Key == (mode == 0 ? root : expected),
                        "Only duplicated edges remap cycles; shared root references stay original despite an acquired root/forced cycle");
                }
            }
        }
        Console.WriteLine($"Resource edge policy: {resources.Length} physical/base families, 390 mode/path/order cases, 30 root-cycle cases and per-edge aliases passed.");
    }

    private static EdgeGraph CopyEdges(EdgeGraph source, int mode) =>
        (EdgeGraph)(mode < 2 ? source.Duplicate(mode == 1) : source.DuplicateDeep((DeepDuplicateMode)(mode - 2)));
    private static bool OrdinaryDuplicates(Resource source, int mode) =>
        mode == 4 || (mode is 1 or 3) && source.IsBuiltIn;

    private sealed class EdgeScene : Entity
    {
        private static readonly PropertyDescriptor<EdgeScene, EdgeGraph?> GraphProperty = new(nameof(Graph), n => n.Graph, (n, v) => n.Graph = v, _ => null, stored: true);
        private static readonly PropertyDescriptor<EdgeScene, Resource?> DirectProperty = new(nameof(Direct), n => n.Direct, (n, v) => n.Direct = v, _ => null, stored: true);
        public EdgeGraph? Graph { get; set; }
        public Resource? Direct { get; set; }
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(GraphProperty).Append(DirectProperty);
        protected override Func<Node> CreateSceneInstanceFactory() => CreateEdgeScene;
    }
    private static Node CreateEdgeScene() => new EdgeScene();

    private static void ResourceEdgeScenes(Node physicalRoot, World world)
    {
        using var circle = new CircleShape { Radius = 2 }; using var material = new PhysicsMaterial { Friction = .4f };
        var worldLocal = world.ResourceLocalToScene;
        try
        {
            foreach (var resource in new Resource[] { circle, material, world })
                foreach (var local in new[] { false, true })
                    foreach (var firstForced in new[] { false, true })
                    {
                        resource.ResourceLocalToScene = local;
                        using var graph = new EdgeGraph
                        {
                            Ordinary = resource,
                            Never = resource,
                            Items = [resource, resource],
                            Map = new() { [resource] = resource },
                            ForceFirst = firstForced,
                            ResourceLocalToScene = true
                        };
                        using var source = new EdgeScene { Name = "Source", Graph = graph, Direct = resource };
                        var body = new StaticBody { Name = "Body", PhysicsMaterialOverride = material }; var shape = new CollisionShape { Name = "Shape", Shape = circle };
                        source.AddChild(body); body.Owner = source; body.AddChild(shape); shape.Owner = source;
                        using var packed = new PackedScene(); packed.Pack(source);
                        using var first = (EdgeScene)packed.Instantiate(); using var second = (EdgeScene)packed.Instantiate();
                        var a = first.Graph!; var b = second.Graph!;
                        Check(a.Forced != b.Forced && a.Never == resource && b.Never == resource && a.ForcedAlias == a.Forced && b.ForcedAlias == b.Forced,
                            "Each physical scene owns its acquired resource aliases while never-copy edges remain borrowed");
                        var expectedA = local ? a.Forced : resource; var expectedB = local ? b.Forced : resource;
                        Check(a.Ordinary == expectedA && b.Ordinary == expectedB && first.Direct == expectedA && second.Direct == expectedB &&
                            a.Items.All(r => r == expectedA) && b.Items.All(r => r == expectedB) && a.Map.Single().Key == expectedA && b.Map.Single().Value == expectedB,
                            "Scene-local edge selection precedes caches for direct, nested and collection resources, regardless of force order");
                        if (local) Check(a.Ordinary!.GetLocalScene() == first && b.Ordinary!.GetLocalScene() == second,
                            "Local acquired resources borrow their own scene root");
                        first.Name = "EdgeFirst"; second.Name = "EdgeSecond"; first.Position = new(90, 0); second.Position = new(100, 0);
                        physicalRoot.AddChild(first); physicalRoot.AddChild(second); physicalRoot.Tree!.PhysicsFrame(1d / 60);
                        if (resource == circle)
                            Check(first.GetNode<CollisionShape>("Body/Shape").Shape == expectedA && second.GetNode<CollisionShape>("Body/Shape").Shape == expectedB,
                                "Active physical shape consumers keep the same per-edge resource identity");
                        if (resource == material)
                            Check(first.GetNode<StaticBody>("Body").PhysicsMaterialOverride == expectedA && second.GetNode<StaticBody>("Body").PhysicsMaterialOverride == expectedB,
                                "Active body material consumers keep the same per-edge resource identity");
                        if (resource == world)
                            Check(((World)expectedA!).Space == world.Space && ((World)expectedB!).PhysicsBackend == world.PhysicsBackend,
                                "Both ordinary and acquired world wrappers project the actual selected physics runtime");
                        using var query = new PhysicsPointQueryParameters { Position = first.Position }; var hits = new PhysicsPointResult[1];
                        Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == first.GetNode<StaticBody>("Body").GetRID(),
                            "Selected physical simulation/query remains valid with shared and acquired scene resources");
                        var owned = a.Forced!; first.Dispose();
                        Check(owned.IsDisposed && !resource.IsDisposed && !b.Forced!.IsDisposed,
                            "Scene retirement releases only its acquired targets, preserving caller and the other scene");
                    }
        }
        finally { world.ResourceLocalToScene = worldLocal; }
        Console.WriteLine("Resource scene edges: shape/material/world, both local flags and force orders, direct/nested/body identities, actual physics/query and retirement passed.");
    }
}
