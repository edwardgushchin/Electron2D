namespace Electron2D;

/// <summary>Plays borrowed streams with scene-position attenuation and stereo panning.</summary>
/// <remarks>A private internal <see cref="AudioStreamPlayer"/> child owns playback and native voices.
/// This spatial node borrows its stream and
/// uses its viewport's listener and scene areas for routing. Current root viewports listen from their client center unless an AudioListener is selected.</remarks>
public sealed class AudioStreamEmitter : Entity
{
    private readonly AudioStreamPlayer _player = new();
    internal AudioStreamPlayer AnimationPlayer => _player;
    internal void RefreshAnimationSpatial() => RefreshSpatial(queryArea: true);
    private string _bus = "Master";
    private bool _autoplay, _autoplayPending;
    private uint _areaMask;
    private float _maxDistance = 2000, _attenuation = 1, _panningStrength = 1;
    private readonly float _globalPanningStrength;

    /// <summary>Creates a detached spatial player with Master routing and one voice.</summary>
    public AudioStreamEmitter()
    {
        _globalPanningStrength = ProjectSettings.GetWithOverride(ProjectSettings.AudioGeneral2DPanningStrength);
        _player.Name = "SpatialPlayback";
        _player.Finished += () => Finished?.Invoke();
        AddChild(_player, InternalMode.Back);
    }

    private void Check() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); }

    /// <summary>Gets or sets the borrowed stream; replacement stops prepared voices.</summary>
    public AudioStream? Stream { get { Check(); return _player.Stream; } set { EnsureMutable(); _player.Stream = value; } }
    /// <summary>Gets or sets automatic playback at the first fixed scene step after entry.</summary>
    public bool Autoplay { get { Check(); return _autoplay; } set { EnsureMutable(); _autoplay = value; } }
    /// <summary>Gets or sets the pause state of active voices.</summary>
    /// <remarks>Stream pause prepares a final fade before freezing cursors; native sample pause stops without flushing its buffer.</remarks>
    public bool StreamPaused { get { Check(); return _player.StreamPaused; } set { EnsureMutable(); _player.StreamPaused = value; } }
    /// <summary>Gets or sets the authored bus, before an eligible Area overrides routing.</summary>
    /// <value>Master initially; missing names resolve to Master.</value>
    public string Bus
    {
        get { Check(); return AudioServer.GetBusIndex(_bus) >= 0 ? _bus : "Master"; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); _bus = value; if (_areaMask == 0) _player.Bus = value; }
    }
    /// <summary>Gets or sets decibel source gain; negative infinity silences output.</summary>
    /// <remarks>Stream gain edits interpolate over the next native block; native samples update their matrix; unrepresentable linear gain rejects.</remarks>
    public float VolumeDB { get { Check(); return _player.VolumeDB; } set { EnsureMutable(); _player.VolumeDB = value; } }
    /// <summary>Gets or sets nonnegative linear source gain.</summary>
    /// <remarks>Stream edits interpolate over the next native block; native samples update their matrix.</remarks>
    public float VolumeLinear { get { Check(); return _player.VolumeLinear; } set { EnsureMutable(); _player.VolumeLinear = value; } }
    /// <summary>Gets or sets positive finite playback pitch.</summary>
    public float PitchScale { get { Check(); return _player.PitchScale; } set { EnsureMutable(); _player.PitchScale = value; } }
    /// <summary>Gets or sets the positive voice capacity.</summary>
    public int MaxPolyphony { get { Check(); return _player.MaxPolyphony; } set { EnsureMutable(); _player.MaxPolyphony = value; } }
    /// <summary>Gets or sets stream or native-sample playback selection.</summary>
    public AudioServer.PlaybackType PlaybackType { get { Check(); return _player.PlaybackType; } set { EnsureMutable(); _player.PlaybackType = value; } }
    /// <summary>Gets or sets whether any stream voice is playing.</summary>
    public bool Playing { get => IsPlaying(); set { if (value) Play(); else Stop(); } }

    /// <summary>Gets or sets the positive finite maximum audible distance in scene units.</summary>
    /// <value>2000 initially; farther sources are silent.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonpositive or nonfinite.</exception>
    public float MaxDistance
    {
        get { Check(); return _maxDistance; }
        set { EnsureMutable(); if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); _maxDistance = value; }
    }
    /// <summary>Gets or sets the finite distance-attenuation exponent.</summary>
    /// <value>One initially; zero disables distance rolloff within MaxDistance.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    public float Attenuation
    {
        get { Check(); return _attenuation; }
        set { EnsureMutable(); if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _attenuation = value; }
    }
    /// <summary>Gets or sets nonnegative finite spatial stereo-panning strength.</summary>
    /// <value>One initially; zero centers channels.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    public float PanningStrength
    {
        get { Check(); return _panningStrength; }
        set { EnsureMutable(); if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _panningStrength = value; }
    }
    /// <summary>Gets or sets Area collision-layer bits eligible to override the authored bus.</summary>
    /// <value>Zero initially, disabling Area audio routing.</value>
    public uint AreaMask { get { Check(); return _areaMask; } set { EnsureMutable(); _areaMask = value; } }

    /// <summary>Occurs when one or more voices finish naturally on a scene-owner frame.</summary>
    public event Action? Finished;

    /// <summary>Starts a fresh playback at finite stream time.</summary>
    /// <remarks>The streamed path has 64 silent lookahead frames and retains full attack; samples submit a complete native buffer.
    /// Replaced streamed voices retain their prepared outgoing fade.</remarks>
    /// <param name="fromPosition">Start position in seconds, zero by default.</param>
    /// <exception cref="ArgumentOutOfRangeException">The start time is nonfinite.</exception>
    /// <exception cref="InvalidOperationException">Playback lacks an active scene or requires an invertible canvas transform.</exception>
    public void Play(double fromPosition = 0)
    {
        EnsureMutable();
        if (!double.IsFinite(fromPosition)) throw new ArgumentOutOfRangeException(nameof(fromPosition));
        if (_player.Stream is not null) RefreshSpatial(queryArea: true);
        _player.Play(fromPosition);
    }
    /// <summary>Seeks an active voice by restarting it at finite stream time.</summary>
    /// <remarks>The outgoing prepared fade overlaps the new attack; paused players remain unchanged.</remarks>
    /// <param name="toPosition">Target position in seconds.</param>
    /// <exception cref="ArgumentOutOfRangeException">The target time is nonfinite.</exception>
    public void Seek(double toPosition) { EnsureMutable(); _player.Seek(toPosition); }
    /// <summary>Stops all voices without emitting Finished.</summary>
    /// <remarks>Logical playback stops synchronously; one prepared fading PCM block may still reach output.</remarks>
    /// <exception cref="AggregateException">Final-block mixing or source Stop fails after logical playback stops.</exception>
    public void Stop() { EnsureMutable(); _autoplayPending = false; _player.Stop(); }
    /// <summary>Gets whether at least one voice is actively playing.</summary>
    /// <returns>False when detached, stopped or paused.</returns>
    public bool IsPlaying() { Check(); return _player.IsPlaying(); }
    /// <summary>Gets the most recently started voice position in seconds.</summary>
    /// <returns>Zero without an active/paused voice.</returns>
    public double GetPlaybackPosition() { Check(); return _player.GetPlaybackPosition(); }
    /// <summary>Gets whether a prepared playback is currently available.</summary>
    /// <returns>True for an active or paused voice.</returns>
    public bool HasStreamPlayback() { Check(); return _player.HasStreamPlayback(); }
    /// <summary>Returns the most recent borrowed playback handle.</summary>
    /// <returns>A handle invalidated by its slot's replacement or removal.</returns>
    public AudioStreamPlayback GetStreamPlayback() { Check(); return _player.GetStreamPlayback(); }

    /// <summary>Sets a typed parameter declared by the current stream.</summary>
    /// <typeparam name="TPlayback">Declared playback type.</typeparam>
    /// <typeparam name="TValue">Parameter value type.</typeparam>
    /// <param name="parameter">Current stream descriptor.</param>
    /// <param name="value">Typed authored value.</param>
    public void SetParameter<TPlayback, TValue>(PropertyDescriptor<TPlayback, TValue> parameter, TValue value) where TPlayback : AudioStreamPlayback
    { EnsureMutable(); _player.SetParameter(parameter, value); }
    /// <summary>Gets an authored typed parameter or its declared revert value.</summary>
    /// <typeparam name="TPlayback">Declared playback type.</typeparam>
    /// <typeparam name="TValue">Parameter value type.</typeparam>
    /// <param name="parameter">Current stream descriptor.</param>
    /// <returns>The authored or default value.</returns>
    public TValue GetParameter<TPlayback, TValue>(PropertyDescriptor<TPlayback, TValue> parameter) where TPlayback : AudioStreamPlayback
    { Check(); return _player.GetParameter(parameter); }

    internal void RefreshSpatial(bool queryArea)
    {
        if (_player.Stream is null) return;
        var viewport = GetViewport();
        if (viewport is null || !viewport.AudioListenerEnable2D)
        {
            _player.ConfigureSpatial(0, 0);
            return;
        }
        var size = viewport.GetVisibleRect().Size;
        if (size.X <= 0 || size.Y <= 0) { _player.ConfigureSpatial(0, 0); return; }
        var canvas = viewport.GlobalCanvasTransform * viewport.CanvasTransform;
        var position = GlobalPosition;
        var listener = viewport.GetAudioListener2D();
        Vector2 listenerPosition, relative;
        if (listener is not null)
        {
            listenerPosition = listener.GlobalPosition;
            relative = (position - listenerPosition).Rotated(-listener.GlobalRotation) * canvas.Scale;
        }
        else
        {
            if (canvas.Determinant() == 0) throw new InvalidOperationException("Spatial audio needs an invertible canvas transform.");
            listenerPosition = canvas.AffineInverse() * (size * .5f);
            relative = canvas * position - size * .5f;
        }
        var distance = position.DistanceTo(listenerPosition);
        var multiplier = distance > _maxDistance ? 0 : MathF.Pow(MathF.Max(1e-6f, 1 - distance / _maxDistance), _attenuation);
        var pan = Math.Clamp(relative.X / size.X, -1, 1);
        pan = Math.Clamp(pan * _panningStrength * _globalPanningStrength * .5f + .5f, 0, 1);
        var left = (1 - pan) * multiplier; var right = pan * multiplier;
        if (!float.IsFinite(left) || !float.IsFinite(right)) throw new ArithmeticException("Spatial audio gain exceeds finite output.");
        if (queryArea && _areaMask != 0 && Tree is { } tree)
            _player.Bus = tree.ResolveSpatialAudioBus(position, _areaMask, _bus);
        else if (_areaMask == 0) _player.Bus = _bus;
        _player.ConfigureSpatial(left, right);
    }

    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);
        if (what == NotificationEnterTree)
        {
            _autoplayPending = _autoplay;
            SetInternalProcessing(false, true);
        }
        else if (what == NotificationExitTree) _autoplayPending = false;
        else if (what == NotificationInternalPhysicsProcess)
        {
            RefreshSpatial(queryArea: true);
            if (_autoplayPending) { _autoplayPending = false; _player.Play(); }
        }
    }

    private static readonly PropertyDescriptor[] SpatialProperties =
    [
        new PropertyDescriptor<AudioStreamEmitter, AudioStream?>(nameof(Stream), p => p.Stream, (p, v) => p.Stream = v, _ => null, stored: true),
        new PropertyDescriptor<AudioStreamEmitter, bool>(nameof(Autoplay), p => p.Autoplay, (p, v) => p.Autoplay = v, _ => false, stored: true),
        new PropertyDescriptor<AudioStreamEmitter, bool>(nameof(StreamPaused), p => p.StreamPaused, (p, v) => p.StreamPaused = v, _ => false, stored: true),
        new PropertyDescriptor<AudioStreamEmitter, string>(nameof(Bus), p => p.Bus, (p, v) => p.Bus = v, _ => "Master", stored: true),
        new PropertyDescriptor<AudioStreamEmitter, float>(nameof(VolumeDB), p => p.VolumeDB, (p, v) => p.VolumeDB = v, _ => 0, stored: true),
        new PropertyDescriptor<AudioStreamEmitter, float>(nameof(PitchScale), p => p.PitchScale, (p, v) => p.PitchScale = v, _ => 1, stored: true),
        new PropertyDescriptor<AudioStreamEmitter, int>(nameof(MaxPolyphony), p => p.MaxPolyphony, (p, v) => p.MaxPolyphony = v, _ => 1, stored: true),
        new PropertyDescriptor<AudioStreamEmitter, AudioServer.PlaybackType>(nameof(PlaybackType), p => p.PlaybackType, (p, v) => p.PlaybackType = v, _ => AudioServer.PlaybackType.Default, stored: true),
        new PropertyDescriptor<AudioStreamEmitter, float>(nameof(MaxDistance), p => p.MaxDistance, (p, v) => p.MaxDistance = v, _ => 2000, stored: true),
        new PropertyDescriptor<AudioStreamEmitter, float>(nameof(Attenuation), p => p.Attenuation, (p, v) => p.Attenuation = v, _ => 1, stored: true),
        new PropertyDescriptor<AudioStreamEmitter, float>(nameof(PanningStrength), p => p.PanningStrength, (p, v) => p.PanningStrength = v, _ => 1, stored: true),
        new PropertyDescriptor<AudioStreamEmitter, uint>(nameof(AreaMask), p => p.AreaMask, (p, v) => p.AreaMask = v, _ => 0, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        var properties = base.GetPropertyDescriptors().Concat(SpatialProperties);
        var parameters = Stream?.GetParameterList();
        if (parameters?.Contains(AudioStreamPlayback.LoopingParameter) == true) properties = properties.Append(LoopParameterProperty);
        if (parameters?.Contains(AudioStreamPlaybackInteractive.SwitchToClipParameter) == true) properties = properties.Append(ClipParameterProperty);
        return properties;
    }
    private static readonly PropertyDescriptor<AudioStreamEmitter, string> ClipParameterProperty = new("Parameters/SwitchToClip", p => p.GetParameter(AudioStreamPlaybackInteractive.SwitchToClipParameter), (p, v) => p.SetParameter(AudioStreamPlaybackInteractive.SwitchToClipParameter, v), _ => string.Empty, stored: true);
    private static readonly PropertyDescriptor<AudioStreamEmitter, bool?> LoopParameterProperty = new("Parameters/LoopingOverride", p => p.GetParameter(AudioStreamPlayback.LoopingParameter), (p, v) => p.SetParameter(AudioStreamPlayback.LoopingParameter, v), _ => null, stored: true);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreateSpatialPlayer;
    private static Node CreateSpatialPlayer() => new AudioStreamEmitter();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing) Finished = null;
        base.Dispose(disposing);
    }
}
