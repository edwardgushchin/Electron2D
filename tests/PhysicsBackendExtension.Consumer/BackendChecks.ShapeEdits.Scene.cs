using Electron2D;

public static partial class BackendChecks
{
    private static void SceneShapeEditChecks(World world, SubViewport root, SceneTree tree, Solver? solver, CircleShape circle, CircleShape replacement)
    {
        var failures = 0;
        foreach (var sensor in new[] { false, true })
        {
            using CollisionObject collider = sensor ? new Area() : new StaticBody();
            using var identity = new Node();
            collider.Position = new(300, sensor ? 100 : 0);
            var owner = collider.CreateShapeOwner(identity);
            var child = new CollisionShape { Shape = circle, Position = new(12, 0) };
            collider.AddChild(child); root.AddChild(collider);
            var rid = collider.GetRID(); var childOwner = collider.ShapeFindOwner(0);
            using var point = new PhysicsPointQueryParameters { CollideWithBodies = !sensor, CollideWithAreas = sensor };
            var hits = new PhysicsPointResult[8];
            try
            {
                for (var operation = 0; operation < 6; operation++)
                {
                    Reset(operation == 3); Hit(12, 2);
                    var attempts = 0;
                    var error = new ApplicationException("scene-shape-preparation");
                    if (solver is not null) solver.ShapeHook = id =>
                    {
                        if (id != rid) return;
                        attempts++;
                        Check(Count() == (operation == 5 ? 0 : operation == 4 ? 2 : 3), "Selected preparation reads committed scene topology");
                        solver.Bodies[id].Shapes = 0;
                        throw error;
                    };
                    Edit(operation);
                    Check(attempts == 0, "Scene indexed authoring defers selected geometry preparation");
                    Authored(operation);
                    if (solver is not null)
                    {
                        for (var retry = 0; retry < 2; retry++)
                        {
                            var errors = ReleaseErrors(() => world.DirectSpaceState.IntersectPoint(point, hits));
                            Check(errors.Length == 1 && ReferenceEquals(errors[0], error), "Failed scene preparation retains the original error on every retry");
                            Authored(operation); failures++;
                        }
                        Check(attempts == 2, "An incomplete selected scene rebuild remains pending");
                        solver.ShapeHook = null;
                    }
                    if (operation == 0) Hit(4, 1);
                    else if (operation == 1) { Hit(0, -1); Hit(24, 1); }
                    else if (operation == 2) Hit(0, -1);
                    else if (operation == 3) Hit(0, 1);
                    else if (operation == 4) { Hit(-12, -1); Hit(0, 0); Hit(12, 1); }
                    else { Hit(-12, -1); Hit(0, -1); Hit(12, -1); }
                }
                child.Shape = replacement;
                Check(Count() == 1 && collider.ShapeFindOwner(0) == childOwner && ReferenceEquals(child.Parent, collider),
                    "Editing a cleared child repopulates its retained owner at the new global index");
                Hit(16, 0);
                Reset(false);
                for (var i = 0; i < 128; i++) Frame(i);
                Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame(i);
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Check(own == 0 && all == 0, $"Scene indexed edits/query/full frames allocate {own}/{all} B");
                Console.WriteLine($"Scene shape edits Area={sensor}: six operations, retained groups/child, deferred failure/retry; 128 full frames {own}/{all} B.");
            }
            finally { if (solver is not null) solver.ShapeHook = null; root.RemoveChild(collider); }

            void Reset(bool disabled)
            {
                Clear(); collider.ShapeOwnerAddShape(owner, circle); collider.ShapeOwnerAddShape(owner, circle);
                child.Shape = null; child.Shape = circle;
                SetTransform(0, new(0, new Vector2(-12, 0))); SetDisabled(1, disabled);
                Check(Count() == 3 && collider.ShapeFindOwner(0) == owner && collider.ShapeFindOwner(1) == owner &&
                    collider.ShapeFindOwner(2) == childOwner, "Manual and child slots share append-order global indices");
            }
            void Edit(int operation)
            {
                if (operation == 0) { if (sensor) PhysicsServer.AreaSetShape(rid, 1, replacement.GetRID()); else PhysicsServer.BodySetShape(rid, 1, replacement.GetRID()); }
                else if (operation == 1) SetTransform(1, new(0, new Vector2(24, 0)));
                else if (operation == 2) SetDisabled(1, true);
                else if (operation == 3) SetDisabled(1, false);
                else if (operation == 4) { if (sensor) PhysicsServer.AreaRemoveShape(rid, 0); else PhysicsServer.BodyRemoveShape(rid, 0); }
                else Clear();
            }
            void Authored(int operation)
            {
                Check(Count() == (operation == 5 ? 0 : operation == 4 ? 2 : 3) && ReferenceEquals(collider.ShapeOwnerGetOwner(owner), identity) &&
                    collider.ShapeOwnerGetTransform(owner) == Transform.Identity && !collider.IsShapeOwnerDisabled(owner) &&
                    ReferenceEquals(child.Parent, collider) && ReferenceEquals(child.Shape, circle) && child.Position == new Vector2(12, 0),
                    "Indexed edits preserve owner identity, group authoring and child configuration");
                if (operation == 0) Check(ReferenceEquals(collider.ShapeOwnerGetShape(owner, 1), replacement), "Replacement reaches the existing group slot");
                else if (operation == 1) Check((sensor ? PhysicsServer.AreaGetShapeTransform(rid, 1) : PhysicsServer.BodyGetShapeTransform(rid, 1)).Origin == new Vector2(24, 0), "Committed local override survives preparation failure");
                else if (operation == 4) Check(collider.ShapeFindOwner(0) == owner && collider.ShapeFindOwner(1) == childOwner, "Removal reindexes across owner boundaries");
                else if (operation == 5) Check(collider.ShapeOwnerGetShapeCount(owner) == 0 && collider.ShapeOwnerGetShapeCount(childOwner) == 0, "Clear retains empty owner groups");
            }
            void Frame(int i)
            {
                SetTransform(1, new(0, new Vector2(i & 1, 0))); SetDisabled(0, (i & 1) == 0);
                Hit(12, 2); tree.PhysicsFrame(1d / 60);
                Check(collider.ShapeFindOwner(2) == childOwner && collider.ShapeOwnerGetTransform(owner) == Transform.Identity,
                    "Warm edited frames retain child/global index and stored group pose");
            }
            int Count() => sensor ? PhysicsServer.AreaGetShapeCount(rid) : PhysicsServer.BodyGetShapeCount(rid);
            void Clear() { if (sensor) PhysicsServer.AreaClearShapes(rid); else PhysicsServer.BodyClearShapes(rid); }
            void SetTransform(int index, Transform pose) { if (sensor) PhysicsServer.AreaSetShapeTransform(rid, index, pose); else PhysicsServer.BodySetShapeTransform(rid, index, pose); }
            void SetDisabled(int index, bool value) { if (sensor) PhysicsServer.AreaSetShapeDisabled(rid, index, value); else PhysicsServer.BodySetShapeDisabled(rid, index, value); }
            void Hit(float x, int index)
            {
                point.Position = collider.Position + new Vector2(x, 0); var count = world.DirectSpaceState.IntersectPoint(point, hits);
                Check(count == (index < 0 ? 0 : 1) && (count == 0 || hits[0].ColliderRID == rid && hits[0].ShapeIndex == index),
                    "Scene indexed edits publish actual geometry and exact logical query indices");
            }
        }
        Console.WriteLine($"Scene indexed preparation failures: {failures}; authored state retained across repeated failures.");
    }
}
