namespace Electron2D;

/// <summary>Illuminates canvas items inside a transformed texture mask.</summary>
/// <remarks>A missing texture emits no light. The texture is borrowed, centered at Offset and scaled with this entity.
/// Pixel and size changes are read by the next rendered frame.</remarks>
public class PointLight : Light
{
    private Texture? _texture;
    private Vector2 _offset;
    private float _textureScale = 1;

    /// <summary>Creates a point light without a texture.</summary>
    public PointLight() { }

    /// <summary>Gets or sets the borrowed light mask.</summary>
    /// <value>Null initially; disposed resources cannot be assigned.</value>
    public Texture? Texture { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _texture; } set { EnsureMutable(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value)); _texture = value; } }

    /// <summary>Gets or sets the finite local center offset of the light texture.</summary>
    /// <value>Zero initially; the shadow origin remains the light's entity origin.</value>
    public Vector2 Offset { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _offset; } set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _offset = value; } }

    /// <summary>Gets or sets the finite texture size multiplier.</summary>
    /// <value>One initially; zero becomes 0.00001 to preserve an invertible mask.</value>
    public float TextureScale { get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _textureScale; } set { EnsureMutable(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); _textureScale = value == 0 ? 0.00001f : value; } }

    /// <summary>Gets or sets finite light height in local scene units for normal and specular mapping.</summary>
    /// <value>Zero initially.</value>
    public float Height { get => GetHeight(); set => SetHeight(value); }

    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(PointLight) ? CreatePoint : base.CreateSceneInstanceFactory();
    private static Node CreatePoint() => new PointLight();

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<PointLight, Texture?>(nameof(Texture), n => n.Texture, (n, v) => n.Texture = v, _ => null, stored: true),
        new PropertyDescriptor<PointLight, Vector2>(nameof(Offset), n => n.Offset, (n, v) => n.Offset = v, _ => Vector2.Zero, stored: true),
        new PropertyDescriptor<PointLight, float>(nameof(TextureScale), n => n.TextureScale, (n, v) => n.TextureScale = v, _ => 1, stored: true),
        new PropertyDescriptor<PointLight, float>(nameof(Height), n => n.Height, (n, v) => n.Height = v, _ => 0, stored: true),
    ]);
}
