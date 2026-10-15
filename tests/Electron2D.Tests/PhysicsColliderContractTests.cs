using Electron2D;

internal static class PhysicsColliderContractTests
{
    internal static void Run(PhysicsServer.Backend backend)
    {
        if (backend is not (PhysicsServer.Backend.CPU or PhysicsServer.Backend.GPU)) throw new ArgumentOutOfRangeException(nameof(backend));
        (string Name, Action Run)[] suites =
        [
            (nameof(PhysicsBodyTests), () => PhysicsBodyTests.Run(backend)),
            (nameof(AreaTests), () => AreaTests.Run(backend)),
            (nameof(CapsuleShapeTests), () => CapsuleShapeTests.Run(backend)),
            (nameof(SegmentShapeTests), () => SegmentShapeTests.Run(backend)),
            (nameof(ConvexPolygonShapeTests), () => ConvexPolygonShapeTests.Run(backend)),
            (nameof(ConcavePolygonShapeTests), () => ConcavePolygonShapeTests.Run(backend)),
            (nameof(SeparationRayShapeTests), () => SeparationRayShapeTests.Run(backend)),
            (nameof(WorldBoundaryTests), () => WorldBoundaryTests.Run(backend)),
            (nameof(CollisionPolygonTests), () => CollisionPolygonTests.Run(backend)),
            (nameof(OneWayCollisionTests), () => OneWayCollisionTests.Run(backend)),
            (nameof(ShapeOwnerTests), () => ShapeOwnerTests.Run(backend)),
            (nameof(PhysicsMaterialTests), () => PhysicsMaterialTests.Run(backend)),
            (nameof(PhysicsMassProfileTests), () => PhysicsMassProfileTests.Run(backend)),
            (nameof(PhysicsServerShapeSlotTests), () => PhysicsServerShapeSlotTests.Run(backend)),
            (nameof(PhysicsAreaMonitorTests), () => PhysicsAreaMonitorTests.Run(backend)),
            (nameof(PhysicsAreaFieldTests), () => PhysicsAreaFieldTests.Run(backend)),
            (nameof(PhysicsServerAreaFieldTests), () => PhysicsServerAreaFieldTests.Run(backend)),
            (nameof(PhysicsPointGravityRangeTests), () => PhysicsPointGravityRangeTests.Run(backend)),
            (nameof(CollisionDisableModeTests), () => CollisionDisableModeTests.Run(backend)),
            (nameof(PhysicsQueryTests), () => PhysicsQueryTests.Run(backend)),
            (nameof(PhysicsCanvasTests), () => PhysicsCanvasTests.Run(backend)),
            (nameof(PhysicsObjectBindingTests), () => PhysicsObjectBindingTests.Run(backend)),
            (nameof(PhysicsShapeQueryTests), () => PhysicsShapeQueryTests.Run(backend)),
            (nameof(PhysicsMotionTests), () => PhysicsMotionTests.Run(backend)),
            (nameof(PhysicsCCDTests), () => PhysicsCCDTests.Run(backend)),
            (nameof(ShapeCollisionTests), () => ShapeCollisionTests.Run()),
            (nameof(SceneChangeTests), SceneChangeTests.Run),
            (nameof(PinJointTests), () => PinJointTests.Run(backend)),
            (nameof(GrooveJointTests), () => GrooveJointTests.Run(backend)),
            (nameof(DampedSpringJointTests), () => DampedSpringJointTests.Run(backend)),
            (nameof(PhysicsBodyStateTests), () => PhysicsBodyStateTests.Run(backend)),
            (nameof(PhysicsServerForceTests), () => PhysicsServerForceTests.Run(backend)),
            (nameof(PhysicsServerStateTests), () => PhysicsServerStateTests.Run(backend)),
            (nameof(PhysicsBodyParameterTests), () => PhysicsBodyParameterTests.Run(backend)),
            (nameof(PhysicsSurfaceVelocityTests), () => PhysicsSurfaceVelocityTests.Run(backend)),
            (nameof(PhysicsContactImpulseTests), () => PhysicsContactImpulseTests.Run(backend)),
            (nameof(PhysicsServerJointTests), () => PhysicsServerJointTests.Run(backend)),
            (nameof(PhysicsJointPolicyTests), () => PhysicsJointPolicyTests.Run(backend)),
            (nameof(PhysicsSleepPolicyTests), () => PhysicsSleepPolicyTests.Run(backend)),
            (nameof(PhysicsContactPolicyTests), () => PhysicsContactPolicyTests.Run(backend)),
            (nameof(PhysicsSolverIterationTests), () => PhysicsSolverIterationTests.Run(backend)),
            (nameof(PhysicsContactPersistenceTests), () => PhysicsContactPersistenceTests.Run(backend)),
            (nameof(PhysicsCollisionPriorityTests), () => PhysicsCollisionPriorityTests.Run(backend)),
            (nameof(WorldTests), () => WorldTests.Run(backend)),
            (nameof(AnimatableBodyTests), () => AnimatableBodyTests.Run(backend)),
            (nameof(CharacterBodyTests), () => CharacterBodyTests.Run(backend)),
            (nameof(RigidFreezeModeTests), () => RigidFreezeModeTests.Run(backend)),
            (nameof(PhysicalBoneTests), () => PhysicalBoneTests.Run(backend)),
        ];
        var failures = new List<Exception>();
        foreach (var (name, run) in suites)
        {
            Console.WriteLine($"Collider contract: {backend}/{name}");
            try { run(); }
            catch (Exception error) { Console.Error.WriteLine($"{backend}/{name}: {error}"); failures.Add(error); }
        }
        if (failures.Count != 0) throw new AggregateException("Shared public collider contract failed.", failures);
        Console.WriteLine($"Shared collider contract passed: {backend}, separate={ProjectSettings.GetWithOverride(ProjectSettings.Physics2DRunOnSeparateThread)}, suites={suites.Length}.");
    }
}
