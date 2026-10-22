namespace Electron2D;

/// <summary>Authors a scene-owned GPU particle emitter with native simulation and canvas output.</summary>
/// <remarks>Configuration and stored scenes need no rendering session. Native processing requires the GPU renderer.
/// Textures and materials remain borrowed. The renderer owns transient particle storage for its current session.</remarks>
public partial class GPUParticles : Entity
{
    private readonly ArrayMesh _mesh = new();
    private readonly RandomNumberGenerator _random = new();
    private RenderingServer? _server;
    private RID _particles;
    private bool _preparing, _meshDirty = true, _emitting = true, _active, _signalCanceled, _oneShot, _sceneStarted;
    private double _time, _emissionTime, _activeTime;
    private int _amount = 8, _fixedFPS = 30, _trailSections = 8, _trailSectionSubdivisions = 4;
    private double _lifetime = 1, _preprocess, _speedScale = 1, _trailLifetime = .3;
    private float _amountRatio = 1, _explosiveness, _randomness, _interpToEnd, _collisionBaseSize = 1;
    private bool _fractDelta = true, _interpolate = true, _localCoords, _useFixedSeed, _trailEnabled;
    private uint _seed;
    private Rect2 _visibilityRect = new(-100, -100, 200, 200);
    private ParticlesDrawOrder _drawOrder = ParticlesDrawOrder.Lifetime;
    private Texture? _texture;
    private Material? _processMaterial;
    private string _subEmitter = string.Empty;
    private Vector2 _previousPosition, _previousVelocity;
    private Transform _appliedEmission;
    private float _appliedSpeed = float.NaN;
    private bool NativeLive => _particles.IsValid() && ReferenceEquals(_server, RenderingServer.Service);
    /// <summary>Creates eight emitting particles with a one-second lifetime and no native allocation.</summary>
    public GPUParticles() { _seed = _random.Randi(); NotifyTransformChanges = true; SetInternalProcessing(true, true); }
    private void Read() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }
    private void Edit() { EnsureMutable(); if (_preparing) throw new InvalidOperationException("Particle configuration cannot change during preparation."); }
    private T Read<T>(ref T value) { Read(); return value; }
    private void Assign<T>(ref T field, T value, Action<RID, T>? apply = null, bool mesh = false)
    {
        Edit(); if (EqualityComparer<T>.Default.Equals(field, value)) return;
        if (NativeLive) apply?.Invoke(_particles, value);
        field = value; if (mesh) { _meshDirty = true; InvalidateCanvas(); }
    }
    private static double Number(double value, bool positive = false)
    { if (!double.IsFinite(value) || value < 0 || value > float.MaxValue || positive && (float)value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); return value; }
    private static float Ratio(float value)
    { if (!float.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); return Math.Clamp(value, 0, 1); }
    /// <summary>Occurs once when an uncancelled one-shot emission reaches its scene-process completion time.</summary>
    /// <remarks>State commits before handlers run. Handlers may restart or dispose the emitter; exceptions do not replay the event.</remarks>
    public event Action? Finished;
    /// <summary>Gets or sets emission, restarting one-shot completion tracking for an idle emitter.</summary>
    /// <value>True initially. Stopping emission allows existing particles to drain.</value>
    public bool Emitting
    {
        get => Read(ref _emitting);
        set
        {
            Edit();
            if (NativeLive) RenderingServer.ParticlesSetEmitting(_particles, value);
            if (value && value != _emitting && !_useFixedSeed && _oneShot) Seed = _random.Randi();
            if (value && _oneShot)
            {
                if (!_active && !_emitting) BeginCycle(); else _signalCanceled = true;
            }
            _emitting = value; SetInternalProcessing(_oneShot, true);
        }
    }
    /// <summary>Gets or sets whether emission stops after one cycle.</summary>
    /// <value>False initially.</value>
    public bool OneShot
    {
        get => Read(ref _oneShot);
        set
        {
            Edit(); if (NativeLive) { RenderingServer.ParticlesSetOneShot(_particles, value); if (_emitting && !value) RenderingServer.ParticlesRestart(_particles); }
            var starts = value && !_oneShot && _emitting && IsInsideTree;
            _oneShot = value; if (starts) BeginCycle(); SetInternalProcessing(value && (_emitting || _active), true);
        }
    }
    private void BeginCycle() { _active = true; _time = 0; _signalCanceled = false; _emissionTime = _lifetime; _activeTime = _lifetime * (2 - _explosiveness); }
    /// <summary>Gets or sets the borrowed ordinary particle texture.</summary>
    /// <value>Null uses a one-pixel white quad.</value>
    public Texture? Texture
    {
        get => Read(ref _texture);
        set
        {
            Edit(); if (value is not null) ObjectDisposedException.ThrowIf(value.IsDisposed, value);
            if (ReferenceEquals(value, _texture)) return;
            if (NativeLive) RenderingServer.ParticlesSetCollisionBaseSize(_particles, CollisionRadius(value));
            if (_texture is not null) _texture.Changed -= TextureChanged;
            _texture = value; if (value is not null) value.Changed += TextureChanged;
            _meshDirty = true; InvalidateCanvas();
        }
    }
    private void TextureChanged(Resource _) { if (!IsDisposed) { _meshDirty = true; InvalidateCanvas(); } }
    /// <summary>Gets or sets the borrowed built-in or authored particle compute material.</summary>
    /// <value>Null uses the renderer's default process.</value>
    public Material? ProcessMaterial
    {
        get => Read(ref _processMaterial);
        set
        {
            Edit(); if (value is not null) { ObjectDisposedException.ThrowIf(value.IsDisposed, value); if (value is not ParticleProcessMaterial and not ShaderMaterial || value.GetCanvasState() is { } state && state.Program.Mode != Shader.Mode.Particles) throw new NotSupportedException("Particle processing requires a particle compute material."); }
            Assign(ref _processMaterial, value, static (rid, material) => RenderingServer.ParticlesSetProcessMaterial(rid, material?.GetRID() ?? default));
        }
    }
    /// <summary>Gets or sets the relative path to another GPU emitter receiving subparticles.</summary>
    /// <value>Empty initially. Missing and wrong-kind targets remain unbound; native cycles reject.</value>
    public string SubEmitter
    {
        get => Read(ref _subEmitter);
        set { Edit(); ArgumentNullException.ThrowIfNull(value); if (NativeLive) BindSubEmitter(value); _subEmitter = value; }
    }
    /// <summary>Gets or sets local simulation coordinates instead of retained world positions.</summary>
    /// <value>False initially.</value>
    public bool LocalCoords
    {
        get => Read(ref _localCoords);
        set { Assign(ref _localCoords, value, RenderingServer.ParticlesSetUseLocalCoordinates); if (NativeLive) RenderingServer.ParticlesSetEmissionTransform(_particles, GetGlobalTransform()); }
    }
    /// <summary>Gets or sets the independent restart seed policy.</summary>
    /// <value>False initially; Restart preserves Seed when this is true.</value>
    public bool UseFixedSeed
    {
        get => Read(ref _useFixedSeed);
        set { var old = _useFixedSeed; Assign(ref _useFixedSeed, value); if (old != value) NotifyPropertyListChanged(); }
    }
    /// <summary>Gets or sets automatic simulation time scale.</summary>
    /// <value>One initially; zero pauses simulation independently of scene-process completion timing.</value>
    public double SpeedScale
    {
        get => Read(ref _speedScale);
        set { Number(value); Edit(); if (NativeLive) RenderingServer.ParticlesSetSpeedScale(_particles, IsInsideTree && !CanProcess() ? 0 : (float)value); _speedScale = value; }
    }
    /// <summary>Gets or sets the particle collision radius multiplier relative to the texture's mean half size.</summary>
    /// <value>One initially.</value>
    public float CollisionBaseSize
    {
        get => Read(ref _collisionBaseSize);
        set { Number(value); Edit(); if (NativeLive) RenderingServer.ParticlesSetCollisionBaseSize(_particles, CollisionRadius(_texture, value)); _collisionBaseSize = value; }
    }
    /// <summary>Gets or sets segmented native history and velocity alignment.</summary>
    /// <value>False initially.</value>
    public bool TrailEnabled
    {
        get => Read(ref _trailEnabled);
        set { Edit(); if (NativeLive) { RenderingServer.ParticlesSetTrails(_particles, value, (float)_trailLifetime); RenderingServer.ParticlesSetTransformAlign(_particles, value ? RenderingServer.ParticlesTransformAlign.YToVelocity : RenderingServer.ParticlesTransformAlign.Disabled); } _trailEnabled = value; _meshDirty = true; InvalidateCanvas(); }
    }
    /// <summary>Gets or sets trail history duration in seconds.</summary>
    /// <value>0.3 initially; values below 0.01 reject, and native storage caps at ten seconds.</value>
    public double TrailLifetime
    {
        get => Read(ref _trailLifetime);
        set { Number(value); if (value < .01) throw new ArgumentOutOfRangeException(nameof(value)); Edit(); if (NativeLive) RenderingServer.ParticlesSetTrails(_particles, _trailEnabled, (float)value); _trailLifetime = value; }
    }
}
