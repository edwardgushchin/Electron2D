namespace Electron2D;

/// <summary>Combines borrowed diffuse, normal and specular textures for canvas lighting.</summary>
/// <remarks>The diffuse channel defines image queries and logical dimensions. Missing channels use white diffuse,
/// a perpendicular normal and white specular. Changes and disposal of channels propagate to consumers.
/// This resource has its own stable rendering identity and never owns its channels. Device-image channels preserve raw signed/unsigned values in built-in lighting. Integer channels
/// require nearest filtering; normal and specular mapping use the ordinary channel equations.</remarks>
public sealed class CanvasTexture : Texture
{
    private Texture? _diffuse, _normal, _specular;
    private Color _specularColor = Colors.White;
    private float _specularShininess = 1;
    private TextureFilter _textureFilter;
    private TextureRepeat _textureRepeat;

    /// <summary>Creates a one-pixel texture with no assigned channels.</summary>
    public CanvasTexture() { }

    /// <summary>Gets or sets the borrowed color channel.</summary>
    /// <value>Null initially; channel assignments cannot contain another CanvasTexture.</value>
    public Texture? DiffuseTexture { get { lock (TextureGraphGate) { ThrowIfDisposed(); return _diffuse; } } set => SetChannel(ref _diffuse, value); }
    /// <summary>Gets or sets the borrowed normal map with positive X, Y and Z coordinates.</summary>
    /// <value>Null initially; normal maps affect illuminated fragments.</value>
    public Texture? NormalTexture { get { lock (TextureGraphGate) { ThrowIfDisposed(); return _normal; } } set => SetChannel(ref _normal, value); }
    /// <summary>Gets or sets the borrowed specular color map.</summary>
    /// <value>Null initially.</value>
    public Texture? SpecularTexture { get { lock (TextureGraphGate) { ThrowIfDisposed(); return _specular; } } set => SetChannel(ref _specular, value); }

    /// <summary>Gets or sets the finite multiplier of specular reflection colors.</summary>
    /// <value>White initially.</value>
    public Color SpecularColor { get { lock (TextureGraphGate) { ThrowIfDisposed(); return _specularColor; } } set { lock (TextureGraphGate) { ThrowIfDisposed(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _specularColor = value; } EmitChanged(); } }
    /// <summary>Gets or sets the finite specular exponent control.</summary>
    /// <value>One initially, which disables reflections; lower values broaden reflections.</value>
    public float SpecularShininess { get { lock (TextureGraphGate) { ThrowIfDisposed(); return _specularShininess; } } set { lock (TextureGraphGate) { ThrowIfDisposed(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); _specularShininess = value; } EmitChanged(); } }
    /// <summary>Gets or sets filtering independently of the drawing item.</summary>
    /// <value>ParentNode initially, which uses the item's resolved filter.</value>
    public TextureFilter TextureFilter { get { lock (TextureGraphGate) { ThrowIfDisposed(); return _textureFilter; } } set { lock (TextureGraphGate) { ThrowIfDisposed(); if (value is < TextureFilter.ParentNode or >= TextureFilter.Max) throw new ArgumentOutOfRangeException(nameof(value)); _textureFilter = value; } EmitChanged(); } }
    /// <summary>Gets or sets repeating independently of the drawing item.</summary>
    /// <value>ParentNode initially, which uses the item's resolved repeat policy.</value>
    public TextureRepeat TextureRepeat { get { lock (TextureGraphGate) { ThrowIfDisposed(); return _textureRepeat; } } set { lock (TextureGraphGate) { ThrowIfDisposed(); if (value is < TextureRepeat.ParentNode or >= TextureRepeat.Max) throw new ArgumentOutOfRangeException(nameof(value)); _textureRepeat = value; } EmitChanged(); } }

    private void SetChannel(ref Texture? channel, Texture? value)
    {
        lock (TextureGraphGate)
        {
            ThrowIfDisposed();
            if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value));
            for (var next = value; next is not null; next = (next as AtlasTexture)?.Atlas)
                if (next is CanvasTexture) throw new ArgumentException("Canvas texture channels cannot contain a canvas texture.", nameof(value));
            if (ReferenceEquals(channel, value)) return;
            var previous = channel; channel = value;
            if (previous is not null && !HasChannel(previous)) { previous.Changed -= ChannelChanged; previous.Disposed -= ChannelDisposed; }
            if (value is not null && !ReferenceEquals(value, previous) && ChannelCount(value) == 1) { value.Changed += ChannelChanged; value.Disposed += ChannelDisposed; }
        }
        EmitChanged();
    }
    private bool HasChannel(Texture texture) => ReferenceEquals(_diffuse, texture) || ReferenceEquals(_normal, texture) || ReferenceEquals(_specular, texture);
    private int ChannelCount(Texture texture) => (ReferenceEquals(_diffuse, texture) ? 1 : 0) + (ReferenceEquals(_normal, texture) ? 1 : 0) + (ReferenceEquals(_specular, texture) ? 1 : 0);
    private void ChannelChanged(Resource _) { if (!IsDisposed) EmitChanged(); }
    private void ChannelDisposed(ElectronObject resource)
    {
        lock (TextureGraphGate)
        {
            var texture = (Texture)resource; texture.Changed -= ChannelChanged; texture.Disposed -= ChannelDisposed;
            if (ReferenceEquals(_diffuse, texture)) _diffuse = null;
            if (ReferenceEquals(_normal, texture)) _normal = null;
            if (ReferenceEquals(_specular, texture)) _specular = null;
        }
        if (!IsDisposed) EmitChanged();
    }

    /// <inheritdoc />
    public override int GetWidth() { lock (TextureGraphGate) { ThrowIfDisposed(); return _diffuse?.GetWidth() ?? 1; } }
    /// <inheritdoc />
    public override int GetHeight() { lock (TextureGraphGate) { ThrowIfDisposed(); return _diffuse?.GetHeight() ?? 1; } }
    /// <inheritdoc />
    public override Vector2 GetSize() { lock (TextureGraphGate) { ThrowIfDisposed(); return _diffuse?.GetSize() ?? Vector2.One; } }
    /// <inheritdoc />
    public override bool HasAlpha { get { lock (TextureGraphGate) { ThrowIfDisposed(); return _diffuse?.HasAlpha ?? false; } } }
    /// <inheritdoc />
    public override bool IsPixelOpaque(int x, int y) { lock (TextureGraphGate) { ThrowIfDisposed(); return _diffuse?.IsPixelOpaque(x, y) ?? false; } }
    /// <inheritdoc />
    public override Image? GetImage() { lock (TextureGraphGate) { ThrowIfDisposed(); return _diffuse?.GetImage(); } }
    internal override TexturePixels? CapturePixels() { lock (TextureGraphGate) { ThrowIfDisposed(); return _diffuse?.CapturePixels(); } }
    internal override bool ReferencesTexture(Texture target, int depth) => base.ReferencesTexture(target, depth) ||
        _diffuse?.ReferencesTexture(target, depth + 1) == true || _normal?.ReferencesTexture(target, depth + 1) == true || _specular?.ReferencesTexture(target, depth + 1) == true;

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CanvasTexture();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (CanvasTexture)target;
        copy.DiffuseTexture = deep ? (Texture?)duplicateSubresource(DiffuseTexture) : DiffuseTexture;
        copy.NormalTexture = deep ? (Texture?)duplicateSubresource(NormalTexture) : NormalTexture;
        copy.SpecularTexture = deep ? (Texture?)duplicateSubresource(SpecularTexture) : SpecularTexture;
        copy._specularColor = _specularColor; copy._specularShininess = _specularShininess; copy._textureFilter = _textureFilter; copy._textureRepeat = _textureRepeat;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) lock (TextureGraphGate)
            {
                var diffuse = _diffuse; var normal = _normal; var specular = _specular;
                _diffuse = _normal = _specular = null;
                if (diffuse is not null) { diffuse.Changed -= ChannelChanged; diffuse.Disposed -= ChannelDisposed; }
                if (normal is not null && !ReferenceEquals(normal, diffuse)) { normal.Changed -= ChannelChanged; normal.Disposed -= ChannelDisposed; }
                if (specular is not null && !ReferenceEquals(specular, diffuse) && !ReferenceEquals(specular, normal)) { specular.Changed -= ChannelChanged; specular.Disposed -= ChannelDisposed; }
            }
        base.Dispose(disposing);
    }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<CanvasTexture, Texture?>(nameof(DiffuseTexture), n => n.DiffuseTexture, (n, v) => n.DiffuseTexture = v, _ => null, stored: true),
        new PropertyDescriptor<CanvasTexture, Texture?>(nameof(NormalTexture), n => n.NormalTexture, (n, v) => n.NormalTexture = v, _ => null, stored: true),
        new PropertyDescriptor<CanvasTexture, Texture?>(nameof(SpecularTexture), n => n.SpecularTexture, (n, v) => n.SpecularTexture = v, _ => null, stored: true),
        new PropertyDescriptor<CanvasTexture, Color>(nameof(SpecularColor), n => n.SpecularColor, (n, v) => n.SpecularColor = v, _ => Colors.White, stored: true),
        new PropertyDescriptor<CanvasTexture, float>(nameof(SpecularShininess), n => n.SpecularShininess, (n, v) => n.SpecularShininess = v, _ => 1, stored: true),
        new PropertyDescriptor<CanvasTexture, TextureFilter>(nameof(TextureFilter), n => n.TextureFilter, (n, v) => n.TextureFilter = v, _ => TextureFilter.ParentNode, stored: true),
        new PropertyDescriptor<CanvasTexture, TextureRepeat>(nameof(TextureRepeat), n => n.TextureRepeat, (n, v) => n.TextureRepeat = v, _ => TextureRepeat.ParentNode, stored: true),
    ]);
}
