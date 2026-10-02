namespace Electron2D;

/// <summary>Plays borrowed non-spatial streams through native audio buses.</summary>
/// <remarks>Node configuration and events require the scene owner thread. Stream mixing runs on the native audio
/// thread through bounded prepared buffers. The node owns its playbacks/voices and never disposes its stream.</remarks>
public class AudioStreamPlayer : Node
{
    /// <summary>Selects where non-spatial stereo audio is routed.</summary>
    public enum MixTarget
    {
        /// <summary>Route to left/right.</summary>
        Stereo = 0,
        /// <summary>Route front, center/low-frequency and supported rear stereo pairs.</summary>
        Surround = 1,
        /// <summary>Route the left input to center and right input to low-frequency output when present.</summary>
        Center = 2
    }
    private AudioStream? _stream;
    private bool _audioOperation;
    private readonly Dictionary<PropertyDescriptor, StoredPropertyValue> _parameters = [];
    private bool _autoplay;
    private volatile bool _registered;
    private string _bus = "Master";
    private float _volumeDB, _pitch = 1;
    private int _maximum = 1;
    private MixTarget _mixTarget;
    private bool _spatial;
    private float _spatialLeft, _spatialRight;
    private AudioServer.PlaybackType _type;
    private readonly List<FAudioStreamVoice> _voices = [];
    private readonly List<long> _ages = [];
    private long _sequence;
    /// <summary>Creates an empty player with one voice capacity, Master bus and unity gain/rate.</summary>
    public AudioStreamPlayer() { }
    private void Check() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); if (_registered) AudioServer.Instance.Check(); }
    /// <summary>Gets or sets the borrowed stream.</summary>
    /// <value>Null initially. Replacement stops current voices before preparing the new resource.</value>
    /// <exception cref="ObjectDisposedException">The player or assigned stream is disposed.</exception>
    /// <exception cref="InvalidOperationException">Mutation is off-owner/capture-owned.</exception>
    public AudioStream? Stream
    {
        get { Check(); return _stream; }
        set { EnsureMutable(); if (value is { IsDisposed: true }) throw new ObjectDisposedException(nameof(value)); if (ReferenceEquals(_stream, value)) return; ReleaseVoices(); _parameters.Clear(); _stream = value; }
    }
    /// <summary>Gets or sets Autoplay configuration.</summary>
    /// <value>False; actual EnterTree starts a configured stream.</value>
    /// <exception cref="ArgumentOutOfRangeException">The numeric or enum value is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner/capture-owned or native configuration fails.</exception>
    /// <exception cref="ObjectDisposedException">The player is disposed.</exception>
    public bool Autoplay
    {
        get { Check(); return _autoplay; }
        set { EnsureMutable(); if (_autoplay == value) return; _autoplay = value; }
    }
    /// <summary>Gets or sets StreamPaused configuration.</summary>
    /// <value>False without active voices; pauses/resumes existing voices without advancing cursors. New Play uses tree pause policy.</value>
    /// <exception cref="ArgumentOutOfRangeException">The numeric or enum value is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner/capture-owned or native configuration fails.</exception>
    /// <exception cref="ObjectDisposedException">The player is disposed.</exception>
    public bool StreamPaused
    {
        get { Check(); foreach (var voice in _voices) if (voice.Playing) return voice.Paused; return false; }
        set { EnsureMutable(); foreach (var voice in _voices) if (voice.Playing) voice.Pause(value || !IsInsideTree || !CanProcess()); }
    }
    /// <summary>Gets or sets Bus configuration.</summary>
    /// <value>Master initially; absent names query and route as Master while retaining their requested identity.</value>
    /// <exception cref="ArgumentOutOfRangeException">The numeric or enum value is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner/capture-owned or native configuration fails.</exception>
    /// <exception cref="ObjectDisposedException">The player is disposed.</exception>
    public string Bus
    {
        get { Check(); return AudioServer.Instance.GetBusIndex(_bus) >= 0 ? _bus : "Master"; }
        set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); if (_bus == value) return; _bus = value; foreach (var voice in _voices) { voice.SetSend(AudioServer.Instance.ResolveBus(value)); voice.SetMixTarget(_mixTarget); } RefreshVolume(); }
    }
    /// <summary>Gets or sets VolumeDB configuration.</summary>
    /// <value>Zero dB initially; negative infinity silences volume-scaled channels. The center/surround low-frequency route retains unity player gain.</value>
    /// <exception cref="ArgumentOutOfRangeException">The numeric or enum value is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner/capture-owned or native configuration fails.</exception>
    /// <exception cref="ObjectDisposedException">The player is disposed.</exception>
    public float VolumeDB
    {
        get { Check(); return _volumeDB; }
        set { EnsureMutable(); if (float.IsNaN(value) || float.IsPositiveInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value)); if (_volumeDB == value) return; _volumeDB = value; RefreshVolume(); }
    }
    /// <summary>Gets or sets PitchScale configuration.</summary>
    /// <value>One initially; finite strictly positive values only.</value>
    /// <exception cref="ArgumentOutOfRangeException">The numeric or enum value is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner/capture-owned or native configuration fails.</exception>
    /// <exception cref="ObjectDisposedException">The player is disposed.</exception>
    public float PitchScale
    {
        get { Check(); return _pitch; }
        set { EnsureMutable(); if (!float.IsFinite(value) || value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_pitch == value) return; _pitch = value; RefreshPitch(); }
    }
    /// <summary>Gets or sets MaxPolyphony configuration.</summary>
    /// <value>One initially; oldest voices are replaced when all slots are active.</value>
    /// <exception cref="ArgumentOutOfRangeException">The numeric or enum value is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner/capture-owned or native configuration fails.</exception>
    /// <exception cref="ObjectDisposedException">The player is disposed.</exception>
    public int MaxPolyphony
    {
        get { Check(); return _maximum; }
        set { EnsureMutable(); if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); if (_maximum == value) return; _maximum = value; }
    }
    /// <summary>Gets or sets MixTargetMode configuration.</summary>
    /// <value>Stereo initially; undefined values reject.</value>
    /// <exception cref="ArgumentOutOfRangeException">The numeric or enum value is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner/capture-owned or native configuration fails.</exception>
    /// <exception cref="ObjectDisposedException">The player is disposed.</exception>
    public MixTarget MixTargetMode
    {
        get { Check(); return _mixTarget; }
        set { EnsureMutable(); if (value is < MixTarget.Stereo or > MixTarget.Center) throw new ArgumentOutOfRangeException(nameof(value)); if (_mixTarget == value) return; _mixTarget = value; foreach (var voice in _voices) voice.SetMixTarget(value); }
    }
    /// <summary>Gets or sets PlaybackType configuration.</summary>
    /// <value>Default initially; explicit native sample playback requires a sample-capable stream.</value>
    /// <exception cref="ArgumentOutOfRangeException">The numeric or enum value is invalid.</exception>
    /// <exception cref="InvalidOperationException">Access is off-owner/capture-owned or native configuration fails.</exception>
    /// <exception cref="ObjectDisposedException">The player is disposed.</exception>
    public AudioServer.PlaybackType PlaybackType
    {
        get { Check(); return _type; }
        set { EnsureMutable(); if (value is < AudioServer.PlaybackType.Default or >= AudioServer.PlaybackType.Max) throw new ArgumentOutOfRangeException(nameof(value)); if (_type == value) return; _type = value; ReleaseVoices(); }
    }
    /// <summary>Gets or sets linear gain.</summary>
    /// <value>One initially; zero maps to negative infinity dB.</value>
    /// <exception cref="ArgumentOutOfRangeException">The gain is negative or nonfinite.</exception>
    public float VolumeLinear { get => (float)Mathf.DBToLinear(VolumeDB); set { if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); VolumeDB = (float)Mathf.LinearToDB(value); } }
    /// <summary>Gets or sets whether any voice is playing.</summary>
    /// <value>Setting true starts at zero; setting false stops all voices.</value>
    public bool Playing { get => IsPlaying(); set { if (value) Play(); else Stop(); } }
    /// <summary>Occurs when one or more voices complete naturally during an owner-thread frame.</summary>
    /// <remarks>Explicit Stop and stream replacement do not emit Finished.</remarks>
    public event Action? Finished;
    private AudioStreamPlayback PreparePlayback()
    {
        var playback = _stream!.InstantiatePlayback();
        try { foreach (var parameter in _parameters) parameter.Key.RestoreStoredValue(playback, parameter.Value, static resource => resource); return playback; }
        catch { playback.Dispose(); throw; }
    }
    private FAudioStreamVoice CreateVoice(AudioStreamPlayback playback)
    {
        var server = AudioServer.Instance; var voice = server.Native.CreateStream(playback, server.ResolveBus(_bus));
        try { voice.SetPitch(_pitch); voice.SetVolume((float)Mathf.DBToLinear(_volumeDB), server.ResolveSourceGain(_bus, 1)); voice.SetMixTarget(_mixTarget); if (_spatial) voice.SetSpatial(_spatialLeft, _spatialRight); return voice; }
        catch { voice.Dispose(); throw; }
    }
    /// <summary>Creates one fresh playback and starts a voice at a requested stream time, replacing the oldest when capacity is full.</summary>
    /// <remarks>Native slots are prepared lazily and reused. A replaced slot disposes its old borrowed playback handle.
    /// Stream callbacks cannot reenter player mutation or disposal while an audio operation is in progress.</remarks>
    /// <param name="fromPosition">Finite seconds; zero initially.</param>
    /// <exception cref="InvalidOperationException">The node is detached or native output is unavailable.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The time is nonfinite.</exception>
    public void Play(double fromPosition = 0)
    {
        EnsureMutable(); if (!IsInsideTree) throw new InvalidOperationException("Audio playback requires an attached player."); if (!double.IsFinite(fromPosition)) throw new ArgumentOutOfRangeException(nameof(fromPosition)); if (_stream is null) return;
        if (_type == AudioServer.PlaybackType.Sample) throw new NotSupportedException("Prepared sample-driver playback requires its separate native sample storage integration.");
        _audioOperation = true;
        try
        {
            if (_stream.IsMonophonic()) StopVoices();
            TrimVoices(); var index = -1;
            for (var i = 0; i < _voices.Count; i++) if (!_voices[i].Playing) { index = i; break; }
            if (index < 0 && _voices.Count >= _maximum) { index = 0; for (var i = 1; i < _voices.Count; i++) if (_ages[i] < _ages[index]) index = i; }
            var playback = PreparePlayback(); FAudioStreamVoice? voice = index < 0 ? null : _voices[index];
            try
            {
                if (voice is null) { voice = CreateVoice(playback); index = _voices.Count; _voices.Add(voice); _ages.Add(0); }
                else voice.ReplacePlayback(playback);
            }
            catch { if (voice is null || !ReferenceEquals(voice.Playback, playback)) playback.Dispose(); throw; }
            _ages[index] = checked(++_sequence); voice.Play(fromPosition); voice.Pause(!CanProcess());
        }
        finally { _audioOperation = false; }
    }
    /// <summary>Stops active voices and starts one voice at the requested time.</summary>
    /// <param name="toPosition">Finite seconds.</param>
    public void Seek(double toPosition) { EnsureMutable(); if (!double.IsFinite(toPosition)) throw new ArgumentOutOfRangeException(nameof(toPosition)); if (IsPlaying()) { Stop(); Play(toPosition); } }
    /// <summary>Stops all voices without emitting Finished.</summary>
    public void Stop()
    {
        EnsureMutable(); _audioOperation = true; try { StopVoices(); } finally { _audioOperation = false; }
    }
    private void StopVoices()
    {
        List<Exception>? errors = null;
        foreach (var voice in _voices) try { StopVoice(voice); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Audio stopping failed.", errors);
    }
    private void StopVoice(FAudioStreamVoice voice)
    {
        var previous = _audioOperation; _audioOperation = true; try { voice.Stop(); } finally { _audioOperation = previous; }
    }
    /// <summary>Gets whether any voice is actively playing rather than paused.</summary>
    /// <returns>False for an empty/stopped player.</returns>
    public bool IsPlaying() { Check(); foreach (var voice in _voices) if (voice.Playing && !voice.Paused) return true; return false; }
    /// <summary>Gets the current position of the most recently started voice.</summary>
    /// <returns>Concrete stream seconds including resampling prefetch; zero without an active/paused voice.</returns>
    public double GetPlaybackPosition() { Check(); var index = LastActive(); return index < 0 ? 0 : _voices[index].Position; }
    /// <summary>Gets whether a prepared stream playback is available.</summary>
    /// <returns>True while an active or paused voice exists.</returns>
    public bool HasStreamPlayback() { Check(); return LastActive() >= 0; }
    /// <summary>Returns the most recently prepared borrowed stream playback.</summary>
    /// <returns>The active player-owned playback; stopped pool slots are not exposed.</returns>
    /// <remarks>The handle becomes disposed when its slot is replaced, removed or released. Bus graph edits retain it.</remarks>
    /// <exception cref="InvalidOperationException">No playback is prepared.</exception>
    public AudioStreamPlayback GetStreamPlayback() { Check(); if (!HasStreamPlayback()) throw new InvalidOperationException("No active stream playback is available."); return _voices[LastActive()].Playback; }
    private int LastActive()
    {
        var result = -1;
        for (var i = 0; i < _voices.Count; i++) if (_voices[i].Playing && (result < 0 || _ages[i] > _ages[result])) result = i;
        return result;
    }
    /// <summary>Writes a declared typed stream parameter and applies it to all prepared voices.</summary>
    /// <typeparam name="TPlayback">Playback owner type declared by the parameter.</typeparam>
    /// <typeparam name="TValue">Exact parameter value type.</typeparam>
    /// <param name="parameter">A descriptor returned by the current stream parameter list.</param>
    /// <param name="value">Typed value; null can restore a nullable parameter's stream default.</param>
    /// <exception cref="InvalidOperationException">The descriptor is undeclared or playback types disagree.</exception>
    /// <exception cref="ObjectDisposedException">The player or stream is disposed.</exception>
    public void SetParameter<TPlayback, TValue>(PropertyDescriptor<TPlayback, TValue> parameter, TValue value) where TPlayback : AudioStreamPlayback
    {
        EnsureMutable(); ValidateParameter(parameter);
        if (_voices.Count == 0)
        {
            using var temporary = _stream!.InstantiatePlayback();
            if (temporary is not TPlayback owner) throw new InvalidOperationException("Parameter playback type differs from the stream.");
            parameter.SetValue(owner, value);
        }
        else
        {
            lock (AudioServer.Instance.Native.Gate)
            {
                foreach (var voice in _voices) if (voice.Playback is not TPlayback) throw new InvalidOperationException("Parameter playback type differs from the stream.");
                foreach (var voice in _voices) parameter.SetValue((TPlayback)voice.Playback, value);
            }
        }
        _parameters[parameter] = new StoredPropertyValue<TValue>(value);
    }
    /// <summary>Reads an authored typed parameter or the stream's declared revert value.</summary>
    /// <typeparam name="TPlayback">Declared playback owner type.</typeparam>
    /// <typeparam name="TValue">Exact parameter value type.</typeparam>
    /// <param name="parameter">A descriptor returned by the current stream parameter list.</param>
    /// <returns>The authored value, or the configured typed revert value before an override exists.</returns>
    /// <exception cref="InvalidOperationException">The descriptor, playback type or default is unavailable.</exception>
    /// <exception cref="ObjectDisposedException">The player or stream is disposed.</exception>
    public TValue GetParameter<TPlayback, TValue>(PropertyDescriptor<TPlayback, TValue> parameter) where TPlayback : AudioStreamPlayback
    {
        Check(); ValidateParameter(parameter);
        if (_parameters.TryGetValue(parameter, out var stored) && stored.TryGetValue<TValue>(out var value)) return value;
        using var temporary = _stream!.InstantiatePlayback();
        if (temporary is not TPlayback owner || !parameter.TryGetRevertValue(owner, out var result)) throw new InvalidOperationException("The typed parameter default is unavailable.");
        return result;
    }
    private void ValidateParameter(PropertyDescriptor parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);
        if (_stream is null || !_stream.GetParameterList().Contains(parameter) || parameter.IsReadOnly) throw new InvalidOperationException("The stream does not declare this writable parameter.");
    }
    internal void RefreshVolume() { var gate = AudioServer.Instance.ResolveSourceGain(_bus, 1); foreach (var voice in _voices) voice.SetVolume((float)Mathf.DBToLinear(_volumeDB), gate); }
    internal void ConfigureSpatial(float left, float right)
    {
        if (!float.IsFinite(left) || !float.IsFinite(right) || left < 0 || right < 0)
            throw new ArgumentOutOfRangeException(nameof(left));
        _spatial = true; _spatialLeft = left; _spatialRight = right;
        foreach (var voice in _voices) voice.SetSpatial(left, right);
    }
    internal void RefreshPitch() { foreach (var voice in _voices) voice.SetPitch(_pitch); }
    internal void RefreshRouting()
    {
        foreach (var voice in _voices) { voice.SetSend(AudioServer.Instance.ResolveBus(_bus)); voice.SetMixTarget(_mixTarget); }
    }
    private void TrimVoices()
    {
        while (_voices.Count > _maximum)
        {
            var index = 0; for (var i = 1; i < _voices.Count; i++) if (_ages[i] < _ages[index]) index = i;
            var voice = _voices[index]; voice.Dispose(); voice.Playback.Dispose(); _voices.RemoveAt(index); _ages.RemoveAt(index);
        }
    }
    internal void ReleaseVoices()
    {
        var previous = _audioOperation; _audioOperation = true; try { ReleaseVoiceStorage(); } finally { _audioOperation = previous; }
    }
    private void ReleaseVoiceStorage()
    {
        List<Exception>? errors = null;
        foreach (var voice in _voices)
        {
            try { voice.Dispose(); } catch (Exception error) { CollectException(ref errors, error); }
            try { voice.Playback.Dispose(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        _voices.Clear(); _ages.Clear();
        ThrowCollected("Audio playback cleanup failed.", errors);
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        List<Exception>? errors = null;
        try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        if (!IsDisposed) try
            {
                if (what == NotificationEnterTree)
                {
                    if (!_registered) { AudioServer.Instance.Attach(this); _registered = true; }
                    SetInternalProcessing(true, false);
                    foreach (var voice in _voices) if (voice.Playing) voice.Pause(!CanProcess());
                    if (_autoplay) Play();
                }
                else if (what == NotificationExitTree) { foreach (var voice in _voices) if (voice.Playing) voice.Pause(true); }
                else if (what is NotificationPaused or NotificationUnpaused or NotificationDisabled or NotificationEnabled)
                { foreach (var voice in _voices) if (voice.Playing) voice.Pause(!CanProcess()); }
                else if (what == NotificationInternalProcess)
                {
                    var ended = false;
                    foreach (var voice in _voices) if (voice.Playing)
                        {
                            try { if (voice.Finished) { StopVoice(voice); ended = true; } }
                            catch (Exception error) { try { StopVoice(voice); } catch (Exception cleanup) { CollectException(ref errors, cleanup); } CollectException(ref errors, error); }
                        }
                    if (ended) Finished?.Invoke();
                }
            }
            catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Audio player notification callbacks failed.", errors);
    }

    private static readonly PropertyDescriptor[] PlayerProperties =
    [
        new PropertyDescriptor<AudioStreamPlayer, AudioStream?>(nameof(Stream), p => p.Stream, (p, v) => p.Stream = v, _ => null, stored: true),
        new PropertyDescriptor<AudioStreamPlayer, bool>(nameof(Autoplay), p => p.Autoplay, (p, v) => p.Autoplay = v, _ => false, stored: true),
        new PropertyDescriptor<AudioStreamPlayer, bool>(nameof(StreamPaused), p => p.StreamPaused, (p, v) => p.StreamPaused = v, _ => false, stored: true),
        new PropertyDescriptor<AudioStreamPlayer, string>(nameof(Bus), p => p.Bus, (p, v) => p.Bus = v, _ => "Master", stored: true),
        new PropertyDescriptor<AudioStreamPlayer, float>(nameof(VolumeDB), p => p.VolumeDB, (p, v) => p.VolumeDB = v, _ => 0, stored: true),
        new PropertyDescriptor<AudioStreamPlayer, float>(nameof(PitchScale), p => p.PitchScale, (p, v) => p.PitchScale = v, _ => 1, stored: true),
        new PropertyDescriptor<AudioStreamPlayer, int>(nameof(MaxPolyphony), p => p.MaxPolyphony, (p, v) => p.MaxPolyphony = v, _ => 1, stored: true),
        new PropertyDescriptor<AudioStreamPlayer, MixTarget>(nameof(MixTargetMode), p => p.MixTargetMode, (p, v) => p.MixTargetMode = v, _ => MixTarget.Stereo, stored: true),
        new PropertyDescriptor<AudioStreamPlayer, AudioServer.PlaybackType>(nameof(PlaybackType), p => p.PlaybackType, (p, v) => p.PlaybackType = v, _ => AudioServer.PlaybackType.Default, stored: true),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors()
    {
        var properties = base.GetPropertyDescriptors().Concat(PlayerProperties);
        return _stream?.GetParameterList().Contains(AudioStreamPlayback.LoopingParameter) == true ? properties.Append(LoopParameterProperty) : properties;
    }
    private static readonly PropertyDescriptor<AudioStreamPlayer, bool?> LoopParameterProperty = new("Parameters/LoopingOverride", p => p.GetParameter(AudioStreamPlayback.LoopingParameter), (p, v) => p.SetParameter(AudioStreamPlayback.LoopingParameter, v), _ => null, stored: true);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => GetType() == typeof(AudioStreamPlayer) ? CreatePlayer : base.CreateSceneInstanceFactory();
    private static Node CreatePlayer() => new AudioStreamPlayer();
    /// <inheritdoc />
    protected override void ValidateMutation() { base.ValidateMutation(); if (_audioOperation) throw new InvalidOperationException("Audio playback operations do not allow reentrant player mutation."); if (_registered) AudioServer.Instance.Check(); }
    /// <inheritdoc />
    /// <remarks>A detached player retaining audio registration still requires its configuration owner.</remarks>
    protected override void ValidateDisposal() { base.ValidateDisposal(); if (_audioOperation) throw new InvalidOperationException("Audio playback operations do not allow reentrant player disposal."); if (_registered) AudioServer.Instance.Check(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        List<Exception>? errors = null;
        if (disposing)
        {
            try { ReleaseVoices(); } catch (Exception error) { CollectException(ref errors, error); }
            if (_registered) { _registered = false; try { AudioServer.Instance.Detach(this); } catch (Exception error) { CollectException(ref errors, error); } }
            _parameters.Clear(); _stream = null; Finished = null;
        }
        try { base.Dispose(disposing); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Audio player teardown failed.", errors);
    }
}
