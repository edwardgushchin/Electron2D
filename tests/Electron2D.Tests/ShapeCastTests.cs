using Electron2D;

internal static class ShapeCastTests
{
    internal static void Run()
    {
        VerifyDefaultsAndPacking();
        VerifyForcedAutomaticAndFailureRecovery();
        VerifyMultipleHitsAndFilters();
        VerifyParentAreaRotationAndLifetime();
        Console.WriteLine("ShapeCast scene sweep, contacts, filters, packing and allocation checks passed.");
    }

    private static void VerifyDefaultsAndPacking()
    {
        using var shape = new CircleShape();
        var root = new Node();
        var cast = new ShapeCast { Name = "Probe" };
        root.AddChild(cast); cast.Owner = root;
        Check(cast.Shape is null && cast.TargetPosition == new Vector2(0, 50) && cast.Margin == 0 &&
              cast.MaxResults == 32 && cast.CollisionMask == 1 && cast.Enabled && cast.ExcludeParent &&
              !cast.CollideWithAreas && cast.CollideWithBodies && !cast.IsColliding() &&
              cast.GetCollisionCount() == 0 && cast.CollisionResult.Length == 0 &&
              cast.GetClosestCollisionSafeFraction() == 0 && cast.GetClosestCollisionUnsafeFraction() == 0,
            "A detached cast starts with the pinned options and an empty result snapshot.");
        cast.SetCollisionMaskValue(32, true);
        cast.SetCollisionMaskValue(1, false);
        Check(cast.CollisionMask == 0x80000000u && cast.GetCollisionMaskValue(32),
            "One-based layer operations preserve the high bit.");
        Reject<ArgumentOutOfRangeException>(() => cast.GetCollisionMaskValue(0));
        Reject<ArgumentOutOfRangeException>(() => cast.SetCollisionMaskValue(33, true));
        Reject<ArgumentOutOfRangeException>(() => cast.TargetPosition = new(float.NaN, 0));
        Reject<ArgumentOutOfRangeException>(() => cast.Margin = -1);
        Reject<ArgumentOutOfRangeException>(() => cast.Margin = float.NaN);
        Reject<ArgumentNullException>(() => cast.AddException(null!));
        Reject<ArgumentNullException>(() => cast.RemoveException(null!));
        Reject<ArgumentOutOfRangeException>(() => cast.GetColliderRID(0));
        Check(cast.TargetPosition == new Vector2(0, 50) && cast.Margin == 0,
            "Invalid writes reject without changing options.");

        cast.Shape = shape;
        cast.TargetPosition = new(30, 40);
        cast.Margin = 2;
        cast.MaxResults = 3;
        cast.Enabled = false;
        cast.ExcludeParent = false;
        cast.CollideWithAreas = true;
        cast.CollideWithBodies = false;
        using var packed = new PackedScene(); packed.Pack(root);
        using var copy = packed.Instantiate();
        var restored = copy.GetNode<ShapeCast>("Probe");
        Check(restored.Shape is CircleShape && ReferenceEquals(restored.Shape, shape) &&
              restored.TargetPosition == new Vector2(30, 40) && restored.Margin == 2 &&
              restored.MaxResults == 3 && restored.CollisionMask == 0x80000000u &&
              !restored.Enabled && !restored.ExcludeParent && restored.CollideWithAreas &&
              !restored.CollideWithBodies && restored.GetCollisionCount() == 0,
            "PackedScene restores the exact node, borrowed shape and stored query options without runtime results.");
        cast.Shape = null;
        Check(cast.Shape is null, "Clearing the borrowed shape leaves the node without query geometry.");
    }

    private static void VerifyForcedAutomaticAndFailureRecovery()
    {
        using var probe = new CircleShape();
        using var floorShape = new RectangleShape { Size = new(200, 20) };
        var root = new Node();
        var cast = new ShapeCast { Shape = probe, TargetPosition = new(0, 150) };
        var floor = new StaticBody { Position = new(0, 100), CollisionMask = 0 };
        var collision = new CollisionShape { Shape = floorShape };
        floor.AddChild(collision);
        root.AddChild(cast); root.AddChild(floor);
        using var tree = new SceneTree(root);
        Check(!cast.IsColliding(), "No automatic sample runs before the first physics frame.");
        cast.ForceShapecastUpdate();
        var initial = cast.GetClosestCollisionSafeFraction();
        Check(cast.IsColliding() && cast.GetCollisionCount() == 1 &&
              ReferenceEquals(cast.GetCollider(0), floor) && cast.GetColliderRID(0) == floor.GetRID() &&
              cast.GetColliderShape(0) == 0 && initial is > 0.48f and < 0.58f &&
              cast.GetClosestCollisionUnsafeFraction() > initial &&
              cast.GetCollisionPoint(0).Y is > 88 and < 92 && cast.GetCollisionNormal(0).Y < -0.9f,
            "A forced pre-frame sweep finds a layer-one body despite that body's mask being zero.");
        var copied = cast.CollisionResult;
        copied[0] = default;
        Check(cast.CollisionResult[0].ColliderRID == floor.GetRID(),
            "CollisionResult returns a caller-owned array, not the internal snapshot.");
        probe.Radius = 20;
        cast.ForceShapecastUpdate();
        Check(cast.GetClosestCollisionSafeFraction() < initial,
            "A live borrowed Shape edit advances first contact without replacing the resource.");
        probe.Radius = 10;
        cast.ForceShapecastUpdate();
        cast.Shape = null;
        Reject<InvalidOperationException>(() => cast.ForceShapecastUpdate());
        Check(cast.GetColliderRID(0) == floor.GetRID(),
            "Missing query geometry does not replace a previously committed result.");
        cast.Shape = probe;
        floor.Position = new(0, 120);
        Check(cast.GetClosestCollisionSafeFraction() == initial,
            "A scene transform edit does not mutate the held result before sampling.");
        tree.PhysicsFrame(1d / 60);
        Check(cast.GetClosestCollisionSafeFraction() > initial && cast.GetCollisionPoint(0).Y is > 108 and < 112,
            "An enabled cast refreshes in the internal fixed physics lane.");
        tree.Paused = true;
        floor.Position = new(0, 130);
        tree.PhysicsFrame(1d / 60);
        Check(cast.GetCollisionPoint(0).Y is > 108 and < 112, "Pause retains the last snapshot.");
        tree.Paused = false;
        tree.PhysicsFrame(1d / 60);
        Check(cast.GetCollisionPoint(0).Y is > 118 and < 122, "Unpause refreshes the snapshot.");

        collision.Scale = new(2, 1);
        var priorPoint = cast.GetCollisionPoint(0);
        Reject<AggregateException>(() => tree.PhysicsFrame(1d / 60));
        Check(cast.IsColliding() && cast.GetCollisionPoint(0) == priorPoint,
            "A failed fixture preparation preserves the last successful sample.");
        collision.Scale = Vector2.One;
        tree.PhysicsFrame(1d / 60);
        tree.PhysicsFrame(1d / 60);
        Check(cast.IsColliding(), "A corrected collider allows the next automatic sample to recover.");

        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        var before = GC.GetAllocatedBytesForCurrentThread();
        for (var frame = 0; frame < 64; frame++) tree.PhysicsFrame(1d / 60);
        Check(GC.GetAllocatedBytesForCurrentThread() - before == 0,
            "Warmed active ShapeCast frames allocate no managed bytes on the owner thread.");

        var retained = cast.GetColliderRID(0);
        cast.Enabled = false;
        Check(!cast.IsColliding() && cast.GetCollisionCount() == 1 && cast.GetColliderRID(0) == retained,
            "Disabling clears only the collision flag, retaining cached contacts.");
        floor.Position = new(0, 300);
        tree.PhysicsFrame(1d / 60);
        Check(cast.GetCollisionCount() == 1, "A disabled cast does not sample automatically.");
        cast.ForceShapecastUpdate();
        Check(!cast.IsColliding() && cast.GetCollisionCount() == 0 &&
              cast.GetClosestCollisionSafeFraction() == 1 && cast.GetClosestCollisionUnsafeFraction() == 1,
            "A forced miss while disabled clears contacts and returns no-hit motion fractions.");
        floor.Position = new(0, 100);
        cast.ForceShapecastUpdate();
        Check(cast.IsColliding(), "A disabled cast can still be sampled explicitly.");
        cast.Enabled = false;
        Check(!cast.IsColliding(), "An equal disabled write clears the forced collision flag.");
        Reject<InvalidOperationException>(() => Task.Run(cast.ForceShapecastUpdate).GetAwaiter().GetResult());
        root.RemoveChild(cast);
        Reject<InvalidOperationException>(() => cast.ForceShapecastUpdate());
        floor.Dispose();
        Check(cast.GetCollider(0) is null && cast.CollisionResult[0].Collider is null &&
              cast.CollisionResult[0].ColliderID == 0,
            "Cached typed results resolve a disposed scene collider as null while retaining its RID.");
        cast.Dispose();
    }

    private static void VerifyMultipleHitsAndFilters()
    {
        using var probe = new CircleShape();
        using var block = new RectangleShape { Size = new(80, 20) };
        var root = new Node();
        var cast = new ShapeCast { Shape = probe, TargetPosition = new(0, 150) };
        var first = new StaticBody { Name = "First", Position = new(0, 100) };
        var second = new StaticBody { Name = "Second", Position = new(0, 100) };
        first.AddChild(new CollisionShape { Shape = block });
        second.AddChild(new CollisionShape { Shape = block });
        root.AddChild(cast); root.AddChild(first); root.AddChild(second);
        using var tree = new SceneTree(root);
        cast.ForceShapecastUpdate();
        Check(cast.GetCollisionCount() == 2 && cast.GetColliderRID(0) == first.GetRID() &&
              cast.GetColliderRID(1) == second.GetRID() && cast.CollisionResult.Length == 2,
            "Repeated rest queries report distinct collider RIDs at the first impact.");
        cast.MaxResults = 1;
        cast.ForceShapecastUpdate();
        Check(cast.GetCollisionCount() == 1, "The result cap limits distinct colliders.");
        cast.MaxResults = 0;
        cast.ForceShapecastUpdate();
        Check(!cast.IsColliding() && cast.GetCollisionCount() == 0 &&
              cast.GetClosestCollisionSafeFraction() < 1,
            "A zero cap retains motion fractions without contact records.");
        cast.MaxResults = -1;
        cast.ForceShapecastUpdate();
        Check(cast.GetCollisionCount() == 0, "The reference's negative cap also yields no contacts.");
        cast.MaxResults = 32;
        cast.AddException(first);
        cast.ForceShapecastUpdate();
        Check(cast.GetCollisionCount() == 1 && cast.GetColliderRID(0) == second.GetRID(),
            "A scene exception excludes only its collider RID.");
        cast.AddExceptionRID(second.GetRID());
        cast.ForceShapecastUpdate();
        Check(!cast.IsColliding(), "Two RID exceptions leave no eligible collider.");
        cast.RemoveExceptionRID(first.GetRID());
        cast.ForceShapecastUpdate();
        Check(cast.GetColliderRID(0) == first.GetRID(), "Removing one exception restores that collider.");
        cast.ClearExceptions();
        cast.ForceShapecastUpdate();
        Check(cast.GetCollisionCount() == 2, "ClearExceptions restores both independent colliders.");
        cast.CollisionMask = 2;
        cast.ForceShapecastUpdate();
        Check(!cast.IsColliding(), "The collision mask tests collider layers.");
        cast.CollisionMask = 1;
        cast.CollideWithBodies = false;
        cast.ForceShapecastUpdate();
        Check(!cast.IsColliding(), "Body filtering can suppress every target independently.");
    }

    private static void VerifyParentAreaRotationAndLifetime()
    {
        using var probe = new CircleShape();
        using var parentShape = new RectangleShape { Size = new(40, 40) };
        using var targetShape = new RectangleShape { Size = new(40, 10) };
        var root = new Node();
        var parent = new StaticBody { Name = "Parent" };
        parent.AddChild(new CollisionShape { Shape = parentShape });
        var cast = new ShapeCast { Shape = probe, TargetPosition = Vector2.Zero };
        parent.AddChild(cast);
        var target = new StaticBody { Name = "Target", Position = new(0, 60) };
        target.AddChild(new CollisionShape { Shape = targetShape });
        root.AddChild(parent); root.AddChild(target);
        using var tree = new SceneTree(root);
        cast.ForceShapecastUpdate();
        Check(!cast.IsColliding() && cast.GetClosestCollisionSafeFraction() == 0 &&
              cast.GetClosestCollisionUnsafeFraction() == 0,
            "A zero target samples only the origin and excludes a direct parent by default.");
        cast.ExcludeParent = false;
        cast.ForceShapecastUpdate();
        Check(cast.GetColliderRID(0) == parent.GetRID(),
            "Disabling parent exclusion exposes an initial overlap.");
        cast.ExcludeParent = true;
        cast.ClearExceptions();
        cast.ForceShapecastUpdate();
        Check(cast.GetColliderRID(0) == parent.GetRID(),
            "ClearExceptions removes the current parent RID until the exclusion is re-added.");
        cast.ExcludeParent = false;
        cast.ExcludeParent = true;
        cast.TargetPosition = new(0, 100);
        cast.ForceShapecastUpdate();
        Check(cast.GetColliderRID(0) == target.GetRID(),
            "A parent exclusion and nonzero target find a separate body.");

        var area = new Area { Position = new(0, 45), CollisionLayer = 2 };
        area.AddChild(new CollisionShape { Shape = targetShape });
        root.AddChild(area);
        cast.CollisionMask = 3;
        cast.CollideWithAreas = true;
        cast.ForceShapecastUpdate();
        Check(cast.GetColliderRID(0) == area.GetRID() && cast.CollisionResult[0].LinearVelocity == Vector2.Zero,
            "An Area at an earlier impact reports a sensor contact and zero velocity.");
        cast.CollideWithAreas = false;
        cast.CollisionMask = 1;
        cast.TargetPosition = Vector2.Zero;
        target.Position = new(0, 0);
        cast.ForceShapecastUpdate();
        Check(cast.GetColliderRID(0) == target.GetRID(),
            "Zero motion detects immediate overlap with another body.");
        target.Position = new(0, 60);
        cast.TargetPosition = new(0, 100);
        cast.Rotation = Mathf.Pi / 2;
        target.Position = new(-60, 0);
        cast.ForceShapecastUpdate();
        Check(cast.GetColliderRID(0) == target.GetRID() && cast.GetCollisionNormal(0).X > 0.9f,
            "The target follows the node rotation into global space.");

        var server = PhysicsServer.Service;
        var body = PhysicsServer.BodyCreate();
        var shapeRID = PhysicsServer.CircleShapeCreate();
        PhysicsServer.BodyAddShape(body, shapeRID);
        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static);
        PhysicsServer.BodySetTransform(body, new(0, Vector2.One, 0, new(-40, 0)));
        PhysicsServer.BodySetSpace(body, cast.GetWorld2D()!.Space);
        target.Position = new(0, 200);
        cast.ForceShapecastUpdate();
        Check(cast.GetColliderRID(0) == body && cast.GetCollider(0) is null &&
              cast.CollisionResult[0].ColliderID == 0,
            "A server-only body retains RID and contact data without a scene object.");
        PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(shapeRID);
        Check(cast.GetCollider(0) is null && cast.GetColliderRID(0).IsValid(),
            "A cached RID remains readable after the server collider is freed.");
        cast.ForceShapecastUpdate();
        Check(!cast.IsColliding(), "The next sample clears a freed collider result.");
        cast.TargetPosition = Vector2.Zero;
        parent.RemoveChild(cast);
        root.AddChild(cast);
        cast.ForceShapecastUpdate();
        Check(cast.GetColliderRID(0) == parent.GetRID(),
            "Leaving a collision parent removes its automatic RID exclusion before a new attachment.");
        probe.Dispose();
        Reject<InvalidOperationException>(() => cast.ForceShapecastUpdate());
        cast.Shape = null;
        Reject<InvalidOperationException>(() => cast.ForceShapecastUpdate());
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
