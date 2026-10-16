namespace Electron2D;

internal delegate int VideoAudioSink(ReadOnlySpan<float> samples, int frames);

/// <summary>Owns a video's independent decoder, time, pause state and frame texture.</summary>
/// <remarks>Standalone operations are affine to the creating thread. A player's prepared state may follow
/// a detached hierarchy to its initial scene owner before first Play; subsequent operations stay owner-affine.
/// A player owns attached state; its texture is
/// borrowed. Custom decoders feed interleaved PCM through MixAudio from OnUpdate. Video hooks never run
/// on the audio thread. Repeated prepared updates and audio submissions must reuse storage.</remarks>
public abstract class VideoStreamPlayback : Resource
{
    private int _thread = Environment.CurrentManagedThreadId;
    private bool _calling, _everPlayed;
    private int _channels;
    internal VideoStreamPlayer? PlayerOwner;
    internal VideoAudioSink? AudioSink;
    /// <summary>Initializes independent decoder extension state on the current thread.</summary>
    protected VideoStreamPlayback() { }
    internal void BindOwner(VideoStreamPlayer owner)
    {
        if (!ReferenceEquals(PlayerOwner, owner)) throw new InvalidOperationException("Video playback belongs to a different player.");
        if (_thread == Environment.CurrentManagedThreadId) return;
        if (_calling || _everPlayed) throw new InvalidOperationException("Started video playback cannot move between scene owner threads.");
        _thread = Environment.CurrentManagedThreadId;
    }
    private void Check()
    {
        ThrowIfDisposed();
        if (Environment.CurrentManagedThreadId != _thread) throw new InvalidOperationException("Video playback requires its creating thread.");
    }
    private void Enter()
    {
        Check(); if (_calling) throw new InvalidOperationException("Video decoder hooks cannot reenter playback operations."); _calling = true;
    }
    /// <summary>Returns the source audio channel count.</summary>
    /// <returns>Zero for silent video, otherwise one through eight interleaved channels.</returns>
    /// <exception cref="InvalidOperationException">The decoder reports an unsupported channel count.</exception>
    public int GetChannels()
    {
        Enter(); try { var value = OnGetChannels(); if ((uint)value > 8) throw new InvalidOperationException("Video audio requires zero through eight channels."); return _channels = value; } finally { _calling = false; }
    }
    /// <summary>Returns the source audio frequency.</summary>
    /// <returns>Hertz; zero when no audio is present.</returns>
    public int GetMixRate() { Enter(); try { return OnGetMixRate(); } finally { _calling = false; } }
    /// <summary>Returns the known source duration.</summary>
    /// <returns>Nonnegative seconds, or zero when unknown.</returns>
    public double GetLength()
    {
        Enter(); try { var value = OnGetLength(); if (!double.IsFinite(value) || value < 0) throw new InvalidOperationException("Video duration must be finite and nonnegative."); return value; } finally { _calling = false; }
    }
    /// <summary>Returns the current source timestamp.</summary>
    /// <returns>Seconds in the video's presentation timeline; configured delay may make the initial timestamp negative.</returns>
    /// <exception cref="InvalidOperationException">The decoder reports a nonfinite timestamp.</exception>
    public double GetPlaybackPosition()
    {
        Enter(); try { var value = OnGetPlaybackPosition(); if (!double.IsFinite(value)) throw new InvalidOperationException("Video timestamp must be finite."); return value; } finally { _calling = false; }
    }
    internal virtual void RestoreTimingFrom(VideoStreamPlayback previous)
    {
        if (previous.IsPlaying()) Play(); Seek(previous.GetPlaybackPosition());
    }
    /// <summary>Returns the borrowed current-frame texture.</summary>
    /// <returns>The decoder-owned texture, or null before a custom decoder supplies an image.</returns>
    public Texture? GetTexture() { Enter(); try { return OnGetTexture(); } finally { _calling = false; } }
    /// <summary>Returns whether playback is active, including explicit pause.</summary>
    /// <returns>True until stop or natural completion.</returns>
    public bool IsPlaying() { Enter(); try { return OnIsPlaying(); } finally { _calling = false; } }
    /// <summary>Returns the decoder's pause state.</summary>
    /// <returns>The state last supplied through SetPaused.</returns>
    public bool IsPaused() { Enter(); try { return OnIsPaused(); } finally { _calling = false; } }
    /// <summary>Starts playback from the beginning without changing explicit pause.</summary>
    public void Play() { Enter(); try { _everPlayed = true; OnPlay(); } finally { _calling = false; } }
    /// <summary>Stops playback and resets its timestamp while preserving the displayed frame.</summary>
    public void Stop() { Enter(); try { OnStop(); } finally { _calling = false; } }
    /// <summary>Moves to a source timestamp.</summary>
    /// <param name="time">Finite source seconds; built-in playback ignores a position at or beyond its duration.
    /// Negative positions retain the first picture until the timeline reaches zero.</param>
    /// <exception cref="ArgumentOutOfRangeException">The timestamp is nonfinite.</exception>
    public void Seek(double time)
    {
        if (!double.IsFinite(time)) throw new ArgumentOutOfRangeException(nameof(time));
        Enter(); try { OnSeek(time); } finally { _calling = false; }
    }
    internal virtual bool SeekForPlayer(double time) { Seek(time); return true; }
    /// <summary>Selects the source's embedded audio track.</summary>
    /// <param name="index">Zero-based track, or -1 to disable embedded audio. Missing tracks produce silent video.</param>
    /// <exception cref="ArgumentOutOfRangeException">The index is below -1.</exception>
    public void SetAudioTrack(int index)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(index, -1); Enter(); try { OnSetAudioTrack(index); } finally { _calling = false; }
    }
    /// <summary>Changes explicit decoder pause without ending playback.</summary>
    /// <param name="paused">True freezes the source timeline.</param>
    public void SetPaused(bool paused) { Enter(); try { OnSetPaused(paused); } finally { _calling = false; } }
    /// <summary>Advances the decoder and submits prepared PCM on its owner.</summary>
    /// <param name="delta">Finite nonnegative elapsed source seconds.</param>
    /// <exception cref="ArgumentOutOfRangeException">The delta is negative or nonfinite.</exception>
    public void Update(double delta)
    {
        if (!double.IsFinite(delta) || delta < 0) throw new ArgumentOutOfRangeException(nameof(delta));
        Enter(); try { OnUpdate(delta); } finally { _calling = false; }
    }
    /// <summary>Copies interleaved source PCM into the attached player's bounded queue.</summary>
    /// <param name="numFrames">Nonnegative frame count, each containing GetChannels floats.</param>
    /// <param name="buffer">Borrowed finite interleaved source samples.</param>
    /// <param name="offset">Starting float index in buffer, zero initially.</param>
    /// <returns>Accepted frames, zero when full, or -1 when no player audio queue is attached.</returns>
    /// <exception cref="ArgumentException">The buffer extent or a sample is invalid.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The count or offset is negative.</exception>
    public int MixAudio(int numFrames, ReadOnlySpan<float> buffer = default, int offset = 0)
    {
        Check(); ArgumentOutOfRangeException.ThrowIfNegative(numFrames); ArgumentOutOfRangeException.ThrowIfNegative(offset);
        if (offset > buffer.Length) throw new ArgumentException("PCM offset exceeds the input buffer.", nameof(offset));
        if (AudioSink is not { } sink) return -1;
        if ((long)numFrames * _channels > buffer.Length - offset) throw new ArgumentException("PCM buffer is shorter than the requested interleaved frames.", nameof(buffer));
        var samples = buffer.Slice(offset, checked(numFrames * _channels));
        foreach (var sample in samples) if (!float.IsFinite(sample)) throw new ArgumentException("PCM must contain finite samples.", nameof(buffer));
        var result = sink(samples, numFrames);
        if ((uint)result > (uint)numFrames) throw new InvalidOperationException("Video audio queue reported an invalid accepted count.");
        return result;
    }
    /// <summary>Supplies the source channel count.</summary>
    /// <returns>Zero through eight.</returns>
    protected abstract int OnGetChannels();
    /// <summary>Supplies source audio frequency.</summary>
    /// <returns>Hertz, or zero for silent video.</returns>
    protected abstract int OnGetMixRate();
    /// <summary>Supplies duration.</summary>
    /// <returns>Nonnegative seconds; zero when unknown.</returns>
    protected abstract double OnGetLength();
    /// <summary>Supplies the current timestamp.</summary>
    /// <returns>Source seconds.</returns>
    protected abstract double OnGetPlaybackPosition();
    /// <summary>Supplies the decoder-owned frame texture.</summary>
    /// <returns>A borrowed texture, or null.</returns>
    protected abstract Texture? OnGetTexture();
    /// <summary>Supplies the active state, including pause.</summary>
    /// <returns>True while playing.</returns>
    protected abstract bool OnIsPlaying();
    /// <summary>Supplies the explicit pause state.</summary>
    /// <returns>True while paused.</returns>
    protected abstract bool OnIsPaused();
    /// <summary>Starts or restarts playback, preserving explicit pause.</summary>
    protected abstract void OnPlay();
    /// <summary>Stops and rewinds time while retaining the displayed frame.</summary>
    protected abstract void OnStop();
    /// <summary>Moves to a source timestamp.</summary>
    /// <param name="time">Finite source seconds.</param>
    protected abstract void OnSeek(double time);
    /// <summary>Selects an embedded audio track.</summary>
    /// <param name="index">Zero-based track or -1 for silence.</param>
    protected abstract void OnSetAudioTrack(int index);
    /// <summary>Changes explicit pause.</summary>
    /// <param name="paused">Requested pause state.</param>
    protected abstract void OnSetPaused(bool paused);
    /// <summary>Advances decoding and submits prepared PCM through MixAudio.</summary>
    /// <param name="delta">Finite nonnegative elapsed source seconds.</param>
    protected abstract void OnUpdate(double delta);
    /// <inheritdoc />
    protected override void ValidateDisposal() { Check(); if (_calling || PlayerOwner is not null) throw new InvalidOperationException("Attached or executing video playback cannot be disposed directly."); base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { AudioSink = null; base.Dispose(disposing); }
}
