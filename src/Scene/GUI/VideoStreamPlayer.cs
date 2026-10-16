namespace Electron2D;

/// <summary>Displays a borrowed video stream and routes its queued soundtrack through audio buses.</summary>
/// <remarks>The control owns independent decoder/texture/PCM state. Play requires an attached scene;
/// explicit pause and scene pause preserve active status. Source replacement is cold and prepares a
/// complete replacement before retiring the previous playback. Attached operations require the scene owner;
/// prepared detached state may transfer to its initial owner before first Play.</remarks>
public sealed class VideoStreamPlayer : Control
{
    private readonly AudioStreamPlayer _audio = new() { Name = "VideoAudio", MixTargetMode = AudioStreamPlayer.MixTarget.Surround, DeferOutputPreparation = true };
    private VideoStream? _stream;
    private VideoStreamPlayback? _playback;
    private VideoAudioStream? _soundtrack;
    private bool _autoplay, _expand, _loop, _paused, _firstFrame, _audioStarted, _operating;
    private int _audioTrack, _bufferingMSEC = 500;
    private float _speedScale = 1, _volume = 1;
    private string _bus = "Master";
    private bool _replacementCommitted;
    private bool _ownMutation;
    private readonly Action<Resource> _textureChanged;
    private Texture? _trackedTexture;
    private Vector2 _textureSize;
    private int _textureDirty;
    /// <summary>Creates an empty nonexpanding control with one-times speed and Master routing.</summary>
    public VideoStreamPlayer()
    {
        _textureChanged = _ => Volatile.Write(ref _textureDirty, 1);
        MouseFilter = MouseFilter.Ignore;
        AddChild(_audio, InternalMode.Back);
    }
    private void Check() { ThrowIfDisposed(); Tree?.EnsureOwnerThread(); _playback?.BindOwner(this); }
    private T Query<T>(Func<VideoStreamPlayback, T> read, T fallback)
    {
        Check(); if (_playback is not { } playback) return fallback;
        var previous = _operating; _operating = true;
        try { return read(playback); } finally { _operating = previous; }
    }
    private void Begin() { EnsureMutable(); _playback?.BindOwner(this); _operating = true; }
    private void Dirty(bool size = false)
    {
        _ownMutation = true;
        try { if (size) UpdateMinimumSize(); QueueRedraw(); }
        finally { _ownMutation = false; }
    }
    private void SyncTexture()
    {
        var texture = _playback?.GetTexture();
        if (!ReferenceEquals(texture, _trackedTexture))
        {
            if (_trackedTexture is not null) _trackedTexture.Changed -= _textureChanged;
            _trackedTexture = texture; if (texture is not null) texture.Changed += _textureChanged;
            Volatile.Write(ref _textureDirty, 1);
        }
        if (Interlocked.Exchange(ref _textureDirty, 0) == 0) return;
        var size = NaturalSize(texture);
        var changed = size != _textureSize; _textureSize = size; Dirty(changed);
    }
    /// <summary>Gets or sets the borrowed video source.</summary>
    /// <value>Null initially. Replacement stops playback and prepares its first frame and soundtrack queue.</value>
    /// <exception cref="ObjectDisposedException">The control or assigned source is disposed.</exception>
    public VideoStream? Stream
    {
        get { Check(); return _stream; }
        set { Begin(); try { Replace(value); } finally { _operating = false; } }
    }
    private void SourceChanged(Resource _) { Stream = _stream; }
    private void Replace(VideoStream? source, bool preserveTime = false)
    {
        _replacementCommitted = false;
        if (source is { IsDisposed: true }) throw new ObjectDisposedException(nameof(source));
        VideoStreamPlayback? playback = null; VideoAudioStream? soundtrack = null;
        var active = preserveTime && _playback?.IsPlaying() == true;
        try
        {
            playback = source?.InstantiatePlayback();
            if (playback is not null)
            {
                playback.SetAudioTrack(_audioTrack); var channels = playback.GetChannels();
                if (channels > 0) { soundtrack = new(playback.GetMixRate(), channels, _bufferingMSEC); soundtrack.Queue.Gain = _volume; }
                playback.SetPaused(_paused || _speedScale == 0 || !CanProcess());
                _ = playback.GetLength(); _ = NaturalSize(playback.GetTexture());
                if (preserveTime && _playback is { } previousPlayback) playback.RestoreTimingFrom(previousPlayback);
            }
        }
        catch { try { playback?.Dispose(); } finally { soundtrack?.Dispose(); } throw; }
        var previous = _playback; var oldAudio = _soundtrack;
        try { _audio.Stream = null; }
        catch { try { playback?.Dispose(); } finally { soundtrack?.Dispose(); } throw; }
        if (_stream is not null) _stream.Changed -= SourceChanged;
        _audioStarted = false;
        _stream = source; _playback = playback; _soundtrack = soundtrack;
        _replacementCommitted = true;
        if (source is not null) source.Changed += SourceChanged;
        if (playback is not null) { playback.PlayerOwner = this; playback.AudioSink = soundtrack is null ? null : soundtrack.Queue.Push; }
        List<Exception>? errors = null;
        if (preserveTime && previous is not null && playback is not null)
            try { VideoStreamTheora.PreserveFrame(previous, playback); } catch (Exception error) { CollectException(ref errors, error); }
        if (previous is not null) { previous.PlayerOwner = null; previous.AudioSink = null; try { previous.Dispose(); } catch (Exception error) { CollectException(ref errors, error); } }
        try { oldAudio?.Dispose(); } catch (Exception error) { CollectException(ref errors, error); }
        try
        {
            _audio.Stream = soundtrack;
            _firstFrame = true;
            if (active) { StartAudio(); RefreshPause(); }
            SyncTexture(); Dirty(true);
        }
        catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Video replacement cleanup or publication callbacks failed.", errors);
    }
    /// <summary>Gets or sets automatic playback when this control enters the scene.</summary>
    /// <value>False initially.</value>
    public bool Autoplay { get { Check(); return _autoplay; } set { EnsureMutable(); _autoplay = value; } }
    /// <summary>Gets or sets scaling to the complete control rectangle.</summary>
    /// <value>False uses the video dimensions as both drawing size and intrinsic minimum.</value>
    public bool Expand { get { Check(); return _expand; } set { EnsureMutable(); if (_expand == value) return; _expand = value; UpdateMinimumSize(); QueueRedraw(); } }
    /// <summary>Gets or sets automatic restart on natural completion.</summary>
    /// <value>False initially. Looping does not emit Finished.</value>
    public bool Loop { get { Check(); return _loop; } set { EnsureMutable(); _loop = value; } }
    /// <summary>Gets or sets explicit pause, independently of scene pause.</summary>
    /// <value>False initially. Paused playback is still playing.</value>
    public bool Paused
    {
        get { Check(); return _paused; }
        set { Begin(); try { _paused = value; RefreshPause(); } finally { _operating = false; } }
    }
    /// <summary>Gets or sets the nonnegative finite playback speed.</summary>
    /// <value>One initially; zero pauses both video and audio while retaining active status.</value>
    /// <exception cref="ArgumentOutOfRangeException">The speed is negative or nonfinite.</exception>
    public float SpeedScale
    {
        get { Check(); return _speedScale; }
        set { if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); Begin(); try { _speedScale = value; RefreshPause(); } finally { _operating = false; } }
    }
    /// <summary>Gets or sets the embedded audio track selection.</summary>
    /// <value>Zero initially; -1 or a missing track selects silent video.</value>
    /// <remarks>A changed selection prepares replacement decoder/queue state while preserving time, activity, pause
    /// and the built-in current-frame texture identity.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The selection is below -1.</exception>
    public int AudioTrack
    {
        get { Check(); return _audioTrack; }
        set { ArgumentOutOfRangeException.ThrowIfLessThan(value, -1); Begin(); var old = _audioTrack; try { if (old == value) return; _audioTrack = value; Replace(_stream, true); } catch { if (!_replacementCommitted) _audioTrack = old; throw; } finally { _operating = false; } }
    }
    /// <summary>Gets or sets the requested PCM queue duration.</summary>
    /// <value>500 milliseconds initially; a positive duration whose prepared storage fits 2^24 source frames.</value>
    /// <remarks>Changing the duration prepares replacement playback/queue state while preserving time, activity, pause
    /// and the built-in current-frame texture identity.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The duration is nonpositive or requires excessive storage.</exception>
    public int BufferingMSEC
    {
        get { Check(); return _bufferingMSEC; }
        set { ArgumentOutOfRangeException.ThrowIfNegativeOrZero(value); Begin(); var old = _bufferingMSEC; try { if (old == value) return; _bufferingMSEC = value; Replace(_stream, true); } catch { if (!_replacementCommitted) _bufferingMSEC = old; throw; } finally { _operating = false; } }
    }
    /// <summary>Gets or sets the authored destination bus name.</summary>
    /// <value>Master initially; unavailable names route to Master through the ordinary audio transport.</value>
    public string Bus { get { Check(); return _bus; } set { EnsureMutable(); ArgumentNullException.ThrowIfNull(value); _audio.Bus = value; _bus = value; } }
    /// <summary>Gets or sets finite nonnegative linear soundtrack volume.</summary>
    /// <value>One initially; zero silences all routed channels.</value>
    /// <exception cref="ArgumentOutOfRangeException">The value is negative or nonfinite.</exception>
    public float Volume
    {
        get { Check(); return _volume; }
        set { EnsureMutable(); if (!float.IsFinite(value) || value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _volume = value; if (_soundtrack is { } audio) audio.Queue.Gain = value; }
    }
    /// <summary>Gets or sets soundtrack volume in decibels.</summary>
    /// <value>Zero dB initially; negative infinity means silence.</value>
    /// <exception cref="ArgumentOutOfRangeException">The resulting linear gain is not finite.</exception>
    public float VolumeDB { get => (float)Mathf.LinearToDB(Volume); set { if (float.IsNaN(value) || float.IsPositiveInfinity(value)) throw new ArgumentOutOfRangeException(nameof(value)); Volume = (float)Mathf.DBToLinear(value); } }
    /// <summary>Gets or sets the source timestamp.</summary>
    /// <value>Presentation seconds, possibly negative with configured delay. Explicit seeks use uncompensated source seconds;
    /// a position at or beyond the built-in source's duration is ignored.</value>
    /// <remarks>An accepted seek clears queued old PCM and publishes the sought frame without changing active or pause state.
    /// An ignored built-in seek preserves queued audio and interpolation history.</remarks>
    public double StreamPosition
    {
        get => Query(static p => p.GetPlaybackPosition(), 0d);
        set { if (!double.IsFinite(value)) throw new ArgumentOutOfRangeException(nameof(value)); Begin(); try { if (_playback is { } playback && !playback.SeekForPlayer(value)) return; _soundtrack?.Queue.Reset(); _audio.ResetVideoHistory(); Dirty(); } finally { _operating = false; } }
    }
    /// <summary>Returns the assigned stream duration.</summary>
    /// <returns>Seconds, or zero without a stream or for an unknown duration.</returns>
    public double GetStreamLength() => Query(static p => p.GetLength(), 0d);
    /// <summary>Returns the source name.</summary>
    /// <returns>The resource name, or &lt;No Stream&gt; when unassigned.</returns>
    public string GetStreamName() { Check(); return _stream?.ResourceName ?? "<No Stream>"; }
    /// <summary>Returns the borrowed current-frame texture.</summary>
    /// <returns>A decoder-owned texture, or null without a supplied image.</returns>
    public Texture? GetVideoTexture() => Query(static p => p.GetTexture(), (Texture?)null);
    /// <summary>Returns active playback, including explicit and scene pause.</summary>
    /// <returns>True while the assigned decoder remains active.</returns>
    public bool IsPlaying() => Query(static p => p.IsPlaying(), false);
    /// <summary>Starts or restarts the assigned video from the beginning.</summary>
    /// <remarks>Explicit pause remains in effect. A missing stream performs no work.</remarks>
    /// <exception cref="InvalidOperationException">The control is outside a scene tree.</exception>
    public void Play()
    {
        Begin();
        try
        {
            if (!IsInsideTree) throw new InvalidOperationException("Video playback requires an attached control.");
            try { PlayCore(); }
            catch (Exception error)
            {
                List<Exception>? errors = null; CollectException(ref errors, error);
                try { StopCore(); } catch (Exception cleanup) { CollectException(ref errors, cleanup); }
                ThrowCollected("Video start and cleanup callbacks failed.", errors);
            }
        }
        finally { _operating = false; }
    }
    private void PlayCore(double? frameScale = null)
    {
        if (_playback is null) return;
        _soundtrack?.Queue.Reset(); _audio.ResetVideoHistory(); _playback.Play(); _firstFrame = true;
        StartAudio(); RefreshPause(frameScale);
    }
    private void StartAudio()
    {
        if (_soundtrack is null || _audioStarted || !_audio.IsInsideTree) return;
        _audio.Play(); _audioStarted = true;
    }
    /// <summary>Stops playback and rewinds time while retaining the displayed frame.</summary>
    /// <remarks>Explicit stop does not emit Finished.</remarks>
    public void Stop() { Begin(); try { StopCore(); } finally { _operating = false; } }
    private void StopCore()
    {
        List<Exception>? errors = null;
        try { _playback?.Stop(); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (_audioStarted) _audio.StreamPaused = true; } catch (Exception error) { CollectException(ref errors, error); }
        try { _soundtrack?.Queue.Reset(); } catch (Exception error) { CollectException(ref errors, error); }
        try { _audio.ResetVideoHistory(); } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Video stop callbacks failed.", errors);
    }
    private void RefreshPause(double? frameScale = null)
    {
        var paused = _paused || _speedScale == 0 || (frameScale ?? Engine.TimeScale) == 0 || !IsInsideTree || !CanProcess();
        List<Exception>? errors = null;
        try { _playback?.SetPaused(paused); } catch (Exception error) { CollectException(ref errors, error); }
        try { if (_audioStarted) _audio.StreamPaused = errors is not null || paused || _playback?.IsPlaying() != true; } catch (Exception error) { CollectException(ref errors, error); }
        ThrowCollected("Video pause callbacks failed.", errors);
    }
    /// <summary>Occurs once on natural nonlooping completion, on the scene owner.</summary>
    /// <remarks>State has stopped before callbacks. A callback may assign another source or restart playback.</remarks>
    public event Action? Finished;
    private static readonly PropertyDescriptor[] VideoProperties =
    [
        new PropertyDescriptor<VideoStreamPlayer, VideoStream?>(nameof(Stream), p => p.Stream, (p, v) => p.Stream = v, _ => null, stored: true),
        new PropertyDescriptor<VideoStreamPlayer, bool>(nameof(Autoplay), p => p.Autoplay, (p, v) => p.Autoplay = v, _ => false, stored: true),
        new PropertyDescriptor<VideoStreamPlayer, bool>(nameof(Expand), p => p.Expand, (p, v) => p.Expand = v, _ => false, stored: true),
        new PropertyDescriptor<VideoStreamPlayer, bool>(nameof(Loop), p => p.Loop, (p, v) => p.Loop = v, _ => false, stored: true),
        new PropertyDescriptor<VideoStreamPlayer, bool>(nameof(Paused), p => p.Paused, (p, v) => p.Paused = v, _ => false, stored: true),
        new PropertyDescriptor<VideoStreamPlayer, float>(nameof(SpeedScale), p => p.SpeedScale, (p, v) => p.SpeedScale = v, _ => 1, stored: true),
        new PropertyDescriptor<VideoStreamPlayer, int>(nameof(AudioTrack), p => p.AudioTrack, (p, v) => p.AudioTrack = v, _ => 0, stored: true),
        new PropertyDescriptor<VideoStreamPlayer, int>(nameof(BufferingMSEC), p => p.BufferingMSEC, (p, v) => p.BufferingMSEC = v, _ => 500, stored: true),
        new PropertyDescriptor<VideoStreamPlayer, string>(nameof(Bus), p => p.Bus, (p, v) => p.Bus = v, _ => "Master", stored: true),
        new PropertyDescriptor<VideoStreamPlayer, float>(nameof(VolumeDB), p => p.VolumeDB, (p, v) => p.VolumeDB = v, _ => 0, stored: true),
        new PropertyDescriptor<VideoStreamPlayer, float>(nameof(Volume), p => p.Volume, (p, v) => p.Volume = v, _ => 1),
        new PropertyDescriptor<VideoStreamPlayer, double>(nameof(StreamPosition), p => p.StreamPosition, (p, v) => p.StreamPosition = v, _ => 0),
        new PropertyDescriptor<VideoStreamPlayer, MouseFilter>(nameof(MouseFilter), p => p.MouseFilter, (p, v) => p.MouseFilter = v, _ => MouseFilter.Ignore, stored: true)
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Where(p => p.Name != nameof(MouseFilter)).Concat(VideoProperties);
    /// <inheritdoc />
    protected override Func<Node> CreateSceneInstanceFactory() => CreatePlayer;
    private static VideoStreamPlayer CreatePlayer() => new();
    /// <inheritdoc />
    protected override Vector2 OnGetMinimumSize() => _expand ? Vector2.Zero : Query(static p => NaturalSize(p.GetTexture()), Vector2.Zero);
    private static Vector2 NaturalSize(Texture? texture)
    {
        if (texture is { IsDisposed: true }) throw new ObjectDisposedException(nameof(texture));
        var size = texture?.GetSize() ?? Vector2.Zero;
        if (!size.IsFinite() || size.X < 0 || size.Y < 0) throw new InvalidOperationException("Video texture dimensions must be finite and nonnegative.");
        return size;
    }
    private bool TickVideo()
    {
        SyncTexture();
        if (_playback is not { } playback || !playback.IsPlaying()) return false;
        var unscaled = GetUnscaledProcessDelta(false);
        var frameScale = unscaled > 0 ? ProcessDeltaTime / unscaled : Engine.TimeScale;
        StartAudio(); RefreshPause(frameScale);
        if (playback.IsPaused()) return false;
        if (_audioStarted)
        {
            var pitch = frameScale * _speedScale;
            if (!double.IsFinite(pitch) || !float.IsFinite((float)pitch)) throw new InvalidOperationException("Video audio speed exceeds the finite transport range.");
            if (pitch > 0) _audio.PitchScale = (float)pitch;
        }
        var delta = _firstFrame ? 0 : ProcessDeltaTime * _speedScale; _firstFrame = false;
        playback.Update(delta); SyncTexture(); Dirty();
        if (playback.IsPlaying()) return false;
        if (_loop) { PlayCore(frameScale); return false; }
        StopCore(); return true;
    }
    /// <inheritdoc />
    protected override void OnNotification(int what)
    {
        List<Exception>? errors = null;
        if (what == NotificationEnterTree) _playback?.BindOwner(this);
        try { base.OnNotification(what); } catch (Exception error) { CollectException(ref errors, error); }
        var finished = false;
        if (!IsDisposed)
        {
            if (what == NotificationDraw)
            {
                try
                {
                    var (texture, natural) = Query(static p => { var texture = p.GetTexture(); return (texture, NaturalSize(texture)); }, ((Texture?)null, Vector2.Zero));
                    if (texture is not null && natural.X > 0 && natural.Y > 0) DrawTextureRect(texture, new(Vector2.Zero, _expand ? Size : natural), false);
                }
                catch (Exception error) { CollectException(ref errors, error); }
            }
            else
            {
                var previous = _operating; _operating = true;
                try
                {
                    if (what == NotificationEnterTree)
                    {
                        _ownMutation = true; try { SetInternalProcessing(true, false); } finally { _ownMutation = false; }
                        if (_autoplay) PlayCore(); else RefreshPause();
                    }
                    else if (what == NotificationExitTree) StopCore();
                    else if (what is NotificationPaused or NotificationUnpaused or NotificationDisabled or NotificationEnabled) RefreshPause();
                    else if (what == NotificationInternalProcess) finished = TickVideo();
                }
                catch (Exception error)
                {
                    CollectException(ref errors, error);
                    try { StopCore(); } catch (Exception cleanup) { CollectException(ref errors, cleanup); }
                }
                finally { _operating = previous; }
            }
        }
        ThrowCollected("Video notification callbacks failed.", errors);
        if (finished) Finished?.Invoke();
    }
    /// <inheritdoc />
    protected override void ValidateMutation() { base.ValidateMutation(); if (_operating && !_ownMutation) throw new InvalidOperationException("Video decoder callbacks cannot mutate their player."); }
    /// <inheritdoc />
    protected override void ValidateDisposal() { base.ValidateDisposal(); if (_operating) throw new InvalidOperationException("Video decoder callbacks cannot dispose their player."); _playback?.BindOwner(this); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        List<Exception>? errors = null; Finished = null;
        if (_stream is not null) _stream.Changed -= SourceChanged;
        if (_trackedTexture is not null) _trackedTexture.Changed -= _textureChanged;
        _trackedTexture = null;
        if (_playback is { } playback)
        {
            _playback = null; playback.PlayerOwner = null; playback.AudioSink = null;
            try { playback.Dispose(); } catch (Exception error) { CollectException(ref errors, error); }
        }
        try { base.Dispose(disposing); } catch (Exception error) { CollectException(ref errors, error); }
        try { _audio.Dispose(); } catch (Exception error) { CollectException(ref errors, error); }
        try { _soundtrack?.Dispose(); } catch (Exception error) { CollectException(ref errors, error); }
        _soundtrack = null; _stream = null;
        ThrowCollected("Video player cleanup failed.", errors);
    }
}
