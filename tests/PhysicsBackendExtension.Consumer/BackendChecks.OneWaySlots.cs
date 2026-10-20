using Electron2D;

public static partial class BackendChecks
{
    private static void OneWaySlotPolicyChecks(World world, SubViewport root, SceneTree tree, Solver? solver, CircleShape circle)
    {
        using var rectangle = new RectangleShape { Size = new(10, 2) };
        using var motion = new PhysicsTestMotionParameters { Margin = .001f };
        using var result = new PhysicsTestMotionResult();
        var mover = PhysicsServer.BodyCreate(); var area = PhysicsServer.AreaCreate();
        try
        {
            var retired = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(retired);
            Reject<ArgumentException>(() => PhysicsServer.BodySetShapeAsOneWayCollision(retired, 0, true, 2));
            PhysicsServer.BodyAddShape(mover, circle.GetRID()); PhysicsServer.BodySetGravityScale(mover, 0);
            PhysicsServer.BodySetTransform(mover, new(0, new Vector2(1000, 1000))); PhysicsServer.BodySetSpace(mover, world.Space);
            for (var role = 0; role < 3; role++)
            {
                using var scene = role == 0 ? null : new StaticBody { Rotation = .2f };
                CollisionShape? child = null; uint owner = 0; var maximumAllowanceExcess = 0f;
                var body = scene?.GetRID() ?? PhysicsServer.BodyCreate();
                try
                {
                    if (scene is null)
                    {
                        PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetTransform(body, new(.2f, Vector2.Zero));
                        PhysicsServer.BodyAddShape(body, circle.GetRID(), new(0, new Vector2(-100, 0)));
                        PhysicsServer.BodyAddShape(body, rectangle.GetRID(), new(.3f, Vector2.Zero));
                        PhysicsServer.BodyAddShape(body, circle.GetRID(), new(0, new Vector2(100, 0))); PhysicsServer.BodySetSpace(body, world.Space);
                    }
                    else
                    {
                        owner = scene.CreateShapeOwner(null); scene.ShapeOwnerAddShape(owner, circle);
                        if (role == 1)
                        {
                            scene.ShapeOwnerAddShape(owner, rectangle); scene.ShapeOwnerAddShape(owner, circle);
                            scene.ShapeOwnerSetOneWayCollision(owner, true); scene.ShapeOwnerSetOneWayCollisionMargin(owner, 4);
                            scene.ShapeOwnerSetOneWayCollisionDirection(owner, Vector2.Up);
                        }
                        else
                        {
                            child = new CollisionShape { Shape = rectangle, Rotation = .3f, OneWayCollision = true, OneWayCollisionMargin = 4, OneWayCollisionDirection = Vector2.Up };
                            scene.AddChild(child); root.AddChild(scene); owner = scene.ShapeFindOwner(1);
                            var tail = scene.CreateShapeOwner(null); scene.ShapeOwnerAddShape(tail, circle);
                        }
                        if (scene.Parent is null) root.AddChild(scene);
                        PhysicsServer.BodySetShapeTransform(body, 0, new(0, new Vector2(-100, 0)));
                        PhysicsServer.BodySetShapeTransform(body, 1, new(.3f, Vector2.Zero));
                        PhysicsServer.BodySetShapeTransform(body, 2, new(0, new Vector2(100, 0)));
                    }
                    Set(true, 2); Policy(true, 2, Vector2.Down);
                    foreach (var direction in new[] { Vector2.Right, new Vector2(float.MaxValue, float.MaxValue), Vector2.Zero })
                    {
                        Set(true, 2, direction); Policy(true, 2, direction == Vector2.Zero ? Vector2.Zero : direction == Vector2.Right ? Vector2.Right : new Vector2(1, 1).Normalized());
                    }
                    Set(false, 0, Vector2.Zero); Policy(false, 0, Vector2.Zero);
                    Set(true, 0); Policy(true, 0, Vector2.Down);
                    motion.From = new(0, -Vector2.Down.Rotated(.5f) * 2.5f); motion.Motion = Vector2.Zero; motion.RecoveryAsCollision = true;
                    Check(!PhysicsServer.BodyTestMotion(mover, motion), "Zero one-way margin ignores deep initial recovery");
                    Set(true, 1);
                    Check(PhysicsServer.BodyTestMotion(mover, motion, result) && result.GetColliderRID() == body && result.GetColliderShape() == 1,
                        "A larger one-way margin permits front-face recovery with exact logical identity");
                    motion.RecoveryAsCollision = false;
                    Set(true, 2, Vector2.Right); Policy(true, 2, Vector2.Right);
                    var calls = 0; if (solver is not null) solver.ShapeHook = id => { if (id == body) calls++; };
                    Set(true, 2, new(8, 0)); Policy(true, 2, Vector2.Right);
                    if (solver is not null) solver.ShapeHook = null;
                    Check(calls == 0, "Equal normalized one-way input does not rebuild the selected collider");
                    foreach (var margin in new[] { -1f, float.NaN, float.PositiveInfinity, float.NegativeInfinity }) Reject<ArgumentOutOfRangeException>(() => Set(false, margin));
                    foreach (var direction in new[] { new Vector2(float.NaN, 0), new Vector2(0, float.PositiveInfinity) }) Reject<ArgumentOutOfRangeException>(() => Set(false, 2, direction));
                    Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetShapeAsOneWayCollision(body, -1, true, 2));
                    Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetShapeAsOneWayCollision(body, 3, true, 2));
                    Reject<ArgumentException>(() => PhysicsServer.BodySetShapeAsOneWayCollision(area, 0, true, 2));
                    Reject<ArgumentException>(() => PhysicsServer.BodySetShapeAsOneWayCollision(rectangle.GetRID(), 0, true, 2));
                    Task.Run(() => Reject<InvalidOperationException>(() => Set(false, 2))).GetAwaiter().GetResult(); Policy(true, 2, Vector2.Right);
                    if (solver is not null)
                    {
                        solver.ShapeHook = id => { if (id == body) Reject<InvalidOperationException>(() => Set(false, 2)); };
                        Set(true, 3); Policy(true, 3, Vector2.Down); solver.ShapeHook = null;
                        var attempts = 0; var apply = new ApplicationException("one-way-apply"); var restore = new ApplicationException("one-way-restore");
                        solver.ShapeHook = id => { if (id == body && ++attempts <= 2) throw attempts == 1 ? apply : restore; };
                        if (scene is null)
                        {
                            var errors = ReleaseErrors(() => Set(true, 4, Vector2.Left));
                            Check(errors.Length == 2 && ReferenceEquals(errors[0], apply) && ReferenceEquals(errors[1], restore), "Raw one-way edit preserves apply/rollback failures");
                            Policy(true, 3, Vector2.Down);
                        }
                        else
                        {
                            Set(true, 4, Vector2.Left);
                            for (var retry = 0; retry < 2; retry++)
                            {
                                var errors = ReleaseErrors(() => Probe(Vector2.Right, true, 4));
                                Check(errors.Length == 1 && ReferenceEquals(errors[0], retry == 0 ? apply : restore), "Deferred scene one-way preparation retains each original failure");
                            }
                            Policy(true, 4, Vector2.Left);
                        }
                        Check(attempts == 3, "Both raw rollback debt and deferred scene policy retry the selected numerical preparation"); solver.ShapeHook = null;
                    }
                    if (scene is not null)
                    {
                        for (var field = 0; field < 3; field++)
                        {
                            if (child is null)
                            {
                                scene.ShapeOwnerSetOneWayCollision(owner, true); scene.ShapeOwnerSetOneWayCollisionMargin(owner, 4);
                                scene.ShapeOwnerSetOneWayCollisionDirection(owner, Vector2.Up);
                            }
                            else { child.OneWayCollision = true; child.OneWayCollisionMargin = 4; child.OneWayCollisionDirection = Vector2.Up; }
                            Set(true, 8, Vector2.Right);
                            if (field == 0) { if (child is null) scene.ShapeOwnerSetOneWayCollision(owner, false); else child.OneWayCollision = false; }
                            else if (field == 1) { if (child is null) scene.ShapeOwnerSetOneWayCollisionMargin(owner, 5); else child.OneWayCollisionMargin = 5; }
                            else { if (child is null) scene.ShapeOwnerSetOneWayCollisionDirection(owner, Vector2.Left); else child.OneWayCollisionDirection = Vector2.Left; }
                            Policy(field != 0, field == 1 ? 5 : 4, field == 2 ? Vector2.Left : Vector2.Up);
                            Check(scene.IsShapeOwnerOneWayCollisionEnabled(owner) == (field != 0) && scene.GetShapeOwnerOneWayCollisionMargin(owner) == (field == 1 ? 5 : 4) &&
                                scene.GetShapeOwnerOneWayCollisionDirection(owner) == (field == 2 ? Vector2.Left : Vector2.Up), "An individual group/child edit reclaims all stored one-way fields");
                        }
                        if (child is null) scene.ShapeOwnerSetOneWayCollision(owner, true); else child.OneWayCollision = true;
                        Policy(true, 4, Vector2.Left);
                    }
                    Set(true, 2); Policy(true, 2, Vector2.Down);
                    PhysicsServer.BodySetShapeDisabled(body, 1, true); Probe(Vector2.Down, false, 2); PhysicsServer.BodySetShapeDisabled(body, 1, false); Policy(true, 2, Vector2.Down);
                    for (var i = 0; i < 128; i++) Frame();
                    Collect(); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                    for (var i = 0; i < 128; i++) Frame();
                    own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                    Check(own == 0 && all == 0, $"One-way slot role={role}: {own}/{all} B");
                    Console.WriteLine($"One-way slot {world.PhysicsBackendName}, role={role}: defaults, normalization, margin, guards, reclaim and retry; 128 full query/frames {own}/{all} B; max allowance rounding={maximumAllowanceExcess}.");
                    void Set(bool enable, float margin, Vector2? direction = null) => PhysicsServer.BodySetShapeAsOneWayCollision(body, 1, enable, margin, direction);
                    void Frame() { Set(true, 2); Policy(true, 2, Vector2.Down); tree.PhysicsFrame(1d / 60); }
                    void Policy(bool enabled, float margin, Vector2 direction)
                    {
                        var pass = (direction == Vector2.Zero ? Vector2.Down : direction).Rotated(.5f);
                        var allowance = enabled ? MathF.Max(margin, motion.Margin) : float.PositiveInfinity;
                        Probe(pass, !enabled || direction != Vector2.Zero, allowance); Probe(-pass, !enabled, allowance);
                        Check(PhysicsServer.BodyGetShapeCount(body) == 3 && PhysicsServer.BodyGetShape(body, 1) == rectangle.GetRID(), "One-way edits preserve borrowed geometry and all logical indices");
                        if (solver is null) return;
                        foreach (var part in solver.Bodies[body].Parts.AsSpan(0, solver.Bodies[body].Shapes))
                            if (part.Index == 1) Check(enabled ? part.OneWay is { } policy && policy.Margin == margin && policy.LocalDirection.IsEqualApprox(direction.Rotated(.3f)) : part.OneWay is null,
                                "Registered input contains the exact immutable transformed slot policy");
                            else Check(role == 1 && scene!.IsShapeOwnerOneWayCollisionEnabled(owner)
                                ? part.OneWay is { } sibling && sibling.Margin == scene.GetShapeOwnerOneWayCollisionMargin(owner) && sibling.LocalDirection == scene.GetShapeOwnerOneWayCollisionDirection(owner)
                                : part.OneWay is null, "A per-index policy leaves every sibling on its stored group policy");
                    }
                    void Probe(Vector2 direction, bool expected, float allowance)
                    {
                        motion.From = new(0, -direction * 20); motion.Motion = direction * 40;
                        var hit = PhysicsServer.BodyTestMotion(mover, motion, result);
                        if (hit != expected || hit && (result.GetColliderRID() != body || result.GetColliderShape() != 1 || result.GetCollisionLocalShape() != 0))
                            throw new InvalidOperationException($"One-way side policy role={role}, direction={direction}, expected={expected}, hit={hit}, local={result.GetCollisionLocalShape()}, target={result.GetColliderShape()}, travel={result.GetTravel()}, fractions={result.GetCollisionSafeFraction()}/{result.GetCollisionUnsafeFraction()}.");
                        if (hit && float.IsFinite(allowance))
                        {
                            maximumAllowanceExcess = MathF.Max(maximumAllowanceExcess, result.GetCollisionDepth() - allowance);
                            Check(result.GetCollisionDepth() <= allowance + .00001f, "A one-way contact stays inside its allowance plus distance rounding tolerance");
                        }
                    }
                }
                finally { if (solver is not null) solver.ShapeHook = null; if (scene is null) PhysicsServer.FreeRID(body); else root.RemoveChild(scene); }
            }
        }
        finally { PhysicsServer.FreeRID(mover); PhysicsServer.FreeRID(area); }
    }
}
