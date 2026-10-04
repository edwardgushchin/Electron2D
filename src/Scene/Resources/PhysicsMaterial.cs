namespace Electron2D;

/// <summary>Reusable friction and bounce settings for a two-dimensional physics body.</summary>
/// <remarks>Bodies borrow this resource. Assigning a property raises <see cref="Resource.Changed"/> and updates attached body fixtures before their next physics step.</remarks>
public sealed class PhysicsMaterial : Resource
{
    private float _friction = 1f;
    private float _bounce;
    private bool _rough;
    private bool _absorbent;
    private ulong _revision;

    /// <summary>Creates a material with friction one, bounce zero, and both modifiers disabled.</summary>
    public PhysicsMaterial() { }

    /// <summary>Gets or sets the finite friction coefficient.</summary>
    /// <value>One by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is not finite.</exception>
    public float Friction
    {
        get { ThrowIfDisposed(); return _friction; }
        set { ThrowIfDisposed(); Finite(value); _friction = value; _revision++; EmitChanged(); }
    }

    /// <summary>Gets or sets the finite bounce coefficient.</summary>
    /// <value>Zero by default.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is not finite.</exception>
    public float Bounce
    {
        get { ThrowIfDisposed(); return _bounce; }
        set { ThrowIfDisposed(); Finite(value); _bounce = value; _revision++; EmitChanged(); }
    }

    /// <summary>Gets or sets whether this surface's friction takes precedence over a nonrough surface.</summary>
    /// <value>False by default. When both surfaces are rough, the greater friction wins.</value>
    public bool Rough
    {
        get { ThrowIfDisposed(); return _rough; }
        set { ThrowIfDisposed(); _rough = value; _revision++; EmitChanged(); }
    }

    /// <summary>Gets or sets whether this surface subtracts its bounce from the other surface's bounce.</summary>
    /// <value>False by default.</value>
    public bool Absorbent
    {
        get { ThrowIfDisposed(); return _absorbent; }
        set { ThrowIfDisposed(); _absorbent = value; _revision++; EmitChanged(); }
    }

    internal float ComputedFriction => _rough ? -_friction : _friction;
    internal float ComputedBounce => _absorbent ? -_bounce : _bounce;
    internal ulong Revision => _revision;

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<PhysicsMaterial, float>(nameof(Friction), p => p.Friction, (p, v) => p.Friction = v, _ => 1f, stored: true);
        yield return new PropertyDescriptor<PhysicsMaterial, float>(nameof(Bounce), p => p.Bounce, (p, v) => p.Bounce = v, _ => 0f, stored: true);
        yield return new PropertyDescriptor<PhysicsMaterial, bool>(nameof(Rough), p => p.Rough, (p, v) => p.Rough = v, _ => false, stored: true);
        yield return new PropertyDescriptor<PhysicsMaterial, bool>(nameof(Absorbent), p => p.Absorbent, (p, v) => p.Absorbent = v, _ => false, stored: true);
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new PhysicsMaterial();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (PhysicsMaterial)target;
        copy._friction = _friction;
        copy._bounce = _bounce;
        copy._rough = _rough;
        copy._absorbent = _absorbent;
    }

    private static void Finite(float value)
    {
        if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
    }
}
