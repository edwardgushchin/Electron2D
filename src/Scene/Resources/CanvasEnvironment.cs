using System.Runtime.CompilerServices;

namespace Electron2D;

/// <summary>Stores shared GPU canvas glow, tone mapping and color adjustment settings.</summary>
/// <remarks>Assign this resource to a <see cref="WorldEnvironment"/>. Changes affect the next canvas frame.
/// Resources and textures are borrowed by the scene. The effects work in linear light on GPU viewports with byte or half-float storage.</remarks>
public sealed partial class CanvasEnvironment : Resource
{
    /// <summary>Combines the blurred glow and the original canvas color.</summary>
    public enum GlowBlendMode
    {
        /// <summary>Adds glow to the original color.</summary>
        Additive = 0,
        /// <summary>Uses screen blending.</summary>
        Screen = 1,
        /// <summary>Uses soft-light blending.</summary>
        SoftLight = 2,
        /// <summary>Displays the glow alone.</summary>
        Replace = 3,
        /// <summary>Interpolates between the canvas and glow.</summary>
        Mix = 4
    }

    private readonly object _gate = new();
    private GlowSettings _glow = GlowSettings.Default;
    private RID _rid;

    /// <summary>Creates a canvas environment with glow disabled.</summary>
    public CanvasEnvironment() { }

    /// <summary>Enables bright-pixel glow on the GPU canvas.</summary>
    /// <value>false initially.</value>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public bool GlowEnabled
    {
        get => Read(ref _glow.Enabled);
        set
        {
            Set(ref _glow.Enabled, value);
        }
    }

    /// <summary>Controls how much below-threshold color contributes.</summary>
    /// <value>0 initially.</value>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported range.</exception>
    public float GlowBloom
    {
        get => Read(ref _glow.Bloom);
        set
        {
            ValidateGlow(value);
            Set(ref _glow.Bloom, value);
        }
    }

    /// <summary>Caps the extracted brightness per channel.</summary>
    /// <value>12 initially.</value>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported range.</exception>
    public float GlowHDRLuminanceCap
    {
        get => Read(ref _glow.LuminanceCap);
        set
        {
            ValidateGlow(value);
            Set(ref _glow.LuminanceCap, value);
        }
    }

    /// <summary>Controls the width of the transition above the HDR threshold.</summary>
    /// <value>2 initially.</value>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported range.</exception>
    public float GlowHDRScale
    {
        get => Read(ref _glow.HDRScale);
        set
        {
            ValidateGlow(value);
            Set(ref _glow.HDRScale, value);
        }
    }

    /// <summary>Sets the linear brightness at which glow extraction starts.</summary>
    /// <value>1 initially.</value>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported range.</exception>
    public float GlowHDRThreshold
    {
        get => Read(ref _glow.Threshold);
        set
        {
            ValidateGlow(value);
            Set(ref _glow.Threshold, value);
        }
    }

    /// <summary>Scales the composite glow in blend modes other than Mix.</summary>
    /// <value>0.3 initially.</value>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported range.</exception>
    public float GlowIntensity
    {
        get => Read(ref _glow.Intensity);
        set
        {
            ValidateGlow(value);
            Set(ref _glow.Intensity, value);
        }
    }

    /// <summary>Controls how strongly the glow map modulates RGB.</summary>
    /// <value>0.8 initially.</value>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported range.</exception>
    public float GlowMapStrength
    {
        get => Read(ref _glow.MapStrength);
        set
        {
            ValidateGlow(value, 1);
            Set(ref _glow.MapStrength, value);
        }
    }

    /// <summary>Controls the interpolation between the original canvas and glow in Mix mode.</summary>
    /// <value>0.05 initially.</value>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported range.</exception>
    public float GlowMix
    {
        get => Read(ref _glow.Mix);
        set
        {
            ValidateGlow(value, 1);
            Set(ref _glow.Mix, value);
        }
    }

    /// <summary>Normalizes the seven level weights to sum to one when their sum is positive.</summary>
    /// <value>false initially.</value>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public bool GlowNormalized
    {
        get => Read(ref _glow.Normalized);
        set
        {
            Set(ref _glow.Normalized, value);
        }
    }

    /// <summary>Controls the strength of the glow blur.</summary>
    /// <value>1 initially.</value>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported range.</exception>
    public float GlowStrength
    {
        get => Read(ref _glow.Strength);
        set
        {
            ValidateGlow(value);
            Set(ref _glow.Strength, value);
        }
    }

    /// <summary>Selects how the glow is combined with the original canvas.</summary>
    /// <value>Screen initially.</value>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is outside the supported range.</exception>
    public GlowBlendMode GlowBlend
    {
        get => Read(ref _glow.Blend);
        set
        {
            if ((uint)value > (uint)GlowBlendMode.Mix) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _glow.Blend, value);
        }
    }

    /// <summary>Gets or sets the borrowed RGB map stretched over the output.</summary>
    /// <value>Null initially.</value>
    /// <exception cref="ObjectDisposedException">This resource or the supplied texture is disposed.</exception>
    public Texture? GlowMap
    {
        get => Read(ref _glow.Map);
        set
        {
            ObjectDisposedException.ThrowIf(value is { IsDisposed: true }, value!);
            Set(ref _glow.Map, value);
        }
    }

    /// <summary>Reads a zero-based glow level's authored weight.</summary>
    /// <param name="index">Zero through six, from the narrowest to the widest blur.</param>
    /// <returns>The weight before normalization.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the seven levels.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public float GetGlowLevel(int index)
    {
        ValidateIndex(index);
        lock (_gate) { ThrowIfDisposed(); return _glow.Levels[index]; }
    }

    /// <summary>Sets a zero-based glow level's authored weight.</summary>
    /// <param name="index">Zero through six, from the narrowest to the widest blur.</param>
    /// <param name="intensity">A finite, nonnegative weight.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index or weight is invalid.</exception>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public void SetGlowLevel(int index, float intensity)
    {
        ValidateIndex(index);
        ValidateGlow(intensity);
        lock (_gate)
        {
            ThrowIfDisposed();
            if (_glow.Levels[index] == intensity) return;
            _glow.Levels[index] = intensity;
        }
        EmitChanged();
    }

    /// <summary>Gets the stable borrowed identity of this environment.</summary>
    /// <returns>A live environment identity, independent of renderer startup.</returns>
    /// <exception cref="ObjectDisposedException">This resource is disposed.</exception>
    public override RID GetRID()
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            return _rid.IsValid() ? _rid : _rid = RenderingProgramRegistry<CanvasEnvironment>.Register(this);
        }
    }

    internal CanvasPostProcess SnapshotPostProcess()
    {
        lock (_gate) return IsDisposed ? default : new(_glow, _tonemap, _adjustment);
    }

    private T Read<T>(ref T value)
    {
        lock (_gate) { ThrowIfDisposed(); return value; }
    }

    private void Set<T>(ref T field, T value)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            if (EqualityComparer<T>.Default.Equals(field, value)) return;
            field = value;
        }
        EmitChanged();
    }

    internal void SetServerGlow(GlowSettings settings)
    {
        lock (_gate)
        {
            ThrowIfDisposed();
            settings.Normalized = false;
            _glow = settings;
        }
        EmitChanged();
    }

    internal static void ValidateGlow(float value, float maximum = 65504)
    {
        if (!(value >= 0 && value <= maximum)) throw new ArgumentOutOfRangeException(nameof(value));
    }

    private static void ValidateIndex(int index)
    {
        if ((uint)index >= GlowSettings.LevelCount) throw new ArgumentOutOfRangeException(nameof(index));
    }

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new CanvasEnvironment();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        var settings = SnapshotPostProcess();
        var glow = settings.Glow;
        var adjustment = settings.Adjustment;
        if (deep)
        {
            glow.Map = (Texture?)duplicateSubresource(glow.Map);
            adjustment.Lookup = (Texture?)duplicateSubresource(adjustment.Lookup);
        }
        var copy = (CanvasEnvironment)target;
        lock (copy._gate)
        {
            copy._glow = glow;
            copy._adjustment = adjustment;
            copy._tonemap = settings.Tonemap.GetValueOrDefault(TonemapSettings.Default);
        }
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties).Concat(LevelProperties).Concat(TonemapProperties).Concat(AdjustmentProperties);

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!disposing) { base.Dispose(disposing); return; }
        try { base.Dispose(disposing); }
        finally
        {
            lock (_gate)
            {
                if (_rid.IsValid()) RenderingProgramRegistry<CanvasEnvironment>.Remove(_rid);
                _rid = default;
                _glow.Map = null;
                _adjustment.Lookup = null;
            }
            RenderingServer.NotifyRenderMutation();
        }
    }

    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<CanvasEnvironment, bool>(nameof(GlowEnabled), n => n.GlowEnabled, (n, v) => n.GlowEnabled = v, _ => false, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(GlowBloom), n => n.GlowBloom, (n, v) => n.GlowBloom = v, _ => 0f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(GlowHDRLuminanceCap), n => n.GlowHDRLuminanceCap, (n, v) => n.GlowHDRLuminanceCap = v, _ => 12f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(GlowHDRScale), n => n.GlowHDRScale, (n, v) => n.GlowHDRScale = v, _ => 2f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(GlowHDRThreshold), n => n.GlowHDRThreshold, (n, v) => n.GlowHDRThreshold = v, _ => 1f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(GlowIntensity), n => n.GlowIntensity, (n, v) => n.GlowIntensity = v, _ => .3f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(GlowMapStrength), n => n.GlowMapStrength, (n, v) => n.GlowMapStrength = v, _ => .8f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(GlowMix), n => n.GlowMix, (n, v) => n.GlowMix = v, _ => .05f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, bool>(nameof(GlowNormalized), n => n.GlowNormalized, (n, v) => n.GlowNormalized = v, _ => false, stored: true),
        new PropertyDescriptor<CanvasEnvironment, float>(nameof(GlowStrength), n => n.GlowStrength, (n, v) => n.GlowStrength = v, _ => 1f, stored: true),
        new PropertyDescriptor<CanvasEnvironment, GlowBlendMode>(nameof(GlowBlend), n => n.GlowBlend, (n, v) => n.GlowBlend = v, _ => GlowBlendMode.Screen, stored: true),
        new PropertyDescriptor<CanvasEnvironment, Texture?>(nameof(GlowMap), n => n.GlowMap, (n, v) => n.GlowMap = v, _ => null, stored: true),
    ];

    private static readonly PropertyDescriptor[] LevelProperties = Enumerable.Range(0, GlowSettings.LevelCount).Select(index =>
        new PropertyDescriptor<CanvasEnvironment, float>($"GlowLevels/{index + 1}", n => n.GetGlowLevel(index),
            (n, v) => n.SetGlowLevel(index, v), _ => GlowSettings.DefaultLevel(index), stored: true)).ToArray();
}

[InlineArray(GlowSettings.LevelCount)]
internal struct GlowLevels
{
    private float _value;
}

internal struct GlowSettings
{
    internal const int LevelCount = 7;
    internal bool Enabled, Normalized, Bicubic;
    internal float Bloom, LuminanceCap, HDRScale, Threshold, Intensity, MapStrength, Mix, Strength;
    internal CanvasEnvironment.GlowBlendMode Blend;
    internal GlowLevels Levels;
    internal Texture? Map;

    internal static float DefaultLevel(int index) => index switch { 1 => .8f, 2 => .4f, 3 => .1f, _ => 0 };

    internal static GlowSettings Default
    {
        get
        {
            var settings = new GlowSettings
            {
                LuminanceCap = 12,
                HDRScale = 2,
                Threshold = 1,
                Intensity = .3f,
                MapStrength = .8f,
                Mix = .05f,
                Strength = 1,
                Blend = CanvasEnvironment.GlowBlendMode.Screen
            };
            for (var i = 0; i < LevelCount; i++) settings.Levels[i] = DefaultLevel(i);
            return settings;
        }
    }
}
