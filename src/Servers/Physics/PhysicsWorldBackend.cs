using Box2D.NET;

namespace Electron2D;

/// <summary>Owns one selected solver implementation, complete world-step dispatch and direct-space queries.</summary>
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
    internal abstract void Step(double delta);
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
