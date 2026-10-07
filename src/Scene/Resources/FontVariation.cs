namespace Electron2D;

/// <summary>Creates independent font instances with OpenType coordinates, synthetic outlines, palettes and spacing.</summary>
/// <remarks>The base font and fallbacks remain borrowed. Null BaseFont selects the theme fallback.
/// Numeric coordinates use four-byte OpenType tags; SetVariationOpenType accepts readable axis aliases.
/// Configuration/source changes retire native caches after active readers complete.</remarks>
public partial class FontVariation : Font
{
    private Font? _baseFont, _watchedBase;
    private FontData? _data;
    private Dictionary<uint, float> _coordinates = [];
    private Dictionary<string, int> _features = new(StringComparer.Ordinal);
    private NativeFontFeature[] _compiledFeatures = [];
    private readonly Dictionary<FontData, (NativeFontFeature[] Defaults, NativeFontFeature[] Values)> _shapingFeatures = [];
    private Color[] _colors = [];
    private readonly int[] _spacing = new int[(int)TextSpacingType.Max];
    private float _embolden, _baseline;
    private int _face, _palette;
    private Transform _transform = Transform.Identity;
    private bool _dirty = true, _themeSubscribed;
    private readonly Action _themeChanged;
    private readonly Action<Resource> _baseChanged;
    private readonly Action<ElectronObject> _baseDisposed;
    /// <summary>Creates an unmodified lazy instance of the borrowed theme fallback font.</summary>
    public FontVariation()
    {
        Changed += ClearShapingFeatures;
        var weak = new WeakReference<FontVariation>(this);
        Action? themeChanged = null; Action<Resource>? baseChanged = null; Action<ElectronObject>? baseDisposed = null;
        themeChanged = () => { if (weak.TryGetTarget(out var owner) && !owner.IsDisposed) owner.ThemeChanged(); else ThemeDB.FallbackChanged -= themeChanged; };
        baseChanged = resource => { if (weak.TryGetTarget(out var owner) && !owner.IsDisposed) owner.BaseChanged(resource); else resource.Changed -= baseChanged; };
        baseDisposed = resource => { if (weak.TryGetTarget(out var owner) && !owner.IsDisposed) owner.BaseChanged((Resource)resource); else resource.Disposed -= baseDisposed; };
        _themeChanged = themeChanged; _baseChanged = baseChanged; _baseDisposed = baseDisposed;
    }
    private void ClearShapingFeatures(Resource _) { lock (FontGate) _shapingFeatures.Clear(); }
    private void ThemeChanged() { lock (FontGate) { if (_baseFont != null || IsDisposed) return; RetireLocked(); } EmitChanged(); }
    private Font? ResolveBase()
    {
        lock (FontGate)
        {
            ThrowIfDisposed(); var font = _baseFont;
            if (font == null)
            {
                if (!_themeSubscribed) { ThemeDB.FallbackChanged += _themeChanged; _themeSubscribed = true; }
                font = ThemeDB.FallbackFont;
            }
            if (ReferenceEquals(font, this)) throw new InvalidOperationException("Cyclic theme fallback font.");
            if (!ReferenceEquals(font, _watchedBase))
            {
                Unwatch(); _watchedBase = font;
                if (font != null) { font.Changed += _baseChanged; font.Disposed += _baseDisposed; }
                RetireLocked();
            }
            return font;
        }
    }
    internal override Font? BaseDependency => _baseFont ?? _watchedBase;
    internal override Font?[] EffectiveFallbacks => LocalFallbacks.Length > 0 ? LocalFallbacks : ResolveBase()?.EffectiveFallbacks ?? [];
    internal override FontData? PrimaryData
    {
        get
        {
            lock (FontGate)
            {
                ThrowIfDisposed(); var source = ResolveBase();
                var cursor = source; for (var depth = 0; cursor is FontVariation variation; depth++) { if (depth >= 64 || ReferenceEquals(cursor, this)) throw new InvalidOperationException("Cyclic or excessive font base chain."); cursor = variation.ResolveBase(); }
                if (!_dirty) return _data;
                FontData? created = null;
                var instance = new FontInstance(_face, _coordinates, _embolden, _transform, _baseline, _palette, _colors);
                if (cursor is FontFile file) created = file.CreateVariationData(instance, _spacing);
                else if (cursor?.PrimaryData is { } baseData) created = baseData.CreateVariation(instance);
                _data = created; _dirty = false; return _data;
            }
        }
    }
    internal override NativeFontFeature[]? GetShapingFeatures(FontData source)
    {
        lock (FontGate)
        {
            ThrowIfDisposed(); if (_compiledFeatures.Length == 0) return null;
            var defaults = source.OpenTypeFeatures;
            if (_shapingFeatures.TryGetValue(source, out var cached) && ReferenceEquals(cached.Defaults, defaults)) return cached.Values;
            var merged = new Dictionary<uint, NativeFontFeature>();
            foreach (var feature in defaults) merged[feature.Tag] = feature;
            foreach (var feature in _compiledFeatures) merged[feature.Tag] = feature;
            var result = merged.Values.ToArray(); _shapingFeatures[source] = (defaults, result); return result;
        }
    }
    private void Unwatch() { if (_watchedBase != null) { _watchedBase.Changed -= _baseChanged; _watchedBase.Disposed -= _baseDisposed; } _watchedBase = null; }
    private void BaseChanged(Resource _) { if (IsDisposed) return; lock (FontGate) { if (IsDisposed) return; RetireLocked(); } EmitChanged(); }
    private void RetireLocked() { _shapingFeatures.Clear(); var old = _data; _data = null; _dirty = true; InvalidateFontStateLocked(); old?.Dispose(); }
    /// <summary>Gets or sets the borrowed base font; null uses the current theme fallback.</summary>
    /// <value>Null initially. The source remains borrowed; equal assignments are silent.</value>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    /// <exception cref="ArgumentException">The font graph would be cyclic or exceed sixty-four levels.</exception>
    public Font? BaseFont
    {
        get { lock (FontGate) { ThrowIfDisposed(); return _baseFont; } }
        set { lock (FallbackMutationGate) { if (value != null) ValidateFallback(value, this, 1); lock (FontGate) { ThrowIfDisposed(); if (ReferenceEquals(_baseFont, value)) return; Unwatch(); _baseFont = value; _watchedBase = value; if (value != null) { value.Changed += _baseChanged; value.Disposed += _baseDisposed; } RetireLocked(); } } EmitChanged(); NotifyPropertyListChanged(); }
    }
    /// <summary>Gets or replaces copied numeric OpenType design coordinates; missing axes use defaults and known values clamp to axis bounds.</summary>
    /// <value>Independent dictionary snapshot, initially empty; at most 65,536 finite coordinates.</value>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    /// <exception cref="ArgumentNullException">The dictionary is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A coordinate is nonfinite or the entry count exceeds the storage bound.</exception>
    public Dictionary<uint, float> VariationOpenType
    {
        get { lock (FontGate) { ThrowIfDisposed(); return new(_coordinates); } }
        set { ArgumentNullException.ThrowIfNull(value); if (value.Count > 65536) throw new ArgumentOutOfRangeException(nameof(value)); foreach (var pair in value) if (!float.IsFinite(pair.Value)) throw new ArgumentOutOfRangeException(nameof(value)); lock (FontGate) { ThrowIfDisposed(); if (_coordinates.Count == value.Count && value.All(pair => _coordinates.TryGetValue(pair.Key, out var old) && old == pair.Value)) return; _coordinates = new(value); RetireLocked(); } EmitChanged(); }
    }
    /// <summary>Replaces design coordinates supplied by registered readable axis aliases or custom four-byte tag names.</summary>
    /// <param name="coordinates">Nonnull finite design coordinates. Names use the FontFile OpenType alias rules.</param>
    /// <exception cref="ArgumentNullException">The dictionary is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Coordinates are nonfinite or exceed the storage bound.</exception>
    public void SetVariationOpenType(Dictionary<string, float> coordinates)
    {
        ArgumentNullException.ThrowIfNull(coordinates);
        if (coordinates.Count > 65536) throw new ArgumentOutOfRangeException(nameof(coordinates));
        var numeric = new Dictionary<uint, float>();
        foreach (var pair in coordinates) numeric[OpenTypeFeatureTags.Resolve(pair.Key)] = pair.Value;
        VariationOpenType = numeric;
    }
    /// <summary>Gets or replaces copied span OpenType feature settings; registered names and custom tags follow FontFile rules.</summary>
    /// <value>Independent dictionary snapshot, initially empty. Negative values are stored but omitted from shaping.</value>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    /// <exception cref="ArgumentNullException">The dictionary is null.</exception>
    public Dictionary<string, int> OpenTypeFeatures
    {
        get { lock (FontGate) { ThrowIfDisposed(); return new(_features, StringComparer.Ordinal); } }
        set { ArgumentNullException.ThrowIfNull(value); var copy = new Dictionary<string, int>(value, StringComparer.Ordinal); var compiled = FontFile.CompileFeatures(copy); lock (FontGate) { ThrowIfDisposed(); if (_features.Count == copy.Count && copy.All(pair => _features.TryGetValue(pair.Key, out var old) && old == pair.Value)) return; _features = copy; _compiledFeatures = compiled; RetireLocked(); } EmitChanged(); }
    }
    /// <inheritdoc />
    public override Dictionary<string, int> GetOpenTypeFeatures() => OpenTypeFeatures;
    /// <summary>Gets or sets finite synthetic outline strength; negative values thin outlines.</summary>
    /// <value>Zero initially; horizontal advances include strength times logical size divided by sixty-four.</value>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The configuration is invalid.</exception>
    public float VariationEmbolden { get { lock (FontGate) { ThrowIfDisposed(); return _embolden; } } set { Finite(value); lock (FontGate) { ThrowIfDisposed(); if (_embolden == value) return; _embolden = value; RetireLocked(); } EmitChanged(); } }
    /// <summary>Gets or sets the finite outline basis; its translation is retained but does not move the glyph baseline.</summary>
    /// <value>Identity initially. Basis entries are bounded to plus or minus 32,767.</value>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The configuration is invalid.</exception>
    public Transform VariationTransform { get { lock (FontGate) { ThrowIfDisposed(); return _transform; } } set { if (!value.IsFinite() || Math.Abs(value.X.X) > 32767 || Math.Abs(value.X.Y) > 32767 || Math.Abs(value.Y.X) > 32767 || Math.Abs(value.Y.Y) > 32767) throw new ArgumentOutOfRangeException(nameof(value)); lock (FontGate) { ThrowIfDisposed(); if (_transform == value) return; _transform = value; RetireLocked(); } EmitChanged(); } }
    /// <summary>Gets or sets the nonnegative collection face index used by the next native instance.</summary>
    /// <value>Zero initially. An unavailable index fails on realization and may be corrected for retry.</value>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The configuration is invalid.</exception>
    public int VariationFaceIndex { get { lock (FontGate) { ThrowIfDisposed(); return _face; } } set { ArgumentOutOfRangeException.ThrowIfNegative(value); lock (FontGate) { ThrowIfDisposed(); if (_face == value) return; _face = value; RetireLocked(); } EmitChanged(); } }
    /// <summary>Gets or sets the finite baseline shift as a fraction of native ascent plus descent.</summary>
    /// <value>Zero initially. Positive values shift horizontal shaped glyphs downward and vertical glyphs rightward.</value>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The configuration is invalid.</exception>
    public float BaselineOffset { get { lock (FontGate) { ThrowIfDisposed(); return _baseline; } } set { Finite(value); lock (FontGate) { ThrowIfDisposed(); if (_baseline == value) return; _baseline = value; RetireLocked(); } EmitChanged(); } }
    /// <summary>Gets or sets the selected predefined palette index; rendering clamps it to available palettes.</summary>
    /// <value>Zero initially; signed values remain stored and clamp only during native realization.</value>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    public int PaletteIndex { get { lock (FontGate) { ThrowIfDisposed(); return _palette; } } set { lock (FontGate) { ThrowIfDisposed(); if (_palette == value) return; _palette = value; RetireLocked(); } EmitChanged(); } }
    /// <summary>Gets or replaces copied palette overrides; transparent black retains the predefined entry.</summary>
    /// <value>Independent array snapshot, initially empty. Finite channels clamp to zero through one.</value>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    /// <exception cref="ArgumentNullException">The array is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A channel is nonfinite.</exception>
    public Color[] PaletteCustomColors
    {
        get { lock (FontGate) { ThrowIfDisposed(); return (Color[])_colors.Clone(); } }
        set { ArgumentNullException.ThrowIfNull(value); foreach (var c in value) { Finite(c.R); Finite(c.G); Finite(c.B); Finite(c.A); } lock (FontGate) { ThrowIfDisposed(); if (_colors.AsSpan().SequenceEqual(value)) return; _colors = (Color[])value.Clone(); RetireLocked(); } EmitChanged(); }
    }
    private static void Finite(float value) { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    /// <summary>Assigns signed pixel spacing in one valid text-spacing category.</summary><param name="spacing">Glyph, Space, Top or Bottom.</param><param name="value">Extra pixels.</param>
    /// <exception cref="ArgumentOutOfRangeException">The spacing category is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    public void SetSpacing(TextSpacingType spacing, int value) { if (spacing is < TextSpacingType.Glyph or >= TextSpacingType.Max) throw new ArgumentOutOfRangeException(nameof(spacing)); lock (FontGate) { ThrowIfDisposed(); if (_spacing[(int)spacing] == value) return; _spacing[(int)spacing] = value; InvalidateFontStateLocked(); } EmitChanged(); }
    /// <inheritdoc />
    public override int GetSpacing(TextSpacingType spacing) { if (spacing is < TextSpacingType.Glyph or >= TextSpacingType.Max) throw new ArgumentOutOfRangeException(nameof(spacing)); lock (FontGate) { ThrowIfDisposed(); return _spacing[(int)spacing]; } }
    /// <summary>Gets or sets extra pixels between graphical glyphs.</summary>
    /// <value>Zero initially. The terminal advancing glyph receives no extra spacing.</value>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    public int SpacingGlyph { get => GetSpacing(TextSpacingType.Glyph); set => SetSpacing(TextSpacingType.Glyph, value); }
    /// <summary>Gets or sets extra width of space glyphs.</summary>
    /// <value>Zero initially; zero uses glyph spacing. The terminal advancing glyph receives no extra spacing.</value>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    public int SpacingSpace { get => GetSpacing(TextSpacingType.Space); set => SetSpacing(TextSpacingType.Space, value); }
    /// <summary>Gets or sets extra line-top pixels.</summary>
    /// <value>Zero initially; signed pixel spacing.</value>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    public int SpacingTop { get => GetSpacing(TextSpacingType.Top); set => SetSpacing(TextSpacingType.Top, value); }
    /// <summary>Gets or sets extra line-bottom pixels.</summary>
    /// <value>Zero initially; signed pixel spacing.</value>
    /// <exception cref="ObjectDisposedException">This font is disposed.</exception>
    public int SpacingBottom { get => GetSpacing(TextSpacingType.Bottom); set => SetSpacing(TextSpacingType.Bottom, value); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => GetType() == typeof(FontVariation) ? new FontVariation() : base.CreateDuplicateInstance();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode, Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource); var copy = (FontVariation)target; copy.BaseFont = deep ? (Font?)duplicateSubresource(BaseFont) : BaseFont; copy.VariationOpenType = VariationOpenType; copy.OpenTypeFeatures = OpenTypeFeatures; copy.VariationEmbolden = VariationEmbolden; copy.VariationTransform = VariationTransform; copy.VariationFaceIndex = VariationFaceIndex; copy.BaselineOffset = BaselineOffset; copy.PaletteIndex = PaletteIndex; copy.PaletteCustomColors = PaletteCustomColors; foreach (var spacing in new[] { TextSpacingType.Glyph, TextSpacingType.Space, TextSpacingType.Top, TextSpacingType.Bottom }) copy.SetSpacing(spacing, GetSpacing(spacing));
    }
    /// <inheritdoc />
    protected override void OnResetState() { BaseFont = null; VariationOpenType = []; OpenTypeFeatures = new(StringComparer.Ordinal); VariationEmbolden = 0; VariationTransform = Transform.Identity; VariationFaceIndex = 0; BaselineOffset = 0; PaletteIndex = 0; PaletteCustomColors = []; Array.Clear(_spacing); base.OnResetState(); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var p in base.GetPropertyDescriptors()) yield return p;
        yield return new PropertyDescriptor<FontVariation, Font?>(nameof(BaseFont), f => f.BaseFont, (f, v) => f.BaseFont = v, _ => null, stored: true);
        yield return new PropertyDescriptor<FontVariation, Dictionary<uint, float>>(nameof(VariationOpenType), f => f.VariationOpenType, (f, v) => f.VariationOpenType = v, _ => [], stored: true);
        yield return new PropertyDescriptor<FontVariation, Dictionary<string, int>>(nameof(OpenTypeFeatures), f => f.OpenTypeFeatures, (f, v) => f.OpenTypeFeatures = v, _ => new(StringComparer.Ordinal), stored: true);
        yield return new PropertyDescriptor<FontVariation, float>(nameof(VariationEmbolden), f => f.VariationEmbolden, (f, v) => f.VariationEmbolden = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<FontVariation, Transform>(nameof(VariationTransform), f => f.VariationTransform, (f, v) => f.VariationTransform = v, _ => Transform.Identity, stored: true);
        yield return new PropertyDescriptor<FontVariation, int>(nameof(VariationFaceIndex), f => f.VariationFaceIndex, (f, v) => f.VariationFaceIndex = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<FontVariation, float>(nameof(BaselineOffset), f => f.BaselineOffset, (f, v) => f.BaselineOffset = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<FontVariation, int>(nameof(PaletteIndex), f => f.PaletteIndex, (f, v) => f.PaletteIndex = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<FontVariation, Color[]>(nameof(PaletteCustomColors), f => f.PaletteCustomColors, (f, v) => f.PaletteCustomColors = v, _ => [], stored: true);
        yield return new PropertyDescriptor<FontVariation, int>(nameof(SpacingGlyph), f => f.SpacingGlyph, (f, v) => f.SpacingGlyph = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<FontVariation, int>(nameof(SpacingSpace), f => f.SpacingSpace, (f, v) => f.SpacingSpace = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<FontVariation, int>(nameof(SpacingTop), f => f.SpacingTop, (f, v) => f.SpacingTop = v, _ => 0, stored: true);
        yield return new PropertyDescriptor<FontVariation, int>(nameof(SpacingBottom), f => f.SpacingBottom, (f, v) => f.SpacingBottom = v, _ => 0, stored: true);
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        FontData? data;
        lock (FontGate) { Unwatch(); if (_themeSubscribed) ThemeDB.FallbackChanged -= _themeChanged; _themeSubscribed = false; data = _data; _data = null; _baseFont = null; }
        try { data?.Dispose(); } finally { base.Dispose(disposing); }
    }
}
