using Electron2D;

internal static partial class PhysicsBackendOwnershipTests
{
    private sealed class MotionCharacter : CharacterBody
    {
        internal MotionCharacter() => PhysicsProcessEnabled = true;
        internal bool MoveRequested, Collided;
        protected override void OnPhysicsProcess(double delta)
        {
            if (MoveRequested) Collided = MoveAndSlide();
            base.OnPhysicsProcess(delta);
        }
    }
    internal static void MotionPublication(PhysicsServer.Backend backend)
    {
        ViewBackend? selected = null;
        using var world = new World(new WorldRuntime(false, backend, factory: space => selected = new(space, backend)));
        using var foreignWorld = new World(backend);
        using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 3 };
        using var association = new Node();
        using var replacement = new Node();
        var scene = new MotionCharacter { CollisionMask = 4, MotionMode = CharacterMotionMode.Floating, MaxSlides = 1 };
        scene.AddChild(new CollisionShape { Shape = shape }); root.AddChild(scene);
        using var tree = new SceneTree(root);
        var raw = PhysicsServer.BodyCreate(); var collider = PhysicsServer.BodyCreate(); var foreign = PhysicsServer.BodyCreate();
        var area = PhysicsServer.AreaCreate(); var stale = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(stale);
        using var parameters = new PhysicsTestMotionParameters { Motion = new(10, 0) };
        using var result = new PhysicsTestMotionResult();
        var space = PhysicsServer.Service.GetSceneSpace(world.Space);
        try
        {
            foreach (var body in new[] { raw, collider, foreign })
            {
                PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodyAddShape(body, shape.GetRID());
                PhysicsServer.BodySetTransform(body, new(0, new(100, 0)));
            }
            PhysicsServer.BodySetCollisionMask(raw, 4); PhysicsServer.BodySetCollisionLayer(collider, 4);
            PhysicsServer.BodySetCollisionMask(collider, 0);
            PhysicsServer.BodyAttachObject(collider, association);
            PhysicsServer.BodySetSpace(raw, world.Space); PhysicsServer.BodySetSpace(collider, world.Space); PhysicsServer.BodySetSpace(foreign, foreignWorld.Space);
            PhysicsServer.AreaAddShape(area, shape.GetRID()); PhysicsServer.AreaSetSpace(area, world.Space);
            selected!.MotionHook = (_, data) => data with { Travel = new(float.NaN, 0) };
            result.SetMotion(new(9, 8));
            Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(raw, parameters, result));
            Check(result.GetTravel() == new Vector2(9, 8), "Rejected selected motion leaves caller output intact");
            Reject<InvalidOperationException>(() => scene.MoveAndCollide(new(10, 0)));
            Check(scene.Position == Vector2.Zero, "Rejected selected motion leaves the scene pose intact");

            selected.MotionHook = Collision;
            Evaluate();
            Check(result.GetColliderID() == association.InstanceID && ReferenceEquals(result.GetCollider(), association), "Engine captures collider association instead of trusting supplied identity");
            PhysicsServer.BodyAttachObject(collider, replacement);
            Check(ReferenceEquals(result.GetCollider(), association), "Retained motion sample never retargets after rebind");
            Evaluate();
            Check(ReferenceEquals(result.GetCollider(), replacement), "Next motion samples the new association");
            using (var collision = scene.MoveAndCollide(new(10, 0)))
                Check(collision is not null && scene.Position == new Vector2(4, 0) && collision.GetColliderRID() == collider, "Scene motion consumes selected travel and contact");
            scene.Position = Vector2.Zero; scene.Velocity = new(60, 0); scene.MoveRequested = true; tree.PhysicsFrame(1d / 60); scene.MoveRequested = false;
            Check(scene.Collided && scene.Position.X == 4, "CharacterBody reaches the same selected motion output");
            scene.Position = Vector2.Zero;

            var good = Collision(raw, default);
            Task.Run(() => Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(raw, parameters, result))).GetAwaiter().GetResult();
            foreach (var invalid in new[]
            {
                good with { OwnerRID = collider }, good with { ColliderRID = raw }, good with { ColliderRID = area },
                good with { ColliderRID = foreign }, good with { ColliderRID = stale }, good with { LocalShape = -1 },
                good with { ColliderShape = 1 }, good with { Point = new(float.PositiveInfinity, 0) },
                good with { Normal = new(0, float.NaN) }, good with { ColliderVelocity = new(float.NaN, 0) },
                good with { Remainder = new(0, float.NaN) }, good with { Depth = -1 },
                good with { SafeFraction = -.1f }, good with { UnsafeFraction = .2f }, good with { UnsafeFraction = 1.1f }
            })
            {
                selected.MotionHook = (_, _) => invalid;
                Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(raw, parameters, result));
                Check(result.GetTravel() == good.Travel && result.GetColliderRID() == collider, "Invalid selected output is atomic");
            }
            selected.MotionHook = Collision;
            parameters.ExcludeBodies = [collider]; Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(raw, parameters, result)); parameters.ExcludeBodies = [];
            parameters.ExcludeObjects = [replacement.InstanceID]; Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(raw, parameters, result)); parameters.ExcludeObjects = [];
            PhysicsServer.BodyAddCollisionException(collider, raw); Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(raw, parameters, result)); PhysicsServer.BodyRemoveCollisionException(collider, raw);
            PhysicsServer.BodySetCollisionLayer(collider, 2); Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(raw, parameters, result)); PhysicsServer.BodySetCollisionLayer(collider, 4);
            PhysicsServer.BodySetShapeDisabled(collider, 0, true); Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(raw, parameters, result)); PhysicsServer.BodySetShapeDisabled(collider, 0, false);
            PhysicsServer.BodySetShapeDisabled(raw, 0, true); Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(raw, parameters, result)); PhysicsServer.BodySetShapeDisabled(raw, 0, false);

            selected.MotionHook = (body, data) =>
            {
                Reject<InvalidOperationException>(() => PhysicsServer.FreeRID(collider));
                Reject<InvalidOperationException>(() => PhysicsServer.BodySetSpace(raw, foreignWorld.Space));
                Reject<InvalidOperationException>(() => root.RemoveChild(scene));
                Reject<InvalidOperationException>(space.EnsureWorldRelease);
                Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(body, parameters));
                return Collision(body, data);
            };
            Evaluate();
            selected.MotionHook = (_, _) => throw new InvalidOperationException("Injected selected motion failure.");
            Reject<InvalidOperationException>(() => PhysicsServer.BodyTestMotion(raw, parameters, result));
            Check(space.Tick == 1 && !space.HasBackendFailure, "Query failure retires borrowing and preserves usable world and tick");
            selected.MotionHook = Collision;
            Collect(); for (var i = 0; i < 64; i++) { tree.PhysicsFrame(1d / 60); Evaluate(); }
            var all = GC.GetTotalAllocatedBytes(true); var owner = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 64; i++) { tree.PhysicsFrame(1d / 60); Evaluate(); }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"Selected motion publication allocated {owner}/{all} owner/all-thread bytes");
            selected.MotionHook = (body, _) => good with { OwnerRID = body, Collided = false, Travel = new(10, 0), Remainder = default, SafeFraction = 1, UnsafeFraction = 1 };
            Check(!PhysicsServer.BodyTestMotion(raw, parameters, result) && result.GetColliderRID() == default && result.GetColliderID() == 0 &&
                result.GetCollisionPoint() == default && result.GetCollisionNormal() == default && result.GetRemainder() == default, "Miss clears all preceding contact payload");
            selected.MotionHook = null;
            Check(!PhysicsServer.BodyTestMotion(raw, parameters, result), "Built-in query remains usable after selected failures");
            Console.WriteLine($"{backend}: selected scene/raw/CharacterBody motion, atomic validation, sampled identity, filters, borrow/failure recovery and 64 warmed full-frame/query cycles: {owner}/{all} owner/all-thread B passed.");
        }
        finally { PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(collider); PhysicsServer.FreeRID(foreign); PhysicsServer.FreeRID(area); }
        MotionResultData Collision(RID body, MotionResultData _) => new(body, collider, ulong.MaxValue, 0, 0, new(7, 0), Vector2.Left, .25f,
            new(2, 3), new(4, 0), new(6, 0), .4f, .5f, true);
        void Evaluate() => Check(PhysicsServer.BodyTestMotion(raw, parameters, result) && result.GetTravel() == new Vector2(4, 0) &&
            result.GetCollisionPoint() == new Vector2(7, 0) && result.GetColliderVelocity() == new Vector2(2, 3), "Selected numerical geometry reaches reusable raw result");
    }
}
