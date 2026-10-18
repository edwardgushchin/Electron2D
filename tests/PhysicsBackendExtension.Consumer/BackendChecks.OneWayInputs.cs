using Electron2D;

public static partial class BackendChecks
{
    public static void RunOneWayInputs(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("OneWayInputs", () => solver = new());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("OneWayInputs");
            using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 2 };
            var moving = new RigidBody
            {
                Position = new(0, 300),
                LinearVelocity = new(1, 0),
                GravityScale = 0,
                CanSleep = false,
                CollisionMask = 0,
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            moving.AddChild(new CollisionShape { Shape = circle }); root.AddChild(moving);
            using var tree = new SceneTree(root);
            Check(world.PhysicsBackendName == (backend?.ToString() ?? "OneWayInputs"), "One-way input preparation uses the selected implementation");
            for (var role = 0; role < 3; role++)
            {
                using var body = new StaticBody { Position = new(100, 0) };
                var owner = body.CreateShapeOwner(null);
                CollisionShape? child = null; CollisionPolygon? polygon = null;
                if (role == 0)
                {
                    body.ShapeOwnerAddShape(owner, circle); body.ShapeOwnerAddShape(owner, circle);
                    body.ShapeOwnerSetOneWayCollision(owner, true);
                }
                else if (role == 1) { child = new CollisionShape { Shape = circle, OneWayCollision = true }; body.AddChild(child); }
                else
                {
                    polygon = new CollisionPolygon { Polygon = [new(-2, -2), new(2, -2), new(2, 2), new(-2, 2)], OneWayCollision = true };
                    body.AddChild(polygon);
                }
                root.AddChild(body); var rid = body.GetRID(); var count = PhysicsServer.BodyGetShapeCount(rid);
                try
                {
                    for (var field = 0; field < 3; field++)
                    {
                        Pose(Transform.Identity); Margin(2); Direction(Vector2.Down);
                        for (var i = 0; i < 128; i++) { Edit(i); tree.PhysicsFrame(1d / 60); }
                        var held = solver?.Bodies[rid].LastShape.OneWay; var heldDirection = held?.LocalDirection; var heldMargin = held?.Margin;
                        var start = moving.Position.X; long own = 0, all = 0, authoring = 0;
                        Collect();
                        for (var i = 0; i < 128; i++)
                        {
                            var before = GC.GetAllocatedBytesForCurrentThread(); Edit(i); authoring += GC.GetAllocatedBytesForCurrentThread() - before;
                            var allBefore = GC.GetTotalAllocatedBytes(true); before = GC.GetAllocatedBytesForCurrentThread();
                            tree.PhysicsFrame(1d / 60);
                            own += GC.GetAllocatedBytesForCurrentThread() - before; all += GC.GetTotalAllocatedBytes(true) - allBefore;
                            if (solver is not null)
                            {
                                var way = solver.Bodies[rid].LastShape.OneWay;
                                var pose = PhysicsServer.BodyGetShapeTransform(rid, count - 1);
                                var direction = field == 2 && (i & 1) != 0 ? Vector2.Right : Vector2.Down;
                                var margin = field == 1 ? 2 + (i & 1) : 2;
                                Check(way is not null && way.Margin == margin && way.LocalDirection.IsEqualApprox(direction.Rotated(pose.Rotation)),
                                    "The selected owner receives the current transformed immutable one-way policy");
                            }
                        }
                        Check(own == 0 && all == 0, $"One-way deferred step role={role}, field={field}: {own}/{all} B; authoring={authoring} B.");
                        Check(Math.Abs(moving.Position.X - start - 128f / 60) < .01f && PhysicsServer.BodyGetShapeCount(rid) == count &&
                            (held is null || held.LocalDirection == heldDirection && held.Margin == heldMargin),
                            "Real moving frames preserve slot identity and every retained one-way input value");
                        Console.WriteLine($"One-way inputs {backend?.ToString() ?? "registered"}/{threaded}, role={role}, field={field}: 128 physical frames {own}/{all} B; authoring={authoring} B.");

                        void Edit(int i)
                        {
                            if (field == 0) Pose(new((i & 1) * .1f, new Vector2(20, 0)));
                            else if (field == 1) Margin(2 + (i & 1));
                            else Direction((i & 1) == 0 ? Vector2.Down : Vector2.Right);
                        }
                    }
                }
                finally { root.RemoveChild(body); }
                void Pose(Transform pose) { if (role == 0) body.ShapeOwnerSetTransform(owner, pose); else if (child is not null) child.Transform = pose; else polygon!.Transform = pose; }
                void Margin(float value) { if (role == 0) body.ShapeOwnerSetOneWayCollisionMargin(owner, value); else if (child is not null) child.OneWayCollisionMargin = value; else polygon!.OneWayCollisionMargin = value; }
                void Direction(Vector2 value) { if (role == 0) body.ShapeOwnerSetOneWayCollisionDirection(owner, value); else if (child is not null) child.OneWayCollisionDirection = value; else polygon!.OneWayCollisionDirection = value; }
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }
}
