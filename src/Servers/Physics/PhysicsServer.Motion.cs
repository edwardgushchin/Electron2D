using Box2D.NET;

namespace Electron2D;

public sealed partial class PhysicsServer
{
    /// <summary>Tests a live body's shapes through their owning space without moving that body.</summary>
    /// <param name="body">A scene or server-created body RID.</param>
    /// <param name="parameters">Global starting pose, motion, margin and exclusions.</param>
    /// <param name="result">Optional caller-owned result updated after a successful test.</param>
    /// <returns>Whether motion or requested recovery reached a body contact.</returns>
    public bool BodyTestMotion(RID body, PhysicsTestMotionParameters2D parameters,
        PhysicsTestMotionResult2D? result = null)
    {
        ThrowIfDisposed();
        ArgumentNullException.ThrowIfNull(parameters);
        if (result?.IsDisposed == true) throw new ObjectDisposedException(nameof(result));
        var data = TestMotionData(body, parameters.From, parameters.Motion, parameters.Margin,
            parameters.RecoveryAsCollision, parameters.ExcludedBodies, parameters.ExcludedObjects);
        result?.Set(data);
        return data.Collided;
    }

    internal MotionResultData TestMotionData(RID body, Transform from, Vector2 motion,
        float margin, bool recoveryAsCollision, RID[] excludedBodies, ulong[] excludedObjects)
    {
        ThrowIfDisposed();
        PhysicsSpace? space;
        IReadOnlyList<B2ShapeId> shapes;
        if (ResolveSceneObject(body) is PhysicsBody sceneBody)
        {
            space = sceneBody.Space;
            shapes = sceneBody.BackendShapes;
        }
        else
        {
            var serverBody = GetCollider(body, isArea: false);
            space = serverBody.Space;
            shapes = serverBody.BackendShapes;
        }
        if (space is null) throw new InvalidOperationException("A body motion test requires a registered physics space.");
        return space.TestBodyMotion(body, shapes, from, motion, margin, recoveryAsCollision,
            excludedBodies, excludedObjects);
    }
}
