namespace Electron2D;

/// <summary>Owns independent track order, timeline and outgoing fade state for a playlist.</summary>
/// <remarks>Created by AudioStreamPlaylist. Declared beat duration takes precedence over sample duration.
/// Playback timing uses the output clock and child rate one; parent rateScale does not alter the sequence.
/// A prepared 128-frame pair of buffers supplies finite full-request stereo PCM and bounded transitions.</remarks>
public sealed class AudioStreamPlaybackPlaylist : AudioStreamPlayback
{
    private readonly AudioStreamPlaylist _source;
    private AudioStreamPlayback?[] _children;
    private AudioStream?[] _streams;
    private readonly int[] _order = new int[AudioStreamPlaylist.MaxStreams];
    private readonly Vector2[] _scratch = new Vector2[128], _tail = new Vector2[128];
    private int _count, _current = -1, _fade = -1, _loops;
    private double _remaining, _offset, _fadeGain;
    private bool _active, _busy, _querying;
    private FAudioStreamVoice? _nativeOwner;
    internal AudioStreamPlaybackPlaylist(AudioStreamPlaylist source, AudioStreamPlayback?[] children, AudioStream?[] streams, int count) { _source = source; _children = children; _streams = streams; _count = count; foreach (var child in children) if (child is not null) child.CompositeOwner = this; }
    private void Check() { ThrowIfDisposed(); ObjectDisposedException.ThrowIf(_source.IsDisposed, _source); }
    internal void EnsureIdle() { if (_busy || _querying) throw new InvalidOperationException("Playlist child callbacks cannot reenter control, mixing or disposal."); }
    internal void CheckControlOwner() { if (RequiresAudioOwner) AudioServer.Instance.Check(); }
    internal override bool RequiresAudioOwner { get { if (base.RequiresAudioOwner) return true; foreach (var child in _children) if (child?.RequiresAudioOwner == true) return true; return false; } }
    internal override void UpdateNativeOwner(FAudioStreamVoice? owner, bool enabled = true) { _nativeOwner = owner; for (var i = 0; i < _count; i++) _children[i]?.UpdateNativeOwner(owner, enabled && _active && (i == Current || i == _fade)); }
    private int Current => _current < 0 ? -1 : _order[_current];
    internal AudioStreamPlayback?[] ReplaceChildren(AudioStreamPlayback?[] children, AudioStream?[] streams, int count) { EnsureIdle(); var previous = _children; _children = children; _streams = streams; _count = count; _active = false; _current = _fade = -1; foreach (var child in children) if (child is not null) child.CompositeOwner = this; UpdateNativeOwner(_nativeOwner); return previous; }
    internal static Exception? ReleaseChildren(AudioStreamPlayback?[] children) { foreach (var child in children) if (child is not null) { child.CompositeOwner = null; child.UpdateNativeOwner(null); } return AudioStreamPlaybackSynchronized.ReleaseChildren(children); }
    internal override void PrepareQueuedControls() { var server = AudioServer.Instance; server.Lock(); try { Check(); EnsureIdle(); CheckControlOwner(); foreach (var child in _children) child?.PrepareQueuedControls(); } finally { server.Unlock(); } }
    private double Length(int index)
    {
        _querying = true; try { var stream = _streams[index]!; var length = AudioStreamPlaylist.Duration(stream); Check(); return length; } finally { _querying = false; }
    }
    private void Order()
    {
        for (var i = 0; i < _count; i++) _order[i] = i;
        if (_source.Shuffle) for (var i = 0; i < _count; i++) { var other = Random.Shared.Next(_count); (_order[i], _order[other]) = (_order[other], _order[i]); }
    }
    private Exception? StopCore(bool queued)
    {
        _active = false; _current = _fade = -1; Exception? error = null;
        foreach (var child in _children) if (child is not null && !child.IsDisposed) try { if (queued) child.StopQueued(); else child.Stop(); } catch (Exception failure) { error = AudioStreamPlaylist.Combine(error, failure); }
        return error;
    }
    private void StartCore(double position, bool queued)
    {
        Check(); EnsureIdle(); if (!queued) CheckControlOwner(); if (_source.Editing) throw new InvalidOperationException("Playlist factories cannot start playback."); _busy = true;
        try
        {
            var stopped = StopCore(queued); if (stopped is not null) throw stopped; _loops = 0; _offset = 0; position = Math.Max(0, position);
            double total = 0; for (var i = 0; i < _count; i++) if (_streams[i] is not null) { total += Length(i); if (!double.IsFinite(total)) throw new ArithmeticException("Playlist length exceeds finite seconds."); }
            Check(); if (total <= 0) return;
            if (position >= total) { if (!_source.Loop) return; position %= total; }
            Order(); var cursor = position;
            for (var i = 0; i < _count; i++) if (_children[_order[i]] is not null)
                {
                    var length = Length(_order[i]); if (length <= 0) continue;
                    if (cursor < length) { _current = i; _remaining = length - cursor; if (queued) _children[Current]!.StartQueued(cursor); else _children[Current]!.Start(cursor); Check(); _active = true; _offset = position; return; }
                    cursor -= length;
                }
        }
        catch (Exception error) { Resource.ThrowCombined(error, StopCore(queued)); throw; }
        finally { _busy = false; }
    }
    internal override void StartQueued(double time) { lock (AudioServer.Instance.StreamGate) StartCore(time, true); }
    internal override void StopQueued() { lock (AudioServer.Instance.StreamGate) { EnsureIdle(); _busy = true; try { var error = StopCore(true); if (error is not null) throw error; } finally { _busy = false; } } }
    /// <inheritdoc />
    protected override void OnStart(double fromPosition) { var server = AudioServer.Instance; server.Lock(); try { StartCore(fromPosition, false); } finally { server.Unlock(); } }
    /// <inheritdoc />
    protected override void OnStop() { var server = AudioServer.Instance; server.Lock(); try { ThrowIfDisposed(); EnsureIdle(); CheckControlOwner(); _busy = true; try { var error = StopCore(false); if (error is not null) throw error; } finally { _busy = false; } } finally { server.Unlock(); } }
    /// <inheritdoc />
    protected override void OnSeek(double time) => OnStart(time);
    /// <inheritdoc />
    protected override bool OnIsPlaying() { lock (AudioServer.Instance.StreamGate) { Check(); return _active; } }
    /// <inheritdoc />
    protected override int OnGetLoopCount() { Check(); return _loops; }
    /// <inheritdoc />
    protected override double OnGetPlaybackPosition() { Check(); return _offset; }
    private bool Next()
    {
        var previous = Current;
        for (var attempts = 0; attempts < _count; attempts++)
        {
            _current++;
            if (_current >= _count)
            {
                if (!_source.Loop) { var error = StopCore(true); if (error is not null) throw error; return false; }
                Order(); _current = 0; _loops = _loops == int.MaxValue ? int.MaxValue : _loops + 1; _offset = 0;
            }
            var index = Current; if (_children[index] is null) continue; var length = Length(index); if (length <= 0) continue;
            if (_fade >= 0) { _children[_fade]!.StopQueued(); _fade = -1; }
            if (previous == index)
            {
                if (!_streams[index]!.ReadLoop()) _children[index]!.StartQueued(0);
            }
            else
            {
                if (_source.FadeTime > 0) { _fade = previous; _fadeGain = 1; } else _children[previous]!.StopQueued();
                _children[index]!.StartQueued(0);
            }
            _remaining = length; return true;
        }
        var stopped = StopCore(true); if (stopped is not null) throw stopped; return false;
    }
    /// <inheritdoc />
    protected override int OnMix(Span<Vector2> buffer, float rateScale)
    {
        lock (AudioServer.Instance.StreamGate)
        {
            Check(); EnsureIdle(); buffer.Clear(); if (!_active || buffer.IsEmpty) return 0; _source.EnterCall(4); _busy = true;
            try
            {
                var rate = AudioServer.Instance.GetMixRate(); var offset = 0;
                while (offset < buffer.Length && _active)
                {
                    if (_remaining <= 1e-12) { if (!Next()) break; }
                    var count = Math.Min(128, buffer.Length - offset); count = Math.Min(count, Math.Max(1, (int)Math.Min(count, Math.Ceiling(_remaining * rate - 1e-9))));
                    var scratch = _scratch.AsSpan(0, count); scratch.Clear(); var mixed = _children[Current]!.MixInto(scratch, 1); Check(); scratch[mixed..].Clear();
                    var tail = _tail.AsSpan(0, count); tail.Clear(); if (_fade >= 0) { var faded = _children[_fade]!.MixInto(tail, 1); Check(); tail[faded..].Clear(); }
                    var fadeTime = _source.FadeTime; var decrement = fadeTime == 0 ? double.PositiveInfinity : 1 / (fadeTime * rate);
                    for (var i = 0; i < count; i++)
                    {
                        var gain = _fade >= 0 && fadeTime > 0 ? Math.Max(0, _fadeGain) : 0;
                        if (!scratch[i].IsFinite() || !tail[i].IsFinite()) throw new ArithmeticException("Playlist child PCM must be finite.");
                        var frame = new Vector2((float)((double)scratch[i].X + tail[i].X * gain), (float)((double)scratch[i].Y + tail[i].Y * gain)); if (!frame.IsFinite()) throw new ArithmeticException("Playlist sum exceeds finite PCM."); buffer[offset + i] = frame;
                        if (_fade >= 0) _fadeGain -= decrement;
                    }
                    if (_fade >= 0 && (_fadeGain <= 0 || fadeTime == 0)) { _children[_fade]!.StopQueued(); _fade = -1; }
                    var seconds = count / (double)rate; _remaining -= seconds; _offset += seconds; offset += count;
                    if (_remaining <= 1e-12 && offset == buffer.Length) Next();
                }
                return buffer.Length;
            }
            catch (Exception error) { buffer.Clear(); Resource.ThrowCombined(error, StopCore(true)); throw; }
            finally { AudioStream.ExitCall(); _busy = false; }
        }
    }
    /// <inheritdoc />
    protected override void ValidateDisposal() { var server = AudioServer.Instance; server.Lock(); try { EnsureIdle(); CheckControlOwner(); if (_source.Editing) throw new InvalidOperationException("Playlist factories cannot dispose playback."); } finally { server.Unlock(); } base.ValidateDisposal(); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { lock (AudioServer.Instance.StreamGate) { _busy = true; _active = false; try { _source.Unregister(this); var children = _children; _children = []; _streams = []; _count = 0; var error = ReleaseChildren(children); if (error is not null) throw error; } finally { _busy = false; _nativeOwner = null; base.Dispose(disposing); } } } else base.Dispose(disposing); }
}
