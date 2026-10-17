using Electron2D;

public static partial class BackendChecks
{
    private static void ShapeFactoryChecks(PhysicsServer.Backend? backend)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ShapeFactories", () => solver = new());
        using var world = backend is { } builtin ? new World(builtin) : new World("ShapeFactories");
        using var root = new SubViewport { World = world };
        var scene = new StaticBody { Name = "ShapeView" }; root.AddChild(scene);
        using var tree = new SceneTree(root);
        (Func<RID> Create, PhysicsServer.ShapeType Kind, Shape Data)[] cases =
        [
            (PhysicsServer.CircleShapeCreate, PhysicsServer.ShapeType.Circle, new CircleShape { Radius = 8 }),
            (PhysicsServer.CapsuleShapeCreate, PhysicsServer.ShapeType.Capsule, new CapsuleShape { Radius = 2, Height = 9 }),
            (PhysicsServer.RectangleShapeCreate, PhysicsServer.ShapeType.Rectangle, new RectangleShape { Size = new(8, 10) }),
            (PhysicsServer.SegmentShapeCreate, PhysicsServer.ShapeType.Segment, new SegmentShape { A = new(-2, 1), B = new(4, 3) }),
            (PhysicsServer.WorldBoundaryShapeCreate, PhysicsServer.ShapeType.WorldBoundary, new WorldBoundaryShape { Normal = new(0, -2), Distance = 6 }),
            (PhysicsServer.SeparationRayShapeCreate, PhysicsServer.ShapeType.SeparationRay, new SeparationRayShape { Length = 13, SlideOnSlope = true }),
            (PhysicsServer.ConvexPolygonShapeCreate, PhysicsServer.ShapeType.ConvexPolygon, new ConvexPolygonShape { Points = [Vector2.Zero, new(5, 0), new(0, 5)] }),
            (PhysicsServer.ConcavePolygonShapeCreate, PhysicsServer.ShapeType.ConcavePolygon, new ConcavePolygonShape { Segments = [Vector2.Zero, new(5, 0), new(0, 5), new(5, 5)] })
        ];
        try
        {
            foreach (var item in cases)
            {
                var id = item.Create(); var raw = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
                try
                {
                    Check(PhysicsServer.ShapeGetType(id) == item.Kind, "Every factory assigns the exact common geometry type");
                    Reject<InvalidOperationException>(() => PhysicsServer.ShapeGetData(id));
                    PhysicsServer.ShapeSetData(id, item.Data);
                    using var defaults = PhysicsServer.ShapeGetData(id);
                    Check(defaults.GetType() == item.Data.GetType() && defaults.CustomSolverBias == 0, "Explicitly configured data is an independent typed resource");
                    PhysicsServer.BodyAddShape(scene.GetRID(), id, disabled: true);
                    var owner = scene.GetShapeOwners().Single(ownerID => scene.ShapeOwnerGetShapeCount(ownerID) != 0); var old = scene.ShapeOwnerGetShape(owner, 0); var retained = old.GetGeometryData();
                    Reject<InvalidOperationException>(() => old.Dispose());
                    PhysicsServer.BodyAddShape(raw, id, disabled: true); PhysicsServer.AreaAddShape(area, id, disabled: true);
                    PhysicsServer.BodySetSpace(raw, world.Space); PhysicsServer.AreaSetSpace(area, world.Space);
                    item.Data.CustomSolverBias = .3f;
                    PhysicsServer.ShapeSetData(id, item.Data);
                    var current = scene.ShapeOwnerGetShape(owner, 0);
                    Check(old.IsDisposed && !ReferenceEquals(old, current) && current.GetRID() == id &&
                        current.GetType() == item.Data.GetType() && Signature(current) == Signature(item.Data) && current.CustomSolverBias == .3f,
                        "Replacement retains RID and exact typed geometry/policy while retiring the previous private view");
                    _ = retained.Bounds;
                    using var copy = PhysicsServer.ShapeGetData(id);
                    Check(!ReferenceEquals(copy, current) && Signature(copy) == Signature(current), "Reads duplicate typed geometry rather than expose private authoring");
                    copy.CustomSolverBias = .6f;
                    Check(current.CustomSolverBias == .3f, "Returned resource edits leave server geometry independent");
                    if (solver is not null)
                        Check(ReferenceEquals(solver.Bodies[raw].LastShape.Shape, current) && ReferenceEquals(solver.Bodies[area].LastShape.Shape, current),
                            "Selected raw Body/Area descriptors receive the new common resource identity even for disabled slots");
                    using var wrong = item.Data is CircleShape ? (Shape)new RectangleShape() : new CircleShape();
                    Reject<ArgumentException>(() => PhysicsServer.ShapeSetData(id, wrong));
                    Exception? failure = null;
                    var thread = new Thread(() =>
                    {
                        try { Reject<InvalidOperationException>(() => PhysicsServer.ShapeSetData(id, item.Data)); }
                        catch (Exception error) { failure = error; }
                    });
                    thread.Start(); thread.Join(); if (failure is not null) throw failure;
                    Check(ReferenceEquals(current, scene.ShapeOwnerGetShape(owner, 0)), "Wrong type and wrong-owner facade writes preserve the live private view");
                    PhysicsServer.FreeRID(id);
                    Check(current.IsDisposed && PhysicsServer.BodyGetShapeCount(raw) == 0 && PhysicsServer.AreaGetShapeCount(area) == 0 &&
                        scene.ShapeOwnerGetShapeCount(owner) == 0, "Shape release retires every scene/raw slot and current view");
                    Reject<ArgumentException>(() => PhysicsServer.ShapeGetType(id));
                }
                finally
                {
                    PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(area);
                    try { PhysicsServer.FreeRID(id); } catch (ArgumentException) { }
                }
            }
            Console.WriteLine($"Shape resources {backend?.ToString() ?? "registered"}: eight factories, type/data/copy/policy, private view lifetime and scene/raw release passed.");
        }
        finally { foreach (var item in cases) item.Data.Dispose(); }
    }

    private static string Signature(Shape shape) => shape switch
    {
        CircleShape circle => circle.Radius.ToString(),
        CapsuleShape capsule => $"{capsule.Radius}/{capsule.Height}",
        RectangleShape rectangle => rectangle.Size.ToString(),
        SegmentShape segment => $"{segment.A}/{segment.B}",
        WorldBoundaryShape boundary => $"{boundary.Normal}/{boundary.Distance}",
        SeparationRayShape ray => $"{ray.Length}/{ray.SlideOnSlope}",
        ConvexPolygonShape polygon => string.Join('|', polygon.Points),
        ConcavePolygonShape polygon => string.Join('|', polygon.Segments),
        _ => throw new InvalidOperationException("Unexpected typed factory geometry")
    };
}
