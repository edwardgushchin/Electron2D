namespace Electron2D;

/// <summary>A caller-owned snapshot of one physics body's motion contact.</summary>
public sealed class KinematicCollision2D : ElectronObject
{
    private MotionResultData _data;

    /// <summary>Creates an empty result for an optional <see cref="PhysicsBody.TestMove"/> output.</summary>
    public KinematicCollision2D() { }

    internal KinematicCollision2D(in MotionResultData data) => _data = data;
    internal void Set(in MotionResultData data) { ThrowIfDisposed(); _data = data; }

    /// <summary>Returns the positive angle between the contact normal and an up direction.</summary>
    /// <param name="upDirection">A finite nonzero up direction, up by default.</param>
    /// <returns>An angle in radians from zero through pi.</returns>
    public float GetAngle(Vector2? upDirection = null)
    {
        ThrowIfDisposed();
        var up = upDirection ?? Vector2.Up;
        if (!up.IsFinite() || up == Vector2.Zero) throw new ArgumentOutOfRangeException(nameof(upDirection));
        return MathF.Abs(_data.Normal.AngleTo(up));
    }

    /// <summary>Returns the moving body's direct shape-owner scene node.</summary>
    /// <returns>A live CollisionShape or CollisionPolygon, or null for a server-only body.</returns>
    public ElectronObject? GetLocalShape()
    {
        ThrowIfDisposed();
        return (PhysicsServer2D.Instance.ResolveSceneObject(_data.OwnerRID) as PhysicsBody)?
            .GetShapeNode(_data.LocalShape);
    }

    /// <summary>Returns the live scene collider, if any.</summary>
    /// <returns>A scene object or null for a freed or server-only body.</returns>
    public ElectronObject? GetCollider() { ThrowIfDisposed(); return PhysicsServer2D.Instance.ResolveSceneObject(_data.ColliderRID); }

    /// <summary>Returns the sampled collider instance ID.</summary>
    /// <returns>Zero for a server-only body.</returns>
    public ulong GetColliderID() { ThrowIfDisposed(); return _data.ColliderID; }

    /// <summary>Returns the sampled collider RID.</summary>
    /// <returns>An empty RID before collision.</returns>
    public RID GetColliderRID() { ThrowIfDisposed(); return _data.ColliderRID; }

    /// <summary>Returns the collider's direct shape-owner scene node.</summary>
    /// <returns>A live CollisionShape or CollisionPolygon, or null for a server-only body.</returns>
    public ElectronObject? GetColliderShape()
    {
        ThrowIfDisposed();
        return (PhysicsServer2D.Instance.ResolveSceneObject(_data.ColliderRID) as PhysicsBody)?
            .GetShapeNode(_data.ColliderShape);
    }

    /// <summary>Returns the collider's direct shape-owner index.</summary>
    /// <returns>Zero before collision.</returns>
    public int GetColliderShapeIndex() { ThrowIfDisposed(); return _data.ColliderShape; }

    /// <summary>Returns collider point velocity in scene units per second.</summary>
    /// <returns>Zero before collision.</returns>
    public Vector2 GetColliderVelocity() { ThrowIfDisposed(); return _data.ColliderVelocity; }

    /// <summary>Returns collision overlap along the contact normal in scene units.</summary>
    /// <returns>Zero before collision.</returns>
    public float GetDepth() { ThrowIfDisposed(); return _data.Depth; }

    /// <summary>Returns the contact normal pointing away from the collider.</summary>
    /// <returns>Zero before collision.</returns>
    public Vector2 GetNormal() { ThrowIfDisposed(); return _data.Normal; }

    /// <summary>Returns the global contact point on the collider.</summary>
    /// <returns>Zero before collision.</returns>
    public Vector2 GetPosition() { ThrowIfDisposed(); return _data.Point; }

    /// <summary>Returns untraveled requested motion.</summary>
    /// <returns>Zero on a motion without obstruction.</returns>
    public Vector2 GetRemainder() { ThrowIfDisposed(); return _data.Remainder; }

    /// <summary>Returns body travel before collision, including any recovery displacement.</summary>
    /// <returns>The traveled displacement in scene units.</returns>
    public Vector2 GetTravel() { ThrowIfDisposed(); return _data.Travel; }
}
