namespace Electron2D;

public abstract partial class PhysicsDirectSpaceStateExtension
{
    /// <summary>Computes the nearest eligible ray hit.</summary>
    /// <param name="from">The global ray origin.</param>
    /// <param name="to">The global ray end.</param>
    /// <param name="collisionMask">The sampled collider-layer mask.</param>
    /// <param name="collideWithBodies">Whether body colliders are eligible.</param>
    /// <param name="collideWithAreas">Whether Area colliders are eligible.</param>
    /// <param name="hitFromInside">Whether origin containment may report an inside hit.</param>
    /// <returns>The nearest typed hit, or null; exclusions are available through IsBodyExcludedFromQuery.</returns>
    /// <remarks>Called synchronously on the bound world owner after preparation. Use the current scoped exclusions.
    /// The library validates results and samples current associations before publication; hook exceptions leave caller output unchanged.</remarks>
    protected abstract PhysicsRayResult? IntersectRayCore(Vector2 from, Vector2 to, uint collisionMask, bool collideWithBodies, bool collideWithAreas, bool hitFromInside);

    /// <summary>Computes filled shapes containing a point.</summary>
    /// <param name="position">The global point.</param>
    /// <param name="canvasInstanceID">The sampled canvas filter; zero selects the default canvas.</param>
    /// <param name="collisionMask">The sampled collider-layer mask.</param>
    /// <param name="collideWithBodies">Whether body colliders are eligible.</param>
    /// <param name="collideWithAreas">Whether Area colliders are eligible.</param>
    /// <param name="results">Borrowed output for unique RID/shape-ordered hits; never retain this span.</param>
    /// <returns>The written count, between zero and the supplied output length; excess hits must retain the first RID/shape-ordered results.</returns>
    /// <remarks>Called synchronously on the bound world owner after preparation. Use the current scoped exclusions.
    /// The library validates results and samples current associations before publication; hook exceptions leave caller output unchanged.</remarks>
    protected abstract int IntersectPointCore(Vector2 position, ulong canvasInstanceID, uint collisionMask, bool collideWithBodies, bool collideWithAreas, Span<PhysicsPointResult> results);

    /// <summary>Computes overlaps across the query shape and its swept region.</summary>
    /// <param name="shape">The live query shape identity.</param>
    /// <param name="transform">The global unit-scale shape pose.</param>
    /// <param name="motion">The global scene-unit sweep motion.</param>
    /// <param name="margin">The nonnegative scene-unit query margin.</param>
    /// <param name="collisionMask">The sampled collider-layer mask.</param>
    /// <param name="collideWithBodies">Whether body colliders are eligible.</param>
    /// <param name="collideWithAreas">Whether Area colliders are eligible.</param>
    /// <param name="results">Borrowed output for unique RID/shape-ordered hits; never retain this span.</param>
    /// <returns>The written count, between zero and the supplied output length; excess hits must retain the first RID/shape-ordered results.</returns>
    /// <remarks>Called synchronously on the bound world owner after preparation. Use the current scoped exclusions.
    /// The library validates results and samples current associations before publication; hook exceptions leave caller output unchanged.</remarks>
    protected abstract int IntersectShapeCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas, Span<PhysicsShapeResult> results);

    /// <summary>Computes the bracket around the first new shape collision, ignoring initial overlaps.</summary>
    /// <param name="shape">The live query shape identity.</param>
    /// <param name="transform">The global unit-scale shape pose.</param>
    /// <param name="motion">The global scene-unit sweep motion.</param>
    /// <param name="margin">The nonnegative scene-unit query margin.</param>
    /// <param name="collisionMask">The sampled collider-layer mask.</param>
    /// <param name="collideWithBodies">Whether body colliders are eligible.</param>
    /// <param name="collideWithAreas">Whether Area colliders are eligible.</param>
    /// <returns>Finite ordered fractions in [0,1], or (1,1) on a miss.</returns>
    /// <remarks>Called synchronously on the bound world owner after preparation. Use the current scoped exclusions.
    /// The library validates results and samples current associations before publication; hook exceptions leave caller output unchanged.</remarks>
    protected abstract (float SafeFraction, float UnsafeFraction) CastMotionCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas);

    /// <summary>Computes contact-point pairs across a shape and its swept region.</summary>
    /// <param name="shape">The live query shape identity.</param>
    /// <param name="transform">The global unit-scale shape pose.</param>
    /// <param name="motion">The global scene-unit sweep motion.</param>
    /// <param name="margin">The nonnegative scene-unit query margin.</param>
    /// <param name="collisionMask">The sampled collider-layer mask.</param>
    /// <param name="collideWithBodies">Whether body colliders are eligible.</param>
    /// <param name="collideWithAreas">Whether Area colliders are eligible.</param>
    /// <param name="results">Even-length borrowed point output, query point followed by collider point for each contact; never retain this span.</param>
    /// <returns>The complete pair count, between zero and half of the supplied output length.</returns>
    /// <remarks>Called synchronously on the bound world owner after preparation. Use the current scoped exclusions.
    /// The library validates results and samples current associations before publication; hook exceptions leave caller output unchanged.</remarks>
    protected abstract int CollideShapeCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas, Span<Vector2> results);

    /// <summary>Computes the deepest eligible contact across a shape and its swept region.</summary>
    /// <param name="shape">The live query shape identity.</param>
    /// <param name="transform">The global unit-scale shape pose.</param>
    /// <param name="motion">The global scene-unit sweep motion.</param>
    /// <param name="margin">The nonnegative scene-unit query margin.</param>
    /// <param name="collisionMask">The sampled collider-layer mask.</param>
    /// <param name="collideWithBodies">Whether body colliders are eligible.</param>
    /// <param name="collideWithAreas">Whether Area colliders are eligible.</param>
    /// <returns>The closest typed contact and point velocity, or null on a miss.</returns>
    /// <remarks>Called synchronously on the bound world owner after preparation. Use the current scoped exclusions.
    /// The library validates results and samples current associations before publication; hook exceptions leave caller output unchanged.</remarks>
    protected abstract PhysicsRestInfo? GetRestInfoCore(RID shape, Transform transform, Vector2 motion, float margin, uint collisionMask, bool collideWithBodies, bool collideWithAreas);

}
