using System.Buffers.Binary;

namespace Electron2D;

/// <summary>A two-dimensional texture generated from a borrowed noise resource.</summary>
/// <remarks>Settings and source changes invalidate the image. The next image or renderer read bakes a coherent
/// snapshot; the texture never disposes its noise or color ramp. Generation may call user-defined Noise methods.</remarks>
public sealed class NoiseTexture : Texture
{
    [ThreadStatic] private static HashSet<NoiseTexture>? s_baking;
    private readonly object _gate = new();
    private int _width = 512, _height = 512;
    private bool _generateMipmaps = true, _normalize = true, _seamless, _invert, _asNormalMap;
    private float _seamlessBlendSkirt = 0.1f, _bumpStrength = 8f;
    private Noise? _noise;
    private Gradient? _colorRamp;
    private TexturePixels? _pixels;
    private bool _dirty = true;
    private long _version;

    /// <summary>Creates an uninitialized 512-by-512 noise texture without a noise source.</summary>
    public NoiseTexture() { }

    /// <summary>Gets or sets the generated image width in pixels.</summary>
    /// <value>512 by default; valid values are 1 through 16384.</value>
    public int Width { get { lock (_gate) { ThrowIfDisposed(); return _width; } } set { Dimension(value); Change(ref _width, value); } }

    /// <summary>Gets or sets the generated image height in pixels.</summary>
    /// <value>512 by default; valid values are 1 through 16384.</value>
    public int Height { get { lock (_gate) { ThrowIfDisposed(); return _height; } } set { Dimension(value); Change(ref _height, value); } }

    /// <summary>Gets or sets whether the generated image contains a complete mipmap chain.</summary>
    /// <value>True by default.</value>
    public bool GenerateMipmaps { get { lock (_gate) { ThrowIfDisposed(); return _generateMipmaps; } } set => Change(ref _generateMipmaps, value); }

    /// <summary>Gets or sets the borrowed noise source; null leaves the texture without pixels.</summary>
    /// <value>Null by default.</value>
    public Noise? Noise
    {
        get { lock (_gate) { ThrowIfDisposed(); return _noise; } }
        set
        {
            lock (_gate)
            {
                ThrowIfDisposed(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value));
                if (ReferenceEquals(_noise, value)) return;
                if (_noise is not null) _noise.Changed -= SourceChanged;
                _noise = value;
                if (_noise is not null) _noise.Changed += SourceChanged;
                Invalidate();
            }
            EmitChanged();
        }
    }

    /// <summary>Gets or sets the borrowed gradient applied to sampled luminance.</summary>
    /// <value>Null by default; a null ramp leaves the sampled image unchanged.</value>
    public Gradient? ColorRamp
    {
        get { lock (_gate) { ThrowIfDisposed(); return _colorRamp; } }
        set
        {
            lock (_gate)
            {
                ThrowIfDisposed(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value));
                if (ReferenceEquals(_colorRamp, value)) return;
                if (_colorRamp is not null) _colorRamp.Changed -= SourceChanged;
                _colorRamp = value;
                if (_colorRamp is not null) _colorRamp.Changed += SourceChanged;
                Invalidate();
            }
            EmitChanged();
        }
    }

    /// <summary>Gets or sets whether noise sampling uses the seamless overlap path.</summary>
    /// <value>False by default.</value>
    public bool Seamless
    {
        get { lock (_gate) { ThrowIfDisposed(); return _seamless; } }
        set { if (Change(ref _seamless, value)) NotifyPropertyListChanged(); }
    }

    /// <summary>Gets or sets whether sampled grayscale values are inverted before further processing.</summary>
    /// <value>False by default.</value>
    public bool Invert { get { lock (_gate) { ThrowIfDisposed(); return _invert; } } set => Change(ref _invert, value); }

    /// <summary>Gets or sets whether the sampled image becomes a wrapping tangent-space normal map.</summary>
    /// <value>False by default.</value>
    public bool AsNormalMap
    {
        get { lock (_gate) { ThrowIfDisposed(); return _asNormalMap; } }
        set { if (Change(ref _asNormalMap, value)) NotifyPropertyListChanged(); }
    }

    /// <summary>Gets or sets whether noise samples are normalized across the image.</summary>
    /// <value>True by default.</value>
    public bool Normalize { get { lock (_gate) { ThrowIfDisposed(); return _normalize; } } set => Change(ref _normalize, value); }

    /// <summary>Gets or sets the overlap fraction for seamless generation.</summary>
    /// <value>0.1 by default; valid values are 0 through 1.</value>
    public float SeamlessBlendSkirt
    {
        get { lock (_gate) { ThrowIfDisposed(); return _seamlessBlendSkirt; } }
        set
        {
            if (!float.IsFinite(value) || value is < 0f or > 1f) throw new ArgumentOutOfRangeException(nameof(value));
            Change(ref _seamlessBlendSkirt, value);
        }
    }

    /// <summary>Gets or sets the finite normal-map bump multiplier.</summary>
    /// <value>8 by default; a change rebakes only while AsNormalMap is enabled.</value>
    public float BumpStrength
    {
        get { lock (_gate) { ThrowIfDisposed(); return _bumpStrength; } }
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            lock (_gate)
            {
                ThrowIfDisposed(); if (_bumpStrength == value) return;
                _bumpStrength = value;
                if (!_asNormalMap) return;
                Invalidate();
            }
            EmitChanged();
        }
    }

    /// <inheritdoc />
    public override int GetWidth() => Width;
    /// <inheritdoc />
    public override int GetHeight() => Height;
    /// <inheritdoc />
    public override Vector2 GetSize() { lock (_gate) { ThrowIfDisposed(); return new(_width, _height); } }
    /// <summary>Reports no alpha in the noise texture's declared sampling contract.</summary>
    /// <value>False, including when a color ramp produces RGBA pixels.</value>
    public override bool HasAlpha { get { ThrowIfDisposed(); return false; } }
    /// <inheritdoc />
    public override Image? GetImage() => CapturePixels()?.CopyImage();

    internal override TexturePixels? CapturePixels()
    {
        var baking = s_baking ??= [];
        if (!baking.Add(this)) throw new InvalidOperationException("Noise texture generation is reentrant.");
        try { return BakePixels(); }
        finally { baking.Remove(this); }
    }

    private TexturePixels? BakePixels()
    {
        // ponytail: retry a few concurrent source changes; a perpetually changing source must fail rather than spin.
        for (var attempt = 0; attempt < 8; attempt++)
        {
            int width, height; bool mipmaps, normalize, seamless, invert, normal;
            float skirt, strength; Noise? noise; Gradient? ramp; long version;
            lock (_gate)
            {
                ThrowIfDisposed();
                if (!_dirty) return _pixels;
                (width, height, mipmaps, normalize, seamless, invert, normal, skirt, strength, noise, ramp, version) =
                    (_width, _height, _generateMipmaps, _normalize, _seamless, _invert, _asNormalMap,
                        _seamlessBlendSkirt, _bumpStrength, _noise, _colorRamp, _version);
            }

            TexturePixels? pixels = null;
            if (noise is not null)
            {
                using var sampled = (seamless ? noise.GetSeamlessImage(width, height, invert, skirt, normalize)
                    : noise.GetImage(width, height, invert, normalize))
                    ?? throw new InvalidOperationException("The noise resource returned no image.");
                if (sampled.Width != width || sampled.Height != height || sampled.HasMipmaps)
                    throw new InvalidOperationException("The noise resource returned an incompatible image.");
                using var mapped = ramp is null ? null : ApplyRamp(sampled, ramp);
                var image = mapped ?? sampled;
                if (normal) image.BumpMapToNormalMap(strength);
                if (mipmaps) image.GenerateMipmaps();
                pixels = TexturePixels.FromImage(image);
            }

            lock (_gate)
            {
                ThrowIfDisposed();
                if (version != _version) continue;
                _pixels = pixels;
                _dirty = false;
                return pixels;
            }
        }
        throw new InvalidOperationException("Noise texture sources changed throughout generation.");
    }

    private static Image ApplyRamp(Image source, Gradient ramp)
    {
        var width = source.Width; var height = source.Height;
        var data = new byte[checked(width * height * 4)];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var color = ramp.Sample(source.GetPixel(x, y).Luminance);
                BinaryPrimitives.WriteUInt32LittleEndian(data.AsSpan((y * width + x) * 4), color.ToABGR32());
            }
        return Image.CreateFromData(width, height, false, Image.Format.Rgba8, data);
    }

    private void SourceChanged(Resource source)
    {
        lock (_gate)
        {
            if (IsDisposed || !ReferenceEquals(source, _noise) && !ReferenceEquals(source, _colorRamp)) return;
            Invalidate();
        }
        EmitChanged();
    }

    private bool Change<T>(ref T field, T value) where T : IEquatable<T>
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (field.Equals(value)) return false;
            field = value; Invalidate();
        }
        EmitChanged();
        return true;
    }

    private void Invalidate() { _dirty = true; _version++; }
    private static void Dimension(int value) { if (value is < 1 or > 16_384) throw new ArgumentOutOfRangeException(nameof(value)); }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<NoiseTexture, int>(nameof(Width), t => t.Width, (t, v) => t.Width = v, _ => 512),
        new PropertyDescriptor<NoiseTexture, int>(nameof(Height), t => t.Height, (t, v) => t.Height = v, _ => 512),
        new PropertyDescriptor<NoiseTexture, bool>(nameof(GenerateMipmaps), t => t.GenerateMipmaps, (t, v) => t.GenerateMipmaps = v, _ => true),
        new PropertyDescriptor<NoiseTexture, Noise?>(nameof(Noise), t => t.Noise, (t, v) => t.Noise = v, _ => null),
        new PropertyDescriptor<NoiseTexture, Gradient?>(nameof(ColorRamp), t => t.ColorRamp, (t, v) => t.ColorRamp = v, _ => null),
        new PropertyDescriptor<NoiseTexture, bool>(nameof(Seamless), t => t.Seamless, (t, v) => t.Seamless = v, _ => false),
        new PropertyDescriptor<NoiseTexture, bool>(nameof(Invert), t => t.Invert, (t, v) => t.Invert = v, _ => false),
        new PropertyDescriptor<NoiseTexture, bool>(nameof(AsNormalMap), t => t.AsNormalMap, (t, v) => t.AsNormalMap = v, _ => false),
        new PropertyDescriptor<NoiseTexture, bool>(nameof(Normalize), t => t.Normalize, (t, v) => t.Normalize = v, _ => true),
        new PropertyDescriptor<NoiseTexture, float>(nameof(SeamlessBlendSkirt), t => t.SeamlessBlendSkirt, (t, v) => t.SeamlessBlendSkirt = v, _ => 0.1f),
        new PropertyDescriptor<NoiseTexture, float>(nameof(BumpStrength), t => t.BumpStrength, (t, v) => t.BumpStrength = v, _ => 8f),
    ]);

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new NoiseTexture();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        int width, height; bool mipmaps, normalize, seamless, invert, normal; float skirt, strength;
        Noise? noise; Gradient? ramp;
        lock (_gate)
        {
            ThrowIfDisposed();
            (width, height, mipmaps, normalize, seamless, invert, normal, skirt, strength, noise, ramp) =
                (_width, _height, _generateMipmaps, _normalize, _seamless, _invert, _asNormalMap,
                    _seamlessBlendSkirt, _bumpStrength, _noise, _colorRamp);
        }
        if (deep) { noise = (Noise?)duplicateSubresource(noise); ramp = (Gradient?)duplicateSubresource(ramp); }
        var copy = (NoiseTexture)target;
        lock (copy._gate)
        {
            if (copy._noise is not null) copy._noise.Changed -= copy.SourceChanged;
            if (copy._colorRamp is not null) copy._colorRamp.Changed -= copy.SourceChanged;
            copy._width = width; copy._height = height; copy._generateMipmaps = mipmaps; copy._normalize = normalize;
            copy._seamless = seamless; copy._invert = invert; copy._asNormalMap = normal;
            copy._seamlessBlendSkirt = skirt; copy._bumpStrength = strength;
            copy._noise = noise; copy._colorRamp = ramp; copy._pixels = null;
            if (noise is not null) noise.Changed += copy.SourceChanged;
            if (ramp is not null) ramp.Changed += copy.SourceChanged;
            copy.Invalidate();
        }
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
            lock (_gate)
            {
                if (_noise is not null) _noise.Changed -= SourceChanged;
                if (_colorRamp is not null) _colorRamp.Changed -= SourceChanged;
                _noise = null; _colorRamp = null; _pixels = null; Invalidate();
            }
        base.Dispose(disposing);
    }
}
