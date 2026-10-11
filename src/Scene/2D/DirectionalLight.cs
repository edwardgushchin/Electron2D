namespace Electron2D;

/// <summary>Illuminates selected canvas layers along its transformed direction.</summary>
/// <remarks>Rotation zero shines downward. Receiving item masks are ignored; shadow masks still select contours.</remarks>
public class DirectionalLight : Light
{
    private float _maxDistance = 10000;

    /// <summary>Creates a directional light with a 10000-unit shadow range.</summary>
    public DirectionalLight() { }

    /// <summary>Gets or sets the finite maximum distance from a receiving pixel to a shadow contour.</summary>
    /// <value>10000 initially; nonpositive distances contribute no shadow.</value>
    public float MaxDistance { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _maxDistance; } set { EnsureMutable(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); _maxDistance = value; } }

    /// <summary>Gets or sets finite normalized light tilt for normal and specular mapping.</summary>
    /// <value>Zero initially; one points perpendicular to the canvas.</value>
    public float Height { get => GetHeight(); set => SetHeight(value); }

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(DirectionalLight) ? CreateDirectional : base.CreateSceneInstanceFactory();
    private static Node CreateDirectional() => new DirectionalLight();

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<DirectionalLight, float>(nameof(MaxDistance), n => n.MaxDistance, (n, v) => n.MaxDistance = v, _ => 10000, stored: true),
        new PropertyDescriptor<DirectionalLight, float>(nameof(Height), n => n.Height, (n, v) => n.Height = v, _ => 0, stored: true),
    ]);
}
