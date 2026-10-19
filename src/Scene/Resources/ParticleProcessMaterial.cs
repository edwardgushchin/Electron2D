namespace Electron2D;

/// <summary>Configures native GPU particle distributions, motion, appearance, collisions and subemission.</summary>
/// <remarks>Textures are borrowed. Simulation uses GPU storage and requires the GPU renderer. Parameter changes
/// affect the next process step; initial distributions and velocity affect subsequent births. Resource copying and
/// file storage preserve the typed settings and borrowed texture graph. Coordinate multi-call edits with rendering.</remarks>
public sealed partial class ParticleProcessMaterial : Material
{
    /// <summary>Selects a particle simulation or appearance channel.</summary>
    public enum Parameter
    {
        /// <summary>Initial speed in pixels per second.</summary>
        InitialLinearVelocity = 0,
        /// <summary>Angular velocity.</summary>
        AngularVelocity = 1,
        /// <summary>Clockwise orbit speed in turns per second.</summary>
        OrbitVelocity = 2,
        /// <summary>Acceleration along physical velocity.</summary>
        LinearAccel = 3,
        /// <summary>Acceleration away from the emitter.</summary>
        RadialAccel = 4,
        /// <summary>Acceleration tangent to the emitter radius.</summary>
        TangentialAccel = 5,
        /// <summary>Physical velocity damping.</summary>
        Damping = 6,
        /// <summary>Initial angle in degrees.</summary>
        Angle = 7,
        /// <summary>Base scale multiplier.</summary>
        Scale = 8,
        /// <summary>Hue rotation in turns.</summary>
        HueVariation = 9,
        /// <summary>Animation cycles over particle life.</summary>
        AnimSpeed = 10,
        /// <summary>Initial normalized animation offset.</summary>
        AnimOffset = 11,
        /// <summary>Lifetime curve multiplying turbulence influence; has no scalar range.</summary>
        TurbInfluenceOverLife = 12,
        /// <summary>Turbulence velocity influence; has no texture channel.</summary>
        TurbVelocityInfluence = 13,
        /// <summary>Initial turbulence displacement; has no texture channel.</summary>
        TurbInitialDisplacement = 14,
        /// <summary>Controlled velocity away from the pivot.</summary>
        RadialVelocity = 15,
        /// <summary>Controlled directional velocity texture multiplier.</summary>
        DirectionalVelocity = 16,
        /// <summary>Velocity interval used to sample the scale texture.</summary>
        ScaleOverVelocity = 17,
        /// <summary>Nonselectable bound.</summary>
        Max = 18,
    }
    /// <summary>Selects a planar particle basis or damping policy.</summary>
    public enum ParticleFlags
    {
        /// <summary>Aligns the Y axis to final velocity.</summary>
        AlignYToVelocity = 0,
        /// <summary>Applies quadratic friction instead of constant speed loss.</summary>
        DampingAsFriction = 3,
        /// <summary>Multiplies scale by the emission basis lengths.</summary>
        InheritEmitterScale = 4,
        /// <summary>Nonselectable bound.</summary>
        Max = 5,
    }
    /// <summary>Selects signed-distance-field contact behavior.</summary>
    public enum CollisionModeEnum
    {
        /// <summary>Ignores the canvas distance field.</summary>
        Disabled = 0,
        /// <summary>Separates contacts and applies friction and bounce.</summary>
        Rigid = 1,
        /// <summary>Deactivates particles at contact.</summary>
        HideOnContact = 2,
        /// <summary>Nonselectable bound.</summary>
        Max = 3,
    }
    /// <summary>Selects when a parent emits particles into its configured child.</summary>
    public enum SubEmitterModeEnum
    {
        /// <summary>Disables subemission.</summary>
        Disabled = 0,
        /// <summary>Emits at the configured frequency.</summary>
        Constant = 1,
        /// <summary>Emits as the parent reaches the end of life.</summary>
        AtEnd = 2,
        /// <summary>Emits at distance-field contact.</summary>
        AtCollision = 3,
        /// <summary>Emits at birth.</summary>
        AtStart = 4,
        /// <summary>Nonselectable bound.</summary>
        Max = 5,
    }
    private readonly object _gate = new();
    private readonly Vector4[] _ranges = new Vector4[18];
    private readonly Texture?[] _curves = new Texture?[23];
    private MaterialState? _state;
    private bool _dirty = true, _preparing;
    private int _flags;
    private static readonly Lazy<Shader> BuiltInShader = new(() => LoadShader("ParticleProcess"));
    private static readonly Lazy<Shader> MotionShader = new(() => LoadShader("ParticleMotion"));
    private static Shader LoadShader(string name)
    {
        using var source = typeof(Shader).Assembly.GetManifestResourceStream("Electron2D.Shaders." + name + ".comp.spv")!;
        using var bytes = new MemoryStream(); source.CopyTo(bytes);
        return Shader.CreateFromSPIRV(bytes.ToArray(), Shader.Mode.Particles);
    }
    private static readonly Lazy<ImageTexture> Fallback = new(() =>
    {
        using var image = Image.CreateEmpty(1, 1, false, Image.Format.Rgbaf); image.Fill(Colors.White);
        return ImageTexture.CreateFromImage(image);
    });

    /// <summary>Creates a material with unit scale, white color, rightward direction and numeric gravity (0, -9.8).</summary>
    public ParticleProcessMaterial()
    {
        _ranges[8] = new(1, 1, 0, 0); _ranges[13] = new(.1f, .1f, 0, 0); _ranges[16] = new(1, 1, 0, 0);
    }
    private T Read<T>(ref T field) { lock (_gate) { ThrowIfDisposed(); return field; } }
    private void Edit() { ThrowIfDisposed(); if (_preparing) throw new InvalidOperationException("Particle settings cannot change during texture preparation."); _dirty = true; if (_state is not null) _state.ParticleConfigurationVersion++; }
    private void Set<T>(ref T field, T value)
    {
        lock (_gate) { ThrowIfDisposed(); if (EqualityComparer<T>.Default.Equals(field, value)) return; Edit(); field = value; }
        EmitChanged();
    }
    private static float Finite(float value) => float.IsFinite(value) ? value : throw new ArgumentOutOfRangeException(nameof(value));
    private static float Nonnegative(float value) => Finite(value) >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    private static float Positive(float value) => Finite(value) > 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    private static float Ratio(float value) => Finite(value) is >= 0 and <= 1 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    private static int Nonnegative(int value) => value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
    private static Vector2 Finite(Vector2 value) => value.IsFinite() ? value : throw new ArgumentException("Components must be finite.", nameof(value));
    private static Vector3 Finite(Vector3 value) => value.IsFinite() ? value : throw new ArgumentException("Components must be finite.", nameof(value));
    private static Color Finite(Color value) => value.IsFinite() ? value : throw new ArgumentException("Channels must be finite.", nameof(value));
    private static int Channel(Parameter parameter, bool texture)
    {
        if (parameter < 0 || parameter >= Parameter.Max || (texture ? parameter is Parameter.InitialLinearVelocity or Parameter.TurbVelocityInfluence or Parameter.TurbInitialDisplacement : parameter == Parameter.TurbInfluenceOverLife))
            throw new ArgumentOutOfRangeException(nameof(parameter), "The selected channel has no parameter of this role.");
        return (int)parameter;
    }
    /// <summary>Reads a scalar channel's minimum and maximum.</summary>
    /// <param name="parameter">A scalar channel; the turbulence lifetime-curve channel is invalid here.</param>
    /// <returns>The minimum and maximum pair.</returns>
    public Vector2 GetParam(Parameter parameter) { var i = Channel(parameter, false); lock (_gate) { ThrowIfDisposed(); return new(_ranges[i].X, _ranges[i].Y); } }
    /// <summary>Reads a scalar channel's minimum.</summary>
    /// <param name="parameter">A scalar channel.</param>
    /// <returns>The lower bound.</returns>
    public float GetParamMin(Parameter parameter) => GetParam(parameter).X;
    /// <summary>Reads a scalar channel's maximum.</summary>
    /// <param name="parameter">A scalar channel.</param>
    /// <returns>The upper bound.</returns>
    public float GetParamMax(Parameter parameter) => GetParam(parameter).Y;
    /// <summary>Replaces a scalar range; an inverted pair collapses to its supplied maximum.</summary>
    /// <param name="parameter">A scalar channel.</param>
    /// <param name="value">Finite minimum and maximum values.</param>
    public void SetParam(Parameter parameter, Vector2 value)
    { var i = Channel(parameter, false); Finite(value); lock (_gate) { Edit(); _ranges[i] = new(Math.Min(value.X, value.Y), value.Y, 0, 0); } EmitChanged(); }
    /// <summary>Sets a minimum, raising the maximum when necessary.</summary>
    /// <param name="parameter">A scalar channel.</param>
    /// <param name="value">The finite lower bound.</param>
    public void SetParamMin(Parameter parameter, float value)
    { var i = Channel(parameter, false); Finite(value); lock (_gate) { Edit(); _ranges[i].X = value; _ranges[i].Y = Math.Max(value, _ranges[i].Y); } EmitChanged(); }
    /// <summary>Sets a maximum, lowering the minimum when necessary.</summary>
    /// <param name="parameter">A scalar channel.</param>
    /// <param name="value">The finite upper bound.</param>
    public void SetParamMax(Parameter parameter, float value)
    { var i = Channel(parameter, false); Finite(value); lock (_gate) { Edit(); _ranges[i].Y = value; _ranges[i].X = Math.Min(value, _ranges[i].X); } EmitChanged(); }
    /// <summary>Reads a borrowed curve texture.</summary>
    /// <param name="parameter">A texture channel; initial velocity and the two scalar turbulence channels are invalid here.</param>
    /// <returns>The borrowed texture, or null.</returns>
    public Texture? GetParamTexture(Parameter parameter) => ReadCurve(Channel(parameter, true));
    /// <summary>Sets a borrowed curve texture and initializes missing scalar curves with unit samples.</summary>
    /// <param name="parameter">A texture channel.</param>
    /// <param name="texture">A floating-point sampled texture, or null to remove modulation.</param>
    public void SetParamTexture(Parameter parameter, Texture? texture) => SetCurve(Channel(parameter, true), texture);
    private Texture? ReadCurve(int index) { lock (_gate) { ThrowIfDisposed(); return _curves[index]; } }
    private void SetCurve(int index, Texture? texture)
    {
        if (texture is not null) ObjectDisposedException.ThrowIf(texture.IsDisposed, texture);
        lock (_gate) { Edit(); _curves[index] = texture; }
        var range = index switch { 1 or 7 => new Vector2(-360, 360), 2 => new Vector2(-2, 2), 3 or 4 or 5 => new Vector2(-200, 200), 6 => new Vector2(0, 100), 8 or 12 => new Vector2(0, 1), 9 => new Vector2(-1, 1), 10 => new Vector2(0, 200), 17 => new Vector2(0, 3), _ => new Vector2(0, 0) };
        if (range.X != range.Y)
        {
            if (texture is CurveTexture scalar && scalar.Curve is null) scalar.Curve = UnitCurve(range);
            if (texture is CurveXYZTexture channels)
            {
                if (channels.CurveX is null) channels.CurveX = UnitCurve(range);
                if (channels.CurveY is null) channels.CurveY = UnitCurve(range);
                if (channels.CurveZ is null) channels.CurveZ = UnitCurve(range);
            }
        }
        EmitChanged();
    }
    private static Curve UnitCurve(Vector2 range)
    {
        var curve = new Curve(); curve.AddPoint(new(0, 1)); curve.AddPoint(new(1, 1)); curve.MinValue = range.X; curve.MaxValue = range.Y; return curve;
    }
    private static int Flag(ParticleFlags flag) => flag is ParticleFlags.AlignYToVelocity or ParticleFlags.DampingAsFriction or ParticleFlags.InheritEmitterScale ? 1 << (int)flag : throw new ArgumentOutOfRangeException(nameof(flag));
    /// <summary>Reads a planar basis or damping flag.</summary>
    /// <param name="flag">The selected applicable flag.</param>
    /// <returns>Whether the policy is enabled.</returns>
    public bool GetParticleFlag(ParticleFlags flag) { var bit = Flag(flag); lock (_gate) { ThrowIfDisposed(); return (_flags & bit) != 0; } }
    /// <summary>Changes a planar basis or damping flag.</summary>
    /// <param name="flag">The selected applicable flag.</param>
    /// <param name="enabled">Whether to enable the policy.</param>
    public void SetParticleFlag(ParticleFlags flag, bool enabled) { var bit = Flag(flag); lock (_gate) { Edit(); _flags = enabled ? _flags | bit : _flags & ~bit; } EmitChanged(); }
}
