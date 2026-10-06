namespace Electron2D;

/// <summary>Configures a direct ray query in world coordinates.</summary>
public sealed class PhysicsRayQueryParameters : ElectronObject
{
    private Vector2 _from;
    private Vector2 _to;
    private RID[] _exclude = [];
    internal RID[] ExclusionsArray { get { ThrowIfDisposed(); return _exclude; } }
    private uint _collisionMask = uint.MaxValue;
    private bool _collideWithBodies = true;

    /// <summary>Creates an empty ray with all collision layers enabled.</summary>
    public PhysicsRayQueryParameters() { }

    /// <summary>Creates parameters for a ray between two global points.</summary>
    /// <param name="from">Finite ray origin in scene units.</param>
    /// <param name="to">Finite ray endpoint in scene units.</param>
    /// <param name="collisionMask">Accepted collision layers, all by default.</param>
    /// <param name="exclude">Collider RIDs to skip; null means empty.</param>
    /// <returns>A caller-owned parameter object.</returns>
    public static PhysicsRayQueryParameters Create(Vector2 from, Vector2 to,
        uint collisionMask = uint.MaxValue, RID[]? exclude = null) => new()
        {
            From = from,
            To = to,
            CollisionMask = collisionMask,
            Exclude = exclude ?? []
        };

    /// <summary>Gets or sets the finite global start point.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is nonfinite.</exception>
    public Vector2 From
    {
        get { ThrowIfDisposed(); return _from; }
        set { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _from = value; }
    }

    /// <summary>Gets or sets the finite global end point.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is nonfinite.</exception>
    public Vector2 To
    {
        get { ThrowIfDisposed(); return _to; }
        set { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _to = value; }
    }

    /// <summary>Gets or sets accepted collision-layer bits.</summary>
    /// <value>All 32 layers by default.</value>
    public uint CollisionMask
    {
        get { ThrowIfDisposed(); return _collisionMask; }
        set { ThrowIfDisposed(); _collisionMask = value; }
    }

    /// <summary>Gets or sets a caller-owned copy of excluded collider RIDs.</summary>
    /// <value>Empty by default.</value>
    /// <exception cref="ArgumentNullException">The assigned array is null.</exception>
    public RID[] Exclude
    {
        get { ThrowIfDisposed(); return (RID[])_exclude.Clone(); }
        set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _exclude = (RID[])value.Clone(); }
    }

    /// <summary>Gets or sets whether Area sensors are eligible.</summary>
    /// <value>False by default.</value>
    public bool CollideWithAreas
    {
        get { ThrowIfDisposed(); return _collideWithAreas; }
        set { ThrowIfDisposed(); _collideWithAreas = value; }
    }
    private bool _collideWithAreas;

    /// <summary>Gets or sets whether physics bodies are eligible.</summary>
    /// <value>True by default.</value>
    public bool CollideWithBodies
    {
        get { ThrowIfDisposed(); return _collideWithBodies; }
        set { ThrowIfDisposed(); _collideWithBodies = value; }
    }

    /// <summary>Gets or sets whether a filled shape containing the origin produces a zero-normal hit.</summary>
    /// <value>False by default.</value>
    public bool HitFromInside
    {
        get { ThrowIfDisposed(); return _hitFromInside; }
        set { ThrowIfDisposed(); _hitFromInside = value; }
    }
    private bool _hitFromInside;
}

/// <summary>Configures a direct point query in world coordinates.</summary>
public sealed class PhysicsPointQueryParameters : ElectronObject
{
    private Vector2 _position;
    private RID[] _exclude = [];
    internal RID[] ExclusionsArray { get { ThrowIfDisposed(); return _exclude; } }
    private uint _collisionMask = uint.MaxValue;
    private bool _collideWithBodies = true;

    /// <summary>Creates a point query at the world origin with all layers enabled.</summary>
    public PhysicsPointQueryParameters() { }

    /// <summary>Gets or sets the finite global sample point.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is nonfinite.</exception>
    public Vector2 Position
    {
        get { ThrowIfDisposed(); return _position; }
        set { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _position = value; }
    }

    /// <summary>Gets or sets accepted collision-layer bits.</summary>
    /// <value>All 32 layers by default.</value>
    public uint CollisionMask
    {
        get { ThrowIfDisposed(); return _collisionMask; }
        set { ThrowIfDisposed(); _collisionMask = value; }
    }

    /// <summary>Gets or sets a caller-owned copy of excluded collider RIDs.</summary>
    /// <value>Empty by default.</value>
    /// <exception cref="ArgumentNullException">The assigned array is null.</exception>
    public RID[] Exclude
    {
        get { ThrowIfDisposed(); return (RID[])_exclude.Clone(); }
        set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _exclude = (RID[])value.Clone(); }
    }

    /// <summary>Gets or sets whether Area sensors are eligible.</summary>
    /// <value>False by default.</value>
    public bool CollideWithAreas
    {
        get { ThrowIfDisposed(); return _collideWithAreas; }
        set { ThrowIfDisposed(); _collideWithAreas = value; }
    }
    private bool _collideWithAreas;

    /// <summary>Gets or sets whether physics bodies are eligible.</summary>
    /// <value>True by default.</value>
    public bool CollideWithBodies
    {
        get { ThrowIfDisposed(); return _collideWithBodies; }
        set { ThrowIfDisposed(); _collideWithBodies = value; }
    }
}
