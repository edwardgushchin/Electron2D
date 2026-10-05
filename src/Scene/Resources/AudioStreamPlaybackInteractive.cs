namespace Electron2D;

/// <summary>Owns independent named-clip selection, musical waits, fades and automatic progression.</summary>
/// <remarks>Created by AudioStreamInteractive. Start prepares child ownership on the control thread and starts
/// the initial clip at zero; finite start/seek positions do not move the interactive timeline. Mixing advances
/// at the output clock (child rate one), using reusable 1024-frame blocks. The parent stays active during silence
/// until Stop or structural invalidation. Prepared scheduled controls support nested recording streams.</remarks>
public sealed class AudioStreamPlaybackInteractive : AudioStreamPlayback
{
    private sealed class State(AudioStream stream, AudioStreamPlayback playback)
    {
        internal readonly AudioStream Stream = stream;
        internal readonly AudioStreamPlayback Playback = playback;
        internal bool Active, First;
        internal double Wait, Gain, Speed, Previous;
        internal int Auto = -1;
        internal long Order;
    }
    private readonly AudioStreamInteractive _source;
    private State?[] _states = new State?[AudioStreamInteractive.Capacity];
    private readonly Vector2[] _scratch = new Vector2[1024];
    private readonly int[] _events = new int[AudioStreamInteractive.Capacity];
    private long _version = -1, _order, _currentOrder;
    private bool _active, _busy;
    private int _current = -1, _request = -1, _held = -1;
    internal AudioStreamPlaybackInteractive(AudioStreamInteractive source) => _source = source;
    internal override void UpdateNativeOwner(FAudioStreamVoice? owner, bool enabled = true) { foreach (var state in _states) if (state is not null) state.Playback.UpdateNativeOwner(owner, enabled && _active && state.Active && state.Wait <= 0); }
    internal override bool RequiresAudioOwner { get { if (base.RequiresAudioOwner) return true; foreach (var state in _states) if (state?.Playback.RequiresAudioOwner == true) return true; return false; } }
    /// <summary>Gets the typed clip-name parameter used by AudioStreamPlayer and AudioStreamEmitter.</summary>
    /// <value>An empty string cancels a pending request; names resolve to the first active matching slot.</value>
    public static PropertyDescriptor<AudioStreamPlaybackInteractive, string> SwitchToClipParameter { get; } = new("switch_to_clip", p => p.ParameterName, (p, v) => p.SwitchToClipByName(v), _ => string.Empty, stored: true);
    private string ParameterName
    {
        get { lock (AudioServer.Service.StreamGate) { Check(); Idle(); var c = _source.Capture(); var index = _request >= 0 ? _request : _current; return index >= 0 && index < c.Count ? c.Clips[index].Name : string.Empty; } }
    }
    private void Check() { ThrowIfDisposed(); ObjectDisposedException.ThrowIf(_source.IsDisposed, _source); }
    private void Idle() { if (_busy) throw new InvalidOperationException("Interactive child callbacks cannot reenter controls, queries or mixing."); }
    /// <summary>Queues a clip switch for the next nonempty active mix.</summary>
    /// <param name="clipIndex">An active clip index, or minus one to cancel a pending request.</param>
    /// <remarks>Requests made before Start are retained. A null clip is ignored without starting another child.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The index is outside the active prefix.</exception>
    /// <exception cref="InvalidOperationException">A child callback reenters controls.</exception>
    public void SwitchToClip(int clipIndex)
    {
        var server = AudioServer.Service; server.LockCore();
        try { Check(); Idle(); var c = _source.Capture(); if (clipIndex < -1 || clipIndex >= c.Count) throw new ArgumentOutOfRangeException(nameof(clipIndex)); _request = clipIndex; }
        finally { server.UnlockCore(); }
    }
    /// <summary>Queues the first active clip with a matching literal name.</summary>
    /// <param name="clipName">Non-null name; an empty string cancels a pending request.</param>
    /// <exception cref="ArgumentNullException">The name is null.</exception>
    /// <exception cref="ArgumentException">No active clip has that name.</exception>
    public void SwitchToClipByName(string clipName)
    {
        ArgumentNullException.ThrowIfNull(clipName); var server = AudioServer.Service; server.LockCore();
        try { Check(); Idle(); if (clipName.Length == 0) { _request = -1; return; } var c = _source.Capture(); for (var i = 0; i < c.Count; i++) if (c.Clips[i].Name == clipName) { _request = i; return; } throw new ArgumentException("The clip name is absent.", nameof(clipName)); }
        finally { server.UnlockCore(); }
    }
    /// <summary>Gets the last clip whose scheduled first frame became current.</summary>
    /// <returns>Minus one before a successful start, otherwise the last current index, also after Stop.</returns>
    public int GetCurrentClipIndex() { lock (AudioServer.Service.StreamGate) { Check(); Idle(); return _current; } }
    private void Prepare()
    {
        Check(); Idle(); _source.EnsurePlaybackOwner();
        _source.EnterCall(0); _busy = true;
        try
        {
            _source.BeginFactories();
            try
            {
                var c = _source.Capture();
                if (_version != c.Version)
                {
                    var next = new State?[_states.Length];
                    try
                    {
                        for (var i = 0; i < c.Count; i++) if (c.Clips[i].Stream is { } stream)
                            {
                                if (_states[i] is { } old && ReferenceEquals(old.Stream, stream)) next[i] = old;
                                else next[i] = new(stream, stream.InstantiatePlayback());
                                Check(); next[i]!.Playback.PrepareQueuedControls(); Check();
                            }
                    }
                    catch (Exception error) { var cleanup = Release(next, _states); if (cleanup is not null) Resource.ThrowCombined(error, cleanup); throw; }
                    var previous = _states; var stopped = StopStates(false); _states = next; _version = c.Version;
                    Resource.ThrowCombined(stopped, Release(previous, next));
                }
                else foreach (var state in _states) state?.Playback.PrepareQueuedControls();
            }
            finally { _source.EndFactories(); }
        }
        finally { _busy = false; AudioStream.ExitCall(); }
    }
    private static Exception? Release(State?[] states, State?[]? retained = null)
    {
        Exception? error = null;
        for (var i = 0; i < states.Length; i++) if (states[i] is { } state && (retained is null || !ReferenceEquals(state, retained[i])))
            {
                if (!state.Playback.IsDisposed) try { state.Playback.Stop(); } catch (Exception failure) { error = AudioStreamSynchronized.Combine(error, failure); }
                try { state.Playback.Dispose(); } catch (Exception failure) { error = AudioStreamSynchronized.Combine(error, failure); }
            }
        return error;
    }
    internal override void PrepareQueuedControls()
    {
        var server = AudioServer.Service; server.LockCore(); try { Prepare(); } finally { server.UnlockCore(); }
    }
    /// <inheritdoc />
    protected override void OnStart(double fromPosition)
    {
        var server = AudioServer.Service; server.LockCore(); try { Prepare(); StartCore(); } finally { server.UnlockCore(); }
    }
    internal override void StartQueued(double time) { lock (AudioServer.Service.StreamGate) { Check(); Idle(); if (_source.Capture().Version != _version) { var error = StopStates(true); if (error is not null) throw error; return; } StartCore(); } }
    private void StartCore()
    {
        Check(); Idle(); _source.EnterCall(1); _busy = true;
        try
        {
            var error = StopStates(true); if (error is not null) throw error;
            _current = -1; _held = -1; _currentOrder = 0; var c = _source.Capture();
            if ((uint)c.Initial < (uint)c.Count && _states[c.Initial] is not null) { _active = true; Queue(c.Initial, false, c); }
        }
        catch (Exception error) { var cleanup = StopStates(true); if (cleanup is not null) Resource.ThrowCombined(error, cleanup); throw; }
        finally { _busy = false; AudioStream.ExitCall(); }
    }
    private Exception? StopStates(bool queued)
    {
        _active = false; Exception? error = null;
        foreach (var state in _states) if (state is not null)
            {
                state.Active = false; state.Auto = -1; state.Wait = state.Gain = state.Speed = 0; state.First = true;
                if (!state.Playback.IsDisposed) try { if (queued) state.Playback.StopQueued(); else state.Playback.Stop(); } catch (Exception failure) { error = AudioStreamSynchronized.Combine(error, failure); }
            }
        return error;
    }
    /// <inheritdoc />
    protected override void OnStop()
    {
        var server = AudioServer.Service; server.LockCore(); try { Idle(); if (RequiresAudioOwner) server.Check(); _source.EnterCall(1); _busy = true; try { _request = -1; var error = StopStates(false); if (error is not null) throw error; } finally { _busy = false; AudioStream.ExitCall(); } } finally { server.UnlockCore(); }
    }
    internal override void StopQueued() { lock (AudioServer.Service.StreamGate) { ThrowIfDisposed(); Idle(); _source.EnterCall(1); _busy = true; try { _request = -1; var error = StopStates(true); if (error is not null) throw error; } finally { _busy = false; AudioStream.ExitCall(); } } }
    /// <inheritdoc />
    protected override bool OnIsPlaying() { lock (AudioServer.Service.StreamGate) { Check(); return _active; } }
    /// <inheritdoc />
    protected override double OnGetPlaybackPosition() { Check(); return 0; }
    /// <inheritdoc />
    protected override void OnSeek(double time) { Check(); }
    private static double Length(AudioStream stream)
    {
        var bpm = stream.ReadBPM(); var length = bpm > 0 && stream.ReadBeatCount() > 0 ? stream.ReadBeatCount() * (60 / bpm) : stream.GetLength();
        if (!double.IsFinite(bpm) || bpm < 0 || !double.IsFinite(length) || length < 0) throw new InvalidOperationException("Clip tempo and duration must be finite and nonnegative."); return length;
    }
    private int Auto(int index, int other, AudioStreamInteractive.Configuration c)
    {
        var clip = c.Clips[index]; var target = clip.Advance == AudioStreamInteractive.AutoAdvanceMode.Enabled ? clip.Next : clip.Advance == AudioStreamInteractive.AutoAdvanceMode.ReturnToHold ? _held : -1;
        if (clip.Advance == AudioStreamInteractive.AutoAdvanceMode.ReturnToHold) _held = -1;
        return target >= 0 && target < c.Count && target != index && target != other && _states[target] is not null ? target : -1;
    }
    private void Begin(int index, double position, double wait, double gain, double speed, int other, AudioStreamInteractive.Configuration c)
    {
        var state = _states[index]!; state.Playback.StartQueued(position); Check();
        state.Active = true; state.First = true; state.Wait = wait; state.Gain = gain; state.Speed = speed; state.Order = ++_order; state.Auto = Auto(index, other, c);
    }
    private void Queue(int destination, bool automatic, AudioStreamInteractive.Configuration c)
    {
        if ((uint)destination >= (uint)c.Count || _states[destination] is not { } to || destination == _current) return;
        if (_current < 0) { Begin(destination, 0, 0, 1, 0, -1, c); _current = destination; _currentOrder = to.Order; return; }
        var from = _states[_current]!;
        var rule = new AudioStreamInteractive.Transition(AudioStreamInteractive.TransitionFromTime.NextBeat, AudioStreamInteractive.TransitionToTime.Start, AudioStreamInteractive.FadeMode.Automatic, 1, false, 0, false);
        if (!c.Transitions.TryGetValue((_current, destination), out rule) && !c.Transitions.TryGetValue((_current, -1), out rule) && !c.Transitions.TryGetValue((-1, destination), out rule) && !c.Transitions.TryGetValue((-1, -1), out rule)) rule = new(AudioStreamInteractive.TransitionFromTime.NextBeat, AudioStreamInteractive.TransitionToTime.Start, AudioStreamInteractive.FadeMode.Automatic, 1, false, 0, false);
        if (rule.Fade == AudioStreamInteractive.FadeMode.Automatic) rule = rule with { Fade = rule.To == AudioStreamInteractive.TransitionToTime.Start ? AudioStreamInteractive.FadeMode.Out : AudioStreamInteractive.FadeMode.Cross };
        if (automatic) rule = rule with { From = AudioStreamInteractive.TransitionFromTime.End, To = rule.To == AudioStreamInteractive.TransitionToTime.SamePosition ? AudioStreamInteractive.TransitionToTime.Start : rule.To };
        var position = from.Playback.GetPlaybackPosition(); var bpm = from.Stream.ReadBPM(); var length = Length(from.Stream); Check();
        if (!double.IsFinite(position) || !double.IsFinite(bpm) || bpm < 0) throw new InvalidOperationException("Clip cursor and tempo must be finite.");
        var beat = bpm > 0 ? 60 / bpm : 1; double wait = 0;
        if (rule.From == AudioStreamInteractive.TransitionFromTime.End && length > 0) wait = Math.Max(0, length - position);
        else if (bpm > 0 && rule.From == AudioStreamInteractive.TransitionFromTime.NextBeat) wait = beat - ((position % beat + beat) % beat);
        else if (bpm > 0 && rule.From == AudioStreamInteractive.TransitionFromTime.NextBar && from.Stream.ReadBarBeats() > 0) { var bar = beat * from.Stream.ReadBarBeats(); wait = bar - ((position % bar + bar) % bar); }
        if (!double.IsFinite(beat) || !double.IsFinite(wait)) throw new InvalidOperationException("Clip timing exceeds finite seconds.");
        var duration = rule.Beats * beat; if (!double.IsFinite(duration)) throw new InvalidOperationException("Fade duration exceeds finite seconds."); var speed = duration > 0 ? 1 / duration : double.PositiveInfinity;
        double seek = 0; var destinationLength = Length(to.Stream);
        if (destinationLength > 0 && rule.To == AudioStreamInteractive.TransitionToTime.PreviousPosition) seek = to.Previous;
        else if (destinationLength > 0 && rule.To == AudioStreamInteractive.TransitionToTime.SamePosition && rule.From != AudioStreamInteractive.TransitionFromTime.End) { seek = position + wait; if (!double.IsFinite(seek) || seek > destinationLength) seek = 0; }
        var filler = rule.UseFiller && rule.Filler >= 0 && rule.Filler < c.Count && rule.Filler != _current && rule.Filler != destination && _states[rule.Filler] is not null ? rule.Filler : -1;
        var fillerLength = filler >= 0 ? Length(_states[filler]!.Stream) : 0;
        if (!double.IsFinite(wait + fillerLength)) throw new InvalidOperationException("Filler wait exceeds finite seconds.");
        // Cancel superseded delayed destinations before starting replacement states.
        for (var i = 0; i < c.Count; i++) if (i != _current && i != destination && _states[i] is { Active: true, Wait: > 0 } pending) { pending.Playback.StopQueued(); pending.Active = false; pending.Auto = -1; }
        var outgoing = rule.Fade is AudioStreamInteractive.FadeMode.Disabled or AudioStreamInteractive.FadeMode.In;
        var naturalEnd = rule.From == AudioStreamInteractive.TransitionFromTime.End && length > 0 && !from.Stream.ReadLoop();
        from.First = false; from.Wait = outgoing && naturalEnd ? 0 : wait; from.Speed = outgoing ? naturalEnd ? 0 : -1000 : -speed; from.Auto = -1;
        if (rule.Hold) _held = _current;
        var incoming = rule.Fade is AudioStreamInteractive.FadeMode.In or AudioStreamInteractive.FadeMode.Cross;
        if (filler >= 0) { Begin(filler, 0, wait, 1, 0, destination, c); _states[filler]!.Auto = -1; }
        Begin(destination, seek, wait + fillerLength, incoming && duration > 0 ? 0 : 1, incoming && duration > 0 ? speed : 0, filler, c);
    }
    private int MixState(int index, Span<Vector2> output, double rate)
    {
        var state = _states[index]!; if (!state.Active) return -1;
        var offset = 0; var dt = 1 / rate;
        if (state.First)
        {
            if (state.Wait >= output.Length * dt) { state.Wait -= output.Length * dt; return -1; }
            offset = Math.Clamp((int)Math.Ceiling(state.Wait * rate - 1e-8), 0, output.Length); state.Wait = 0; if (offset == output.Length) return -1; state.First = false;
            if (state.Order >= _currentOrder) { _currentOrder = state.Order; _current = index; }
        }
        var scratch = _scratch.AsSpan(0, output.Length - offset); scratch.Clear(); state.Previous = state.Playback.GetPlaybackPosition();
        if (!double.IsFinite(state.Previous)) throw new InvalidOperationException("Clip cursor must be finite.");
        var count = state.Playback.MixInto(scratch, 1); scratch[count..].Clear(); Check(); var advance = state.Speed == 0 && state.Auto >= 0;
        for (var i = 0; i < scratch.Length; i++)
        {
            if (state.Wait > 0) { state.Wait = Math.Max(0, state.Wait - dt); if (state.Wait * rate < 1e-8) state.Wait = 0; }
            else if (state.Speed != 0)
            {
                state.Gain = Math.Clamp(state.Gain + state.Speed * dt, 0, 1);
                if (state.Gain >= 1 && state.Speed > 0) { state.Speed = 0; advance = state.Auto >= 0; }
                else if (state.Gain <= 0 && state.Speed < 0) { state.Playback.StopQueued(); state.Active = false; break; }
            }
            if (!scratch[i].IsFinite()) throw new ArithmeticException("Clip PCM must be finite.");
            var value = new Vector2((float)((double)output[offset + i].X + scratch[i].X * state.Gain), (float)((double)output[offset + i].Y + scratch[i].Y * state.Gain));
            if (!value.IsFinite()) throw new ArithmeticException("Interactive PCM exceeds finite float frames."); output[offset + i] = value; state.Previous += dt;
        }
        if (!state.Playback.IsPlaying()) state.Active = false;
        var target = advance ? state.Auto : -1; if (advance) state.Auto = -1; return target;
    }
    private void Ready(AudioStreamInteractive.Configuration c)
    {
        for (var i = 0; i < c.Count; i++) if (_states[i] is { Active: true, First: true } state && state.Wait <= 0)
            {
                state.First = false; if (state.Order >= _currentOrder) { _currentOrder = state.Order; _current = i; }
            }
        if (_current >= 0 && _states[_current] is { Active: true, First: false, Speed: 0, Auto: >= 0 } current)
        {
            var target = current.Auto; current.Auto = -1; Queue(target, true, c);
        }
    }
    private int BlockLength(int remaining, double rate)
    {
        var limit = Math.Min(1024, remaining);
        foreach (var state in _states) if (state is { Active: true })
            {
                var seconds = state.Wait > 0 ? state.Wait : state.Speed > 0 ? (1 - state.Gain) / state.Speed : state.Speed < 0 ? -state.Gain / state.Speed : double.PositiveInfinity;
                if (double.IsFinite(seconds)) limit = (int)Math.Min(limit, Math.Max(1, Math.Ceiling(seconds * rate - 1e-8)));
            }
        return limit;
    }
    /// <inheritdoc />
    protected override int OnMix(Span<Vector2> buffer, float rateScale)
    {
        lock (AudioServer.Service.StreamGate)
        {
            Check(); Idle(); buffer.Clear(); if (!_active || buffer.Length == 0) return 0; _source.EnterCall(1); _busy = true;
            try
            {
                var c = _source.Capture(); if (c.Version != _version) { var error = StopStates(true); if (error is not null) throw error; return 0; }
                if (_request >= 0) { var target = _request; _request = -1; Queue(target, false, c); }
                var rate = AudioServer.GetMixRate();
                for (var offset = 0; offset < buffer.Length;)
                {
                    Ready(c); var length = BlockLength(buffer.Length - offset, rate);
                    var block = buffer.Slice(offset, length); var eventCount = 0;
                    for (var i = 0; i < c.Count; i++) if (_states[i] is { Active: true }) { var target = MixState(i, block, rate); if (target >= 0) _events[eventCount++] = target; }
                    // Apply automatic events once every child's block is mixed, independent of clip slot order.
                    for (var i = 0; i < eventCount; i++) Queue(_events[i], true, c);
                    offset += length;
                }
                return buffer.Length;
            }
            catch (Exception error) { var cleanup = StopStates(true); if (cleanup is not null) Resource.ThrowCombined(error, cleanup); throw; }
            finally { _busy = false; AudioStream.ExitCall(); }
        }
    }
    /// <inheritdoc />
    protected override void ValidateDisposal()
    {
        var server = AudioServer.Service; server.LockCore(); try { Idle(); if (RequiresAudioOwner) server.Check(); } finally { server.UnlockCore(); }
        base.ValidateDisposal();
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (!disposing) { base.Dispose(disposing); return; }
        lock (AudioServer.Service.StreamGate) { _active = false; _busy = true; try { var states = _states; _states = []; var error = Release(states); if (error is not null) throw error; } finally { _busy = false; base.Dispose(disposing); } }
    }
}
