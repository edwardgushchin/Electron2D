namespace Electron2D;

/// <summary>Describes reusable audio data and creates independent playback state.</summary>
/// <remarks>Consumers borrow the stream. Playback objects are caller-owned. Mixing callbacks may run on an audio
/// thread; custom streams must keep their repeated processing allocation-free and synchronize mutable data.</remarks>
public abstract class AudioStream : Resource
{
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
/// A playback borrowed from a player remains player-owned and is not an independent control handle.</remarks>
public abstract class AudioStreamPlayback : ElectronObject
{
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
        set { ThrowIfDisposed(); Volatile.Write(ref _loopingOverride, value is null ? -1 : value.Value ? 1 : 0); }
    }
    /// <summary>Starts playback from a position in seconds.</summary>
    /// <param name="fromPosition">Requested time, zero by default.</param>
    /// <exception cref="ArgumentOutOfRangeException">The time is not finite.</exception>
    /// <exception cref="ObjectDisposedException">The playback or its stream is disposed.</exception>
    public void Start(double fromPosition = 0) { ThrowIfDisposed(); if (!double.IsFinite(fromPosition)) throw new ArgumentOutOfRangeException(nameof(fromPosition)); OnStart(fromPosition); }
    /// <summary>Starts the concrete playback.</summary>
    /// <param name="fromPosition">Finite requested time in seconds.</param>
    protected abstract void OnStart(double fromPosition);
    /// <summary>Stops playback.</summary>
    /// <exception cref="ObjectDisposedException">The playback is disposed.</exception>
    public void Stop() { ThrowIfDisposed(); OnStop(); }
    /// <summary>Stops the concrete playback.</summary>
    protected abstract void OnStop();
    /// <summary>Gets whether playback is currently active.</summary>
    /// <returns>The concrete active state.</returns>
    public bool IsPlaying() { ThrowIfDisposed(); return OnIsPlaying(); }
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
    public double GetPlaybackPosition() { ThrowIfDisposed(); return OnGetPlaybackPosition(); }
    /// <summary>Supplies the playback cursor.</summary>
    /// <returns>Position in seconds.</returns>
    protected abstract double OnGetPlaybackPosition();
    /// <summary>Seeks the concrete playback.</summary>
    /// <param name="time">Finite requested time, zero by default.</param>
    /// <exception cref="ArgumentOutOfRangeException">The time is not finite.</exception>
    /// <exception cref="ObjectDisposedException">The playback or its stream is disposed.</exception>
    public void Seek(double time = 0) { ThrowIfDisposed(); if (!double.IsFinite(time)) throw new ArgumentOutOfRangeException(nameof(time)); OnSeek(time); }
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
        var count = OnMix(buffer, rateScale); if ((uint)count > (uint)buffer.Length) throw new InvalidOperationException("Audio mixing returned an invalid frame count."); return count;
    }
    /// <summary>Fills the prepared stereo frame span and reports frames before the first silence.</summary>
    /// <param name="buffer">Prepared caller-owned output storage.</param>
    /// <param name="rateScale">Finite nonnegative playback-rate multiplier.</param>
    /// <returns>Mixed frame count, between zero and buffer.Length.</returns>
    protected abstract int OnMix(Span<Vector2> buffer, float rateScale);
}
