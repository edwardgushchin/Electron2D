namespace Electron2D;

/// <summary>Owns independent child playback states for one synchronized stream resource.</summary>
/// <remarks>Created by AudioStreamSynchronized. Start uses the same requested time for every child; gain edits
/// are live while structural resource edits stop/rebuild these states. Mixing uses prepared 128-frame blocks.
/// All controls/mixing serialize on the audio gate; callback mutation/reentry rejects before disposal begins.</remarks>
public sealed class AudioStreamPlaybackSynchronized : AudioStreamPlayback
{
    private readonly AudioStreamSynchronized _source;
    private AudioStreamPlayback?[] _children;
    private readonly Vector2[] _scratch = new Vector2[128];
    private int _count;
    private bool _active, _busy, _querying;
    private FAudioStreamVoice? _nativeOwner;
    internal override bool RequiresAudioOwner { get { if (base.RequiresAudioOwner) return true; foreach (var child in _children) if (child?.RequiresAudioOwner == true) return true; return false; } }
    internal AudioStreamPlaybackSynchronized(AudioStreamSynchronized source, AudioStreamPlayback?[] children, int count) { _source = source; _children = children; _count = count; }
    private void Check() { ThrowIfDisposed(); ObjectDisposedException.ThrowIf(_source.IsDisposed, _source); }
    internal void EnsureIdle() { if (_busy || _querying) throw new InvalidOperationException("Synchronized child callbacks cannot reenter playback mutation or mixing."); }
    internal void CheckControlOwner() { if (RequiresAudioOwner) AudioServer.Service.Check(); }
    private void CheckControl() { Check(); EnsureIdle(); CheckControlOwner(); if (_source.Editing) throw new InvalidOperationException("Synchronized factories/cleanup cannot mutate playback."); }
    internal AudioStreamPlayback?[] ReplaceChildren(AudioStreamPlayback?[] children, int count) { EnsureIdle(); var previous = _children; _children = children; _count = count; _active = false; UpdateNativeOwner(_nativeOwner); return previous; }
    internal static Exception? ReleaseChildren(AudioStreamPlayback?[] children)
    {
        Exception? error = null;
        foreach (var child in children) if (child is not null)
            {
                if (!child.IsDisposed) try { child.Stop(); } catch (Exception failure) { error = AudioStreamSynchronized.Combine(error, failure); }
                try { child.Dispose(); } catch (Exception failure) { error = AudioStreamSynchronized.Combine(error, failure); }
            }
        Array.Clear(children); return error;
    }
    internal override void UpdateNativeOwner(FAudioStreamVoice? owner, bool enabled = true) { _nativeOwner = owner; foreach (var child in _children) child?.UpdateNativeOwner(owner, enabled && _active); }
    internal override void PrepareQueuedControls()
    {
        var server = AudioServer.Service; server.LockCore();
        try { CheckControl(); foreach (var child in _children) child?.PrepareQueuedControls(); }
        finally { server.UnlockCore(); }
    }
    private Exception? StopChildren(bool queued = false)
    {
        _active = false; Exception? error = null;
        foreach (var child in _children) if (child is not null && !child.IsDisposed) try { if (queued) child.StopQueued(); else child.Stop(); } catch (Exception failure) { error = AudioStreamSynchronized.Combine(error, failure); }
        return error;
    }
    internal override void StartQueued(double time) { lock (AudioServer.Service.StreamGate) StartCore(time, true); }
    internal override void StopQueued() { lock (AudioServer.Service.StreamGate) StopCore(true); }
    private void StartCore(double fromPosition, bool queued)
    {
        Check(); EnsureIdle(); if (!queued) CheckControlOwner();
        if (_source.Editing) throw new InvalidOperationException("Synchronized factories/cleanup cannot mutate playback.");
        _busy = true;
        try
        {
            if (_active) { var stopped = StopChildren(queued); if (stopped is not null) throw stopped; }
            Exception? error = null;
            for (var i = 0; i < _count; i++) if (_children[i] is { } child)
                {
                    try { if (queued) child.StartQueued(fromPosition); else child.Start(fromPosition); Check(); _active = true; } catch (Exception failure) { error = AudioStreamSynchronized.Combine(error, failure); }
                    if (_source.IsDisposed) break;
                }
            if (error is not null) Resource.ThrowCombined(error, StopChildren(queued));
        }
        finally { _busy = false; }
    }
    private void StopCore(bool queued)
    {
        ThrowIfDisposed(); EnsureIdle(); if (!queued) CheckControlOwner();
        if (_source.Editing) throw new InvalidOperationException("Factory/cleanup cannot stop synchronized playback.");
        _busy = true; try { var error = StopChildren(queued); if (error is not null) throw error; } finally { _busy = false; }
    }
    /// <inheritdoc />
    protected override void OnStart(double fromPosition)
    {
        var server = AudioServer.Service; server.LockCore(); try { StartCore(fromPosition, false); } finally { server.UnlockCore(); }
    }
    /// <inheritdoc />
    protected override void OnStop()
    {
        var server = AudioServer.Service; server.LockCore(); try { StopCore(false); } finally { server.UnlockCore(); }
    }
    /// <inheritdoc />
    protected override bool OnIsPlaying() { lock (AudioServer.Service.StreamGate) { Check(); return _active; } }
    /// <inheritdoc />
    protected override void OnSeek(double time)
    {
        var server = AudioServer.Service; server.LockCore();
        try
        {
            CheckControl(); _busy = true;
            try { Exception? error = null; for (var i = 0; i < _count; i++) if (_children[i] is { } child) try { child.Seek(time); } catch (Exception failure) { error = AudioStreamSynchronized.Combine(error, failure); } if (error is not null) throw error; }
            finally { _busy = false; }
        }
        finally { server.UnlockCore(); }
    }
    private double Cursor(bool loops)
    {
        lock (AudioServer.Service.StreamGate)
        {
            Check(); if (_querying) throw new InvalidOperationException("Child cursor callbacks cannot reenter synchronized queries."); _querying = true;
            try
            {
                var found = false; double result = 0;
                for (var i = 0; i < _count; i++) if (_children[i] is { } child && child.IsPlaying())
                    {
                        var value = loops ? child.GetLoopCount() : child.GetPlaybackPosition();
                        Check(); if (!double.IsFinite(value)) throw new InvalidOperationException("Child playback positions must be finite.");
                        result = !found ? value : loops ? Math.Min(result, value) : Math.Max(result, value); found = true;
                    }
                return result;
            }
            finally { _querying = false; }
        }
    }
    /// <inheritdoc />
    protected override int OnGetLoopCount() => (int)Cursor(loops: true);
    /// <inheritdoc />
    protected override double OnGetPlaybackPosition() => Cursor(loops: false);
    /// <inheritdoc />
    protected override int OnMix(Span<Vector2> buffer, float rateScale)
    {
        lock (AudioServer.Service.StreamGate)
        {
            Check(); EnsureIdle(); buffer.Clear(); if (!_active) return 0; _busy = true;
            try
            {
                var anyActive = false;
                for (var offset = 0; offset < buffer.Length; offset += 128)
                {
                    var output = buffer.Slice(offset, Math.Min(128, buffer.Length - offset));
                    for (var i = 0; i < _count; i++) if (_children[i] is { } child && child.IsPlaying())
                        {
                            var gain = _source.Gain(i); var scratch = _scratch.AsSpan(0, output.Length); scratch.Clear();
                            var count = child.MixInto(scratch, rateScale); Check(); scratch[count..].Clear(); anyActive = true;
                            for (var frame = 0; frame < output.Length; frame++)
                            {
                                if (!scratch[frame].IsFinite()) throw new ArithmeticException("Child PCM must remain finite.");
                                var value = new Vector2((float)((double)output[frame].X + (double)scratch[frame].X * gain), (float)((double)output[frame].Y + (double)scratch[frame].Y * gain));
                                if (!value.IsFinite()) throw new ArithmeticException("Synchronized PCM exceeds finite float frames."); output[frame] = value;
                            }
                        }
                }
                if (!anyActive && buffer.Length != 0) _active = false;
                return buffer.Length;
            }
            finally { _busy = false; }
        }
    }
    /// <inheritdoc />
    protected override void ValidateDisposal()
    {
        var server = AudioServer.Service; server.LockCore(); try { EnsureIdle(); CheckControlOwner(); if (_source.Editing) throw new InvalidOperationException("Factory/cleanup cannot dispose synchronized playback."); } finally { server.UnlockCore(); }
        base.ValidateDisposal();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            lock (AudioServer.Service.StreamGate)
            {
                _busy = true; _active = false;
                try { _source.Unregister(this); var children = _children; _children = []; _count = 0; var error = ReleaseChildren(children); if (error is not null) throw error; }
                finally { _busy = false; base.Dispose(disposing); }
            }
        }
        else base.Dispose(disposing);
    }
}
