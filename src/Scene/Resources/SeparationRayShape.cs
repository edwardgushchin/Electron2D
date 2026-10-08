namespace Electron2D;

/// <summary>A directed ray that separates its endpoint from intersecting collision surfaces.</summary>
/// <remarks>The caller owns this resource. Body motion and sensing use directed separation contacts.
/// Ray and point queries cannot intersect it. Its zero-density sensor fixture contributes no inertia;
/// ordinary dynamic solver separation impulses and contact reports remain incomplete.</remarks>
public sealed class SeparationRayShape : Shape
{
    private float _length = 20f;
    private bool _slideOnSlope;

    /// <summary>Creates a twenty-unit downward separation ray.</summary>
    public SeparationRayShape() { }

    /// <summary>Gets or sets the finite nonnegative local ray length in scene units.</summary>
    /// <value>Twenty by default; zero contributes no separation contact.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative, nonfinite or exceeds the squared backend geometry range.</exception>
    public float Length
    {
        get { ThrowIfDisposed(); return _length; }
        set
        {
            ThrowIfDisposed();
            var backendLength = value * PhysicsSpace.MetersPerUnit;
            if (!float.IsFinite(value) || value < 0 || !float.IsFinite(backendLength * backendLength))
                throw new ArgumentOutOfRangeException(nameof(value));
            if (_length == value) return;
            _length = value;
            EmitGeometryChanged();
        }
    }

    /// <summary>Gets or sets whether separation follows the hit surface normal.</summary>
    /// <value>False by default, which separates opposite the ray direction.</value>
    public bool SlideOnSlope
    {
        get { ThrowIfDisposed(); return _slideOnSlope; }
        set { ThrowIfDisposed(); if (_slideOnSlope == value) return; _slideOnSlope = value; EmitGeometryChanged(); }
    }

    /// <summary>Gets the local drawing envelope of the directed ray.</summary>
    /// <returns>The zero-width ray bounds grown on each side by four times the square root of one half.</returns>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public override Rect2 GetRect()
    {
        ThrowIfDisposed();
        return new Rect2(0, 0, 0, _length).Grow(MathF.Sqrt(0.5f) * 4f);
    }

    internal override PhysicsShapeGeometry GetGeometry()
    {
        ThrowIfDisposed();
        return new() { Kind = PhysicsShapeGeometry.ShapeKind.SeparationRay, B = new(0, _length), SlideOnSlope = _slideOnSlope };
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new SeparationRayShape();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (SeparationRayShape)target;
        copy._length = _length;
        copy._slideOnSlope = _slideOnSlope;
    }
}
