namespace Electron2D;

/// <summary>Describes reusable audio data and creates independent playback state.</summary>
/// <remarks>Consumers borrow the stream. Playback objects are caller-owned. Mixing callbacks may run on an audio
/// thread; custom streams must keep their repeated processing allocation-free and synchronize mutable data.</remarks>
public abstract class AudioStream : Resource
{
    // ponytail: one gate serializes composite authoring and cycle checks; partition only after measured contention.
    internal static readonly object GraphGate = new();
    [ThreadStatic] private static List<(AudioStream Stream, int Operation)>? _callStack;
    internal void EnterCall(int operation)
    {
        var stack = _callStack ??= [];
        if (stack.Count >= 256) throw new InvalidOperationException("Audio resource nesting exceeds 256 operations.");
        foreach (var entry in stack) if (ReferenceEquals(entry.Stream, this) && entry.Operation == operation) throw new InvalidOperationException("Audio resource callbacks are recursive.");
        stack.Add((this, operation));
    }
    internal bool IsCalling(int operation) { if (_callStack is { } stack) foreach (var entry in stack) if (ReferenceEquals(entry.Stream, this) && entry.Operation == operation) return true; return false; }
    internal static void ExitCall() { var stack = _callStack!; stack.RemoveAt(stack.Count - 1); }
    internal virtual void AppendChildren(Stack<AudioStream> pending) { }
    internal virtual void AppendPlaybackChildren(Stack<AudioStream> pending) => AppendChildren(pending);
    internal void EnsurePlaybackOwner()
    {
        if (this is not (AudioStreamMicrophone or AudioStreamRandomizer or AudioStreamSynchronized or AudioStreamInteractive)) return;
        lock (GraphGate)
        {
            var pending = new Stack<AudioStream>(); var visited = new HashSet<AudioStream>(ReferenceEqualityComparer.Instance); pending.Push(this);
            while (pending.TryPop(out var current))
            {
                if (current is AudioStreamMicrophone) { AudioServer.Instance.Check(); return; }
                if (visited.Add(current)) current.AppendPlaybackChildren(pending);
            }
        }
    }
    internal void ValidateChild(AudioStream? stream)
    {
        if (stream is { IsDisposed: true }) throw new ObjectDisposedException(nameof(stream));
        if (stream is null) return;
        var pending = new Stack<AudioStream>(); var visited = new HashSet<AudioStream>(ReferenceEqualityComparer.Instance); pending.Push(stream);
        while (pending.TryPop(out var current))
        {
            if (ReferenceEquals(current, this)) throw new InvalidOperationException("An audio stream graph cannot contain a cycle.");
            if (visited.Add(current)) current.AppendChildren(pending);
        }
    }
    internal double ReadBPM() { ThrowIfDisposed(); return OnGetBPM(); }
    internal bool ReadLoop() { ThrowIfDisposed(); return OnHasLoop(); }
    internal int ReadBarBeats() { ThrowIfDisposed(); return OnGetBarBeats(); }
    internal int ReadBeatCount() { ThrowIfDisposed(); return OnGetBeatCount(); }
    /// <summary>Initializes the independent stream resource.</summary>
    protected AudioStream() { }
    /// <summary>Creates independent playback state for this stream.</summary>
    /// <returns>A new caller-owned playback that borrows this resource.</returns>
    /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
    public AudioStreamPlayback InstantiatePlayback() { ThrowIfDisposed(); var playback = OnInstantiatePlayback(); if (playback is null || playback.IsDisposed) throw new InvalidOperationException("A stream must create a live playback."); return playback; }
    /// <summary>Creates independent playback state.</summary>
    /// <returns>A new caller-owned playback.</returns>
    protected abstract AudioStreamPlayback OnInstantiatePlayback();
    /// <summary>Gets the duration in seconds; zero represents an unknown or empty duration.</summary>
    /// <returns>Duration in seconds.</returns>
    public double GetLength() { ThrowIfDisposed(); return OnGetLength(); }
    /// <summary>Supplies the stream duration.</summary>
    /// <returns>Duration in seconds, zero by default.</returns>
    protected virtual double OnGetLength() => 0;
    /// <summary>Gets whether this resource can prepare finite sample-driver PCM.</summary>
    /// <returns>False by default; finite WAV, MPEG and Vorbis resources override this capability.</returns>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public virtual bool CanBeSampled() { ThrowIfDisposed(); return false; }
    /// <summary>Creates caller-owned copied PCM and loop metadata for sample playback.</summary>
    /// <returns>An independently disposable immutable snapshot.</returns>
    /// <remarks>WAV and initialized mono/stereo compressed sources copy decoded PCM and loop metadata.
    /// Native sample transport retains source Hz and exclusive loop end; live resource edits need explicit re-registration.</remarks>
    /// <exception cref="NotSupportedException">This stream cannot be sampled.</exception>
    /// <exception cref="InvalidOperationException">Concrete sample data or an enabled loop is not initialized/valid.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public virtual AudioSample GenerateSample() { ThrowIfDisposed(); throw new NotSupportedException("This resource has no finite sample representation."); }
    /// <summary>Gets whether the stream permits only one active voice.</summary>
    /// <returns>True by default; ordinary WAV streams are polyphonic.</returns>
    public bool IsMonophonic() { ThrowIfDisposed(); return OnIsMonophonic(); }
    /// <summary>Supplies the monophonic policy.</summary>
    /// <returns>True by default.</returns>
    protected virtual bool OnIsMonophonic() => true;
    /// <summary>Supplies a descriptive stream name for diagnostics and derived stream containers.</summary>
    /// <returns>An empty name by default.</returns>
    protected virtual string OnGetStreamName() => string.Empty;
    /// <summary>Supplies optional musical tempo metadata.</summary>
    /// <returns>Beats per minute, zero when absent.</returns>
    protected virtual double OnGetBPM() => 0;
    /// <summary>Supplies optional looping metadata for composite streams.</summary>
    /// <returns>False by default.</returns>
    protected virtual bool OnHasLoop() => false;
    /// <summary>Supplies the number of beats per bar.</summary>
    /// <returns>Zero when no musical metadata is supplied.</returns>
    protected virtual int OnGetBarBeats() => 0;
    /// <summary>Supplies optional total beat metadata.</summary>
    /// <returns>Zero by default.</returns>
    protected virtual int OnGetBeatCount() => 0;
    /// <summary>Supplies copied textual stream metadata.</summary>
    /// <returns>A caller-owned map, empty by default.</returns>
    protected virtual Dictionary<string, string> OnGetTags() => new(StringComparer.Ordinal);
    /// <summary>Gets whether the resource selects or combines other streams rather than holding ordinary samples.</summary>
    /// <returns>False for ordinary sample resources.</returns>
    public virtual bool IsMetaStream() { ThrowIfDisposed(); return false; }
    /// <summary>Returns typed parameter descriptors for a custom stream.</summary>
    /// <returns>A caller-owned array of descriptors; empty for parameterless streams.</returns>
    public PropertyDescriptor[] GetParameterList() { ThrowIfDisposed(); return (PropertyDescriptor[])OnGetParameterList().Clone(); }
    /// <summary>Supplies custom typed parameter descriptors.</summary>
    /// <returns>A caller-owned array, empty by default.</returns>
    protected virtual PropertyDescriptor[] OnGetParameterList() => [];
    /// <summary>Occurs when the available parameter descriptors change.</summary>
    public event Action? ParameterListChanged;
    /// <summary>Reports an actual custom parameter-list change.</summary>
    /// <exception cref="ObjectDisposedException">The stream is disposed.</exception>
    protected void NotifyParameterListChanged() { ThrowIfDisposed(); ParameterListChanged?.Invoke(); }
    internal virtual void ReloadFrom(AudioStream source)
    {
        ThrowIfDisposed(); if (source.GetType() != GetType()) throw new InvalidOperationException("Audio reload types differ.");
        source.CopyCustomStateTo(this, false, DeepDuplicateMode.Internal, static r => r, static r => r); EmitChanged();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { ParameterListChanged = null; base.Dispose(disposing); }
}

/// <summary>Owns independent time, loop and mixing state for an audio stream.</summary>
/// <remarks>MixAudio allocates a caller-owned result. Engine mixing reuses prepared spans through OnMix.
/// A playback borrowed from a player remains player-owned and is not an independent control handle.
/// With an owned sample association, native output replaces managed OnMix and MixAudio returns no frames.</remarks>
public abstract class AudioStreamPlayback : ElectronObject
{
    private AudioSamplePlayback? _samplePlayback;
    internal virtual bool RequiresAudioOwner => _samplePlayback is not null;
    internal virtual void PrepareQueuedControls() { }
    internal virtual void StartQueued(double time) => Start(time);
    internal virtual void StopQueued() => Stop();
    /// <summary>Gets the borrowed native sample request associated with this playback.</summary>
    /// <returns>Null for ordinary stream playback.</returns>
    public AudioSamplePlayback? GetSamplePlayback() { ThrowIfDisposed(); return _samplePlayback; }
    /// <summary>Transfers ownership of an inactive sample request, or clears this association.</summary>
    /// <param name="playbackSample">Fresh caller-owned request or null.</param>
    /// <remarks>An associated request becomes borrowed. Replacing attached scene sample state rejects; standalone
    /// replacement stops/releases its native voice before disposing the old request. Requires the audio owner.</remarks>
    /// <exception cref="InvalidOperationException">The request belongs to another playback or a scene voice is attached.</exception>
    public void SetSamplePlayback(AudioSamplePlayback? playbackSample)
    {
        var server = AudioServer.Instance; server.Check(); server.Lock();
        try
        {
            ThrowIfDisposed(); if (ReferenceEquals(_samplePlayback, playbackSample)) return;
            if (_samplePlayback?.Native?.Wrapped == true || playbackSample?.Owner is not null) throw new InvalidOperationException("The sample request is already attached.");
            playbackSample?.Check(); var previous = _samplePlayback; previous?.Native?.Dispose(); _samplePlayback = playbackSample; if (playbackSample is not null) playbackSample.Owner = this;
            if (previous is not null) { previous.Owner = null; previous.Dispose(); }
        }
        finally { server.Unlock(); }
    }
    private void StartSample(double time) { var server = AudioServer.Instance; server.Check(); lock (server.StreamGate) { var sample = _samplePlayback!; sample.Native ??= server.PrepareSample(sample); sample.Native.Play(time); } }
    /// <summary>Initializes the independent playback extension state.</summary>
    protected AudioStreamPlayback() { }
    private int _loopingOverride = -1;
    /// <summary>Gets the typed nullable looping parameter descriptor.</summary>
    /// <value>Null restores the concrete stream loop policy; true/false overrides it on supporting streams.</value>
    public static PropertyDescriptor<AudioStreamPlayback, bool?> LoopingParameter { get; } = new(nameof(LoopingOverride), p => p.LoopingOverride, (p, v) => p.LoopingOverride = v, _ => null, stored: true);
    /// <summary>Gets or sets the typed looping override for supporting compressed or composite playbacks.</summary>
    /// <value>Null initially; updates are atomic across owner/audio threads. Parameterless streams do not consume it.</value>
    /// <exception cref="ObjectDisposedException">The playback is disposed.</exception>
    public bool? LoopingOverride
    {
        get { ThrowIfDisposed(); var value = Volatile.Read(ref _loopingOverride); return value < 0 ? null : value != 0; }
        set { ThrowIfDisposed(); if (_samplePlayback?.Native is { } sample) { var server = AudioServer.Instance; server.Check(); lock (server.StreamGate) { var old = _loopingOverride; Volatile.Write(ref _loopingOverride, value is null ? -1 : value.Value ? 1 : 0); try { sample.RefreshLooping(); } catch { Volatile.Write(ref _loopingOverride, old); throw; } } } else Volatile.Write(ref _loopingOverride, value is null ? -1 : value.Value ? 1 : 0); }
    }
    /// <summary>Starts playback from a position in seconds.</summary>
    /// <param name="fromPosition">Requested time, zero by default.</param>
    /// <exception cref="ArgumentOutOfRangeException">The time is not finite.</exception>
    /// <exception cref="ObjectDisposedException">The playback or its stream is disposed.</exception>
    public void Start(double fromPosition = 0) { ThrowIfDisposed(); if (!double.IsFinite(fromPosition)) throw new ArgumentOutOfRangeException(nameof(fromPosition)); if (_samplePlayback is not null) StartSample(fromPosition); else OnStart(fromPosition); }
    /// <summary>Starts the concrete playback.</summary>
    /// <param name="fromPosition">Finite requested time in seconds.</param>
    protected abstract void OnStart(double fromPosition);
    /// <summary>Stops playback.</summary>
    /// <exception cref="ObjectDisposedException">The playback is disposed.</exception>
    public void Stop() { ThrowIfDisposed(); if (_samplePlayback is not null) _samplePlayback.Native?.Stop(); else OnStop(); }
    /// <summary>Stops the concrete playback.</summary>
    protected abstract void OnStop();
    /// <summary>Gets whether playback is currently active.</summary>
    /// <returns>The concrete active state.</returns>
    public bool IsPlaying() { ThrowIfDisposed(); return _samplePlayback is not null ? _samplePlayback.Native?.IsPlaying == true : OnIsPlaying(); }
    /// <summary>Supplies the active state.</summary>
    /// <returns>The concrete active state.</returns>
    protected abstract bool OnIsPlaying();
    /// <summary>Gets the concrete loop counter.</summary>
    /// <returns>The number reported by this playback implementation.</returns>
    public int GetLoopCount() { ThrowIfDisposed(); return OnGetLoopCount(); }
    /// <summary>Supplies the loop counter.</summary>
    /// <returns>Zero by default.</returns>
    protected virtual int OnGetLoopCount() => 0;
    /// <summary>Gets the current stream position in seconds.</summary>
    /// <returns>The concrete playback cursor, which can include resampling prefetch.</returns>
    public double GetPlaybackPosition() { ThrowIfDisposed(); return _samplePlayback is not null ? _samplePlayback.Native?.Position ?? 0 : OnGetPlaybackPosition(); }
    /// <summary>Supplies the playback cursor.</summary>
    /// <returns>Position in seconds.</returns>
    protected abstract double OnGetPlaybackPosition();
    /// <summary>Seeks the concrete playback.</summary>
    /// <param name="time">Finite requested time, zero by default.</param>
    /// <exception cref="ArgumentOutOfRangeException">The time is not finite.</exception>
    /// <exception cref="ObjectDisposedException">The playback or its stream is disposed.</exception>
    public void Seek(double time = 0) { ThrowIfDisposed(); if (!double.IsFinite(time)) throw new ArgumentOutOfRangeException(nameof(time)); if (_samplePlayback is not null) StartSample(time); else OnSeek(time); }
    /// <summary>Moves the concrete cursor.</summary>
    /// <param name="time">Finite time in seconds.</param>
    protected abstract void OnSeek(double time);
    /// <summary>Mixes up to the requested number of stereo frames.</summary>
    /// <param name="rateScale">Finite nonnegative playback-rate multiplier.</param>
    /// <param name="frames">Nonnegative requested frame count.</param>
    /// <returns>A caller-owned array containing only the frames reported mixed.</returns>
    /// <exception cref="ArgumentOutOfRangeException">The rate/count is invalid or the callback reports an invalid count.</exception>
    /// <exception cref="ObjectDisposedException">The playback is disposed.</exception>
    public Vector2[] MixAudio(float rateScale, int frames)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(frames); var result = new Vector2[frames]; var mixed = MixInto(result, rateScale);
        return mixed == frames ? result : result.AsSpan(0, mixed).ToArray();
    }
    internal int MixInto(Span<Vector2> buffer, float rateScale)
    {
        ThrowIfDisposed(); if (!float.IsFinite(rateScale) || rateScale < 0) throw new ArgumentOutOfRangeException(nameof(rateScale));
        if (_samplePlayback is not null) { buffer.Clear(); return 0; }
        var count = OnMix(buffer, rateScale); if ((uint)count > (uint)buffer.Length) throw new InvalidOperationException("Audio mixing returned an invalid frame count."); return count;
    }
    /// <summary>Fills the prepared stereo frame span and reports frames before the first silence.</summary>
    /// <remarks>Player-owned mixing runs on the native output thread or, for final pause/stop preparation,
    /// the scene owner, serialized by the native context gate. Mixing callbacks cannot reenter audio graph
    /// configuration or owner player mutation/disposal. Caller-driven mixing retains caller coordination.</remarks>
    /// <param name="buffer">Prepared caller-owned output storage.</param>
    /// <param name="rateScale">Finite nonnegative playback-rate multiplier.</param>
    /// <returns>Mixed frame count, between zero and buffer.Length.</returns>
    protected abstract int OnMix(Span<Vector2> buffer, float rateScale);
    /// <inheritdoc />
    protected override void ValidateDisposal() { if (_samplePlayback is not null) { AudioServer.Instance.Check(); if (_samplePlayback.Native?.Wrapped == true) throw new InvalidOperationException("A scene-owned native sample must be released by its player."); } base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { try { if (disposing && _samplePlayback is { } sample) { sample.Native?.Dispose(); sample.Owner = null; _samplePlayback = null; sample.Dispose(); } } finally { base.Dispose(disposing); } }

}
