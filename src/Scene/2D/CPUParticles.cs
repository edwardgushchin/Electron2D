namespace Electron2D;

/// <summary>Simulates seeded CPU particles and draws them through the ordinary canvas.</summary>
/// <remarks>Configuration is scene-owned; attached access follows the owner thread and capture guards.
/// Texture, curves, gradients and material remain borrowed. Simulation state is transient; scene copies
/// restart independently. Prepared simulation and drawing reuse particle/order/command storage.
/// Global particles retain emitted canvas positions independently of later emitter transforms.</remarks>
public partial class CPUParticles : Entity
{
    /// <summary>Selects the order of particle quads.</summary>
    public enum DrawOrderMode
    {
        /// <summary>Draw increasing particle indices.</summary>
        Index = 0,
        /// <summary>Draw older particles before younger particles.</summary>
        Lifetime = 1
    }
    /// <summary>Selects the emitter's two-dimensional distribution.</summary>
    public enum EmissionShapeMode
    {
        /// <summary>Emit at the origin.</summary>
        Point = 0,
        /// <summary>Emit inside a circular disk with uniform radius.</summary>
        Sphere = 1,
        /// <summary>Emit the projected surface of a sphere into the disk.</summary>
        SphereSurface = 2,
        /// <summary>Emit inside a rectangle.</summary>
        Rectangle = 3,
        /// <summary>Emit at copied points, with optional matching colors.</summary>
        Points = 4,
        /// <summary>Emit at copied points with optional matching normal bases.</summary>
        DirectedPoints = 5,
        /// <summary>Emit uniformly by area between two radii.</summary>
        Ring = 6,
        /// <summary>Nonselectable bound.</summary>
        Max = 7
    }
    /// <summary>Selects a scalar simulation channel.</summary>
    public enum Parameter
    {
        /// <summary>Controls pixels per second.</summary>
        InitialLinearVelocity = 0,
        /// <summary>Controls degrees per second.</summary>
        AngularVelocity = 1,
        /// <summary>Controls clockwise turns per second.</summary>
        OrbitVelocity = 2,
        /// <summary>Controls pixels per second squared.</summary>
        LinearAccel = 3,
        /// <summary>Controls pixels per second squared.</summary>
        RadialAccel = 4,
        /// <summary>Controls pixels per second squared.</summary>
        TangentialAccel = 5,
        /// <summary>Controls pixels per second squared.</summary>
        Damping = 6,
        /// <summary>Controls degrees.</summary>
        Angle = 7,
        /// <summary>Controls scale multiplier.</summary>
        Scale = 8,
        /// <summary>Controls hue turns.</summary>
        HueVariation = 9,
        /// <summary>Controls animation cycles over particle lifetime.</summary>
        AnimSpeed = 10,
        /// <summary>Controls normalized animation offset.</summary>
        AnimOffset = 11,
        /// <summary>Nonselectable bound.</summary>
        Max = 12
    }
    /// <summary>Selects the applicable particle basis policy.</summary>
    public enum ParticleFlags
    {
        /// <summary>Align the Y basis with velocity.</summary>
        AlignYToVelocity = 0,
        /// <summary>Nonselectable bound of the source flag domain.</summary>
        Max = 3
    }
    private float[] _minimum = new float[12], _maximum = new float[12];
    private readonly Curve?[] _curves = new Curve?[12];
    private bool _emitting = true, _active = true, _oneShot, _localCoords, _fractDelta = true, _useFixedSeed, _splitScale, _alignY;
    private double _lifetime = 1, _preprocess, _speedScale = 1;
    private float _explosiveness, _randomness, _lifetimeRandomness, _spread = 45, _sphereRadius = 1, _ringRadius = 1, _innerRadius = .8f;
    private int _fixedFPS;
    private uint _seed;
    private Vector2 _direction = new(1, 0), _gravity = new(0, 980), _rectExtents = Vector2.One;
    private Vector2[] _points = [], _normals = [];
    private Color[] _colors = [];
    private Color _color = Colors.White;
    private Texture? _texture;
    private Gradient? _ramp, _initialRamp;
    private Curve? _scaleX, _scaleY;
    private DrawOrderMode _drawOrder;
    private EmissionShapeMode _shape;
    /// <summary>Creates eight emitting particles, with world coordinates and interpolation off.</summary>
    public CPUParticles() { _minimum[8] = _maximum[8] = 1; _seed = _rng.Randi(); PhysicsInterpolationMode = PhysicsInterpolationMode.Off; SetInternalProcessing(true, false); }
    private void Read() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private void Edit() { EnsureMutable(); if (_advancing) throw new InvalidOperationException("Particle simulation cannot be reentered."); }
    private static void Finite(double value) { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void Nonnegative(double value) { Finite(value); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static void Ratio(float value) { Finite(value); if (value < 0 || value > 1) throw new ArgumentOutOfRangeException(nameof(value)); }
    private static int Channel(Parameter parameter) { if (parameter < 0 || parameter >= Parameter.Max) throw new ArgumentOutOfRangeException(nameof(parameter)); return (int)parameter; }
    private static void Borrow(Resource? value) { if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value)); }
    /// <summary>Occurs once when an active emission drains, after committing the idle state.</summary>
    /// <remarks>Handler failure propagates without replay. A handler may restart or dispose the emitter.</remarks>
    public event Action? Finished;
    /// <summary>Gets or changes capacity; assignment clears all live particles.</summary>
    /// <value>Eight initially; valid range is 1..65536.</value>
    /// <exception cref="ArgumentOutOfRangeException">Capacity is outside the bounded range.</exception>
    public int Amount { get { Read(); return _particles.Length; } set { Edit(); if (value < 1 || value > 65536) throw new ArgumentOutOfRangeException(nameof(value)); var particles = new Particle[value]; var scratch = new Particle[value]; var order = new int[value]; _particles = particles; _scratch = scratch; _order = order; InvalidateCanvas(); } }
    /// <summary>Gets or sets emission; stopping allows already active particles to drain.</summary>
    /// <value>True initially. Starting an idle emitter enables its internal processing.</value>
    public bool Emitting { get { Read(); return _emitting; } set { Edit(); if (_emitting == value) return; if (value && !_useFixedSeed && _oneShot) _seed = _rng.Randi(); _emitting = value; if (value) { _active = true; SetInternalProcessing(true, false); } InvalidateCanvas(); } }
    /// <summary>Gets or sets positive emission-cycle duration in seconds.</summary>
    /// <value>One second initially; particle lifetime randomness scales this duration.</value>
    /// <exception cref="ArgumentOutOfRangeException">Duration is nonfinite or not positive.</exception>
    public double Lifetime { get { Read(); return _lifetime; } set { Edit(); Finite(value); if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); _lifetime = value; } }
    /// <summary>Emit one cycle, then drain automatically.</summary>
    /// <value>false initially. Values are finite; resources remain caller-owned.</value>
    public bool OneShot { get { Read(); return _oneShot; } set { Edit(); _oneShot = value; } }
    /// <summary>Choose local positions instead of retained world positions.</summary>
    /// <value>false initially. Values are finite; resources remain caller-owned.</value>
    public bool LocalCoords { get { Read(); return _localCoords; } set { Edit(); _localCoords = value; InvalidateCanvas(); } }
    /// <summary>Advance newborn particles for only the remaining birth-frame duration.</summary>
    /// <value>true initially. Values are finite; resources remain caller-owned.</value>
    public bool FractDelta { get { Read(); return _fractDelta; } set { Edit(); _fractDelta = value; } }
    /// <summary>Preserve Seed across restart.</summary>
    /// <value>false initially. Values are finite; resources remain caller-owned.</value>
    public bool UseFixedSeed { get { Read(); return _useFixedSeed; } set { Edit(); if (_useFixedSeed == value) return; _useFixedSeed = value; NotifyPropertyListChanged(); } }
    /// <summary>Use independent X and Y scale curves.</summary>
    /// <value>false initially. Values are finite; resources remain caller-owned.</value>
    public bool SplitScale { get { Read(); return _splitScale; } set { Edit(); _splitScale = value; NotifyPropertyListChanged(); } }
    /// <summary>Align the particle Y basis with its velocity.</summary>
    /// <value>false initially. Values are finite; resources remain caller-owned.</value>
    public bool ParticleFlagAlignY { get { Read(); return _alignY; } set { Edit(); _alignY = value; } }
    /// <summary>Set the seed used for future births.</summary>
    /// <value>A random initial seed; fixed replay uses the explicitly assigned seed.</value>
    public uint Seed { get { Read(); return _seed; } set { Edit(); _seed = value; } }
    /// <summary>Set the initial direction, interpreted by its angle.</summary>
    /// <value>(1,0) initially. Values are finite; resources remain caller-owned.</value>
    public Vector2 Direction { get { Read(); return _direction; } set { Edit(); if (!value.IsFinite()) throw new ArgumentException("Value must be finite.", nameof(value)); _direction = value; } }
    /// <summary>Set acceleration in canvas pixels per second squared.</summary>
    /// <value>(0,980) initially. Values are finite; resources remain caller-owned.</value>
    public Vector2 Gravity { get { Read(); return _gravity; } set { Edit(); if (!value.IsFinite()) throw new ArgumentException("Value must be finite.", nameof(value)); _gravity = value; } }
    /// <summary>Set half extents of rectangle emission.</summary>
    /// <value>(1,1) initially. Values are finite; resources remain caller-owned.</value>
    public Vector2 EmissionRectExtents { get { Read(); return _rectExtents; } set { Edit(); if (!value.IsFinite()) throw new ArgumentException("Value must be finite.", nameof(value)); _rectExtents = value; } }
    /// <summary>Set the base particle color.</summary>
    /// <value>opaque white initially. Values are finite; resources remain caller-owned.</value>
    public Color Color { get { Read(); return _color; } set { Edit(); if (!value.IsFinite()) throw new ArgumentException("Value must be finite.", nameof(value)); _color = value; } }
    /// <summary>Borrow the normalized lifetime color gradient.</summary>
    /// <value>null initially. Values are finite; resources remain caller-owned.</value>
    public Gradient? ColorRamp { get { Read(); return _ramp; } set { Edit(); Borrow(value); _ramp = value; } }
    /// <summary>Borrow the random initial color gradient.</summary>
    /// <value>null initially. Values are finite; resources remain caller-owned.</value>
    public Gradient? ColorInitialRamp { get { Read(); return _initialRamp; } set { Edit(); Borrow(value); _initialRamp = value; } }
    /// <summary>Borrow the X scale curve used with SplitScale.</summary>
    /// <value>null initially. Values are finite; resources remain caller-owned.</value>
    public Curve? ScaleCurveX { get { Read(); return _scaleX; } set { Edit(); Borrow(value); _scaleX = value; } }
    /// <summary>Borrow the Y scale curve used with SplitScale.</summary>
    /// <value>null initially. Values are finite; resources remain caller-owned.</value>
    public Curve? ScaleCurveY { get { Read(); return _scaleY; } set { Edit(); Borrow(value); _scaleY = value; } }
    /// <summary>Set seconds of simulation before the first active update.</summary>
    /// <value>0 initially. Values are finite; resources remain caller-owned.</value>
    public double Preprocess { get { Read(); return _preprocess; } set { Edit(); Nonnegative(value); ValidateSeek(value); _preprocess = value; } }
    /// <summary>Scale ordinary simulation time; zero pauses time.</summary>
    /// <value>1 initially. Values are finite; resources remain caller-owned.</value>
    public double SpeedScale { get { Read(); return _speedScale; } set { Edit(); Nonnegative(value); _speedScale = value; } }
    /// <summary>Compress birth phases toward the cycle start.</summary>
    /// <value>0 initially. Values are finite; resources remain caller-owned.</value>
    public float Explosiveness { get { Read(); return _explosiveness; } set { Edit(); Ratio(value); _explosiveness = value; } }
    /// <summary>Randomize birth phases within index intervals.</summary>
    /// <value>0 initially. Values are finite; resources remain caller-owned.</value>
    public float Randomness { get { Read(); return _randomness; } set { Edit(); Ratio(value); _randomness = value; } }
    /// <summary>Reduce individual lifetime by a random fraction.</summary>
    /// <value>0 initially. Values are finite; resources remain caller-owned.</value>
    public float LifetimeRandomness { get { Read(); return _lifetimeRandomness; } set { Edit(); Ratio(value); _lifetimeRandomness = value; } }
    /// <summary>Set initial angular spread in degrees.</summary>
    /// <value>45 initially. Values are finite; resources remain caller-owned.</value>
    public float Spread { get { Read(); return _spread; } set { Edit(); Finite(value); _spread = value; } }
    /// <summary>Set circular emission radius in pixels.</summary>
    /// <value>1 initially. Values are finite; resources remain caller-owned.</value>
    public float EmissionSphereRadius { get { Read(); return _sphereRadius; } set { Edit(); Nonnegative(value); _sphereRadius = value; } }
    /// <summary>Set the outer ring radius in pixels.</summary>
    /// <value>1 initially. Values are finite; resources remain caller-owned.</value>
    public float EmissionRingRadius { get { Read(); return _ringRadius; } set { Edit(); Nonnegative(value); _ringRadius = value; } }
    /// <summary>Set the inner ring radius in pixels.</summary>
    /// <value>.8 initially. Values are finite; resources remain caller-owned.</value>
    public float EmissionRingInnerRadius { get { Read(); return _innerRadius; } set { Edit(); Nonnegative(value); _innerRadius = value; } }
    /// <summary>Gets or sets fixed simulation updates per second; zero uses frame delta.</summary>
    /// <value>Zero initially; valid range 0..1000. Ordinary fixed stepping caps an input frame at 0.1 seconds.</value>
    public int FixedFPS { get { Read(); return _fixedFPS; } set { Edit(); if (value < 0 || value > 1000) throw new ArgumentOutOfRangeException(nameof(value)); _fixedFPS = value; } }
    /// <summary>Gets or sets quad ordering.</summary>
    /// <value>Index initially.</value>
    public DrawOrderMode DrawOrder { get { Read(); return _drawOrder; } set { Edit(); if (value < DrawOrderMode.Index || value > DrawOrderMode.Lifetime) throw new ArgumentOutOfRangeException(nameof(value)); _drawOrder = value; InvalidateCanvas(); } }
    /// <summary>Gets or sets the distribution for future births.</summary>
    /// <value>Point initially. Mismatched point normals/colors are ignored.</value>
    public EmissionShapeMode EmissionShape { get { Read(); return _shape; } set { Edit(); if (value < 0 || value >= EmissionShapeMode.Max) throw new ArgumentOutOfRangeException(nameof(value)); _shape = value; NotifyPropertyListChanged(); } }
    /// <summary>Gets or sets the borrowed particle image.</summary>
    /// <value>Null initially, drawing one-pixel centered quads. Sprite-sheet geometry is the full image size.</value>
    public Texture? Texture { get { Read(); return _texture; } set { Edit(); Borrow(value); if (ReferenceEquals(value, _texture)) return; if (_texture is not null) _texture.Changed -= TextureChanged; _texture = value; if (_texture is not null) _texture.Changed += TextureChanged; InvalidateCanvas(); } }
    private void TextureChanged(Resource _) { if (!IsDisposed) InvalidateCanvas(); }
    /// <summary>Gets a copy or assigns copied finite emission points, bounded to 65536.</summary>
    public Vector2[] EmissionPoints { get { Read(); return (Vector2[])_points.Clone(); } set { Edit(); _points = CopyVectors(value); } }
    /// <summary>Gets a copy or assigns copied finite emission normal bases, bounded to 65536.</summary>
    public Vector2[] EmissionNormals { get { Read(); return (Vector2[])_normals.Clone(); } set { Edit(); _normals = CopyVectors(value); } }
    /// <summary>Gets a copy or assigns copied finite emission colors, bounded to 65536.</summary>
    public Color[] EmissionColors { get { Read(); return (Color[])_colors.Clone(); } set { Edit(); ArgumentNullException.ThrowIfNull(value); if (value.Length > 65536) throw new ArgumentOutOfRangeException(nameof(value)); var copy = (Color[])value.Clone(); foreach (var color in copy) if (!color.IsFinite()) throw new ArgumentException("Colors must be finite.", nameof(value)); _colors = copy; } }
    private static Vector2[] CopyVectors(Vector2[] value) { ArgumentNullException.ThrowIfNull(value); if (value.Length > 65536) throw new ArgumentOutOfRangeException(nameof(value)); var copy = (Vector2[])value.Clone(); foreach (var point in copy) if (!point.IsFinite()) throw new ArgumentException("Points must be finite.", nameof(value)); return copy; }
    /// <summary>Returns a channel's minimum.</summary>
    /// <param name="parameter">Selectable channel.</param><returns>Current minimum.</returns>
    public float GetParamMin(Parameter parameter) { Read(); return _minimum[Channel(parameter)]; }
    /// <summary>Returns a channel's maximum.</summary>
    /// <param name="parameter">Selectable channel.</param><returns>Current maximum.</returns>
    public float GetParamMax(Parameter parameter) { Read(); return _maximum[Channel(parameter)]; }
    /// <summary>Sets a minimum and raises the maximum if needed.</summary>
    /// <param name="parameter">Selectable channel.</param><param name="value">Finite scalar in channel units.</param>
    public void SetParamMin(Parameter parameter, float value) { Edit(); var channel = Channel(parameter); Finite(value); _minimum[channel] = value; if (_maximum[channel] < value) _maximum[channel] = value; }
    /// <summary>Sets a maximum and lowers the minimum if needed.</summary>
    /// <param name="parameter">Selectable channel.</param><param name="value">Finite scalar in channel units.</param>
    public void SetParamMax(Parameter parameter, float value) { Edit(); var channel = Channel(parameter); Finite(value); _maximum[channel] = value; if (_minimum[channel] > value) _minimum[channel] = value; }
    /// <summary>Returns the borrowed normalized lifetime curve of a channel.</summary>
    /// <param name="parameter">Selectable channel.</param><returns>The borrowed curve or null.</returns>
    public Curve? GetParamCurve(Parameter parameter) { Read(); return _curves[Channel(parameter)]; }
    /// <summary>Assigns a borrowed curve, initializing an empty curve to its channel's unit setup.</summary>
    /// <param name="parameter">Selectable channel.</param><param name="curve">Borrowed live curve or null.</param>
    public void SetParamCurve(Parameter parameter, Curve? curve) { Edit(); var channel = Channel(parameter); Borrow(curve); _curves[channel] = curve; if (curve is not null && parameter != Parameter.InitialLinearVelocity && parameter != Parameter.Scale && parameter != Parameter.AnimOffset) { var range = parameter switch { Parameter.Angle or Parameter.AngularVelocity => (-360f, 360f), Parameter.OrbitVelocity => (-500f, 500f), Parameter.LinearAccel or Parameter.RadialAccel or Parameter.TangentialAccel => (-200f, 200f), Parameter.Damping => (0f, 100f), Parameter.HueVariation => (-1f, 1f), _ => (0f, 200f) }; if (curve.PointCount == 0) { curve.MinValue = range.Item1; curve.MaxValue = range.Item2; curve.AddPoint(new(0, 1)); curve.AddPoint(new(1, 1)); } } }
    /// <summary>Returns the applicable alignment flag.</summary>
    /// <param name="particleFlag">AlignYToVelocity; reserved 3D flags are not selectable.</param><returns>Current flag value.</returns>
    public bool GetParticleFlag(ParticleFlags particleFlag) { Read(); if (particleFlag != ParticleFlags.AlignYToVelocity) throw new ArgumentOutOfRangeException(nameof(particleFlag)); return _alignY; }
    /// <summary>Sets the applicable alignment flag.</summary>
    /// <param name="particleFlag">AlignYToVelocity.</param><param name="enable">Whether to align the basis with velocity.</param>
    public void SetParticleFlag(ParticleFlags particleFlag, bool enable) { Edit(); if (particleFlag != ParticleFlags.AlignYToVelocity) throw new ArgumentOutOfRangeException(nameof(particleFlag)); _alignY = enable; }
    /// <summary>Gets or sets the minimum pixels per second; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float InitialVelocityMin { get => GetParamMin(Parameter.InitialLinearVelocity); set => SetParamMin(Parameter.InitialLinearVelocity, value); }
    /// <summary>Gets or sets the maximum pixels per second; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float InitialVelocityMax { get => GetParamMax(Parameter.InitialLinearVelocity); set => SetParamMax(Parameter.InitialLinearVelocity, value); }
    /// <summary>Gets or sets the minimum degrees per second; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float AngularVelocityMin { get => GetParamMin(Parameter.AngularVelocity); set => SetParamMin(Parameter.AngularVelocity, value); }
    /// <summary>Gets or sets the maximum degrees per second; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float AngularVelocityMax { get => GetParamMax(Parameter.AngularVelocity); set => SetParamMax(Parameter.AngularVelocity, value); }
    /// <summary>Gets or sets the borrowed lifetime curve for degrees per second.</summary>
    /// <value>Null initially.</value>
    public Curve? AngularVelocityCurve { get => GetParamCurve(Parameter.AngularVelocity); set => SetParamCurve(Parameter.AngularVelocity, value); }
    /// <summary>Gets or sets the minimum clockwise turns per second; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float OrbitVelocityMin { get => GetParamMin(Parameter.OrbitVelocity); set => SetParamMin(Parameter.OrbitVelocity, value); }
    /// <summary>Gets or sets the maximum clockwise turns per second; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float OrbitVelocityMax { get => GetParamMax(Parameter.OrbitVelocity); set => SetParamMax(Parameter.OrbitVelocity, value); }
    /// <summary>Gets or sets the borrowed lifetime curve for clockwise turns per second.</summary>
    /// <value>Null initially.</value>
    public Curve? OrbitVelocityCurve { get => GetParamCurve(Parameter.OrbitVelocity); set => SetParamCurve(Parameter.OrbitVelocity, value); }
    /// <summary>Gets or sets the minimum pixels per second squared; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float LinearAccelMin { get => GetParamMin(Parameter.LinearAccel); set => SetParamMin(Parameter.LinearAccel, value); }
    /// <summary>Gets or sets the maximum pixels per second squared; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float LinearAccelMax { get => GetParamMax(Parameter.LinearAccel); set => SetParamMax(Parameter.LinearAccel, value); }
    /// <summary>Gets or sets the borrowed lifetime curve for pixels per second squared.</summary>
    /// <value>Null initially.</value>
    public Curve? LinearAccelCurve { get => GetParamCurve(Parameter.LinearAccel); set => SetParamCurve(Parameter.LinearAccel, value); }
    /// <summary>Gets or sets the minimum pixels per second squared; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float RadialAccelMin { get => GetParamMin(Parameter.RadialAccel); set => SetParamMin(Parameter.RadialAccel, value); }
    /// <summary>Gets or sets the maximum pixels per second squared; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float RadialAccelMax { get => GetParamMax(Parameter.RadialAccel); set => SetParamMax(Parameter.RadialAccel, value); }
    /// <summary>Gets or sets the borrowed lifetime curve for pixels per second squared.</summary>
    /// <value>Null initially.</value>
    public Curve? RadialAccelCurve { get => GetParamCurve(Parameter.RadialAccel); set => SetParamCurve(Parameter.RadialAccel, value); }
    /// <summary>Gets or sets the minimum pixels per second squared; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float TangentialAccelMin { get => GetParamMin(Parameter.TangentialAccel); set => SetParamMin(Parameter.TangentialAccel, value); }
    /// <summary>Gets or sets the maximum pixels per second squared; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float TangentialAccelMax { get => GetParamMax(Parameter.TangentialAccel); set => SetParamMax(Parameter.TangentialAccel, value); }
    /// <summary>Gets or sets the borrowed lifetime curve for pixels per second squared.</summary>
    /// <value>Null initially.</value>
    public Curve? TangentialAccelCurve { get => GetParamCurve(Parameter.TangentialAccel); set => SetParamCurve(Parameter.TangentialAccel, value); }
    /// <summary>Gets or sets the minimum pixels per second squared; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float DampingMin { get => GetParamMin(Parameter.Damping); set => SetParamMin(Parameter.Damping, value); }
    /// <summary>Gets or sets the maximum pixels per second squared; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float DampingMax { get => GetParamMax(Parameter.Damping); set => SetParamMax(Parameter.Damping, value); }
    /// <summary>Gets or sets the borrowed lifetime curve for pixels per second squared.</summary>
    /// <value>Null initially.</value>
    public Curve? DampingCurve { get => GetParamCurve(Parameter.Damping); set => SetParamCurve(Parameter.Damping, value); }
    /// <summary>Gets or sets the minimum degrees; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float AngleMin { get => GetParamMin(Parameter.Angle); set => SetParamMin(Parameter.Angle, value); }
    /// <summary>Gets or sets the maximum degrees; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float AngleMax { get => GetParamMax(Parameter.Angle); set => SetParamMax(Parameter.Angle, value); }
    /// <summary>Gets or sets the borrowed lifetime curve for degrees.</summary>
    /// <value>Null initially.</value>
    public Curve? AngleCurve { get => GetParamCurve(Parameter.Angle); set => SetParamCurve(Parameter.Angle, value); }
    /// <summary>Gets or sets the minimum scale multiplier; repairs the paired range.</summary>
    /// <value>1 initially; finite values.</value>
    public float ScaleAmountMin { get => GetParamMin(Parameter.Scale); set => SetParamMin(Parameter.Scale, value); }
    /// <summary>Gets or sets the maximum scale multiplier; repairs the paired range.</summary>
    /// <value>1 initially; finite values.</value>
    public float ScaleAmountMax { get => GetParamMax(Parameter.Scale); set => SetParamMax(Parameter.Scale, value); }
    /// <summary>Gets or sets the borrowed lifetime curve for scale multiplier.</summary>
    /// <value>Null initially.</value>
    public Curve? ScaleAmountCurve { get => GetParamCurve(Parameter.Scale); set => SetParamCurve(Parameter.Scale, value); }
    /// <summary>Gets or sets the minimum hue turns; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float HueVariationMin { get => GetParamMin(Parameter.HueVariation); set => SetParamMin(Parameter.HueVariation, value); }
    /// <summary>Gets or sets the maximum hue turns; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float HueVariationMax { get => GetParamMax(Parameter.HueVariation); set => SetParamMax(Parameter.HueVariation, value); }
    /// <summary>Gets or sets the borrowed lifetime curve for hue turns.</summary>
    /// <value>Null initially.</value>
    public Curve? HueVariationCurve { get => GetParamCurve(Parameter.HueVariation); set => SetParamCurve(Parameter.HueVariation, value); }
    /// <summary>Gets or sets the minimum animation cycles over particle lifetime; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float AnimSpeedMin { get => GetParamMin(Parameter.AnimSpeed); set => SetParamMin(Parameter.AnimSpeed, value); }
    /// <summary>Gets or sets the maximum animation cycles over particle lifetime; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float AnimSpeedMax { get => GetParamMax(Parameter.AnimSpeed); set => SetParamMax(Parameter.AnimSpeed, value); }
    /// <summary>Gets or sets the borrowed lifetime curve for animation cycles over particle lifetime.</summary>
    /// <value>Null initially.</value>
    public Curve? AnimSpeedCurve { get => GetParamCurve(Parameter.AnimSpeed); set => SetParamCurve(Parameter.AnimSpeed, value); }
    /// <summary>Gets or sets the minimum normalized animation offset; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float AnimOffsetMin { get => GetParamMin(Parameter.AnimOffset); set => SetParamMin(Parameter.AnimOffset, value); }
    /// <summary>Gets or sets the maximum normalized animation offset; repairs the paired range.</summary>
    /// <value>0 initially; finite values.</value>
    public float AnimOffsetMax { get => GetParamMax(Parameter.AnimOffset); set => SetParamMax(Parameter.AnimOffset, value); }
    /// <summary>Gets or sets the borrowed lifetime curve for normalized animation offset.</summary>
    /// <value>Null initially.</value>
    public Curve? AnimOffsetCurve { get => GetParamCurve(Parameter.AnimOffset); set => SetParamCurve(Parameter.AnimOffset, value); }
}
