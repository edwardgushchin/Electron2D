using Electron2D;

internal static class RayCastTests
{
    internal static void Run()
    {
        VerifyStateAndPacking();
        VerifyAutomaticAndForcedSampling();
        VerifyRotatedTarget();
        VerifyParentExclusionsAndAreas();
        VerifyServerOnlyHitAndLifetime();
        Console.WriteLine("RayCast scene sampling, exclusions, packed state and allocation checks passed.");
    }

    private static void VerifyStateAndPacking()
    {
        var root = new Node();
        var ray = new RayCast { Name = "Probe" };
        root.AddChild(ray); ray.Owner = root;
        Check(ray.TargetPosition == new Vector2(0, 50) && ray.CollisionMask == 1 && ray.Enabled &&
              ray.ExcludeParent && !ray.HitFromInside && !ray.CollideWithAreas && ray.CollideWithBodies &&
              !ray.IsColliding() && ray.GetCollider() is null && !ray.GetColliderRID().IsValid() &&
              ray.GetColliderShape() == 0 && ray.GetCollisionPoint() == Vector2.Zero &&
              ray.GetCollisionNormal() == Vector2.Zero,
            "A detached ray has pinned defaults and an empty result snapshot.");
        ray.SetCollisionMaskValue(32, true);
        Check(ray.GetCollisionMaskValue(32) && ray.GetCollisionMaskValue(1),
            "The one-based bit API preserves both first and last collision layers.");
        ray.SetCollisionMaskValue(1, false);
        Check(ray.CollisionMask == 0x80000000u, "Clearing one bit retains unrelated bits.");
        Reject<ArgumentOutOfRangeException>(() => ray.GetCollisionMaskValue(0));
        Reject<ArgumentOutOfRangeException>(() => ray.SetCollisionMaskValue(33, true));
        Reject<ArgumentOutOfRangeException>(() => ray.TargetPosition = new(float.NaN, 0));
        Check(ray.CollisionMask == 0x80000000u && ray.TargetPosition == new Vector2(0, 50),
            "Invalid bit and coordinate edits leave the previous state intact.");
        Reject<ArgumentNullException>(() => ray.AddException(null!));
        Reject<ArgumentNullException>(() => ray.RemoveException(null!));

        ray.TargetPosition = new(30, 40);
        ray.Enabled = false;
        ray.ExcludeParent = false;
        ray.HitFromInside = true;
        ray.CollideWithAreas = true;
        ray.CollideWithBodies = false;
        using var packed = new PackedScene(); packed.Pack(root);
        using var copy = packed.Instantiate();
        var restored = copy.GetNode<RayCast>("Probe");
        Check(restored.TargetPosition == new Vector2(30, 40) && restored.CollisionMask == 0x80000000u &&
              !restored.Enabled && !restored.ExcludeParent && restored.HitFromInside &&
              restored.CollideWithAreas && !restored.CollideWithBodies,
            "PackedScene restores the exact RayCast type and stored options.");
    }

    private static void VerifyAutomaticAndForcedSampling()
    {
        using var shape = new RectangleShape { Size = new(200, 20) };
        var root = new Node();
        var ray = new RayCast { Name = "Ray", TargetPosition = new(0, 150) };
        var wall = new StaticBody { Name = "Wall", Position = new(0, 100) };
        var collision = new CollisionShape { Shape = shape };
        wall.AddChild(collision);
        root.AddChild(ray); root.AddChild(wall);
        using var tree = new SceneTree(root);
        Check(!ray.IsColliding(), "The snapshot is empty before automatic sampling.");
        ray.ForceRaycastUpdate();
        Check(ray.IsColliding() && ReferenceEquals(ray.GetCollider(), wall) &&
              ray.GetColliderRID() == wall.GetRID() && ray.GetColliderShape() == 0 &&
              ray.GetCollisionPoint().Y is > 89 and < 91 && ray.GetCollisionNormal().Y < -0.9f,
            "Forced sampling resolves pending fixtures even before the first physics frame.");
        wall.Position = new(0, 120);
        Check(ray.GetCollisionPoint().Y is > 89 and < 91,
            "A transform edit does not change the held result until the next sample.");
        tree.PhysicsFrame(1d / 60);
        Check(ray.IsColliding() && ray.GetCollisionPoint().Y is > 109 and < 111,
            "An enabled ray refreshes from the current world in the fixed physics lane.");

        tree.Paused = true;
        wall.Position = new(0, 130);
        tree.PhysicsFrame(1d / 60);
        Check(ray.GetCollisionPoint().Y is > 109 and < 111,
            "A paused ray retains its previous result instead of running its internal physics callback.");
        tree.Paused = false;
        tree.PhysicsFrame(1d / 60);
        Check(ray.GetCollisionPoint().Y is > 119 and < 121,
            "Unpausing refreshes the ray from the moved collider.");
        wall.Position = new(0, 120);
        tree.PhysicsFrame(1d / 60);

        collision.Scale = new(2, 1);
        var goodPoint = ray.GetCollisionPoint();
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        Check(ray.GetCollisionPoint() == goodPoint && ray.IsColliding(),
            "A failed world preparation leaves the previous ray snapshot committed.");
        collision.Scale = Vector2.One;
        tree.PhysicsFrame(1d / 60);
        Check(ray.IsColliding(), "A corrected collider lets the next automatic sample recover.");

        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0,
            $"Warmed active ray sampling allocates no managed memory on the owner thread: {allocated}.");

        var retainedRID = ray.GetColliderRID();
        var retainedPoint = ray.GetCollisionPoint();
        ray.Enabled = false;
        Check(!ray.IsColliding() && ray.GetColliderRID() == retainedRID && ray.GetCollisionPoint() == retainedPoint,
            "Disabling clears the collision flag but retains the other cached hit fields.");
        wall.Position = new(0, 200);
        tree.PhysicsFrame(1d / 60);
        Check(ray.GetColliderRID() == retainedRID, "A disabled ray does not sample on physics frames.");
        ray.ForceRaycastUpdate();
        Check(!ray.IsColliding() && !ray.GetColliderRID().IsValid() && ray.GetColliderShape() == 0 &&
              ray.GetCollisionPoint() == retainedPoint,
            "A forced miss while disabled clears identity but retains the previous point and normal.");
        wall.Position = new(0, 100);
        ray.ForceRaycastUpdate();
        Check(ray.IsColliding(), "A disabled ray can still be sampled explicitly.");
        ray.Enabled = false;
        Check(!ray.IsColliding(), "An equal disabled assignment clears a forced collision.");
        ray.Enabled = true;
        tree.PhysicsFrame(1d / 60);
        Check(ray.IsColliding(), "Re-enabling resumes automatic sampling.");
        Reject<InvalidOperationException>(() => Task.Run(ray.ForceRaycastUpdate).GetAwaiter().GetResult());
        var releasedRID = ray.GetColliderRID();
        wall.Dispose();
        Check(ray.GetColliderRID() == releasedRID && ray.GetCollider() is null,
            "A cached RID survives a collider's disposal while GetCollider stops returning that object.");
        tree.PhysicsFrame(1d / 60);
        Check(!ray.IsColliding() && !ray.GetColliderRID().IsValid(),
            "The next automatic sample clears a released collider identity.");
        root.RemoveChild(ray);
        Reject<InvalidOperationException>(() => ray.ForceRaycastUpdate());
    }

    private static void VerifyParentExclusionsAndAreas()
    {
        using var parentShape = new RectangleShape { Size = new(30, 30) };
        using var targetShape = new RectangleShape { Size = new(30, 10) };
        var root = new Node();
        var parent = new StaticBody { Name = "Parent" };
        parent.AddChild(new CollisionShape { Shape = parentShape });
        var ray = new RayCast { TargetPosition = new(0, 100), HitFromInside = true };
        parent.AddChild(ray);
        var target = new StaticBody { Name = "Target", Position = new(0, 60) };
        target.AddChild(new CollisionShape { Shape = targetShape });
        root.AddChild(parent); root.AddChild(target);
        using var tree = new SceneTree(root);
        tree.PhysicsFrame(1d / 60);
        Check(ReferenceEquals(ray.GetCollider(), target) && ray.GetCollisionPoint().Y is > 54 and < 56,
            "ExcludeParent skips a direct parent whose shape contains the ray origin.");
        ray.ExcludeParent = false;
        ray.ForceRaycastUpdate();
        Check(ReferenceEquals(ray.GetCollider(), parent) && ray.GetCollisionPoint() == Vector2.Zero &&
              ray.GetCollisionNormal() == Vector2.Zero,
            "Disabling parent exclusion allows an inside-origin hit.");
        ray.TargetPosition = Vector2.Zero;
        ray.ForceRaycastUpdate();
        Check(ReferenceEquals(ray.GetCollider(), parent),
            "A zero target uses the reference's tiny downward fallback ray.");
        ray.TargetPosition = new(0, 100);
        ray.AddException(parent);
        ray.ForceRaycastUpdate();
        Check(ReferenceEquals(ray.GetCollider(), target), "AddException excludes a scene object by RID.");
        ray.RemoveExceptionRID(parent.GetRID());
        ray.ForceRaycastUpdate();
        Check(ReferenceEquals(ray.GetCollider(), parent), "RemoveExceptionRID restores its hit.");
        ray.ExcludeParent = true;
        ray.AddExceptionRID(target.GetRID());
        ray.ForceRaycastUpdate();
        Check(!ray.IsColliding(), "Parent and target RID exclusions can leave the ray with no hit.");
        ray.ClearExceptions();
        ray.ForceRaycastUpdate();
        Check(ReferenceEquals(ray.GetCollider(), target),
            "ClearExceptions re-adds only an enabled direct-parent exception.");

        using var sensorShape = new RectangleShape { Size = new(30, 10) };
        var sensor = new Area { Name = "Sensor", Position = new(0, 30), CollisionLayer = 2 };
        sensor.AddChild(new CollisionShape { Shape = sensorShape });
        root.AddChild(sensor);
        ray.CollisionMask = 3;
        ray.CollideWithAreas = true;
        ray.ForceRaycastUpdate();
        Check(ReferenceEquals(ray.GetCollider(), sensor) && ray.GetColliderRID() == sensor.GetRID(),
            "Area-enabled rays choose a nearer sensor on an accepted layer.");
        ray.CollisionMask = 1;
        ray.ForceRaycastUpdate();
        Check(ReferenceEquals(ray.GetCollider(), target), "Mask one excludes the Area's second layer.");
        ray.CollideWithBodies = false;
        ray.ForceRaycastUpdate();
        Check(!ray.IsColliding(), "Area/body flags independently gate ray targets.");
        parent.RemoveChild(ray);
        root.AddChild(ray);
        ray.CollideWithBodies = true;
        ray.CollideWithAreas = false;
        ray.CollisionMask = 1;
        ray.TargetPosition = Vector2.Zero;
        ray.ForceRaycastUpdate();
        Check(ray.GetColliderRID() == parent.GetRID(),
            "Leaving a collision parent removes its automatic RID exception before a new attachment.");
    }

    private static void VerifyRotatedTarget()
    {
        using var shape = new RectangleShape { Size = new(10, 100) };
        var root = new Node();
        var ray = new RayCast { TargetPosition = new(0, 100), Rotation = Mathf.Pi / 2 };
        var wall = new StaticBody { Position = new(-50, 0) };
        wall.AddChild(new CollisionShape { Shape = shape });
        root.AddChild(ray); root.AddChild(wall);
        using var tree = new SceneTree(root);
        ray.ForceRaycastUpdate();
        Check(ReferenceEquals(ray.GetCollider(), wall) && ray.GetCollisionPoint().X is > -46 and < -44 &&
              ray.GetCollisionNormal().X > 0.9f,
            "The local target follows node rotation before querying global collision geometry.");
    }

    private static void VerifyServerOnlyHitAndLifetime()
    {
        var root = new Node();
        var ray = new RayCast { TargetPosition = new(0, 100) };
        root.AddChild(ray);
        using var tree = new SceneTree(root);
        var server = PhysicsServer.Service;
        var body = PhysicsServer.BodyCreate();
        var shape = PhysicsServer.CircleShapeCreate();
        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodyAddShape(body, shape);
        PhysicsServer.BodySetTransform(body, new(0, Vector2.One, 0, new(0, 30)));
        PhysicsServer.BodySetSpace(body, ray.GetWorld()!.Space);
        tree.PhysicsFrame(1d / 60);
        Check(ray.IsColliding() && ray.GetCollider() is null && ray.GetColliderRID() == body,
            "A server-only hit retains its RID even without a scene collider object.");
        var point = ray.GetCollisionPoint();
        PhysicsServer.FreeRID(body);
        PhysicsServer.FreeRID(shape);
        tree.PhysicsFrame(1d / 60);
        Check(!ray.IsColliding() && !ray.GetColliderRID().IsValid() && ray.GetCollisionPoint() == point,
            "A freed server collider disappears on the next sample without losing prior point data.");
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); }
        catch (T) { return; }
        throw new InvalidOperationException("Expected " + typeof(T).Name);
    }
}
