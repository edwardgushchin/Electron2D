namespace Electron2D;

/// <summary>Retains ordinary texture dimensions without retaining image data.</summary>
/// <remarks>Useful for headless scene layout and stripped assets. Rendering uses diagnostic pixels;
/// this resource does not promise ordinary texture coordinates in shaders or materials.</remarks>
public sealed class PlaceholderTexture : Texture
{
    private readonly object _gate = new();
    private Vector2i _size = Vector2i.One;

    /// <summary>Creates a one-by-one texture placeholder with no image.</summary>
    public PlaceholderTexture() { }

    /// <summary>Gets or replaces the logical dimensions, truncating fractional components toward zero.</summary>
    /// <value>Initially one by one. Zero and negative metadata are retained. Every assignment emits Changed.</value>
    /// <exception cref="ArgumentOutOfRangeException">A component is not finite or cannot be represented by a signed 32-bit pixel dimension.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public Vector2 Size
    {
        get { lock (_gate) { ThrowIfDisposed(); return new(_size.X, _size.Y); } }
        set
        {
            lock (_gate)
            {
                ThrowIfDisposed();
                if (!value.IsFinite() || (double)value.X < int.MinValue || (double)value.X > int.MaxValue || (double)value.Y < int.MinValue || (double)value.Y > int.MaxValue)
                    throw new ArgumentOutOfRangeException(nameof(value), "Placeholder dimensions must be finite signed 32-bit pixel values.");
                _size = new((int)value.X, (int)value.Y);
            }
            EmitChanged();
        }
    }

    /// <inheritdoc />
    public override int GetWidth() { lock (_gate) { ThrowIfDisposed(); return _size.X; } }
    /// <inheritdoc />
    public override int GetHeight() { lock (_gate) { ThrowIfDisposed(); return _size.Y; } }
    /// <inheritdoc />
    public override Vector2 GetSize() => Size;
    /// <inheritdoc />
    public override Image.Format PixelFormat { get { ThrowIfDisposed(); return Image.Format.Max; } }

    private static readonly PropertyDescriptor[] Properties = [new PropertyDescriptor<PlaceholderTexture, Vector2>(nameof(Size), t => t.Size, (t, v) => t.Size = v, _ => Vector2.One, stored: true)];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new PlaceholderTexture();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (PlaceholderTexture)target;
        Vector2i size;
        lock (_gate) { ThrowIfDisposed(); size = _size; }
        lock (copy._gate) { copy.ThrowIfDisposed(); copy._size = size; }
    }
}
