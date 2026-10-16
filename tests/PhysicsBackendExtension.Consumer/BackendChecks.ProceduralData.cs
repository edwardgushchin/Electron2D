using Electron2D;

public static partial class BackendChecks
{
    public static void RunProceduralData(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("ProceduralData", () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("ProceduralData");
            using var otherWorld = backend is { } otherSelected ? new World(otherSelected) : new World("ProceduralData");
            Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && otherWorld.PhysicsBackend == world.PhysicsBackend &&
                world.PhysicsBackendName == (backend?.ToString() ?? "ProceduralData") && otherWorld.PhysicsBackendName == world.PhysicsBackendName,
                "Both real worlds use the explicitly selected implementation without fallback");
            using var root = new SubViewport { World = world };
            using var disk = new CircleShape { Radius = .5f };
            var moving = new RigidBody
            {
                Position = new(0, -500),
                LinearVelocity = new(6, 0),
                GravityScale = 0,
                CanSleep = false,
                CollisionLayer = 0,
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            moving.AddChild(new CollisionShape { Shape = disk }); root.AddChild(moving);
            using var tree = new SceneTree(root);
            using var query = new PhysicsShapeQueryParameters { Shape = disk, CollideWithAreas = true };
            var hits = new PhysicsShapeResult[4]; var pairs = new Vector2[16]; var cases = 0;
            (Func<RID> Factory, ShapeGeometry Geometry, Vector2 Probe)[] inputs =
            [
                (PhysicsServer.CircleShapeCreate, ShapeGeometry.Circle(8), Vector2.Zero),
                (PhysicsServer.CapsuleShapeCreate, ShapeGeometry.Capsule(2, 9), Vector2.Zero),
                (PhysicsServer.RectangleShapeCreate, ShapeGeometry.Rectangle(new(8, 10)), Vector2.Zero),
                (PhysicsServer.SegmentShapeCreate, ShapeGeometry.Segment(new(-2, 1), new(4, 3)), new(1, 2)),
                (PhysicsServer.WorldBoundaryShapeCreate, ShapeGeometry.WorldBoundary(new(2, -3), 6), Vector2.Zero),
                (PhysicsServer.SeparationRayShapeCreate, ShapeGeometry.SeparationRay(13, true), new(0, 13)),
                (PhysicsServer.ConvexPolygonShapeCreate, ShapeGeometry.ConvexPolygon([Vector2.Zero, new(5, 0), new(0, 5)]), new(1, 1)),
                (PhysicsServer.ConcavePolygonShapeCreate, ShapeGeometry.ConcavePolygon([Vector2.Zero, new(5, 0), new(0, 5), new(5, 5)]), new(2, 0)),
                (() => PhysicsServer.CustomShapeCreate(Annulus.Create(2, 6)), Annulus.Create(3, 8), new(5, 0))
            ];
            foreach (var input in inputs)
            {
                var shape = input.Factory(); var body = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate(); var otherArea = PhysicsServer.AreaCreate();
                using var scene = new StaticBody { CollisionMask = 0 };
                root.AddChild(scene);
                using var data = new ProceduralDataShape(input.Geometry)
                {
                    CustomSolverBias = .3f,
                    ResourceName = "canonical",
                    ResourceLocalToScene = true,
                    ResourceSceneUniqueID = "procedural_source",
                    CanTranslateMessages = false,
                    TranslationDomain = "shape-data"
                };
                try
                {
                    var custom = input.Geometry.Type == PhysicsServer.ShapeType.Custom;
                    var enabled = !custom || backend is null;
                    PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(body, 2);
                    PhysicsServer.BodySetCollisionMask(body, 0); PhysicsServer.BodySetTransform(body, new(0, new(30, 0)));
                    PhysicsServer.AreaSetCollisionLayer(area, 4); PhysicsServer.AreaSetTransform(area, new(0, new(60, 0)));
                    foreach (var id in new[] { scene.GetRID(), body })
                    {
                        PhysicsServer.BodyAddShape(id, shape, disabled: true); PhysicsServer.BodyAddShape(id, shape, disabled: !enabled);
                    }
                    PhysicsServer.AreaAddShape(area, shape, disabled: true); PhysicsServer.AreaAddShape(area, shape, disabled: !enabled);
                    PhysicsServer.BodySetSpace(body, world.Space); PhysicsServer.AreaSetSpace(area, world.Space);
                    PhysicsServer.AreaAddShape(otherArea, shape, disabled: true); PhysicsServer.AreaSetSpace(otherArea, otherWorld.Space);
                    var owner = scene.ShapeFindOwner(1); var previous = scene.ShapeOwnerGetShape(owner, 0);
                    var previousGeometry = previous.GetGeometryData();
                    var supplierFailure = new ApplicationException("procedural geometry supply"); data.Read = () => throw supplierFailure;
                    var failures = ReleaseErrors(() => PhysicsServer.ShapeSetData(shape, data));
                    Check(failures.Length == 1 && ReferenceEquals(failures[0], supplierFailure) && !previous.IsDisposed &&
                        ReferenceEquals(previous, scene.ShapeOwnerGetShape(owner, 0)), "Failed procedural evaluation preserves the original error and private geometry");
                    data.Read = () =>
                    {
                        Reject<InvalidOperationException>(() => scene.CollisionLayer = 9);
                        Reject<InvalidOperationException>(() => PhysicsServer.BodySetCollisionLayer(body, 9));
                        Reject<InvalidOperationException>(() => PhysicsServer.AreaSetCollisionMask(area, 9));
                        Reject<InvalidOperationException>(() => PhysicsServer.AreaSetCollisionMask(otherArea, 9));
                        Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(shape));
                        Reject<InvalidOperationException>(() => PhysicsServer.ShapeSetData(shape, data));
                        Reject<InvalidOperationException>(() => tree.Dispose());
                        Reject<InvalidOperationException>(() => otherWorld.Dispose());
                    };
                    PhysicsServer.ShapeSetData(shape, data); data.Read = null;
                    var current = scene.ShapeOwnerGetShape(owner, 0);
                    Check(previous.IsDisposed && current.GetRID() == shape && current is not ProceduralDataShape,
                        "Server data is an independent concrete wrapper, retaining the owned RID");
                    Check(PhysicsServer.ShapeGetType(shape) == input.Geometry.Type && current.GetGeometryData().Type == input.Geometry.Type &&
                        SameData(current.GetGeometryData(), input.Geometry) &&
                        current.CustomSolverBias == .3f && current.ResourceName == "canonical" && current.ResourceLocalToScene && current.ResourceSceneUniqueID.Length == 0 && current.ResourcePath.Length == 0 &&
                        !current.CanTranslateMessages && current.TranslationDomain == "shape-data" && data.Reads == 2,
                        "Procedural data preserves exact primitive, policy and common stored resource state without a duplicate factory");
                    using var copy = PhysicsServer.ShapeGetData(shape);
                    Check(copy.GetType() == current.GetType() && !ReferenceEquals(copy, current) && SameData(copy.GetGeometryData(), input.Geometry),
                        "ShapeGetData returns an independent concrete resource with exact canonical geometry");
                    var sourceID = data.GetRID(); PhysicsServer.AreaAddShape(otherArea, sourceID, disabled: true);
                    data.SetGeometry(input.Geometry);
                    data.Read = () =>
                    {
                        Reject<InvalidOperationException>(() => PhysicsServer.AreaSetCollisionMask(otherArea, 9));
                        Reject<InvalidOperationException>(() => otherWorld.Dispose());
                        Reject<InvalidOperationException>(() => data.SetGeometry(input.Geometry));
                        Reject<InvalidOperationException>(() => data.Dispose());
                    };
                    using var exported = PhysicsServer.ShapeGetData(sourceID); data.Read = null;
                    Check(exported is not ProceduralDataShape && SameData(exported.GetGeometryData(), input.Geometry) &&
                        exported.CustomSolverBias == .3f && exported.ResourceName == "canonical" && data.Reads == 3,
                        "A live borrowed procedural RID exports independent canonical data without a duplicate factory");
                    data.SetGeometry(ShapeGeometry.Circle(123)); data.CustomSolverBias = .7f; data.Dispose();
                    Reject<ArgumentException>(() => PhysicsServer.ShapeGetType(sourceID));
                    Check(PhysicsServer.ShapeGetType(shape) == input.Geometry.Type && current.CustomSolverBias == .3f &&
                        SameData(current.GetGeometryData(), input.Geometry) && SameData(exported.GetGeometryData(), input.Geometry) && previousGeometry.Type == input.Geometry.Type,
                        "Source edits and disposal cannot alter the committed shape kind or policy");
                    if (enabled)
                    {
                        Check(PhysicsServer.ShapeCollide(world.Space, shape, Transform.Identity, Vector2.Zero, disk.GetRID(),
                            new(0, input.Probe), Vector2.Zero, pairs, out _), "Copied geometry participates in selected resource-pair numerics");
                        Query(); tree.PhysicsFrame(1d / 60);
                        for (var i = 0; i < 128; i++) Frame();
                        var x = moving.Position.X; Collect(); var ownerBytes = GC.GetAllocatedBytesForCurrentThread(); var allBytes = GC.GetTotalAllocatedBytes(true);
                        for (var i = 0; i < 128; i++) Frame();
                        ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBytes; allBytes = GC.GetTotalAllocatedBytes(true) - allBytes;
                        Check(ownerBytes == 0 && allBytes == 0 && Math.Abs(moving.Position.X - x - 12.8f) < .01f,
                            $"Copied {input.Geometry.Type} complete frames: {ownerBytes}/{allBytes} bytes");
                    }
                    else Reject<NotSupportedException>(() => PhysicsServer.BodySetShapeDisabled(body, 1, false));
                    using var wrong = new ProceduralDataShape(input.Geometry.Type == PhysicsServer.ShapeType.Circle ? ShapeGeometry.Rectangle(new(2, 2)) : ShapeGeometry.Circle(2));
                    Reject<ArgumentException>(() => PhysicsServer.ShapeSetData(shape, wrong));
                    Check(ReferenceEquals(current, scene.ShapeOwnerGetShape(owner, 0)) && !current.IsDisposed, "Wrong primitive preserves the committed private view");
                    cases++;

                    void Query()
                    {
                        for (var i = 0; i < 3; i++)
                        {
                            query.CollisionMask = 1u << i; query.Transform = new(0, input.Probe + new Vector2(i * 30, 0));
                            var count = world.DirectSpaceState.IntersectShape(query, hits);
                            var expected = i == 0 ? scene.GetRID() : i == 1 ? body : area;
                            Check(count == 1 && hits[0].ColliderRID == expected && hits[0].ShapeIndex == 1,
                                "Scene/raw body/Area numerics retain copied shape geometry and the disabled slot gap");
                        }
                    }
                    void Frame() { Query(); tree.PhysicsFrame(1d / 60); }
                }
                finally { PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(otherArea); PhysicsServer.FreeRID(shape); }
            }
            Console.WriteLine($"Procedural server data {backend?.ToString() ?? "registered"}, threaded={threaded}: {cases} shape kinds, canonical copies/guards, scene/raw queries and 128-frame zero-allocation brackets passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }

    private static bool SameData(ShapeGeometry a, ShapeGeometry b) => a.Type == b.Type &&
        a.Bounds.Position.IsEqualApprox(b.Bounds.Position) && a.Bounds.Size.IsEqualApprox(b.Bounds.Size) && a.Type switch
        {
            PhysicsServer.ShapeType.Circle => a.GetCircleRadius() == b.GetCircleRadius(),
            PhysicsServer.ShapeType.Capsule => a.GetCapsuleDimensions() == b.GetCapsuleDimensions(),
            PhysicsServer.ShapeType.Rectangle => a.GetRectangleSize() == b.GetRectangleSize(),
            PhysicsServer.ShapeType.Segment => a.GetSegmentEndpoints() == b.GetSegmentEndpoints(),
            PhysicsServer.ShapeType.WorldBoundary => a.GetWorldBoundaryPlane().Normal.IsEqualApprox(b.GetWorldBoundaryPlane().Normal) &&
                Math.Abs(a.GetWorldBoundaryPlane().Distance - b.GetWorldBoundaryPlane().Distance) < .00001f,
            PhysicsServer.ShapeType.SeparationRay => a.GetSeparationRayParameters() == b.GetSeparationRayParameters(),
            PhysicsServer.ShapeType.ConvexPolygon => a.GetConvexPolygonPoints().SequenceEqual(b.GetConvexPolygonPoints()),
            PhysicsServer.ShapeType.ConcavePolygon => a.GetConcavePolygonSegments().SequenceEqual(b.GetConcavePolygonSegments()),
            PhysicsServer.ShapeType.Custom => ReferenceEquals(a, b),
            _ => false
        };

    private sealed class ProceduralDataShape(ShapeGeometry geometry) : Shape
    {
        internal Action? Read;
        internal int Reads;
        internal void SetGeometry(ShapeGeometry value) { EnsureGeometryChangeAccess(); geometry = value; EmitGeometryChanged(); }
        protected override ShapeGeometry GetGeometryCore() { Reads++; Read?.Invoke(); return geometry; }
    }
}
