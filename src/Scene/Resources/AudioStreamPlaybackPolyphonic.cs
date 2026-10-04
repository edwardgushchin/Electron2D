namespace Electron2D;

/// <summary>Owns bounded dynamic child voices identified by slot and generation.</summary>
/// <remarks>Stream voices start at the next nonempty mix; native samples start on the audio owner.
/// StopStream invalidates the ID immediately, retaining a streamed voice for one fade. Child factories
/// and cleanup are cold; repeated mixing and scalar controls reuse prepared storage. The parent remains
/// active after children finish, reports zero time/loops and ignores finite Start/Seek positions.</remarks>
public sealed partial class AudioStreamPlaybackPolyphonic : AudioStreamPlayback
{
    /// <summary>Returned when no free voice exists or the requested stream is null.</summary>
    public const long InvalidID = -1;
    private sealed class Voice
    {
        internal AudioStream? Stream;
        internal AudioStreamPlayback? Playback;
        internal PreparedChild? Prepared;
        internal bool Active, Pending, Finishing;
        internal uint Generation;
        internal double Offset;
        internal float Gain = 1, PreviousGain = 1, Pitch = 1, VariationGain = 1, VariationPitch = 1;
    }
    private readonly AudioStreamPolyphonic _source;
    private readonly Voice[] _voices;
    private readonly Vector2[] _scratch = new Vector2[128];
    private uint _generation = 1;
    private bool _active, _busy;
    private FAudioStreamVoice? _nativeOwner;
    private bool _nativeEnabled = true;
    internal AudioStreamPlaybackPolyphonic(AudioStreamPolyphonic source, int count) { _source = source; _voices = new Voice[count]; for (var i = 0; i < count; i++) _voices[i] = new(); }
    internal override bool RequiresAudioOwner { get { if (base.RequiresAudioOwner) return true; foreach (var voice in _voices) if (voice.Playback?.RequiresAudioOwner == true) return true; return false; } }
    private void Check() { ThrowIfDisposed(); ObjectDisposedException.ThrowIf(_source.IsDisposed, _source); }
    private void Idle() { ThrowIfDisposed(); if (_busy) throw new InvalidOperationException("Polyphonic child callbacks cannot reenter control, mixing or disposal."); }
    private void Owner() { if (RequiresAudioOwner) AudioServer.Service.Check(); }
    private Voice? Find(long id)
    {
        var index = (ulong)id >> 32; if (index >= (uint)_voices.Length) return null;
        var voice = _voices[(int)index]; if (voice.Active && voice.Playback?.GetSamplePlayback() is not null && !voice.Playback.IsPlaying()) voice.Active = false; return voice.Active && !voice.Finishing && voice.Generation == (uint)id ? voice : null;
    }
    private static float Gain(float volumeDB) { var gain = (float)Mathf.DBToLinear(volumeDB); if (float.IsNaN(volumeDB) || float.IsPositiveInfinity(volumeDB) || !float.IsFinite(gain)) throw new ArgumentOutOfRangeException(nameof(volumeDB)); return gain; }
    private static void Pitch(float pitchScale) { if (!float.IsFinite(pitchScale) || pitchScale < 0) throw new ArgumentOutOfRangeException(nameof(pitchScale)); }
    private static Exception? Release(Voice voice)
    {
        voice.Active = voice.Pending = voice.Finishing = false; var child = voice.Playback; voice.Playback = null; voice.Stream = null;
        if (child is null) return null;
        if (voice.Prepared is { } prepared) { voice.Prepared = null; prepared.Used = false; try { if (!child.IsDisposed) child.Stop(); return null; } catch (Exception failure) { return failure; } }
        child.CompositeOwner = null; child.UpdateNativeOwner(null);
        Exception? error = null; if (!child.IsDisposed) try { child.Stop(); } catch (Exception failure) { error = failure; }
        try { child.Dispose(); } catch (Exception failure) { error = AudioStreamSynchronized.Combine(error, failure); }
        return error;
    }
    /// <summary>Creates one independently controlled borrowed-stream voice.</summary>
    /// <param name="stream">Borrowed resource; null returns InvalidID.</param>
    /// <param name="fromOffset">Finite start seconds, zero initially.</param>
    /// <param name="volumeDB">Finite representable gain in dB; negative infinity mutes.</param>
    /// <param name="pitchScale">Finite nonnegative stream ratio; native samples require a positive supported ratio.</param>
    /// <param name="playbackType">Default resolves the project transport; nonsampleable sources stream.</param>
    /// <param name="bus">Native sample bus, Master initially. Stream voices use their parent bus.</param>
    /// <returns>A parent-local Int64 slot/generation ID, or InvalidID at capacity/null.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A time, gain, ratio or selector is invalid.</exception>
    /// <exception cref="InvalidOperationException">A callback reenters controls or a factory returns borrowed/shared state.</exception>
    /// <exception cref="NotSupportedException">Native sample rate/effective ratio is unsupported.</exception>
    /// <exception cref="ObjectDisposedException">The parent/resource or requested stream is disposed.</exception>
    public long PlayStream(AudioStream? stream, double fromOffset = 0, float volumeDB = 0, float pitchScale = 1, AudioServer.PlaybackType playbackType = AudioServer.PlaybackType.Default, string bus = "Master")
    {
        var server = AudioServer.Service; server.LockCore();
        try
        {
            Check(); Idle(); Owner(); if (stream is null) return InvalidID;
            if (_preparedRoots.Contains(stream)) { _busy = true; _source.EnterCall(0); try { return PlayPrepared(stream, fromOffset, volumeDB, pitchScale); } finally { AudioStream.ExitCall(); _busy = false; } }
            if (!double.IsFinite(fromOffset) || playbackType is < AudioServer.PlaybackType.Default or >= AudioServer.PlaybackType.Max) throw new ArgumentOutOfRangeException(nameof(fromOffset)); ArgumentNullException.ThrowIfNull(bus); var gain = Gain(volumeDB); Pitch(pitchScale);
            var index = -1;
            for (var i = 0; i < _voices.Length; i++)
            {
                var candidate = _voices[i];
                if (candidate.Active && candidate.Playback?.GetSamplePlayback() is not null && !candidate.Playback.IsPlaying()) candidate.Active = false;
                if (!candidate.Active) { index = i; break; }
            }
            if (index < 0) return InvalidID;
            lock (AudioStream.GraphGate) _source.ValidateChild(stream); stream.EnsurePlaybackOwner();
            var type = playbackType == AudioServer.PlaybackType.Default ? (ProjectSettings.GetWithOverride(ProjectSettings.AudioGeneralDefaultPlaybackType) == AudioDefaultPlaybackType.Sample ? AudioServer.PlaybackType.Sample : AudioServer.PlaybackType.Stream) : playbackType;
            AudioStreamPlayback? child = null; var owned = false; _source.EnterCall(0); _busy = true;
            try
            {
                child = stream.InstantiatePlayback();
                if (ReferenceEquals(child, this) || child.SceneOwner is not null || child.CompositeOwner is not null) throw new InvalidOperationException("Child factories must return independent caller-owned playback.");
                foreach (var existing in _voices) if (ReferenceEquals(existing.Playback, child)) throw new InvalidOperationException("A child playback cannot occupy two slots.");
                owned = true; child.CompositeOwner = this; Check(); ObjectDisposedException.ThrowIf(stream.IsDisposed, stream); child.PrepareQueuedControls();
                if (type == AudioServer.PlaybackType.Sample && stream.CanBeSampled())
                {
                    server.Check(); if (pitchScale == 0) throw new NotSupportedException("Native sample pitch must be positive.");
                    var request = new AudioSamplePlayback(stream) { Bus = bus, PitchScale = pitchScale }; request.SetVoiceGain(gain); child.SetSamplePlayback(request); child.Start(fromOffset);
                }
                child.UpdateNativeOwner(_nativeOwner); if ((!_active || !_nativeEnabled) && _nativeOwner is not null) child.GetSamplePlayback()?.Native?.Pause(true); Check(); ObjectDisposedException.ThrowIf(stream.IsDisposed, stream);
                var voice = _voices[index]; var cleanup = Release(voice); if (cleanup is not null) throw cleanup;
                voice.Stream = stream; voice.Playback = child; voice.Offset = fromOffset; voice.Gain = voice.PreviousGain = gain; voice.Pitch = pitchScale; voice.VariationPitch = 1; voice.VariationGain = 1; voice.Generation = _generation; _generation = unchecked(_generation + 1); voice.Pending = child.GetSamplePlayback() is null; voice.Finishing = false; voice.Active = true; child = null;
                return ((long)index << 32) | voice.Generation;
            }
            catch (Exception error)
            {
                Exception? cleanup = null; if (owned && child is not null) { child.CompositeOwner = null; child.UpdateNativeOwner(null); try { child.Dispose(); } catch (Exception failure) { cleanup = failure; } }
                Resource.ThrowCombined(error, cleanup); throw;
            }
            finally { AudioStream.ExitCall(); _busy = false; }
        }
        finally { server.UnlockCore(); }
    }
    /// <summary>Gets whether an ID still refers to an unfinished child.</summary>
    /// <param name="stream">Parent-local ID returned by PlayStream.</param>
    /// <returns>False for invalid/stopped/stale IDs, including naturally completed native samples.</returns>
    /// <exception cref="InvalidOperationException">A native query reports a contained playback failure.</exception>
    /// <exception cref="ObjectDisposedException">The parent or source is disposed.</exception>
    public bool IsStreamPlaying(long stream) { lock (AudioServer.Service.StreamGate) { Check(); var voice = Find(stream); return voice is not null && (voice.Playback!.GetSamplePlayback() is null || voice.Playback.IsPlaying()); } }
    /// <summary>Updates a live child's scalar volume without restarting it.</summary>
    /// <param name="stream">Parent-local ID; invalid IDs do nothing.</param>
    /// <param name="volumeDB">Representable dB gain; negative infinity mutes.</param>
    /// <remarks>Streaming interpolates from the prior mixed gain across the next requested block. Native matrices update on the owner.</remarks>
    /// <exception cref="InvalidOperationException">Control reentry or an owner-bound operation on a foreign thread rejects.</exception>
    /// <exception cref="ObjectDisposedException">The parent or source is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The gain is invalid for a live ID.</exception>
    /// <exception cref="ArithmeticException">Native gain coefficients overflow; prior configuration is preserved.</exception>
    public void SetStreamVolume(long stream, float volumeDB)
    {
        var server = AudioServer.Service; server.LockCore(); try { Check(); Idle(); var voice = Find(stream); if (voice is null) return; Owner(); var gain = Gain(volumeDB); gain *= voice.VariationGain; voice.Playback!.GetSamplePlayback()?.SetVoiceGain(gain); voice.Gain = gain; } finally { server.UnlockCore(); }
    }
    /// <summary>Updates a live child's pitch without resetting its cursor.</summary>
    /// <param name="stream">Parent-local ID; invalid IDs do nothing.</param>
    /// <param name="pitchScale">Finite nonnegative stream ratio; native ratios must be positive and supported.</param>
    /// <exception cref="InvalidOperationException">Control reentry or an owner-bound operation on a foreign thread rejects.</exception>
    /// <exception cref="ObjectDisposedException">The parent or source is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The ratio is invalid for a live ID.</exception>
    /// <exception cref="NotSupportedException">The effective native ratio is unsupported; prior configuration is preserved.</exception>
    public void SetStreamPitchScale(long stream, float pitchScale)
    {
        var server = AudioServer.Service; server.LockCore(); try { Check(); Idle(); var voice = Find(stream); if (voice is null) return; Owner(); Pitch(pitchScale); pitchScale *= voice.VariationPitch; Pitch(pitchScale); if (voice.Playback!.GetSamplePlayback() is { } sample) { if (pitchScale == 0) throw new NotSupportedException("Native sample pitch must be positive."); sample.PitchScale = pitchScale; } voice.Pitch = pitchScale; } finally { server.UnlockCore(); }
    }
    /// <summary>Invalidates one ID and requests its final streamed fade or immediate native stop.</summary>
    /// <param name="stream">Parent-local ID; invalid IDs do nothing.</param>
    /// <exception cref="InvalidOperationException">Control reentry or an owner-bound operation on a foreign thread rejects.</exception>
    /// <exception cref="ObjectDisposedException">The parent or source is disposed.</exception>
    public void StopStream(long stream)
    {
        var server = AudioServer.Service; server.LockCore(); try { Check(); Idle(); var voice = Find(stream); if (voice is null) return; Owner(); if (voice.Playback!.GetSamplePlayback() is not null) { voice.Playback.Stop(); voice.Active = false; } else voice.Finishing = true; } finally { server.UnlockCore(); }
    }
    internal override void UpdateNativeOwner(FAudioStreamVoice? owner, bool enabled = true)
    {
        _nativeOwner = owner; _nativeEnabled = enabled; foreach (var voice in _voices) if (voice.Playback is { } child) { child.UpdateNativeOwner(owner, enabled); if (owner is not null && (!enabled || !_active || !voice.Active)) child.GetSamplePlayback()?.Native?.Pause(true); }
    }
    internal override void PrepareQueuedControls() { Owner(); foreach (var voice in _voices) voice.Playback?.PrepareQueuedControls(); }
    private Exception? StopCore(bool queued)
    {
        _active = false; Exception? error = null;
        foreach (var voice in _voices)
        {
            if (!queued) error = AudioStreamSynchronized.Combine(error, Release(voice));
            else { voice.Active = voice.Pending = voice.Finishing = false; if (voice.Playback is { } child) try { child.StopQueued(); } catch (Exception failure) { error = AudioStreamSynchronized.Combine(error, failure); } }
        }
        return error;
    }
    private void StartCore(bool queued)
    {
        Check(); Idle(); if (!queued) Owner(); _busy = true; try { if (_active) { var error = StopCore(queued); if (error is not null) throw error; } _active = true; foreach (var voice in _voices) if (voice.Active) voice.Playback?.GetSamplePlayback()?.Native?.PauseQueued(_nativeOwner?.SamplingPaused == true); } finally { _busy = false; }
    }
    internal override void StartQueued(double time) { lock (AudioServer.Service.StreamGate) StartCore(true); }
    internal override void StopQueued() { lock (AudioServer.Service.StreamGate) { Idle(); _busy = true; try { var error = StopCore(true); if (error is not null) throw error; } finally { _busy = false; } } }
    /// <inheritdoc />
    protected override void OnStart(double fromPosition) { var server = AudioServer.Service; server.LockCore(); try { StartCore(false); } finally { server.UnlockCore(); } }
    /// <inheritdoc />
    protected override void OnStop() { var server = AudioServer.Service; server.LockCore(); try { Idle(); Owner(); _busy = true; try { var error = StopCore(false); if (error is not null) throw error; } finally { _busy = false; } } finally { server.UnlockCore(); } }
    /// <inheritdoc />
    protected override bool OnIsPlaying() { lock (AudioServer.Service.StreamGate) { Check(); return _active; } }
    /// <inheritdoc />
    protected override double OnGetPlaybackPosition() { Check(); return 0; }
    /// <inheritdoc />
    protected override void OnSeek(double time) => Check();
    /// <inheritdoc />
    protected override int OnMix(Span<Vector2> buffer, float rateScale)
    {
        lock (AudioServer.Service.StreamGate)
        {
            Check(); Idle(); buffer.Clear(); if (!_active || buffer.IsEmpty) return 0; _source.EnterCall(4); _busy = true;
            try
            {
                foreach (var voice in _voices)
                {
                    if (!voice.Active) continue; var child = voice.Playback!;
                    if (child.GetSamplePlayback() is not null) { if (!child.IsPlaying()) voice.Active = false; continue; }
                    if (voice.Finishing && voice.Pending) { voice.Active = voice.Pending = false; continue; }
                    if (voice.Pending) { child.StartQueued(voice.Offset); voice.Pending = false; Check(); }
                    var next = voice.Finishing ? 0 : voice.Gain; var gain = voice.PreviousGain; var increment = (next - gain) / buffer.Length;
                    var rate = rateScale * voice.Pitch; if (!float.IsFinite(rate)) throw new ArithmeticException("Combined child pitch exceeds finite storage.");
                    for (var offset = 0; offset < buffer.Length; offset += 128)
                    {
                        var count = Math.Min(128, buffer.Length - offset); var scratch = _scratch.AsSpan(0, count); scratch.Clear(); var mixed = child.MixInto(scratch, rate); Check(); scratch[mixed..].Clear();
                        for (var i = 0; i < count; i++) { var frame = scratch[i]; if (!frame.IsFinite()) throw new ArithmeticException("Child PCM must be finite."); var value = new Vector2((float)((double)buffer[offset + i].X + (double)frame.X * gain), (float)((double)buffer[offset + i].Y + (double)frame.Y * gain)); if (!value.IsFinite()) throw new ArithmeticException("Polyphonic PCM exceeds finite storage."); buffer[offset + i] = value; gain += increment; }
                        if (mixed < count) { voice.Active = false; child.StopQueued(); break; }
                    }
                    voice.PreviousGain = next; if (voice.Finishing) { voice.Active = false; child.StopQueued(); }
                }
                return buffer.Length;
            }
            catch { buffer.Clear(); _active = false; foreach (var voice in _voices) { voice.Active = false; try { voice.Playback?.StopQueued(); } catch { } } throw; }
            finally { AudioStream.ExitCall(); _busy = false; }
        }
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() { var server = AudioServer.Service; server.LockCore(); try { Idle(); Owner(); } finally { server.UnlockCore(); } base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { lock (AudioServer.Service.StreamGate) { _busy = true; try { var error = StopCore(false); error = AudioStreamSynchronized.Combine(error, DisposePrepared()); if (error is not null) throw error; } finally { _nativeOwner = null; _busy = false; base.Dispose(disposing); } } } else base.Dispose(disposing); }
}
