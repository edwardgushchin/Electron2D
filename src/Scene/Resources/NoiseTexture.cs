namespace Electron2D;

/// <summary>A two-dimensional texture generated from a borrowed noise resource.</summary>
/// <remarks>Scene changes coalesce into deferred generation; following images compute on workers and publish Changed
/// on the scene owner. Reads retain completed pixels. Without a running scene loop, reads generate synchronously.
/// Sources are borrowed; custom Noise image methods must support worker execution and coordinated state.</remarks>
public sealed partial class NoiseTexture : Texture
{
    [ThreadStatic] private static HashSet<NoiseTexture>? s_baking;
    private readonly object _gate = new();
    private int _width = 512, _height = 512;
    private bool _generateMipmaps = true, _normalize = true, _seamless, _invert, _asNormalMap;
    private float _seamlessBlendSkirt = 0.1f, _bumpStrength = 8f;
    private Noise? _noise;
    private Gradient? _colorRamp;
    private TexturePixels? _pixels, _renderPixels;
    private bool _dirty = true;
    private long _version;

    /// <summary>Creates an uninitialized 512-by-512 noise texture without a noise source.</summary>
    public NoiseTexture() { _updateAction = UpdateTexture; _publishAction = PublishCompletion; }

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
                if (_noise is not null) { _noise.Changed -= SourceChanged; _noise.Disposed -= SourceDisposed; }
                _noise = value;
                if (_noise is not null) { _noise.Changed += SourceChanged; _noise.Disposed += SourceDisposed; }
                Invalidate();
            }
            NotifyInvalidation();
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
                if (_colorRamp is not null) { _colorRamp.Changed -= SourceChanged; _colorRamp.Disposed -= SourceDisposed; }
                _colorRamp = value;
                if (_colorRamp is not null) { _colorRamp.Changed += SourceChanged; _colorRamp.Disposed += SourceDisposed; }
                Invalidate();
            }
            NotifyInvalidation();
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
            NotifyInvalidation();
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
    public override Image? GetImage() { RefreshPixels(); lock (_gate) { ThrowIfDisposed(); return _pixels?.CopyImage(); } }

    internal override TexturePixels? CapturePixels() { RefreshPixels(); lock (_gate) { ThrowIfDisposed(); return _renderPixels; } }

    private TexturePixels? GeneratePixels(GenerationWork work)
    {
        var baking = s_baking ??= []; if (!baking.Add(this)) throw new InvalidOperationException("Noise texture generation is reentrant.");
        try
        {
            var input = work.Input; if (input.Noise is not { } noise) return null;
            ObjectDisposedException.ThrowIf(noise.IsDisposed, noise);
            using var sampled = (input.Seamless ? noise.GetSeamlessImage(input.Width, input.Height, input.Invert, input.Skirt, input.Normalize) : noise.GetImage(input.Width, input.Height, input.Invert, input.Normalize)) ?? throw new InvalidOperationException("The noise resource returned no image.");
            if (sampled.Width != input.Width || sampled.Height != input.Height || sampled.HasMipmaps) throw new InvalidOperationException("The noise resource returned an incompatible image.");
            using var mapped = input.Ramp is null ? null : ApplyRamp(sampled, input.Ramp); var image = mapped ?? sampled;
            if (input.Normal) image.BumpMapToNormalMap(input.Strength);
            if (input.Mipmaps) image.GenerateMipmaps();
            return TexturePixels.FromImage(image);
        }
        finally { baking.Remove(this); }
    }

    private static Image ApplyRamp(Image source, Gradient ramp)
    {
        var width = source.Width; var height = source.Height;
        var data = new byte[checked(width * height * 4)];
        for (var y = 0; y < height; y++)
            for (var x = 0; x < width; x++)
            {
                var color = ramp.Sample(source.GetPixel(x, y).Luminance);
                Image.WriteColor(data, (y * width + x) * 4, Image.Format.Rgba8, color);
            }
        return Image.CreateFromData(width, height, false, Image.Format.Rgba8, data);
    }

    private void SourceDisposed(ElectronObject source) => SourceChanged((Resource)source);

    private void SourceChanged(Resource source)
    {
        lock (_gate)
        {
            if (IsDisposed || !ReferenceEquals(source, _noise) && !ReferenceEquals(source, _colorRamp)) return;
            Invalidate();
        }
        NotifyInvalidation();
    }

    private bool Change<T>(ref T field, T value) where T : IEquatable<T>
    {
        lock (_gate)
        {
            ThrowIfDisposed(); if (field.Equals(value)) return false;
            field = value; Invalidate();
        }
        NotifyInvalidation();
        return true;
    }

    private void Invalidate() { _dirty = true; _version++; }
    private static void Dimension(int value) { if (value is < 1 or > 16_384) throw new ArgumentOutOfRangeException(nameof(value)); }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<NoiseTexture, int>(nameof(Width), t => t.Width, (t, v) => t.Width = v, _ => 512, stored: true),
        new PropertyDescriptor<NoiseTexture, int>(nameof(Height), t => t.Height, (t, v) => t.Height = v, _ => 512, stored: true),
        new PropertyDescriptor<NoiseTexture, bool>(nameof(GenerateMipmaps), t => t.GenerateMipmaps, (t, v) => t.GenerateMipmaps = v, _ => true, stored: true),
        new PropertyDescriptor<NoiseTexture, Noise?>(nameof(Noise), t => t.Noise, (t, v) => t.Noise = v, _ => null, stored: true),
        new PropertyDescriptor<NoiseTexture, Gradient?>(nameof(ColorRamp), t => t.ColorRamp, (t, v) => t.ColorRamp = v, _ => null, stored: true),
        new PropertyDescriptor<NoiseTexture, bool>(nameof(Seamless), t => t.Seamless, (t, v) => t.Seamless = v, _ => false, stored: true),
        new PropertyDescriptor<NoiseTexture, bool>(nameof(Invert), t => t.Invert, (t, v) => t.Invert = v, _ => false, stored: true),
        new PropertyDescriptor<NoiseTexture, bool>(nameof(AsNormalMap), t => t.AsNormalMap, (t, v) => t.AsNormalMap = v, _ => false, stored: true),
        new PropertyDescriptor<NoiseTexture, bool>(nameof(Normalize), t => t.Normalize, (t, v) => t.Normalize = v, _ => true, stored: true),
        new PropertyDescriptor<NoiseTexture, float>(nameof(SeamlessBlendSkirt), t => t.SeamlessBlendSkirt, (t, v) => t.SeamlessBlendSkirt = v, _ => 0.1f, stored: true),
        new PropertyDescriptor<NoiseTexture, float>(nameof(BumpStrength), t => t.BumpStrength, (t, v) => t.BumpStrength = v, _ => 8f, stored: true),
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
            if (copy._noise is not null) { copy._noise.Changed -= copy.SourceChanged; copy._noise.Disposed -= copy.SourceDisposed; }
            if (copy._colorRamp is not null) { copy._colorRamp.Changed -= copy.SourceChanged; copy._colorRamp.Disposed -= copy.SourceDisposed; }
            copy._width = width; copy._height = height; copy._generateMipmaps = mipmaps; copy._normalize = normalize;
            copy._seamless = seamless; copy._invert = invert; copy._asNormalMap = normal;
            copy._seamlessBlendSkirt = skirt; copy._bumpStrength = strength;
            copy._noise = noise; copy._colorRamp = ramp; copy._pixels = copy._renderPixels = null; copy._firstGeneration = true;
            if (noise is not null) { noise.Changed += copy.SourceChanged; noise.Disposed += copy.SourceDisposed; }
            if (ramp is not null) { ramp.Changed += copy.SourceChanged; ramp.Disposed += copy.SourceDisposed; }
            copy.Invalidate();
        }
    }

    /// <inheritdoc />
    /// <exception cref="InvalidOperationException">A sampler disposes its generating texture, or a worker callback disposes unfinished generation.</exception>
    protected override void ValidateDisposal()
    {
        if (s_baking?.Contains(this) == true) throw new InvalidOperationException("A sampler cannot dispose its generating texture.");
        lock (_gate) if (_pending is { } work && !WorkerThreadPool.IsTaskCompleted(work.TaskID) && (WorkerThreadPool.GetCallerTaskID() != -1 || WorkerThreadPool.GetCallerGroupID() != -1)) throw new InvalidOperationException("Pending texture generation must be disposed outside worker callbacks.");
    }

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        try
        {
            if (disposing)
            {
                try { DrainGeneration(); }
                finally { lock (_gate) { if (_noise is not null) { _noise.Changed -= SourceChanged; _noise.Disposed -= SourceDisposed; } if (_colorRamp is not null) { _colorRamp.Changed -= SourceChanged; _colorRamp.Disposed -= SourceDisposed; } _noise = null; _colorRamp = null; _pixels = _renderPixels = null; Invalidate(); } }
            }
        }
        finally { base.Dispose(disposing); }
    }
}
