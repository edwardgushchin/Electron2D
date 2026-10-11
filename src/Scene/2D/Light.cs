namespace Electron2D;

/// <summary>Selects how illumination combines with the accumulated canvas color.</summary>
public enum LightBlendMode
{
    /// <summary>Adds illumination.</summary>
    Add = 0,
    /// <summary>Subtracts illumination.</summary>
    Sub = 1,
    /// <summary>Interpolates by the light mask alpha.</summary>
    Mix = 2,
}

/// <summary>Selects percentage-closer shadow sampling.</summary>
public enum LightShadowFilter
{
    /// <summary>Uses one sample.</summary>
    None = 0,
    /// <summary>Uses five samples.</summary>
    PCF5 = 1,
    /// <summary>Uses thirteen samples.</summary>
    PCF13 = 2,
    /// <summary>Nonselectable bound.</summary>
    Max = 3,
}

/// <summary>Defines shared canvas illumination and shadow selection.</summary>
/// <remarks>Rendering reads visible enabled lights each frame. Reversed layer or Z ranges affect no items.
/// Queries and mutations follow the attached scene's owner-thread contract.</remarks>
public abstract class Light : Entity
{
    private float _height;
    private LightBlendMode _blendMode = LightBlendMode.Add;
    private Color _color = Colors.White;
    private bool _enabled = true;
    private float _energy = 1;
    private uint _rangeItemCullMask = 1u;
    private int _rangeLayerMin = 0;
    private int _rangeLayerMax = 0;
    private int _rangeZMin = -1024;
    private int _rangeZMax = 1024;
    private Color _shadowColor = default;
    private bool _shadowEnabled = false;
    private LightShadowFilter _shadowFilter = LightShadowFilter.None;
    private float _shadowFilterSmooth = 0;
    private uint _shadowItemCullMask = 1u;

    /// <summary>Initializes the common light defaults.</summary>
    protected Light() { }

    /// <summary>Selects additive, subtractive or interpolated light contribution.</summary>
    /// <value>LightBlendMode.Add initially; changes apply on the next rendered frame.</value>
    public LightBlendMode BlendMode
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _blendMode; }
        set { EnsureMutable(); if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value)); _blendMode = value; }
    }

    /// <summary>Gets or sets the finite light color.</summary>
    /// <value>Colors.White initially; changes apply on the next rendered frame.</value>
    public Color Color
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _color; }
        set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _color = value; }
    }

    /// <summary>Gets or sets whether this visible light emits.</summary>
    /// <value>true initially; changes apply on the next rendered frame.</value>
    public bool Enabled
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _enabled; }
        set { EnsureMutable(); _enabled = value; }
    }

    /// <summary>Gets or sets the finite energy multiplier.</summary>
    /// <value>1 initially; changes apply on the next rendered frame.</value>
    public float Energy
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _energy; }
        set { EnsureMutable(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); _energy = value; }
    }

    /// <summary>Gets or sets receiving item membership; directional lights ignore this mask.</summary>
    /// <value>1u initially; changes apply on the next rendered frame.</value>
    public uint RangeItemCullMask
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _rangeItemCullMask; }
        set { EnsureMutable(); _rangeItemCullMask = value; }
    }

    /// <summary>Gets or sets the inclusive minimum receiving canvas layer.</summary>
    /// <value>0 initially; changes apply on the next rendered frame.</value>
    public int RangeLayerMin
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _rangeLayerMin; }
        set { EnsureMutable(); _rangeLayerMin = value; }
    }

    /// <summary>Gets or sets the inclusive maximum receiving canvas layer.</summary>
    /// <value>0 initially; changes apply on the next rendered frame.</value>
    public int RangeLayerMax
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _rangeLayerMax; }
        set { EnsureMutable(); _rangeLayerMax = value; }
    }

    /// <summary>Gets or sets the inclusive minimum receiving item Z.</summary>
    /// <value>-1024 initially; changes apply on the next rendered frame.</value>
    public int RangeZMin
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _rangeZMin; }
        set { EnsureMutable(); _rangeZMin = value; }
    }

    /// <summary>Gets or sets the inclusive maximum receiving item Z.</summary>
    /// <value>1024 initially; changes apply on the next rendered frame.</value>
    public int RangeZMax
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _rangeZMax; }
        set { EnsureMutable(); _rangeZMax = value; }
    }

    /// <summary>Gets or sets the finite color mixed into occluded light.</summary>
    /// <value>default initially; changes apply on the next rendered frame.</value>
    public Color ShadowColor
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _shadowColor; }
        set { EnsureMutable(); if (!value.IsFinite()) throw new ArgumentOutOfRangeException(nameof(value)); _shadowColor = value; }
    }

    /// <summary>Gets or sets whether matching contours cast shadows.</summary>
    /// <value>false initially; changes apply on the next rendered frame.</value>
    public bool ShadowEnabled
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _shadowEnabled; }
        set { EnsureMutable(); _shadowEnabled = value; }
    }

    /// <summary>Gets or sets the shadow sampling pattern.</summary>
    /// <value>LightShadowFilter.None initially; changes apply on the next rendered frame.</value>
    public LightShadowFilter ShadowFilter
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _shadowFilter; }
        set { EnsureMutable(); if (value is < LightShadowFilter.None or >= LightShadowFilter.Max) throw new ArgumentOutOfRangeException(nameof(value)); _shadowFilter = value; }
    }

    /// <summary>Gets or sets the finite shadow sample spacing.</summary>
    /// <value>0 initially; changes apply on the next rendered frame.</value>
    public float ShadowFilterSmooth
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _shadowFilterSmooth; }
        set { EnsureMutable(); if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); _shadowFilterSmooth = value; }
    }

    /// <summary>Gets or sets membership of contours casting shadows.</summary>
    /// <value>1u initially; changes apply on the next rendered frame.</value>
    public uint ShadowItemCullMask
    {
        get { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _shadowItemCullMask; }
        set { EnsureMutable(); _shadowItemCullMask = value; }
    }

    /// <summary>Returns the height used for normal and specular lighting.</summary>
    /// <returns>The finite authored height, zero initially.</returns>
    public float GetHeight() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); return _height; }

    /// <summary>Sets the height used for normal and specular lighting.</summary>
    /// <param name="height">Finite height in scene units for point lights, normalized tilt for directional lights.</param>
    /// <exception cref="ArgumentOutOfRangeException">Height is not finite.</exception>
    public void SetHeight(float height) { EnsureMutable(); if (!float.IsFinite(height)) throw new ArgumentOutOfRangeException(nameof(height)); _height = height; }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(LightProperties);

    private static readonly PropertyDescriptor[] LightProperties =
    [
        new PropertyDescriptor<Light, LightBlendMode>(nameof(BlendMode), n => n.BlendMode, (n, v) => n.BlendMode = v, _ => LightBlendMode.Add, stored: true),
        new PropertyDescriptor<Light, Color>(nameof(Color), n => n.Color, (n, v) => n.Color = v, _ => Colors.White, stored: true),
        new PropertyDescriptor<Light, bool>(nameof(Enabled), n => n.Enabled, (n, v) => n.Enabled = v, _ => true, stored: true),
        new PropertyDescriptor<Light, float>(nameof(Energy), n => n.Energy, (n, v) => n.Energy = v, _ => 1, stored: true),
        new PropertyDescriptor<Light, uint>(nameof(RangeItemCullMask), n => n.RangeItemCullMask, (n, v) => n.RangeItemCullMask = v, _ => 1u, stored: true),
        new PropertyDescriptor<Light, int>(nameof(RangeLayerMin), n => n.RangeLayerMin, (n, v) => n.RangeLayerMin = v, _ => 0, stored: true),
        new PropertyDescriptor<Light, int>(nameof(RangeLayerMax), n => n.RangeLayerMax, (n, v) => n.RangeLayerMax = v, _ => 0, stored: true),
        new PropertyDescriptor<Light, int>(nameof(RangeZMin), n => n.RangeZMin, (n, v) => n.RangeZMin = v, _ => -1024, stored: true),
        new PropertyDescriptor<Light, int>(nameof(RangeZMax), n => n.RangeZMax, (n, v) => n.RangeZMax = v, _ => 1024, stored: true),
        new PropertyDescriptor<Light, Color>(nameof(ShadowColor), n => n.ShadowColor, (n, v) => n.ShadowColor = v, _ => default, stored: true),
        new PropertyDescriptor<Light, bool>(nameof(ShadowEnabled), n => n.ShadowEnabled, (n, v) => n.ShadowEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<Light, LightShadowFilter>(nameof(ShadowFilter), n => n.ShadowFilter, (n, v) => n.ShadowFilter = v, _ => LightShadowFilter.None, stored: true),
        new PropertyDescriptor<Light, float>(nameof(ShadowFilterSmooth), n => n.ShadowFilterSmooth, (n, v) => n.ShadowFilterSmooth = v, _ => 0, stored: true),
        new PropertyDescriptor<Light, uint>(nameof(ShadowItemCullMask), n => n.ShadowItemCullMask, (n, v) => n.ShadowItemCullMask = v, _ => 1u, stored: true),
    ];
}
