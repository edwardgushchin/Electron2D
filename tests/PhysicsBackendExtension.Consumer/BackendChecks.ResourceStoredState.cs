using System.Globalization;
using Electron2D;

public static partial class BackendChecks
{
    private const string StoredResourceDomain = "physical-resource-storage";

    private static void ResourceStoredState(World world)
    {
        Resource[] resources = [new Resource(), new CircleShape { Radius = 3 }, new CapsuleShape { Radius = 2, Height = 9 },
            new RectangleShape { Size = new(4, 7) }, new SegmentShape { A = new(-2, 1), B = new(3, 4) },
            new WorldBoundaryShape { Normal = Vector2.Up, Distance = 4 }, new SeparationRayShape { Length = 7, SlideOnSlope = true },
            new ConvexPolygonShape { Points = [Vector2.Zero, new(5, 0), new(0, 6)] },
            new ConcavePolygonShape { Segments = [Vector2.Zero, new(5, 1), new(5, 1), new(6, 3)] },
            new CustomShape(Annulus.Create(2, 6)), new StoredShape(),
            new PhysicsMaterial { Friction = .6f, Bounce = .4f, Rough = true, Absorbent = true }, world];
        ResourceEdgePolicy(resources);
        ResourceLifecycle(resources);
        var culture = TranslationServer.Culture;
        using var catalog = new Translation { Locale = "en" }; catalog.AddMessage("storage probe", "translated probe");
        TranslationServer.Culture = CultureInfo.GetCultureInfo("en"); TranslationServer.AddTranslation(catalog, StoredResourceDomain);
        try
        {
            foreach (var source in resources)
            {
                if (source != world)
                {
                    source.ResourceName = "stored"; source.ResourceLocalToScene = true;
                    source.SetPathCache("external://stored-" + source.InstanceID); source.ResourceSceneUniqueID = "authored";
                }
                if (source is Shape shape) shape.CustomSolverBias = .3f;
                source.TranslationDomain = StoredResourceDomain;
                foreach (var enabled in new[] { false, true })
                {
                    source.CanTranslateMessages = enabled;
                    for (var mode = 0; mode < 5; mode++)
                    {
                        using var copy = mode < 2 ? source.Duplicate(mode == 1) : source.DuplicateDeep((DeepDuplicateMode)(mode - 2));
                        Check(copy.GetType() == source.GetType() && copy.InstanceID != source.InstanceID &&
                            copy.ResourcePath.Length == 0 && copy.ResourceSceneUniqueID.Length == 0 && copy.GetLocalScene() is null,
                            "Resource duplicates preserve exact type and independent object identity without external scene identity");
                        StoredState(source, copy);
                        var id = copy.InstanceID; copy.SetPathCache("local://target"); copy.ResourceSceneUniqueID = "target";
                        copy.CanTranslateMessages = !enabled; copy.TranslationDomain = "other";
                        var changes = 0; var sourceChanges = 0;
                        copy.Changed += _ => { changes++; StoredState(source, copy); };
                        void Changed(Resource _) => sourceChanges++;
                        source.Changed += Changed;
                        try { copy.CopyFromResource(source); copy.CopyFromResource(copy); }
                        finally { source.Changed -= Changed; }
                        Check(changes == 1 && sourceChanges == 0 && copy.InstanceID == id &&
                            copy.ResourcePath == "local://target" && copy.ResourceSceneUniqueID == "target",
                            "CopyFromResource preserves target identity and publishes one complete stored-state change; self-copy is silent");
                        StoredState(source, copy);
                    }
                }
            }
            using var polygon = new ConvexPolygonShape { Points = [Vector2.Zero, new(5, 0), new(0, 6)] };
            using var duplicate = (ConvexPolygonShape)polygon.Duplicate(); var original = duplicate.GetGeometryData();
            polygon.Points = [Vector2.Zero, new(8, 0), new(0, 9)];
            Check(ReferenceEquals(duplicate.GetGeometryData(), original) && duplicate.Points[1] == new Vector2(5, 0),
                "Duplicated polygon storage remains independent after source authoring changes");
            Console.WriteLine("Physical resource stored state: base, all nine production shapes, application Shape, material and shared World; five copy modes and actual translation policy passed.");
        }
        finally
        {
            TranslationServer.RemoveTranslation(catalog, StoredResourceDomain); TranslationServer.Culture = culture;
            foreach (var resource in resources) if (resource != world) resource.Dispose();
        }
    }

    private static void StoredState(Resource source, Resource copy)
    {
        Check(copy.CanTranslateMessages == source.CanTranslateMessages && copy.TranslationDomain == source.TranslationDomain &&
            copy.Tr("storage probe") == (source.CanTranslateMessages ? "translated probe" : "storage probe"),
            "Resource copying must retain inherited stored translation policy and its actual message behavior");
        Check(copy.ResourceName == source.ResourceName && copy.ResourceLocalToScene == source.ResourceLocalToScene,
            "Stored resource name/locality survive every copy mode");
        if (source is Shape shape)
        {
            var target = (Shape)copy; var a = shape.GetGeometryData(); var b = target.GetGeometryData();
            Check(target.CustomSolverBias == shape.CustomSolverBias && a.Type == b.Type && a.Bounds == b.Bounds,
                "Copied shape retains correction policy, exact primitive type and physical bounds");
            Check(a.Type switch
            {
                PhysicsServer.ShapeType.Circle => a.GetCircleRadius() == b.GetCircleRadius(),
                PhysicsServer.ShapeType.Capsule => a.GetCapsuleDimensions() == b.GetCapsuleDimensions(),
                PhysicsServer.ShapeType.Rectangle => a.GetRectangleSize() == b.GetRectangleSize(),
                PhysicsServer.ShapeType.Segment => a.GetSegmentEndpoints() == b.GetSegmentEndpoints(),
                PhysicsServer.ShapeType.WorldBoundary => a.GetWorldBoundaryPlane() == b.GetWorldBoundaryPlane(),
                PhysicsServer.ShapeType.SeparationRay => a.GetSeparationRayParameters() == b.GetSeparationRayParameters(),
                PhysicsServer.ShapeType.ConvexPolygon => a.GetConvexPolygonPoints().SequenceEqual(b.GetConvexPolygonPoints()),
                PhysicsServer.ShapeType.ConcavePolygon => a.GetConcavePolygonSegments().SequenceEqual(b.GetConcavePolygonSegments()),
                PhysicsServer.ShapeType.Custom => ReferenceEquals(a, b),
                _ => false
            }, "Copied physical geometry retains all authored primitive data");
        }
        if (source is PhysicsMaterial material)
        {
            var target = (PhysicsMaterial)copy;
            Check(target.Friction == material.Friction && target.Bounce == material.Bounce && target.Rough == material.Rough && target.Absorbent == material.Absorbent,
                "Material duplication retains all response parameters");
        }
        if (source is World world)
        {
            var target = (World)copy;
            Check(target.Space == world.Space && target.Canvas == world.Canvas && target.NavigationMap == world.NavigationMap &&
                target.PhysicsBackend == world.PhysicsBackend && target.RequestedPhysicsBackend == world.RequestedPhysicsBackend,
                "World duplicates preserve the accepted shared runtime and backend identities");
        }
    }

    private sealed class StoredShape : Shape
    {
        protected override ShapeGeometry GetGeometryCore() => ShapeGeometry.Circle(4);
        protected override Resource CreateDuplicateInstance() => new StoredShape();
    }
}
