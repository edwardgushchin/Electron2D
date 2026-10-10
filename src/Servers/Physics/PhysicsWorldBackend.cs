using Box2D.NET;

namespace Electron2D;

/// <summary>Owns selected solver policy, capacity, statistics, complete intervals and direct-space queries.</summary>
internal abstract class PhysicsWorldBackend(PhysicsSpace space, PhysicsServer.Backend requested, string? fallbackReason = null) : IDisposable
{
    protected PhysicsSpace Space { get; } = space;
    internal PhysicsServer.Backend Requested { get; } = requested;
    internal string? FallbackReason { get; } = fallbackReason;
    internal abstract PhysicsServer.Backend Kind { get; }
    internal virtual GPUPhysicsBodyStore? GPUStore => null;
    internal virtual B2WorldId WorldID => throw new InvalidOperationException("This physics space has no CPU solver world.");
    internal virtual PhysicsTaskScheduler Tasks => throw new InvalidOperationException("This physics space has no CPU worker scheduler.");
    internal virtual GPUPhysicsWorld? StageGPU => null;
    internal abstract void EnsureAccess();
    internal abstract void SetSleepSettings(in PhysicsSleepSettings settings);
    internal abstract void SetContactSettings(in PhysicsContactSettings settings);
    internal abstract void SetSolverIterations(int value);
    internal abstract void SetConstraintDefaultBias(float value);
    internal abstract int PreparedBodyCapacity { get; }
    internal abstract void PrepareSolverCapacity();
    internal abstract void PrepareMonitoringCapacity();
    internal abstract PhysicsSpace.Statistics ReadStatistics();
    internal virtual void PrepareInterval() => throw new InvalidOperationException("This physics space has no CPU worker scheduler.");
    internal abstract PhysicsColliderImplementation CreateCollider(PhysicsColliderBackend collider);
    internal abstract PhysicsJointImplementation CreateJoint();
    internal void Step(double delta) => Space.ExecuteStep(this, delta);
    internal abstract void BeginStep(double delta);
    internal abstract void PrepareWorld(double delta);
    internal abstract void PrepareBodies(double delta);
    internal abstract void Solve(double delta);
    internal abstract void SyncResults();
    internal abstract bool ResultsReady { get; }
    internal abstract void CompleteBody(PhysicsServerCollider body);
    internal abstract void CompleteBody(PhysicsBody body);
    internal abstract void CollectContacts();
    internal abstract void ScanAreas();
    internal abstract void EndStep(Exception? failure);
    internal virtual void StepNative(float delta, int substeps) => throw new InvalidOperationException("This physics space has no CPU solver world.");
    internal virtual GPUPhysicsWorld EnableGPUIntegration() => throw new InvalidOperationException("GPU stage controls require a CPU-hosted world.");
    internal virtual GPUPhysicsWorld EnableGPUSolver() => throw new InvalidOperationException("GPU stage controls require a CPU-hosted world.");
    internal abstract PhysicsRayResult? IntersectRay(Vector2 from, Vector2 to, uint mask, RID[] excluded,
        bool collideWithAreas, bool collideWithBodies, bool hitFromInside);
    internal abstract List<PhysicsPointResult> CollectPointHits(PhysicsPointQueryParameters parameters);
    internal abstract List<PhysicsShapeResult> CollectShapeHits(PhysicsShapeQueryParameters parameters);
    internal abstract (float SafeFraction, float UnsafeFraction) CastMotion(PhysicsShapeQueryParameters parameters);
    internal abstract List<ShapeContactPair> CollectShapeContacts(PhysicsShapeQueryParameters parameters, int limit);
    internal abstract PhysicsRestInfo? GetRestInfo(PhysicsShapeQueryParameters parameters);
    internal abstract MotionResultData TestBodyMotion(RID body, Transform from, Vector2 motion, float margin,
        bool recoveryAsCollision, RID[] excludedBodies, ulong[] excludedObjects, bool collideSeparationRay = false);
    internal readonly record struct ShapeContactPair(RID RID, int ShapeIndex, int Piece, Vector2 QueryPoint, Vector2 ColliderPoint);
    public abstract void Dispose();

    internal static PhysicsWorldBackend Create(PhysicsSpace space, PhysicsServer.Backend requested, bool allowCPUFallback)
    {
        if (!Enum.IsDefined(requested)) throw new ArgumentOutOfRangeException(nameof(requested));
        if (requested == PhysicsServer.Backend.CPU) return new CPUPhysicsWorldBackend(space, requested);
        try { return new GPUPhysicsWorldBackend(space); }
        catch (Exception error) when (allowCPUFallback && error is InvalidOperationException or NotSupportedException or DllNotFoundException or EntryPointNotFoundException)
        {
            return new CPUPhysicsWorldBackend(space, requested, error.Message);
        }
    }
}
