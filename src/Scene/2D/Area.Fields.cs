namespace Electron2D;

public sealed partial class Area
{
    /// <summary>Controls how one area contributes a gravity or damping field to overlapping bodies.</summary>
    public enum SpaceOverride
    {
        /// <summary>Leaves this field disabled.</summary>
        Disabled = 0,
        /// <summary>Adds this field and continues with lower-priority areas and the world default.</summary>
        Combine = 1,
        /// <summary>Adds this field and ignores lower-priority areas and the world default.</summary>
        CombineReplace = 2,
        /// <summary>Replaces the current field and ignores lower-priority areas and the world default.</summary>
        Replace = 3,
        /// <summary>Replaces the current field but continues with lower-priority areas and the world default.</summary>
        ReplaceCombine = 4
    }

    private static readonly PropertyDescriptor[] FieldProperties =
    [
        new PropertyDescriptor<Area, SpaceOverride>(nameof(GravitySpaceOverride), area => area.GravitySpaceOverride,
            (area, value) => area.GravitySpaceOverride = value, _ => SpaceOverride.Disabled, stored: true),
        new PropertyDescriptor<Area, float>(nameof(Gravity), area => area.Gravity,
            (area, value) => area.Gravity = value, _ => 980f, stored: true),
        new PropertyDescriptor<Area, Vector2>(nameof(GravityDirection), area => area.GravityDirection,
            (area, value) => area.GravityDirection = value, _ => new Vector2(0, 1), stored: true),
        new PropertyDescriptor<Area, bool>(nameof(GravityPoint), area => area.GravityPoint,
            (area, value) => area.GravityPoint = value, _ => false, stored: true),
        new PropertyDescriptor<Area, Vector2>(nameof(GravityPointCenter), area => area.GravityPointCenter,
            (area, value) => area.GravityPointCenter = value, _ => new Vector2(0, 1), stored: true),
        new PropertyDescriptor<Area, float>(nameof(GravityPointUnitDistance), area => area.GravityPointUnitDistance,
            (area, value) => area.GravityPointUnitDistance = value, _ => 0f, stored: true),
        new PropertyDescriptor<Area, SpaceOverride>(nameof(LinearDampSpaceOverride), area => area.LinearDampSpaceOverride,
            (area, value) => area.LinearDampSpaceOverride = value, _ => SpaceOverride.Disabled, stored: true),
        new PropertyDescriptor<Area, float>(nameof(LinearDamp), area => area.LinearDamp,
            (area, value) => area.LinearDamp = value, _ => 0.1f, stored: true),
        new PropertyDescriptor<Area, SpaceOverride>(nameof(AngularDampSpaceOverride), area => area.AngularDampSpaceOverride,
            (area, value) => area.AngularDampSpaceOverride = value, _ => SpaceOverride.Disabled, stored: true),
        new PropertyDescriptor<Area, float>(nameof(AngularDamp), area => area.AngularDamp,
            (area, value) => area.AngularDamp = value, _ => 1f, stored: true),
        new PropertyDescriptor<Area, int>(nameof(Priority), area => area.Priority,
            (area, value) => area.Priority = value, _ => 0, stored: true)
    ];

    private SpaceOverride _gravitySpaceOverride;
    private SpaceOverride _linearDampSpaceOverride;
    private SpaceOverride _angularDampSpaceOverride;
    private float _gravity = 980f;
    private Vector2 _gravityVector = new(0, 1);
    private bool _gravityPoint;
    private float _gravityPointUnitDistance;
    private float _linearDamp = 0.1f;
    private float _angularDamp = 1f;
    private int _priority;

    internal bool HasFieldOverrides => _gravitySpaceOverride != SpaceOverride.Disabled ||
        _linearDampSpaceOverride != SpaceOverride.Disabled || _angularDampSpaceOverride != SpaceOverride.Disabled;

    /// <summary>Gets or sets how this area's gravity combines with other areas and the world.</summary>
    /// <value><see cref="SpaceOverride.Disabled"/> by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode value is not defined.</exception>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public SpaceOverride GravitySpaceOverride
    {
        get { ThrowIfDisposed(); return _gravitySpaceOverride; }
        set { EnsureMutable(); ValidateMode(value); _gravitySpaceOverride = value; }
    }

    /// <summary>Gets or sets the finite gravity strength in scene units per second squared.</summary>
    /// <value>980 by default; signed values are allowed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public float Gravity
    {
        get { ThrowIfDisposed(); return _gravity; }
        set { EnsureMutable(); ValidateFinite(value); _gravity = value; }
    }

    /// <summary>Gets or sets the local gravity direction without normalizing it.</summary>
    /// <value>(0, 1) by default. Shares its stored vector with <see cref="GravityPointCenter"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public Vector2 GravityDirection
    {
        get { ThrowIfDisposed(); return _gravityVector; }
        set { EnsureMutable(); ValidateFinite(value); _gravityVector = value; }
    }

    /// <summary>Gets or sets whether gravity points toward the transformed local center.</summary>
    /// <value>False by default.</value>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public bool GravityPoint
    {
        get { ThrowIfDisposed(); return _gravityPoint; }
        set { EnsureMutable(); _gravityPoint = value; }
    }

    /// <summary>Gets or sets the local attraction point used when <see cref="GravityPoint"/> is true.</summary>
    /// <value>(0, 1) by default. Shares its stored vector with <see cref="GravityDirection"/>.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public Vector2 GravityPointCenter
    {
        get { ThrowIfDisposed(); return _gravityVector; }
        set { EnsureMutable(); ValidateFinite(value); _gravityVector = value; }
    }

    /// <summary>Gets or sets the distance at which point gravity has the configured strength.</summary>
    /// <value>Zero by default for distance-independent point gravity; positive values use inverse-square falloff.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public float GravityPointUnitDistance
    {
        get { ThrowIfDisposed(); return _gravityPointUnitDistance; }
        set { EnsureMutable(); ValidateFinite(value); _gravityPointUnitDistance = value; }
    }

    /// <summary>Gets or sets how this area's linear damping combines with other areas and the world.</summary>
    /// <value><see cref="SpaceOverride.Disabled"/> by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode value is not defined.</exception>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public SpaceOverride LinearDampSpaceOverride
    {
        get { ThrowIfDisposed(); return _linearDampSpaceOverride; }
        set { EnsureMutable(); ValidateMode(value); _linearDampSpaceOverride = value; }
    }

    /// <summary>Gets or sets the finite linear damping rate per second.</summary>
    /// <value>0.1 by default; signed values are allowed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public float LinearDamp
    {
        get { ThrowIfDisposed(); return _linearDamp; }
        set { EnsureMutable(); ValidateFinite(value); _linearDamp = value; }
    }

    /// <summary>Gets or sets how this area's angular damping combines with other areas and the world.</summary>
    /// <value><see cref="SpaceOverride.Disabled"/> by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The mode value is not defined.</exception>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public SpaceOverride AngularDampSpaceOverride
    {
        get { ThrowIfDisposed(); return _angularDampSpaceOverride; }
        set { EnsureMutable(); ValidateMode(value); _angularDampSpaceOverride = value; }
    }

    /// <summary>Gets or sets the finite angular damping rate per second.</summary>
    /// <value>One by default; signed values are allowed.</value>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public float AngularDamp
    {
        get { ThrowIfDisposed(); return _angularDamp; }
        set { EnsureMutable(); ValidateFinite(value); _angularDamp = value; }
    }

    /// <summary>Gets or sets this area's processing priority; greater values are processed first.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="InvalidOperationException">An attached area is changed off its scene owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The area has been disposed.</exception>
    public int Priority
    {
        get { ThrowIfDisposed(); return _priority; }
        set { EnsureMutable(); _priority = value; }
    }

    internal Vector2 ComputeGravity(Vector2 bodyPosition)
    {
        if (!_gravityPoint) return _gravityVector * _gravity;
        var toward = ToGlobal(_gravityVector) - bodyPosition;
        var lengthSquared = toward.LengthSquared();
        if (lengthSquared == 0) return Vector2.Zero;
        var strength = _gravityPointUnitDistance > 0
            ? _gravity * _gravityPointUnitDistance * _gravityPointUnitDistance / lengthSquared
            : _gravity;
        return toward.Normalized() * strength;
    }

    private static void ValidateFinite(float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static void ValidateFinite(Vector2 value)
    {
        if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static void ValidateMode(SpaceOverride value)
    {
        if (value is < SpaceOverride.Disabled or > SpaceOverride.ReplaceCombine)
            throw new ArgumentOutOfRangeException(nameof(value));
    }
}
