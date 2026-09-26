namespace Electron2D;

internal readonly record struct MotionResultData(
    RID OwnerRID, RID ColliderRID, ulong ColliderID, int LocalShape, int ColliderShape,
    Vector2 Point, Vector2 Normal, float Depth, Vector2 ColliderVelocity,
    Vector2 Travel, Vector2 Remainder, float SafeFraction, float UnsafeFraction, bool Collided);

/// <summary>Configures a body motion test in a registered two-dimensional physics space.</summary>
public sealed class PhysicsTestMotionParameters2D : ElectronObject
{
    private Transform _from = Transform.Identity;
    private Vector2 _motion;
    private float _margin = 0.08f;
    private bool _recoveryAsCollision;
    private bool _collideSeparationRay;
    private RID[] _excludeBodies = [];
    private ulong[] _excludeObjects = [];

    internal RID[] ExcludedBodies => _excludeBodies;
    internal ulong[] ExcludedObjects => _excludeObjects;

    /// <summary>Creates identity-pose, zero-motion parameters with a 0.08-unit recovery margin.</summary>
    public PhysicsTestMotionParameters2D() { }

    /// <summary>Gets or sets the finite unit-scale global pose at which testing begins.</summary>
    /// <value>Identity by default.</value>
    public Transform From
    {
        get { ThrowIfDisposed(); return _from; }
        set
        {
            ThrowIfDisposed();
            if (!value.IsFinite() || !value.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(value.Skew))
                throw new ArgumentException("Body motion requires finite translation, unit scale and zero skew.", nameof(value));
            _from = value;
        }
    }

    /// <summary>Gets or sets the finite global displacement to test.</summary>
    /// <value>Zero by default.</value>
    public Vector2 Motion
    {
        get { ThrowIfDisposed(); return _motion; }
        set
        {
            ThrowIfDisposed();
            if (!value.IsFinite() || !float.IsFinite(value.Length()))
                throw new ArgumentOutOfRangeException(nameof(value));
            _motion = value;
        }
    }

    /// <summary>Gets or sets the nonnegative finite recovery margin in scene units.</summary>
    /// <value>0.08 by default.</value>
    public float Margin
    {
        get { ThrowIfDisposed(); return _margin; }
        set
        {
            ThrowIfDisposed();
            if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value));
            _margin = value;
        }
    }

    /// <summary>Gets or sets whether initial depenetration itself counts as a collision.</summary>
    /// <value>False by default.</value>
    public bool RecoveryAsCollision
    {
        get { ThrowIfDisposed(); return _recoveryAsCollision; }
        set { ThrowIfDisposed(); _recoveryAsCollision = value; }
    }

    /// <summary>Gets or sets whether non-sliding separation rays can stop the requested motion.</summary>
    /// <value>False by default; ray recovery always remains enabled.</value>
    public bool CollideSeparationRay
    {
        get { ThrowIfDisposed(); return _collideSeparationRay; }
        set { ThrowIfDisposed(); _collideSeparationRay = value; }
    }

    /// <summary>Gets or sets copied body RID exclusions.</summary>
    /// <value>Empty by default.</value>
    public RID[] ExcludeBodies
    {
        get { ThrowIfDisposed(); return (RID[])_excludeBodies.Clone(); }
        set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _excludeBodies = (RID[])value.Clone(); }
    }

    /// <summary>Gets or sets copied scene object instance-ID exclusions.</summary>
    /// <value>Empty by default. Server-only bodies have no instance ID.</value>
    public ulong[] ExcludeObjects
    {
        get { ThrowIfDisposed(); return (ulong[])_excludeObjects.Clone(); }
        set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _excludeObjects = (ulong[])value.Clone(); }
    }
}

/// <summary>Caller-owned snapshot of a server body motion test.</summary>
public sealed class PhysicsTestMotionResult2D : ElectronObject
{
    private MotionResultData _data;

    /// <summary>Creates an empty writable-by-the-server result.</summary>
    public PhysicsTestMotionResult2D() { }

    internal void Set(in MotionResultData data) { ThrowIfDisposed(); _data = data; }

    /// <summary>Returns a live scene collider, or null for a server-only or freed body.</summary>
    /// <returns>A scene object or null.</returns>
    public ElectronObject? GetCollider() { ThrowIfDisposed(); return PhysicsServer.Instance.ResolveSceneObject(_data.ColliderRID); }

    /// <summary>Returns the sampled collider instance ID, or zero for server-only.</summary>
    /// <returns>The scene instance ID or zero.</returns>
    public ulong GetColliderID() { ThrowIfDisposed(); return _data.ColliderID; }

    /// <summary>Returns the sampled collider RID.</summary>
    /// <returns>An empty RID when no collision occurred.</returns>
    public RID GetColliderRID() { ThrowIfDisposed(); return _data.ColliderRID; }

    /// <summary>Returns the collider's direct shape-owner index.</summary>
    /// <returns>The index, or zero before a collision.</returns>
    public int GetColliderShape() { ThrowIfDisposed(); return _data.ColliderShape; }

    /// <summary>Returns collider velocity at the contact point in scene units per second.</summary>
    /// <returns>Zero when no collision occurred.</returns>
    public Vector2 GetColliderVelocity() { ThrowIfDisposed(); return _data.ColliderVelocity; }

    /// <summary>Returns penetration depth along the contact normal in scene units.</summary>
    /// <returns>Zero when no collision occurred.</returns>
    public float GetCollisionDepth() { ThrowIfDisposed(); return _data.Depth; }

    /// <summary>Returns the moving body's direct shape-owner index.</summary>
    /// <returns>The index, or zero when no collision occurred.</returns>
    public int GetCollisionLocalShape() { ThrowIfDisposed(); return _data.LocalShape; }

    /// <summary>Returns the global contact normal pointing away from the collider.</summary>
    /// <returns>Zero when no collision occurred.</returns>
    public Vector2 GetCollisionNormal() { ThrowIfDisposed(); return _data.Normal; }

    /// <summary>Returns the global point on the collider at contact.</summary>
    /// <returns>Zero when no collision occurred.</returns>
    public Vector2 GetCollisionPoint() { ThrowIfDisposed(); return _data.Point; }

    /// <summary>Returns the safe motion fraction before first impact.</summary>
    /// <returns>One on a miss.</returns>
    public float GetCollisionSafeFraction() { ThrowIfDisposed(); return _data.SafeFraction; }

    /// <summary>Returns the unsafe motion fraction at first impact.</summary>
    /// <returns>One on a miss.</returns>
    public float GetCollisionUnsafeFraction() { ThrowIfDisposed(); return _data.UnsafeFraction; }

    /// <summary>Returns untraveled requested displacement.</summary>
    /// <returns>Zero on an unobstructed motion.</returns>
    public Vector2 GetRemainder() { ThrowIfDisposed(); return _data.Remainder; }

    /// <summary>Returns displacement including recovery before first impact.</summary>
    /// <returns>The requested motion plus recovery on a miss.</returns>
    public Vector2 GetTravel() { ThrowIfDisposed(); return _data.Travel; }
}
