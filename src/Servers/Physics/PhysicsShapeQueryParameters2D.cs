namespace Electron2D;

/// <summary>Configures a direct overlap or motion query for one two-dimensional shape.</summary>
/// <remarks>Assigning <see cref="Shape"/> retains the caller resource and its server RID.
/// Assigning a different <see cref="ShapeRID"/> releases that borrowed reference.</remarks>
public sealed class PhysicsShapeQueryParameters2D : ElectronObject
{
    private Shape? _shape;
    private RID _shapeRID;
    private Transform _transform = Transform.Identity;
    private Vector2 _motion;
    private float _margin;
    private uint _collisionMask = uint.MaxValue;
    private RID[] _exclude = [];
    private bool _collideWithBodies = true;
    private bool _collideWithAreas;

    internal RID[] ExclusionsArray => _exclude;

    /// <summary>Creates an empty query with all layers and body detection enabled.</summary>
    public PhysicsShapeQueryParameters2D() { }

    /// <summary>Gets or sets the caller-owned resource used for shape queries.</summary>
    /// <value>Null by default. A successful assignment also sets ShapeRID.</value>
    /// <exception cref="ArgumentNullException">The assigned resource is null.</exception>
    /// <exception cref="ObjectDisposedException">The assigned resource was disposed.</exception>
    public Shape? Shape
    {
        get { ThrowIfDisposed(); return _shape; }
        set
        {
            ThrowIfDisposed();
            ArgumentNullException.ThrowIfNull(value);
            if (value.IsDisposed) throw new ObjectDisposedException(nameof(value));
            var rid = value.GetQueryRID();
            _shape = value;
            _shapeRID = rid;
        }
    }

    /// <summary>Gets or sets the server identity of the shape to query.</summary>
    /// <value>An empty RID by default. Assigning a different value clears Shape.</value>
    public RID ShapeRID
    {
        get { ThrowIfDisposed(); return _shapeRID; }
        set
        {
            ThrowIfDisposed();
            if (_shapeRID == value) return;
            _shape = null;
            _shapeRID = value;
        }
    }

    /// <summary>Gets or sets the finite, unit-scale global shape pose.</summary>
    /// <value>Identity by default; scale and skew are unsupported in the current physics profile.</value>
    /// <exception cref="ArgumentException">The pose is nonfinite, scaled or skewed.</exception>
    public Transform Transform
    {
        get { ThrowIfDisposed(); return _transform; }
        set
        {
            ThrowIfDisposed();
            if (!value.IsFinite() || !value.Scale.IsEqualApprox(Vector2.One) || !Mathf.IsZeroApprox(value.Skew))
                throw new ArgumentException("A query shape requires finite translation, unit scale and zero skew.", nameof(value));
            _transform = value;
        }
    }

    /// <summary>Gets or sets the finite global motion used by a sweep.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is nonfinite.</exception>
    public Vector2 Motion
    {
        get { ThrowIfDisposed(); return _motion; }
        set { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _motion = value; }
    }

    /// <summary>Gets or sets a nonnegative collision margin in scene units.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
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

    /// <summary>Gets or sets whether physics bodies are eligible.</summary>
    /// <value>True by default.</value>
    public bool CollideWithBodies
    {
        get { ThrowIfDisposed(); return _collideWithBodies; }
        set { ThrowIfDisposed(); _collideWithBodies = value; }
    }

    /// <summary>Gets or sets whether Area sensors are eligible.</summary>
    /// <value>False by default.</value>
    public bool CollideWithAreas
    {
        get { ThrowIfDisposed(); return _collideWithAreas; }
        set { ThrowIfDisposed(); _collideWithAreas = value; }
    }
}
