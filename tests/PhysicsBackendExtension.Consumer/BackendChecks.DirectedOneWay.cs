using Electron2D;

public static partial class BackendChecks
{
    private static void DirectedOneWaySources(Func<World> create, Action<Action, string> warm)
    {
        for (var representation = 0; representation < 2; representation++)
        {
            using var world = create(); using var root = new SubViewport { World = world };
            using Shape ray = representation == 0 ? new SeparationRayShape { Length = 5, SlideOnSlope = true } : new DirectedPolicyShape();
            using var circle = new CircleShape { Radius = 8 }; using var box = new RectangleShape { Size = new(200, 10) };
            var angle = representation == 0 ? 0 : .37f; var axis = Vector2.Down.Rotated(angle);
            var floor = new StaticBody { Position = axis * 25, Rotation = angle };
            floor.AddChild(new CollisionShape { Shape = circle, Disabled = true });
            floor.AddChild(new CollisionShape { Shape = box, OneWayCollision = true, OneWayCollisionMargin = 0, OneWayCollisionDirection = representation == 0 ? Vector2.Zero : Vector2.Up }); root.AddChild(floor);
            var scene = new StaticBody { Position = new(1000, 500), CollisionLayer = 0 };
            scene.AddChild(new CollisionShape { Shape = circle, Disabled = true });
            var raySlot = new CollisionShape { Shape = ray }; scene.AddChild(raySlot);
            scene.AddChild(new CollisionShape { Shape = circle }); root.AddChild(scene);
            using var tree = new SceneTree(root); var body = PhysicsServer.BodyCreate();
            using var query = new PhysicsTestMotionParameters { From = new(angle, Vector2.Zero), Motion = axis * 30, Margin = 0 };
            using var result = new PhysicsTestMotionResult(); using var sceneResult = new KinematicCollision();
            try
            {
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Kinematic); PhysicsServer.BodySetCollisionLayer(body, 0);
                PhysicsServer.BodyAddShape(body, circle.GetRID(), disabled: true); PhysicsServer.BodyAddShape(body, ray.GetRID());
                PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetSpace(body, world.Space);
                warm(Frame, "one-way mixed source and represented ray");
                void Frame()
                {
                    PhysicsServer.BodySetShapeDisabled(body, 1, false); raySlot.Disabled = false; tree.PhysicsFrame(1d / 60);
                    Check(PhysicsServer.BodyTestMotion(body, query, result) && result.GetCollisionLocalShape() == 1 && result.GetColliderShape() == 1 &&
                        result.GetColliderRID() == floor.GetRID() && MathF.Abs(result.GetCollisionUnsafeFraction() - .5f) < 1f / 256 + .00005f,
                        "Ray bypasses the target policy while its ordinary sibling still passes through");
                    Check(scene.TestMove(query.From, query.Motion, sceneResult, 0) && ReferenceEquals(sceneResult.GetLocalShape(), raySlot) &&
                        sceneResult.GetColliderShapeIndex() == 1 && sceneResult.GetColliderRID() == floor.GetRID() && sceneResult.GetTravel().DistanceTo(result.GetTravel()) < .003f,
                        "Scene and raw mixed sources preserve the ray winner and both logical indices");
                    PhysicsServer.BodySetShapeDisabled(body, 1, true); raySlot.Disabled = true; tree.PhysicsFrame(1d / 60);
                    Check(!PhysicsServer.BodyTestMotion(body, query, result) && !scene.TestMove(query.From, query.Motion, sceneResult, 0),
                        "Disabling the ray leaves an ordinary source subject to one-way policy");
                }
                PhysicsServer.BodySetShapeDisabled(body, 1, true); floor.CollisionLayer = 0;
                var target = PhysicsServer.BodyCreate();
                try
                {
                    PhysicsServer.BodySetMode(target, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(target, ray.GetRID());
                    PhysicsServer.BodySetTransform(target, new(angle, Vector2.Zero)); PhysicsServer.BodySetSpace(target, world.Space);
                    query.From = new(angle, axis * 10); query.Motion = Vector2.Zero; query.RecoveryAsCollision = true;
                    Check(PhysicsServer.BodyTestMotion(body, query, result), "An ordinary source recovers from a two-sided target ray");
                    PhysicsServer.BodySetShapeAsOneWayCollision(target, 0, true, 0, Vector2.Zero);
                    Check(!PhysicsServer.BodyTestMotion(body, query, result), "A ray's own target policy still filters an ordinary source");
                }
                finally { PhysicsServer.FreeRID(target); }
            }
            finally { PhysicsServer.FreeRID(body); }
        }
    }
    private sealed class DirectedPolicyShape : Shape
    {
        protected override ShapeGeometry GetGeometryCore() => ShapeGeometry.SeparationRay(5, true);
    }
}
