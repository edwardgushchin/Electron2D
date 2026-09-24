using Backend = Electron2D.Internal.FastNoiseLite;

namespace Electron2D;

/// <summary>Selects the base noise algorithm.</summary>
public enum NoiseType
{
    /// <summary>Simplex mode.</summary>
    Simplex = 0,
    /// <summary>SimplexSmooth mode.</summary>
    SimplexSmooth = 1,
    /// <summary>Cellular mode.</summary>
    Cellular = 2,
    /// <summary>Perlin mode.</summary>
    Perlin = 3,
    /// <summary>ValueCubic mode.</summary>
    ValueCubic = 4,
    /// <summary>Value mode.</summary>
    Value = 5,
}

/// <summary>Selects the fractal algorithm.</summary>
public enum FractalType
{
    /// <summary>None mode.</summary>
    None = 0,
    /// <summary>FBM mode.</summary>
    FBM = 1,
    /// <summary>Ridged mode.</summary>
    Ridged = 2,
    /// <summary>PingPong mode.</summary>
    PingPong = 3,
}

/// <summary>Selects the cellular distance metric.</summary>
public enum CellularDistanceFunction
{
    /// <summary>Euclidean mode.</summary>
    Euclidean = 0,
    /// <summary>EuclideanSquared mode.</summary>
    EuclideanSquared = 1,
    /// <summary>Manhattan mode.</summary>
    Manhattan = 2,
    /// <summary>Hybrid mode.</summary>
    Hybrid = 3,
}

/// <summary>Selects the cellular return value.</summary>
public enum CellularReturnType
{
    /// <summary>CellValue mode.</summary>
    CellValue = 0,
    /// <summary>Distance mode.</summary>
    Distance = 1,
    /// <summary>Distance2 mode.</summary>
    Distance2 = 2,
    /// <summary>Distance2Add mode.</summary>
    Distance2Add = 3,
    /// <summary>Distance2Sub mode.</summary>
    Distance2Sub = 4,
    /// <summary>Distance2Mul mode.</summary>
    Distance2Mul = 5,
    /// <summary>Distance2Div mode.</summary>
    Distance2Div = 6,
}

/// <summary>Selects domain warp type behavior.</summary>
public enum DomainWarpType
{
    /// <summary>Simplex mode.</summary>
    Simplex = 0,
    /// <summary>SimplexReduced mode.</summary>
    SimplexReduced = 1,
    /// <summary>BasicGrid mode.</summary>
    BasicGrid = 2,
}

/// <summary>Selects the domain warp fractal algorithm.</summary>
public enum DomainWarpFractalType
{
    /// <summary>None mode.</summary>
    None = 0,
    /// <summary>Progressive mode.</summary>
    Progressive = 1,
    /// <summary>Independent mode.</summary>
    Independent = 2,
}

/// <summary>Generates repeatable one- and two-dimensional procedural noise.</summary>
/// <remarks>Sampling uses an internal managed algorithm. Coordinate changes and copying require caller coordination.
/// A noise texture may borrow this resource and refreshes after Changed.</remarks>
public sealed class FastNoiseLite : Noise
{
    private readonly Backend _noise = new();
    private readonly Backend _warp = new();
    private NoiseType _noiseType = NoiseType.SimplexSmooth;
    private int _seed = 0;
    private float _frequency = 0.01f;
    private Vector2 _offset = Vector2.Zero;
    private FractalType _fractalType = FractalType.FBM;
    private int _fractalOctaves = 5;
    private float _fractalLacunarity = 2f;
    private float _fractalGain = 0.5f;
    private float _fractalWeightedStrength = 0f;
    private float _fractalPingPongStrength = 2f;
    private CellularDistanceFunction _cellularDistanceFunction = CellularDistanceFunction.Euclidean;
    private CellularReturnType _cellularReturnType = CellularReturnType.Distance;
    private float _cellularJitter = 1f;
    private bool _domainWarpEnabled = false;
    private DomainWarpType _domainWarpType = DomainWarpType.Simplex;
    private float _domainWarpAmplitude = 30f;
    private float _domainWarpFrequency = 0.05f;
    private DomainWarpFractalType _domainWarpFractalType = DomainWarpFractalType.Progressive;
    private int _domainWarpFractalOctaves = 5;
    private float _domainWarpFractalLacunarity = 6f;
    private float _domainWarpFractalGain = 0.5f;

    /// <summary>Creates a generator with the documented default noise and warp settings.</summary>
    public FastNoiseLite() => Configure();

    /// <summary>Gets or sets the noise algorithm.</summary>
    /// <value>NoiseType.SimplexSmooth by default.</value>
    public NoiseType NoiseType
    {
        get { ThrowIfDisposed(); return _noiseType; }
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _noiseType, value, v => _noise.SetNoiseType((Backend.NoiseType)v), true);
        }
    }

    /// <summary>Gets or sets seed.</summary>
    /// <value>0 by default.</value>
    public int Seed
    {
        get { ThrowIfDisposed(); return _seed; }
        set
        {
            Set(ref _seed, value, v => { _noise.SetSeed(v); _warp.SetSeed(v); });
        }
    }

    /// <summary>Gets or sets frequency.</summary>
    /// <value>0.01f by default.</value>
    public float Frequency
    {
        get { ThrowIfDisposed(); return _frequency; }
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _frequency, value, v => _noise.SetFrequency(v));
        }
    }

    /// <summary>Gets or sets offset.</summary>
    /// <value>Vector2.Zero by default.</value>
    public Vector2 Offset
    {
        get { ThrowIfDisposed(); return _offset; }
        set
        {
            if (!float.IsFinite(value.X) || !float.IsFinite(value.Y)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _offset, value, _ => { });
        }
    }

    /// <summary>Gets or sets fractal type.</summary>
    /// <value>FractalType.FBM by default.</value>
    public FractalType FractalType
    {
        get { ThrowIfDisposed(); return _fractalType; }
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _fractalType, value, v => _noise.SetFractalType((Backend.FractalType)v), true);
        }
    }

    /// <summary>Gets or sets fractal octaves.</summary>
    /// <value>5 by default.</value>
    public int FractalOctaves
    {
        get { ThrowIfDisposed(); return _fractalOctaves; }
        set
        {
            if (value < 1 || value > 1024) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _fractalOctaves, value, v => _noise.SetFractalOctaves(v));
        }
    }

    /// <summary>Gets or sets fractal lacunarity.</summary>
    /// <value>2f by default.</value>
    public float FractalLacunarity
    {
        get { ThrowIfDisposed(); return _fractalLacunarity; }
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _fractalLacunarity, value, v => _noise.SetFractalLacunarity(v));
        }
    }

    /// <summary>Gets or sets fractal gain.</summary>
    /// <value>0.5f by default.</value>
    public float FractalGain
    {
        get { ThrowIfDisposed(); return _fractalGain; }
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _fractalGain, value, v => _noise.SetFractalGain(v));
        }
    }

    /// <summary>Gets or sets fractal weighted strength.</summary>
    /// <value>0f by default.</value>
    public float FractalWeightedStrength
    {
        get { ThrowIfDisposed(); return _fractalWeightedStrength; }
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _fractalWeightedStrength, value, v => _noise.SetFractalWeightedStrength(v));
        }
    }

    /// <summary>Gets or sets fractal ping-pong strength.</summary>
    /// <value>2f by default.</value>
    public float FractalPingPongStrength
    {
        get { ThrowIfDisposed(); return _fractalPingPongStrength; }
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _fractalPingPongStrength, value, v => _noise.SetFractalPingPongStrength(v));
        }
    }

    /// <summary>Gets or sets the cellular distance function.</summary>
    /// <value>CellularDistanceFunction.Euclidean by default.</value>
    public CellularDistanceFunction CellularDistanceFunction
    {
        get { ThrowIfDisposed(); return _cellularDistanceFunction; }
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _cellularDistanceFunction, value, v => _noise.SetCellularDistanceFunction((Backend.CellularDistanceFunction)v));
        }
    }

    /// <summary>Gets or sets the cellular return mode.</summary>
    /// <value>CellularReturnType.Distance by default.</value>
    public CellularReturnType CellularReturnType
    {
        get { ThrowIfDisposed(); return _cellularReturnType; }
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _cellularReturnType, value, v => _noise.SetCellularReturnType((Backend.CellularReturnType)v));
        }
    }

    /// <summary>Gets or sets cellular jitter.</summary>
    /// <value>1f by default.</value>
    public float CellularJitter
    {
        get { ThrowIfDisposed(); return _cellularJitter; }
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _cellularJitter, value, v => _noise.SetCellularJitter(v));
        }
    }

    /// <summary>Gets or sets domain warp enabled.</summary>
    /// <value>false by default.</value>
    public bool DomainWarpEnabled
    {
        get { ThrowIfDisposed(); return _domainWarpEnabled; }
        set
        {
            if (_domainWarpEnabled == value) { ThrowIfDisposed(); return; }
            Set(ref _domainWarpEnabled, value, _ => { }, true);
        }
    }

    /// <summary>Gets or sets domain warp type.</summary>
    /// <value>DomainWarpType.Simplex by default.</value>
    public DomainWarpType DomainWarpType
    {
        get { ThrowIfDisposed(); return _domainWarpType; }
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _domainWarpType, value, v => _warp.SetDomainWarpType((Backend.DomainWarpType)v));
        }
    }

    /// <summary>Gets or sets domain warp amplitude.</summary>
    /// <value>30f by default.</value>
    public float DomainWarpAmplitude
    {
        get { ThrowIfDisposed(); return _domainWarpAmplitude; }
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _domainWarpAmplitude, value, v => _warp.SetDomainWarpAmp(v));
        }
    }

    /// <summary>Gets or sets domain warp frequency.</summary>
    /// <value>0.05f by default.</value>
    public float DomainWarpFrequency
    {
        get { ThrowIfDisposed(); return _domainWarpFrequency; }
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _domainWarpFrequency, value, v => _warp.SetFrequency(v));
        }
    }

    /// <summary>Gets or sets domain warp fractal type.</summary>
    /// <value>DomainWarpFractalType.Progressive by default.</value>
    public DomainWarpFractalType DomainWarpFractalType
    {
        get { ThrowIfDisposed(); return _domainWarpFractalType; }
        set
        {
            if (!Enum.IsDefined(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _domainWarpFractalType, value, v => _warp.SetFractalType(v switch { DomainWarpFractalType.None => Backend.FractalType.None, DomainWarpFractalType.Progressive => Backend.FractalType.DomainWarpProgressive, _ => Backend.FractalType.DomainWarpIndependent }));
        }
    }

    /// <summary>Gets or sets domain warp fractal octaves.</summary>
    /// <value>5 by default.</value>
    public int DomainWarpFractalOctaves
    {
        get { ThrowIfDisposed(); return _domainWarpFractalOctaves; }
        set
        {
            if (value < 1 || value > 1024) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _domainWarpFractalOctaves, value, v => _warp.SetFractalOctaves(v));
        }
    }

    /// <summary>Gets or sets domain warp fractal lacunarity.</summary>
    /// <value>6f by default.</value>
    public float DomainWarpFractalLacunarity
    {
        get { ThrowIfDisposed(); return _domainWarpFractalLacunarity; }
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _domainWarpFractalLacunarity, value, v => _warp.SetFractalLacunarity(v));
        }
    }

    /// <summary>Gets or sets domain warp fractal gain.</summary>
    /// <value>0.5f by default.</value>
    public float DomainWarpFractalGain
    {
        get { ThrowIfDisposed(); return _domainWarpFractalGain; }
        set
        {
            if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value));
            Set(ref _domainWarpFractalGain, value, v => _warp.SetFractalGain(v));
        }
    }

    /// <inheritdoc />
    public override float GetNoise1D(float x)
    {
        ThrowIfDisposed();
        if (!float.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x));
        x += _offset.X;
        if (_domainWarpEnabled) { var y = 0f; _warp.DomainWarp(ref x, ref y); }
        return GetNoise2D(x, 0f);
    }

    /// <inheritdoc />
    public override float GetNoise2D(float x, float y)
    {
        ThrowIfDisposed();
        if (!float.IsFinite(x)) throw new ArgumentOutOfRangeException(nameof(x));
        if (!float.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(y));
        x += _offset.X; y += _offset.Y;
        if (!float.IsFinite(x) || !float.IsFinite(y)) throw new ArgumentOutOfRangeException(nameof(x));
        if (_domainWarpEnabled) _warp.DomainWarp(ref x, ref y);
        if (!float.IsFinite(x) || !float.IsFinite(y)) throw new InvalidOperationException("Domain warp produced a nonfinite coordinate.");
        var sample = _noise.GetNoise(x, y);
        if (!float.IsFinite(sample)) throw new InvalidOperationException("The noise generator produced a nonfinite sample.");
        return sample;
    }

    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(
    [
        new PropertyDescriptor<FastNoiseLite, NoiseType>(nameof(NoiseType), n => n.NoiseType, (n, v) => n.NoiseType = v, _ => NoiseType.SimplexSmooth),
        new PropertyDescriptor<FastNoiseLite, int>(nameof(Seed), n => n.Seed, (n, v) => n.Seed = v, _ => 0),
        new PropertyDescriptor<FastNoiseLite, float>(nameof(Frequency), n => n.Frequency, (n, v) => n.Frequency = v, _ => 0.01f),
        new PropertyDescriptor<FastNoiseLite, Vector2>(nameof(Offset), n => n.Offset, (n, v) => n.Offset = v, _ => Vector2.Zero),
        new PropertyDescriptor<FastNoiseLite, FractalType>(nameof(FractalType), n => n.FractalType, (n, v) => n.FractalType = v, _ => FractalType.FBM),
        new PropertyDescriptor<FastNoiseLite, int>(nameof(FractalOctaves), n => n.FractalOctaves, (n, v) => n.FractalOctaves = v, _ => 5),
        new PropertyDescriptor<FastNoiseLite, float>(nameof(FractalLacunarity), n => n.FractalLacunarity, (n, v) => n.FractalLacunarity = v, _ => 2f),
        new PropertyDescriptor<FastNoiseLite, float>(nameof(FractalGain), n => n.FractalGain, (n, v) => n.FractalGain = v, _ => 0.5f),
        new PropertyDescriptor<FastNoiseLite, float>(nameof(FractalWeightedStrength), n => n.FractalWeightedStrength, (n, v) => n.FractalWeightedStrength = v, _ => 0f),
        new PropertyDescriptor<FastNoiseLite, float>(nameof(FractalPingPongStrength), n => n.FractalPingPongStrength, (n, v) => n.FractalPingPongStrength = v, _ => 2f),
        new PropertyDescriptor<FastNoiseLite, CellularDistanceFunction>(nameof(CellularDistanceFunction), n => n.CellularDistanceFunction, (n, v) => n.CellularDistanceFunction = v, _ => CellularDistanceFunction.Euclidean),
        new PropertyDescriptor<FastNoiseLite, CellularReturnType>(nameof(CellularReturnType), n => n.CellularReturnType, (n, v) => n.CellularReturnType = v, _ => CellularReturnType.Distance),
        new PropertyDescriptor<FastNoiseLite, float>(nameof(CellularJitter), n => n.CellularJitter, (n, v) => n.CellularJitter = v, _ => 1f),
        new PropertyDescriptor<FastNoiseLite, bool>(nameof(DomainWarpEnabled), n => n.DomainWarpEnabled, (n, v) => n.DomainWarpEnabled = v, _ => false),
        new PropertyDescriptor<FastNoiseLite, DomainWarpType>(nameof(DomainWarpType), n => n.DomainWarpType, (n, v) => n.DomainWarpType = v, _ => DomainWarpType.Simplex),
        new PropertyDescriptor<FastNoiseLite, float>(nameof(DomainWarpAmplitude), n => n.DomainWarpAmplitude, (n, v) => n.DomainWarpAmplitude = v, _ => 30f),
        new PropertyDescriptor<FastNoiseLite, float>(nameof(DomainWarpFrequency), n => n.DomainWarpFrequency, (n, v) => n.DomainWarpFrequency = v, _ => 0.05f),
        new PropertyDescriptor<FastNoiseLite, DomainWarpFractalType>(nameof(DomainWarpFractalType), n => n.DomainWarpFractalType, (n, v) => n.DomainWarpFractalType = v, _ => DomainWarpFractalType.Progressive),
        new PropertyDescriptor<FastNoiseLite, int>(nameof(DomainWarpFractalOctaves), n => n.DomainWarpFractalOctaves, (n, v) => n.DomainWarpFractalOctaves = v, _ => 5),
        new PropertyDescriptor<FastNoiseLite, float>(nameof(DomainWarpFractalLacunarity), n => n.DomainWarpFractalLacunarity, (n, v) => n.DomainWarpFractalLacunarity = v, _ => 6f),
        new PropertyDescriptor<FastNoiseLite, float>(nameof(DomainWarpFractalGain), n => n.DomainWarpFractalGain, (n, v) => n.DomainWarpFractalGain = v, _ => 0.5f),
    ]);

    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new FastNoiseLite();

    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode subresourceMode,
        Func<Resource?, Resource?> duplicateSubresource, Func<Resource?, Resource?> forceDuplicateSubresource)
    {
        ThrowIfDisposed();
        var copy = (FastNoiseLite)target;
        copy._noiseType = _noiseType;
        copy._seed = _seed;
        copy._frequency = _frequency;
        copy._offset = _offset;
        copy._fractalType = _fractalType;
        copy._fractalOctaves = _fractalOctaves;
        copy._fractalLacunarity = _fractalLacunarity;
        copy._fractalGain = _fractalGain;
        copy._fractalWeightedStrength = _fractalWeightedStrength;
        copy._fractalPingPongStrength = _fractalPingPongStrength;
        copy._cellularDistanceFunction = _cellularDistanceFunction;
        copy._cellularReturnType = _cellularReturnType;
        copy._cellularJitter = _cellularJitter;
        copy._domainWarpEnabled = _domainWarpEnabled;
        copy._domainWarpType = _domainWarpType;
        copy._domainWarpAmplitude = _domainWarpAmplitude;
        copy._domainWarpFrequency = _domainWarpFrequency;
        copy._domainWarpFractalType = _domainWarpFractalType;
        copy._domainWarpFractalOctaves = _domainWarpFractalOctaves;
        copy._domainWarpFractalLacunarity = _domainWarpFractalLacunarity;
        copy._domainWarpFractalGain = _domainWarpFractalGain;
        copy.Configure();
    }

    private void Set<T>(ref T field, T value, Action<T> apply, bool notifyList = false)
    {
        ThrowIfDisposed();
        field = value;
        apply(value);
        EmitChanged();
        if (notifyList) NotifyPropertyListChanged();
    }

    private void Configure()
    {
        _noise.SetNoiseType((Backend.NoiseType)_noiseType);
        _noise.SetSeed(_seed); _warp.SetSeed(_seed);
        _noise.SetFrequency(_frequency);
        _noise.SetFractalType((Backend.FractalType)_fractalType);
        _noise.SetFractalOctaves(_fractalOctaves);
        _noise.SetFractalLacunarity(_fractalLacunarity);
        _noise.SetFractalGain(_fractalGain);
        _noise.SetFractalWeightedStrength(_fractalWeightedStrength);
        _noise.SetFractalPingPongStrength(_fractalPingPongStrength);
        _noise.SetCellularDistanceFunction((Backend.CellularDistanceFunction)_cellularDistanceFunction);
        _noise.SetCellularReturnType((Backend.CellularReturnType)_cellularReturnType);
        _noise.SetCellularJitter(_cellularJitter);
        _warp.SetDomainWarpType((Backend.DomainWarpType)_domainWarpType);
        _warp.SetDomainWarpAmp(_domainWarpAmplitude);
        _warp.SetFrequency(_domainWarpFrequency);
        _warp.SetFractalType(_domainWarpFractalType switch { DomainWarpFractalType.None => Backend.FractalType.None, DomainWarpFractalType.Progressive => Backend.FractalType.DomainWarpProgressive, _ => Backend.FractalType.DomainWarpIndependent });
        _warp.SetFractalOctaves(_domainWarpFractalOctaves);
        _warp.SetFractalLacunarity(_domainWarpFractalLacunarity);
        _warp.SetFractalGain(_domainWarpFractalGain);
    }
}
