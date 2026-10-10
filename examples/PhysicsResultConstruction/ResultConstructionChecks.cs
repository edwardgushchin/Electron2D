using System.Runtime.CompilerServices;
using Electron2D;

namespace Electron2D.Examples.PhysicsResultConstruction;

internal static class ResultConstructionChecks
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        SceneResults(backend);
        using var circle = new CircleShape { Radius = 5 };
        using var owner = new Node(); using var replacement = new Node();
        var space = PhysicsServer.SpaceCreate(backend); var otherSpace = PhysicsServer.SpaceCreate(backend);
        var body = PhysicsServer.BodyCreate(); var collider = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        using var motion = new PhysicsTestMotionResult();
        try
        {
            PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodyAddShape(collider, circle.GetRID()); PhysicsServer.AreaAddShape(area, circle.GetRID());
            PhysicsServer.BodySetSpace(body, space); PhysicsServer.BodySetSpace(collider, space); PhysicsServer.AreaSetSpace(area, space);
            PhysicsServer.BodyAttachObject(collider, owner); PhysicsServer.AreaAttachObject(area, owner);
            var id = owner.InstanceID;
            var ray = new PhysicsRayResult(collider, 0, new(4, 5), Vector2.Up);
            var point = new PhysicsPointResult(area, 0); var shape = new PhysicsShapeResult(collider, 0);
            var rest = new PhysicsRestInfo(collider, 0, new(4, 5), Vector2.Up, new(6, 7));
            Hit();
            Check(ray.ColliderRID == collider && ray.Collider is null && ray.ColliderID == id && ray.ColliderObject == owner && ray.Position == new Vector2(4, 5), "Ray identity and geometry");
            Check(point.ColliderRID == area && point.ColliderObject == owner && shape.ColliderID == id && rest.LinearVelocity == new Vector2(6, 7), "Point/shape/rest identity and data");
            Check(motion.GetCollider() == owner && motion.GetCollisionLocalShape() == 0 && motion.GetCollisionSafeFraction() == .25f && motion.GetTravel() == new Vector2(1, 2), "Motion identity and data");
            PhysicsServer.BodyAttachObject(collider, replacement); PhysicsServer.AreaAttachObject(area, replacement);
            Check(ray.ColliderObject == owner && point.ColliderObject == owner && shape.ColliderObject == owner && rest.ColliderObject == owner && motion.GetCollider() == owner, "Rebinding cannot retarget saved results");
            owner.Dispose();
            Check(ray.ColliderObject is null && point.ColliderObject is null && shape.ColliderObject is null && rest.ColliderObject is null && motion.GetCollider() is null && motion.GetColliderID() == id && ray.ColliderID == id, "Disposal preserves sampled IDs without a live association");
            Hit();
            var before = motion.GetColliderID();
            Reject<ArgumentException>(() => new PhysicsRayResult(circle.GetRID(), 0, default, default));
            Reject<ArgumentException>(() => new PhysicsPointResult(default, 0));
            Reject<ArgumentOutOfRangeException>(() => new PhysicsShapeResult(collider, 1));
            Reject<ArgumentOutOfRangeException>(() => new PhysicsRestInfo(collider, -1, default, default, default));
            Reject<ArgumentOutOfRangeException>(() => new PhysicsRayResult(collider, 0, new(float.NaN, 0), default));
            Reject<ArgumentOutOfRangeException>(() => new PhysicsRestInfo(collider, 0, default, default, new(float.PositiveInfinity, 0)));
            Reject<ArgumentOutOfRangeException>(() => motion.SetCollision(body, 0, collider, 0, default, Vector2.Up, 0, default, default, default, .8f, .2f));
            Reject<ArgumentException>(() => motion.SetCollision(body, 0, area, 0, default, Vector2.Up, 0, default, default, default, 0, 1));
            Reject<ArgumentException>(() => motion.SetCollision(body, 0, body, 0, default, Vector2.Up, 0, default, default, default, 0, 1));
            Reject<ArgumentOutOfRangeException>(() => motion.SetCollision(body, 0, collider, 0, default, default, -1, default, default, default, 0, 1));
            Reject<ArgumentOutOfRangeException>(() => motion.SetCollision(body, 0, collider, 0, new(float.NaN, 0), default, 0, default, default, default, 0, 1));
            Reject<ArgumentOutOfRangeException>(() => motion.SetCollision(body, 0, collider, 0, default, new(float.NaN, 0), 0, default, default, default, 0, 1));
            Reject<ArgumentOutOfRangeException>(() => motion.SetCollision(body, 0, collider, 0, default, default, 0, new(float.NaN, 0), default, default, 0, 1));
            Reject<ArgumentOutOfRangeException>(() => motion.SetCollision(body, 0, collider, 0, default, default, 0, default, new(float.NaN, 0), default, 0, 1));
            Reject<ArgumentOutOfRangeException>(() => motion.SetCollision(body, 0, collider, 0, default, default, 0, default, default, new(float.NaN, 0), 0, 1));
            Reject<ArgumentOutOfRangeException>(() => motion.SetCollision(body, 0, collider, 0, default, default, 0, default, default, default, float.NaN, 1));
            Reject<ArgumentOutOfRangeException>(() => motion.SetCollision(body, 0, collider, 0, default, default, 0, default, default, default, 0, 2));
            Reject<ArgumentOutOfRangeException>(() => motion.SetMotion(new(float.NaN, 0)));
            PhysicsServer.BodySetSpace(body, default); Reject<InvalidOperationException>(Hit); PhysicsServer.BodySetSpace(body, space);
            PhysicsServer.BodySetSpace(collider, otherSpace);
            Reject<InvalidOperationException>(Hit);
            Check(motion.GetColliderID() == before && motion.GetCollisionPoint() == new Vector2(4, 5), "Rejected writes preserve the previous result");
            PhysicsServer.BodySetSpace(collider, space);
            Task.Run(() => Reject<InvalidOperationException>(() => new PhysicsPointResult(collider, 0))).GetAwaiter().GetResult();
            var captured = CaptureWeak(collider);
            Collect();
            Check(!Alive(captured.Owner) && captured.Result.ColliderObject is null && captured.Result.ColliderID == captured.ID, "A copied result must not retain a raw object association");
            PhysicsServer.BodyAttachObject(collider, replacement);
            PhysicsServer.BodySetStateSyncCallback(body, _ =>
            {
                Check(new PhysicsPointResult(collider, 0).ColliderID == replacement.InstanceID, "Result construction in synchronized callback");
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(space, 1d / 60));
            });
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, 1d / 60);
            PhysicsServer.BodySetStateSyncCallback(body, null);
            Collect(); for (var i = 0; i < 128; i++) Frame();
            var all = GC.GetTotalAllocatedBytes(true); var current = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 128; i++) Frame();
            current = GC.GetAllocatedBytesForCurrentThread() - current; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(current == 0 && all == 0, $"Result construction allocated {current}/{all} owner/all-thread B");
            Console.WriteLine($"{backend}: 128 warmed complete result-family construction/miss/hit cycles: {current}/{all} owner/all-thread B.");
            PhysicsServer.SpaceSetActive(space, true); PhysicsServer.SpaceStep(space, 1d / 60);
            using var parameters = new PhysicsTestMotionParameters { Motion = new(1, 0) };
            PhysicsServer.BodyTestMotion(body, parameters, motion);
            PhysicsServer.FreeRID(collider);
            Reject<ArgumentException>(() => new PhysicsShapeResult(collider, 0));
            collider = default;
            motion.Dispose(); Reject<ObjectDisposedException>(() => motion.SetMotion(default));
            if (backend == PhysicsServer.Backend.GPU)
            {
                Reject<AggregateException>(() => PhysicsServer.SpaceStep(space, float.MaxValue));
                Reject<InvalidOperationException>(() => new PhysicsPointResult(body, 0));
                Reject<InvalidOperationException>(() => PhysicsServer.SpaceStep(space, 1d / 60));
            }
            Console.WriteLine($"Public result-family identity, validation, atomic reuse, lifecycle, ownership and ordinary {backend} motion checks passed.");
        }
        finally { PhysicsServer.FreeRID(body); if (collider.IsValid()) PhysicsServer.FreeRID(collider); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(space); PhysicsServer.FreeRID(otherSpace); }
        void Hit() => motion.SetCollision(body, 0, collider, 0, new(4, 5), Vector2.Up, .5f, new(6, 7), new(1, 2), new(3, 4), .25f, .5f);
        void Frame()
        {
            var ray = new PhysicsRayResult(collider, 0, new(4, 5), Vector2.Up); var point = new PhysicsPointResult(area, 0);
            var shape = new PhysicsShapeResult(collider, 0); var rest = new PhysicsRestInfo(collider, 0, new(4, 5), Vector2.Up, new(6, 7));
            Hit(); Check(ray.ColliderID == replacement.InstanceID && point.ColliderID == replacement.InstanceID && shape.ColliderID == replacement.InstanceID && rest.ColliderID == replacement.InstanceID && motion.GetColliderID() == replacement.InstanceID, "Constructed identity remains current");
            motion.SetMotion(new(8, 9));
            Check(!motion.GetColliderRID().IsValid() && motion.GetColliderID() == 0 && motion.GetCollider() is null && motion.GetCollisionNormal() == Vector2.Zero && motion.GetRemainder() == Vector2.Zero && motion.GetCollisionSafeFraction() == 1 && motion.GetCollisionUnsafeFraction() == 1 && motion.GetTravel() == new Vector2(8, 9), "Miss clears all collision state");
        }
    }
    private static void SceneResults(PhysicsServer.Backend backend)
    {
        using var shape = new CircleShape { Radius = 5 };
        using var world = new World(backend);
        var root = new SubViewport { World = world };
        var body = new StaticBody { Name = "Moving" }; body.AddChild(new CollisionShape { Shape = shape });
        var area = new Area(); area.AddChild(new CollisionShape { Shape = shape });
        var collider = new StaticBody { Name = "Collider", Position = new(20, 0) }; collider.AddChild(new CollisionShape { Shape = shape });
        root.AddChild(body); root.AddChild(area); root.AddChild(collider);
        using var tree = new SceneTree(root); tree.PhysicsFrame(1d / 60);
        var ray = new PhysicsRayResult(body.GetRID(), 0, default, Vector2.Up);
        var point = new PhysicsPointResult(area.GetRID(), 0);
        var hit = new PhysicsShapeResult(body.GetRID(), 0);
        var rest = new PhysicsRestInfo(area.GetRID(), 0, default, Vector2.Up, default);
        Check(ray.Collider == body && ray.ColliderObject == body && ray.ColliderID == body.InstanceID &&
            point.Collider == area && point.ColliderObject == area && hit.Collider == body && rest.Collider == area,
            "Constructed scene result family keeps physical collider and sampled object roles");
        using var motion = new PhysicsTestMotionResult();
        motion.SetCollision(body.GetRID(), 0, collider.GetRID(), 0, new(15, 0), Vector2.Left, 0, default, new(10, 0), new(10, 0), .5f, .6f);
        Check(motion.GetCollider() == collider && motion.GetColliderID() == collider.InstanceID, "Scene motion-result identity");
        shape.Dispose();
        Reject<ObjectDisposedException>(() => new PhysicsShapeResult(body.GetRID(), 0));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static (WeakReference<ElectronObject> Owner, PhysicsPointResult Result, ulong ID) CaptureWeak(RID collider)
    {
        var owner = new Node(); PhysicsServer.BodyAttachObject(collider, owner);
        return (new(owner), new(collider, 0), owner.InstanceID);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool Alive(WeakReference<ElectronObject> weak) => weak.TryGetTarget(out _);
    private static void Collect() { GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
}
