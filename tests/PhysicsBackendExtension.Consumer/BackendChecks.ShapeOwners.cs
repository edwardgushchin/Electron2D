using Electron2D;

public static partial class BackendChecks
{
    public static void RunShapeOwners(PhysicsServer.Backend? backend = null)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("ShapeOwners", () => solver = new());
        var space = backend is { } selected ? PhysicsServer.SpaceCreate(selected) : PhysicsServer.SpaceCreate("ShapeOwners");
        using var circle = new CircleShape { Radius = 3 };
        using var identity = new Resource();
        CollisionObject[] colliders = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone(), new Area()];
        var readFamilies = new List<Action[]>();
        try
        {
            PhysicsServer.SpaceSetActive(space, true);
            for (var i = 0; i < colliders.Length; i++)
            {
                var collider = colliders[i]; collider.Position = new(i * 50, 0);
                var owner = collider.CreateShapeOwner(identity); collider.ShapeOwnerAddShape(owner, circle);
                if (collider is Area) PhysicsServer.AreaSetSpace(collider.GetRID(), space); else PhysicsServer.BodySetSpace(collider.GetRID(), space);
                Check(collider.Tree is null, "Explicit collider attachment has no scene owner-thread guard");
                Read(collider, owner);
                Action[] reads = [() => collider.GetShapeOwners(), () => collider.ShapeOwnerGetOwner(owner), () => collider.ShapeOwnerGetTransform(owner),
                    () => collider.IsShapeOwnerDisabled(owner), () => collider.IsShapeOwnerOneWayCollisionEnabled(owner),
                    () => collider.GetShapeOwnerOneWayCollisionMargin(owner), () => collider.GetShapeOwnerOneWayCollisionDirection(owner),
                    () => collider.ShapeOwnerGetShapeCount(owner), () => collider.ShapeOwnerGetShape(owner, 0),
                    () => collider.ShapeOwnerGetShapeIndex(owner, 0), () => collider.ShapeFindOwner(0)];
                readFamilies.Add(reads);
                Task.Run(() => { foreach (var read in reads) Reject<InvalidOperationException>(read); }).GetAwaiter().GetResult();
                if (solver is not null)
                {
                    solver.PointHook = () =>
                    {
                        Read(collider, owner);
                        Reject<InvalidOperationException>(() => collider.ShapeOwnerSetTransform(owner, Transform.Identity));
                        Reject<InvalidOperationException>(() => collider.ShapeOwnerSetDisabled(owner, false));
                        Reject<InvalidOperationException>(() => collider.CreateShapeOwner(null));
                    };
                    try { using var query = new PhysicsPointQueryParameters { Position = collider.Position, CollideWithAreas = true }; _ = PhysicsServer.SpaceGetDirectState(space).IntersectPoint(query); }
                    finally { solver.PointHook = null; }
                }
                var other = collider.CreateShapeOwner(null); collider.ShapeOwnerAddShape(other, circle);
                collider.ShapeOwnerRemoveShape(owner, 0);
                Check(collider.ShapeOwnerGetShapeIndex(other, 0) == 0 && collider.ShapeFindOwner(0) == other,
                    "Removing a shape reindexes all following owners without changing owner identity");
                collider.ShapeOwnerClearShapes(other); collider.RemoveShapeOwner(other);
                Check(collider.CreateShapeOwner(null) == other && collider.ShapeOwnerGetShapeCount(owner) == 0, "Empty groups persist and removed highest owner IDs may be reused");
                collider.ShapeOwnerAddShape(owner, circle);
                collider.ShapeOwnerSetOneWayCollision(owner, true); collider.ShapeOwnerSetOneWayCollisionMargin(owner, 2);
                collider.ShapeOwnerSetOneWayCollisionDirection(owner, new(0, 5));
                Check(collider.IsShapeOwnerOneWayCollisionEnabled(owner) == (collider is not Area) && collider.GetShapeOwnerOneWayCollisionMargin(owner) == (collider is Area ? 0 : 2) &&
                    collider.GetShapeOwnerOneWayCollisionDirection(owner) == Vector2.Down, "One-way group authoring preserves normalized policy across collider roles");
            }
            for (var i = 0; i < 128; i++) Frame();
            Collect(); GC.WaitForPendingFinalizers(); _ = GC.GetTotalAllocatedBytes(true);
            var ownerBytes = GC.GetAllocatedBytesForCurrentThread(); var allBytes = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBytes; allBytes = GC.GetTotalAllocatedBytes(true) - allBytes;
            Check(ownerBytes == 0 && allBytes == 0, $"Owner metadata/full frames allocated {ownerBytes}/{allBytes} B");
            Console.WriteLine($"Shape-owner warm metadata on {backend?.ToString() ?? "registered"}: 128 full frames {ownerBytes}/{allBytes} B.");
            if (solver is not null)
            {
                solver.FailSolve = true;
                Reject<AggregateException>(() => PhysicsServer.SpaceStep(space, 1d / 120));
                foreach (var family in readFamilies) foreach (var read in family) Reject<InvalidOperationException>(read);
                foreach (var collider in colliders)
                {
                    var owner = 0u;
                    Reject<InvalidOperationException>(() => collider.GetShapeOwners());
                    Reject<InvalidOperationException>(() => collider.ShapeOwnerGetShapeCount(owner));
                    Reject<InvalidOperationException>(() => collider.ShapeOwnerSetDisabled(owner, false));
                    Reject<InvalidOperationException>(() => collider.CreateShapeOwner(null));
                }
            }
        }
        finally
        {
            foreach (var collider in colliders) collider.Dispose();
            PhysicsServer.FreeRID(space);
        }
        Console.WriteLine($"Shape-owner {backend?.ToString() ?? "registered"}: all scene body siblings/Area, tree-less owner guards, numerical reads, indexing and failed-world cleanup passed.");

        void Frame()
        {
            PhysicsServer.SpaceStep(space, 1d / 120);
            foreach (var collider in colliders)
                Check(collider.ShapeOwnerGetShapeCount(0) == 1 && collider.ShapeOwnerGetShapeIndex(0, 0) == 0 && collider.ShapeFindOwner(0) == 0,
                    "Warmed metadata keeps owner and logical slot identity through actual simulation");
        }
        void Read(CollisionObject collider, uint owner)
        {
            Check(collider.GetShapeOwners().Contains(owner) && ReferenceEquals(collider.ShapeOwnerGetOwner(owner), identity) &&
                collider.ShapeOwnerGetTransform(owner) == Transform.Identity && !collider.IsShapeOwnerDisabled(owner) &&
                !collider.IsShapeOwnerOneWayCollisionEnabled(owner) && collider.GetShapeOwnerOneWayCollisionMargin(owner) == 0 &&
                collider.GetShapeOwnerOneWayCollisionDirection(owner) == Vector2.Down && collider.ShapeOwnerGetShapeCount(owner) == 1 &&
                ReferenceEquals(collider.ShapeOwnerGetShape(owner, 0), circle) && collider.ShapeOwnerGetShapeIndex(owner, 0) == 0 && collider.ShapeFindOwner(0) == owner,
                "The complete authored owner read family remains available without numerical preparation");
        }
    }
}
