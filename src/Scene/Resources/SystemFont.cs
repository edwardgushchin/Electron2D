namespace Electron2D;

/// <summary>Resolves installed font families and styles into ordinary owned native text sources.</summary>
/// <remarks>Unresolved names use the borrowed theme fallback. Matching and source loading are cold operations;
/// prepared shaping/raster caches use the same Font and canvas paths. Runtime source paths are not stored in resource archives.</remarks>
public class SystemFont : Font
{
    private string[] _names = [];
    private int _weight = 400, _stretch = 100;
    private bool _italic, _allowSystemFallback = true, _remainders = true, _modulate;
    private FontHinting _hinting = FontHinting.Light;
    private FontSubpixelPositioning _subpixel = FontSubpixelPositioning.Auto;
    private float _oversampling;
    private FontData? _data;
    private int[] _faces = [];
    private Font? _theme;
    private bool _themeSubscribed, _usesTheme;
    private readonly Action _themeChanged;
    private readonly Action<Resource> _sourceChanged;
    private readonly Action<ElectronObject> _sourceDisposed;
    /// <summary>Creates an unresolved font with default weight/width and native text policies.</summary>
    public SystemFont()
    {
        var weak = new WeakReference<SystemFont>(this);
        Action? theme = null; Action<Resource>? changed = null; Action<ElectronObject>? disposed = null;
        theme = () => { if (weak.TryGetTarget(out var owner) && !owner.IsDisposed) owner.OnThemeChanged(); else ThemeDB.FallbackChanged -= theme; };
        changed = value => { if (weak.TryGetTarget(out var owner) && !owner.IsDisposed) owner.OnSourceChanged(); else value.Changed -= changed; };
        disposed = value => { if (weak.TryGetTarget(out var owner) && !owner.IsDisposed) owner.OnSourceChanged(); else value.Disposed -= disposed; };
        _themeChanged = theme; _sourceChanged = changed; _sourceDisposed = disposed;
    }
    internal override Font? BaseDependency => _theme;
    internal override Font?[] EffectiveFallbacks => LocalFallbacks.Length > 0 ? LocalFallbacks : _theme?.EffectiveFallbacks ?? [];
    internal override FontData? PrimaryData { get { lock (FontGate) { ThrowIfDisposed(); if (_data == null) ResolveLocked(); return _data; } } }
    private void OnThemeChanged() { lock (FontGate) { if (!_usesTheme) return; RetireLocked(); } EmitChanged(); }
    private void OnSourceChanged() { lock (FontGate) { if (IsDisposed) return; RetireLocked(); } EmitChanged(); }
    private void RetireLocked() { var old = _data; _data = null; _faces = []; InvalidateFontStateLocked(); old?.Dispose(); }
    private void Unwatch()
    {
        if (_theme != null) { _theme.Changed -= _sourceChanged; _theme.Disposed -= _sourceDisposed; _theme = null; }
        if (_themeSubscribed) { ThemeDB.FallbackChanged -= _themeChanged; _themeSubscribed = false; }
    }
    private void ResolveLocked()
    {
        Unwatch(); _usesTheme = false;
        foreach (var family in _names)
        {
            if (family.Length == 0) continue;
            var matches = OS.FontCatalog.Match(family, "", "", _weight, _stretch, _italic);
            foreach (var match in matches)
            {
                FontData? source = null;
                try
                {
                    source = FontData.LoadSystemSource(match); var bytes = source.SourceBytes; var best = 0; var faces = new List<int>();
                    for (var face = 0; face < source.FaceCount; face++)
                    {
                        using var candidate = new FontData(bytes, face);
                        var score = (candidate.FamilyName.Equals(family, StringComparison.OrdinalIgnoreCase) ? 80 : 0) + 20 - Math.Abs(candidate.FontWeight - _weight) / 50 + 20 - Math.Abs(candidate.FontStretch - _stretch) / 10 + (((candidate.FontStyle & FontStyle.Italic) != 0) == _italic ? 30 : 0);
                        if (score > best) faces.Clear(); if (score >= best) { best = score; faces.Add(face); }
                    }
                    if (faces.Count == 0) faces.Add(0);
                    var coordinates = new Dictionary<uint, float>();
                    if (best != 150)
                    {
                        if (source.VariationAxes.ContainsKey(0x77676874)) coordinates[0x77676874] = _weight;
                        if (source.VariationAxes.ContainsKey(0x77647468)) coordinates[0x77647468] = _stretch;
                        if (_italic && source.VariationAxes.ContainsKey(0x6974616c)) coordinates[0x6974616c] = 1;
                    }
                    var data = new FontData(bytes, faces[0], new(faces[0], coordinates, 0, Transform.Identity, 0, 0, []));
                    _faces = faces.ToArray(); data.SystemFaces = _faces; data.AdvertisedFaceCount = _faces.Length; Configure(data); _data = data; return;
                }
                catch (Exception error) when (error is IOException or ArgumentException or InvalidOperationException or NotSupportedException) { }
                finally { source?.Dispose(); }
            }
        }
        _usesTheme = true; ThemeDB.FallbackChanged += _themeChanged; _themeSubscribed = true;
        var theme = ThemeDB.FallbackFont;
        if (ReferenceEquals(theme, this)) throw new InvalidOperationException("Cyclic system font theme fallback.");
        if (theme != null)
        {
            ValidateFallback(theme, this, 1); _theme = theme; theme.Changed += _sourceChanged; theme.Disposed += _sourceDisposed;
            if (theme.PrimaryData is { } data) { _data = data.CreateVariation(new(0, [], 0, Transform.Identity, 0, 0, [])); Configure(_data); _data.AdvertisedFaceCount = 0; }
        }
    }
    private void Configure(FontData data) { data.Hinting = _hinting; data.SubpixelPositioning = _subpixel; data.KeepRoundingRemainders = _remainders; data.ModulateColorGlyphs = _modulate; data.Oversampling = _oversampling; data.AllowSystemFallback = _allowSystemFallback; }
    private void Set<T>(ref T field, T value)
    {
        lock (FontGate) { ThrowIfDisposed(); if (EqualityComparer<T>.Default.Equals(field, value)) return; field = value; RetireLocked(); }
        EmitChanged();
    }
    /// <summary>Gets or replaces ordered preferred family names.</summary>
    /// <value>Independent copied array, initially empty. Empty entries are skipped; unavailable names follow platform matching/default-theme behavior.</value>
    /// <exception cref="ArgumentNullException">The array or a name is null.</exception><exception cref="ArgumentException">A name contains NUL or exceeds its budget.</exception><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public string[] FontNames
    {
        get { lock (FontGate) { ThrowIfDisposed(); return (string[])_names.Clone(); } }
        set { ArgumentNullException.ThrowIfNull(value); if (value.Length > 1024) throw new ArgumentOutOfRangeException(nameof(value)); var copy = (string[])value.Clone(); foreach (var name in copy) { ArgumentNullException.ThrowIfNull(name); if (name.Length > 65536 || name.Contains('\0')) throw new ArgumentException("Invalid font family name.", nameof(value)); } lock (FontGate) { ThrowIfDisposed(); if (_names.AsSpan().SequenceEqual(copy)) return; _names = copy; RetireLocked(); } EmitChanged(); }
    }
    /// <summary>Gets or sets whether italic/oblique is preferred.</summary><value>False initially; matching may substitute the nearest style.</value><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool FontItalic { get { lock (FontGate) { ThrowIfDisposed(); return _italic; } } set => Set(ref _italic, value); }
    /// <summary>Gets or sets preferred font weight.</summary><value>400 initially; assignments clamp to 100 through 999.</value><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int FontWeight { get { lock (FontGate) { ThrowIfDisposed(); return _weight; } } set => Set(ref _weight, Math.Clamp(value, 100, 999)); }
    /// <summary>Gets or sets preferred width percentage.</summary><value>100 initially; assignments clamp to 50 through 200.</value><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public int FontStretch { get { lock (FontGate) { ThrowIfDisposed(); return _stretch; } } set => Set(ref _stretch, Math.Clamp(value, 50, 200)); }
    /// <summary>Gets or sets automatic platform fallback for missing text glyphs.</summary><value>True initially; explicit fallback resources retain precedence.</value><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool AllowSystemFallback { get { lock (FontGate) { ThrowIfDisposed(); return _allowSystemFallback; } } set => Set(ref _allowSystemFallback, value); }
    /// <summary>Gets or sets native glyph hinting independent of shaping metrics.</summary><value>Light initially; unknown values use normal hinting.</value><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public FontHinting Hinting { get { lock (FontGate) { ThrowIfDisposed(); return _hinting; } } set => Set(ref _hinting, value); }
    /// <summary>Gets or sets horizontal subpixel positioning.</summary><value>Auto initially; unknown values use whole pixels.</value><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public FontSubpixelPositioning SubpixelPositioning { get { lock (FontGate) { ThrowIfDisposed(); return _subpixel; } } set => Set(ref _subpixel, value); }
    /// <summary>Gets or sets whole-pixel advance remainder preservation.</summary><value>True initially; subpixel positions retain native fractional metrics.</value><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool KeepRoundingRemainders { get { lock (FontGate) { ThrowIfDisposed(); return _remainders; } } set => Set(ref _remainders, value); }
    /// <summary>Gets or sets RGB modulation of intrinsic color glyphs.</summary><value>False initially; alpha modulation always applies.</value><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public bool ModulateColorGlyphs { get { lock (FontGate) { ThrowIfDisposed(); return _modulate; } } set => Set(ref _modulate, value); }
    /// <summary>Gets or sets the finite raster oversampling override.</summary><value>Zero initially; explicit positive draw factors take precedence.</value><exception cref="ArgumentOutOfRangeException">The value is nonfinite.</exception><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public float Oversampling { get { lock (FontGate) { ThrowIfDisposed(); return _oversampling; } } set { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); Set(ref _oversampling, value); } }
    /// <summary>Returns the number of equally matched logical collection faces.</summary><returns>Selected-face count; zero when using the theme fallback.</returns><exception cref="ObjectDisposedException">The font is disposed.</exception>
    public override int GetFaceCount() { _ = PrimaryData; lock (FontGate) return _faces.Length; }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => GetType() == typeof(SystemFont) ? new SystemFont() : base.CreateDuplicateInstance();
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        foreach (var property in base.GetPropertyDescriptors()) yield return property;
        yield return new PropertyDescriptor<SystemFont, string[]>(nameof(FontNames), f => f.FontNames, (f, v) => f.FontNames = v, _ => [], stored: true);
        yield return new PropertyDescriptor<SystemFont, bool>(nameof(FontItalic), f => f.FontItalic, (f, v) => f.FontItalic = v, _ => false, stored: true);
        yield return new PropertyDescriptor<SystemFont, int>(nameof(FontWeight), f => f.FontWeight, (f, v) => f.FontWeight = v, _ => 400, stored: true);
        yield return new PropertyDescriptor<SystemFont, int>(nameof(FontStretch), f => f.FontStretch, (f, v) => f.FontStretch = v, _ => 100, stored: true);
        yield return new PropertyDescriptor<SystemFont, bool>(nameof(AllowSystemFallback), f => f.AllowSystemFallback, (f, v) => f.AllowSystemFallback = v, _ => true, stored: true);
        yield return new PropertyDescriptor<SystemFont, FontHinting>(nameof(Hinting), f => f.Hinting, (f, v) => f.Hinting = v, _ => FontHinting.Light, stored: true);
        yield return new PropertyDescriptor<SystemFont, FontSubpixelPositioning>(nameof(SubpixelPositioning), f => f.SubpixelPositioning, (f, v) => f.SubpixelPositioning = v, _ => FontSubpixelPositioning.Auto, stored: true);
        yield return new PropertyDescriptor<SystemFont, bool>(nameof(KeepRoundingRemainders), f => f.KeepRoundingRemainders, (f, v) => f.KeepRoundingRemainders = v, _ => true, stored: true);
        yield return new PropertyDescriptor<SystemFont, bool>(nameof(ModulateColorGlyphs), f => f.ModulateColorGlyphs, (f, v) => f.ModulateColorGlyphs = v, _ => false, stored: true);
        yield return new PropertyDescriptor<SystemFont, float>(nameof(Oversampling), f => f.Oversampling, (f, v) => f.Oversampling = v, _ => 0, stored: true);
    }
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var copy = (SystemFont)target;
        copy.FontNames = FontNames; copy.FontItalic = FontItalic; copy.FontWeight = FontWeight; copy.FontStretch = FontStretch;
        copy.AllowSystemFallback = AllowSystemFallback; copy.Hinting = Hinting; copy.SubpixelPositioning = SubpixelPositioning;
        copy.KeepRoundingRemainders = KeepRoundingRemainders; copy.ModulateColorGlyphs = ModulateColorGlyphs; copy.Oversampling = Oversampling;
        base.CopyCustomStateTo(target, deep, subresourceMode, duplicateSubresource, forceDuplicateSubresource);
    }
    /// <inheritdoc />
    protected override void OnResetState()
    {
        FontNames = []; FontItalic = false; FontWeight = 400; FontStretch = 100; AllowSystemFallback = true; Hinting = FontHinting.Light; SubpixelPositioning = FontSubpixelPositioning.Disabled; KeepRoundingRemainders = true; ModulateColorGlyphs = false; Oversampling = 0; base.OnResetState();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        FontData? data; lock (FontGate) { Unwatch(); data = _data; _data = null; _faces = []; _names = []; }
        try { data?.Dispose(); } finally { base.Dispose(disposing); }
    }
}
