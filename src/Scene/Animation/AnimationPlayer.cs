namespace Electron2D;

/// <summary>Plays named typed scene-property timelines forwards, backwards, in sections or in a queue.</summary>
/// <remarks>Play selects immediately and applies keys on the next Advance; Advance(0) applies immediately.
/// Pause retains position and assignment and clears capture caches and queued names. Stop resets position and speed and optionally applies the start state.
/// Loops retain overshoot; finite completion holds the endpoint and emits once. Named/default crossfades retain
/// prior clips while Capture tracks blend from an independent snapshot of current target values.</remarks>
public partial class AnimationPlayer : AnimationMixer
{
    private static readonly PropertyDescriptor[] AnimationProperties =
    [
        new PropertyDescriptor<AnimationPlayer, double>(nameof(PlaybackDefaultBlendTime), n => n.PlaybackDefaultBlendTime, (n, v) => n.PlaybackDefaultBlendTime = v, _ => 0),
        new PropertyDescriptor<AnimationPlayer, bool>(nameof(PlaybackAutoCapture), n => n.PlaybackAutoCapture, (n, v) => n.PlaybackAutoCapture = v, _ => true),
        new PropertyDescriptor<AnimationPlayer, double>(nameof(PlaybackAutoCaptureDuration), n => n.PlaybackAutoCaptureDuration, (n, v) => n.PlaybackAutoCaptureDuration = v, _ => -1),
        new PropertyDescriptor<AnimationPlayer, Tween.TransitionType>(nameof(PlaybackAutoCaptureTransitionType), n => n.PlaybackAutoCaptureTransitionType, (n, v) => n.PlaybackAutoCaptureTransitionType = v, _ => Tween.TransitionType.Linear),
        new PropertyDescriptor<AnimationPlayer, Tween.EaseType>(nameof(PlaybackAutoCaptureEaseType), n => n.PlaybackAutoCaptureEaseType, (n, v) => n.PlaybackAutoCaptureEaseType = v, _ => Tween.EaseType.In),
        new PropertyDescriptor<AnimationPlayer, string>(nameof(Autoplay), n => n.Autoplay, (n, v) => n.Autoplay = v, _ => ""),
        new PropertyDescriptor<AnimationPlayer, double>(nameof(SpeedScale), n => n.SpeedScale, (n, v) => n.SpeedScale = v, _ => 1),
    ];
    /// <summary>Creates an idle player with inherited automatic scheduling and deferred method dispatch.</summary>
    public AnimationPlayer() { }
    private string _assigned = "", _autoplay = "";
    private bool _methodSeekPending, _methodSeekExternal;
    private bool _playing, _stopping;
    private double _position, _speedScale = 1, _customSpeed = 1, _start = -1, _end = -1;
    private int _pingDirection = 1;
    private long _playRevision;
    internal long PlaybackRevision => _playRevision;
    private readonly Queue<string> _queue = new();
    private readonly Dictionary<string, string> _next = new(StringComparer.Ordinal);
    /// <summary>Gets or sets the selected animation; stopped assignment rewinds without playing, while an active assignment switches clips.</summary>
    public string AssignedAnimation { get { ThrowIfDisposed(); return _assigned; } set { EnsureAnimationMutable(); RequireAnimation(value); if (_playing) { var speed = _customSpeed * _pingDirection; Play(value, customSpeed: speed, fromEnd: speed < 0); return; } _hasPlayback = true; _assigned = value; _position = 0; _start = _end = -1; _playRevision++; InvalidateEvaluation(); CurrentAnimationChanged?.Invoke(value); } }
    /// <summary>Gets the playing animation name, or assigns a name to start it; empty or the stop sentinel defers stopping while attached.</summary>
    public string CurrentAnimation
    {
        get { ThrowIfDisposed(); return _playing ? _assigned : ""; }
        set
        {
            EnsureAnimationMutable(); ArgumentNullException.ThrowIfNull(value);
            if (value is "" or "[stop]")
            { if (_playing) { if (Tree is not null) Tree.Defer(StopDeferred); else Stop(); } }
            else if (!_playing) Play(value);
            else if (value != _assigned) { var speed = _customSpeed * _pingDirection; Play(value, customSpeed: speed, fromEnd: speed < 0); }
        }
    }
    private void StopDeferred() { if (!IsDisposed && _playing) Stop(); }

    /// <summary>Gets or sets the name started on the ready notification; empty disables autoplay.</summary>
    public string Autoplay { get { ThrowIfDisposed(); return _autoplay; } set { EnsureAnimationMutable(); ArgumentNullException.ThrowIfNull(value); _autoplay = value; } }
    /// <summary>Gets the complete selected resource duration, or zero without a selection.</summary>
    public double CurrentAnimationLength { get { ThrowIfDisposed(); return HasAnimation(_assigned) ? GetAnimation(_assigned).Length : 0; } }
    /// <summary>Gets the current timeline position in seconds.</summary>
    public double CurrentAnimationPosition { get { ThrowIfDisposed(); return _position; } }
    /// <summary>Gets or sets the finite signed playback multiplier; zero keeps playback enabled.</summary>
    public double SpeedScale { get { ThrowIfDisposed(); return _speedScale; } set { EnsureAnimationMutable(); Animation.Finite(value); Animation.Finite(value * _customSpeed); _speedScale = value; _playRevision++; InvalidateEvaluation(); } }
    /// <summary>Occurs when queued or configured-next playback changes the selected animation.</summary>
    public event Action<string, string>? AnimationChanged;
    /// <summary>Occurs when the current animation selection changes.</summary>
    public event Action<string>? CurrentAnimationChanged;
    /// <summary>Returns whether animation evaluation is active.</summary>
    public bool IsAnimationActive() => Active;
    /// <summary>Returns the animation root path.</summary>
    public string GetRoot() => RootNode;
    /// <summary>Sets the animation root path.</summary>
    /// <param name="path">The relative target path.</param>
    public void SetRoot(string path) => RootNode = path;
    /// <summary>Returns the automatic animation update phase.</summary>
    public AnimationCallbackModeProcess GetProcessCallback() => CallbackModeProcess;
    /// <summary>Sets the automatic animation update phase.</summary>
    /// <param name="mode">The defined update or process mode.</param>
    public void SetProcessCallback(AnimationCallbackModeProcess mode) => CallbackModeProcess = mode;
    /// <summary>Returns whether playback is enabled, including with zero speed.</summary>
    public bool IsPlaying() { ThrowIfDisposed(); return _playing; }
    /// <summary>Returns zero while paused, otherwise the signed effective speed.</summary>
    public double GetPlayingSpeed() { ThrowIfDisposed(); return _playing ? _speedScale * _customSpeed * _pingDirection : 0; }
    /// <summary>Starts or resumes a selected timeline. Negative customBlend selects configured/default crossfade timing; zero switches immediately.</summary>
    /// <param name="name">The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.</param>
    /// <param name="customBlend">Finite crossfade duration; negative uses configured/default timing.</param>
    /// <param name="customSpeed">A finite signed playback multiplier.</param>
    /// <param name="fromEnd">Whether a new or completed selection starts at the section end.</param>
    public void Play(string name = "", double customBlend = -1, double customSpeed = 1, bool fromEnd = false) { if (_autoCapture) PlayWithCapture(name, _autoCaptureDuration, customBlend, customSpeed, fromEnd, _autoCaptureTransition, _autoCaptureEase); else PlaySection(name, -1, -1, customBlend, customSpeed, fromEnd); }
    /// <summary>Starts or resumes reverse playback from the end.</summary>
    /// <param name="name">The exact ordinal name.</param>
    /// <param name="customBlend">The typed argument for this operation, using the defaults described above.</param>
    public void PlayBackwards(string name = "", double customBlend = -1) => Play(name, customBlend, -1, true);
    /// <summary>Plays a bounded timeline section; negative boundaries select the resource endpoints.</summary>
    /// <param name="startTime">The section start in seconds, or a negative value for the resource start.</param>
    /// <param name="endTime">The section end in seconds, or a negative value for the resource end.</param>
    /// <param name="name">The exact ordinal name.</param>
    /// <param name="customBlend">The typed argument for this operation, using the defaults described above.</param>
    /// <param name="customSpeed">The typed argument for this operation, using the defaults described above.</param>
    /// <param name="fromEnd">The typed argument for this operation, using the defaults described above.</param>
    public void PlaySection(string name = "", double startTime = -1, double endTime = -1, double customBlend = -1, double customSpeed = 1, bool fromEnd = false)
    {
        EnsureAnimationMutable(); ArgumentNullException.ThrowIfNull(name); Animation.Finite(customBlend); Animation.Finite(customSpeed); Animation.Finite(_speedScale * customSpeed);
        if (name.Length == 0) name = _assigned; var animation = RequireAnimation(name); ObjectDisposedException.ThrowIf(animation.IsDisposed, animation); ValidateSection(animation, startTime, endTime);
        BeginTransition(BlendDuration(name, customBlend));
        _hasPlayback = true;
        var changed = name != _assigned; if (changed) { StopNestedPlayback(); StopAudioPlayback(); }
        var wasPlaying = _playing; var start = BoundStart(startTime); var end = BoundEnd(animation, endTime);
        if (changed || _position < start || _position > end || (fromEnd && customSpeed < 0 && _position <= start) || (!fromEnd && customSpeed > 0 && _position >= end)) _position = fromEnd ? end : start;
        _methodSeekPending = changed || !wasPlaying || _position == (fromEnd ? end : start); _methodSeekExternal = false;
        _assigned = name; _customSpeed = customSpeed; _pingDirection = 1; _start = startTime; _end = endTime; _playing = true; _queue.Clear(); _playRevision++; InvalidateEvaluation();
        var revision = _playRevision; if (changed) CurrentAnimationChanged?.Invoke(name);
        if (!IsDisposed && revision == _playRevision && (changed || !wasPlaying)) Started(name);
    }
    /// <summary>Plays a bounded section backwards.</summary>
    /// <param name="name">The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.</param>
    /// <param name="startTime">The section start in seconds, or a negative value for the resource start.</param>
    /// <param name="endTime">The section end in seconds, or a negative value for the resource end.</param>
    /// <param name="customBlend">Finite crossfade duration; negative uses configured/default timing.</param>
    public void PlaySectionBackwards(string name = "", double startTime = -1, double endTime = -1, double customBlend = -1) => PlaySection(name, startTime, endTime, customBlend, -1, true);
    /// <summary>Plays a section using named marker times; empty or absent markers select endpoints.</summary>
    /// <param name="startMarker">The start marker, or empty/missing to use the start endpoint.</param>
    /// <param name="endMarker">The end marker, or empty/missing to use the end endpoint.</param>
    /// <param name="customSpeed">A finite signed playback multiplier.</param>
    /// <param name="fromEnd">Whether a new or completed selection starts at the section end.</param>
    /// <param name="name">The exact ordinal name.</param>
    /// <param name="customBlend">The typed argument for this operation, using the defaults described above.</param>
    public void PlaySectionWithMarkers(string name = "", string startMarker = "", string endMarker = "", double customBlend = -1, double customSpeed = 1, bool fromEnd = false)
    { ArgumentNullException.ThrowIfNull(name); var animation = RequireAnimation(name.Length == 0 ? _assigned : name); PlaySection(name, animation.GetMarkerTime(startMarker), animation.GetMarkerTime(endMarker), customBlend, customSpeed, fromEnd); }
    /// <summary>Plays a marker-defined section backwards.</summary>
    /// <param name="name">The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.</param>
    /// <param name="startMarker">The typed argument for this operation, using the defaults described above.</param>
    /// <param name="endMarker">The typed argument for this operation, using the defaults described above.</param>
    /// <param name="customBlend">The typed argument for this operation, using the defaults described above.</param>
    public void PlaySectionWithMarkersBackwards(string name = "", string startMarker = "", string endMarker = "", double customBlend = -1) => PlaySectionWithMarkers(name, startMarker, endMarker, customBlend, -1, true);
    /// <summary>Pauses while retaining selection/position; clears queued/captured work and stops still-controlled child players while keeping their values.</summary>
    public void Pause() { EnsureAnimationMutable(); _playing = false; _queue.Clear(); _playRevision++; InvalidateBindings(false); }
    /// <summary>Stops, resets position/speed and queued work, and stops still-controlled child players; keepState preserves target values.</summary>
    /// <param name="keepState">Whether to leave target property values unchanged while resetting playback.</param>
    public void Stop(bool keepState = false)
    {
        EnsureAnimationMutable(); if (_stopping) return; _stopping = true;
        try
        {
            _blendClips.Clear(); _hasPlayback = false; _playing = false; _position = 0; _customSpeed = 1; _pingDirection = 1; _start = _end = -1; _queue.Clear(); _playRevision++; InvalidateBindings(false);
            if (!keepState && HasAnimation(_assigned)) ApplyAnimation(GetAnimation(_assigned), 0, false, updateOnly: true);
        }
        finally { _stopping = false; }
    }
    /// <summary>Seeks within the current section; update applies values immediately without completion events.</summary>
    /// <param name="seconds">The finite requested seek time, clamped to the current section.</param>
    /// <param name="update">Whether to apply values synchronously.</param>
    /// <param name="updateOnly">Whether to suppress method callbacks while updating properties/curves and sampling nested players without starting stopped targets.</param>
    public void Seek(double seconds, bool update = false, bool updateOnly = false)
    { EnsureAnimationMutable(); Animation.Finite(seconds); if (!Active || !HasAnimation(_assigned)) return; var animation = GetAnimation(_assigned); var previous = _position; _position = Math.Clamp(seconds, GetSectionStartTime(), GetSectionEndTime()); _playRevision++; InvalidateEvaluation(); _methodSeekPending = !update; _methodSeekExternal = true; if (update) { if (_blendClips.Count != 0 || NeedsBlending) MixPlayback(animation, _position, _position < previous, null, 0, false, externalSeek: true, updateOnly: updateOnly); else ApplyAnimation(animation, _position, _position < previous, externalSeek: true, updateOnly: updateOnly); } }
    /// <summary>Queues an existing animation; starts immediately when no animation is playing.</summary>
    /// <param name="name">The ordinal animation, library or marker name; empty names are accepted only where explicitly documented.</param>
    public void Queue(string name) { EnsureAnimationMutable(); RequireAnimation(name); if (!_playing) Play(name); else _queue.Enqueue(name); }
    /// <summary>Clears queued animation names.</summary>
    public void ClearQueue() { EnsureAnimationMutable(); _queue.Clear(); }
    /// <summary>Returns an independent array of queued names in playback order.</summary>
    public string[] GetQueue() { ThrowIfDisposed(); return _queue.ToArray(); }
    /// <summary>Sets the animation played after a finite animation; empty clears the transition.</summary>
    /// <param name="animationFrom">The existing source animation.</param>
    /// <param name="animationTo">The existing next animation, or empty to remove its transition.</param>
    public void AnimationSetNext(string animationFrom, string animationTo) { EnsureAnimationMutable(); RequireAnimation(animationFrom); ArgumentNullException.ThrowIfNull(animationTo); if (animationTo.Length == 0) _next.Remove(animationFrom); else { RequireAnimation(animationTo); _next[animationFrom] = animationTo; } }
    /// <summary>Returns the configured next name, or empty.</summary>
    /// <param name="animationFrom">The typed argument for this operation, using the defaults described above.</param>
    public string AnimationGetNext(string animationFrom) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(animationFrom); return _next.GetValueOrDefault(animationFrom, ""); }
    /// <summary>Sets the current playback section, clamping position into it.</summary>
    /// <param name="startTime">The section start in seconds, or a negative value for the resource start.</param>
    /// <param name="endTime">The section end in seconds, or a negative value for the resource end.</param>
    public void SetSection(double startTime = -1, double endTime = -1) { EnsureAnimationMutable(); var animation = RequireAnimation(_assigned); ValidateSection(animation, startTime, endTime); _start = startTime; _end = endTime; _position = Math.Clamp(_position, GetSectionStartTime(), GetSectionEndTime()); _playRevision++; InvalidateEvaluation(); }
    /// <summary>Sets the current section using markers.</summary>
    /// <param name="startMarker">The start marker, or empty/missing to use the start endpoint.</param>
    /// <param name="endMarker">The end marker, or empty/missing to use the end endpoint.</param>
    public void SetSectionWithMarkers(string startMarker = "", string endMarker = "") { var animation = RequireAnimation(_assigned); SetSection(animation.GetMarkerTime(startMarker), animation.GetMarkerTime(endMarker)); }
    /// <summary>Restores complete-resource playback boundaries.</summary>
    public void ResetSection() { EnsureAnimationMutable(); _start = _end = -1; _playRevision++; InvalidateEvaluation(); }
    /// <summary>Returns whether either section boundary is explicit.</summary>
    public bool HasSection() { ThrowIfDisposed(); return _start >= 0 || _end >= 0; }
    /// <summary>Returns the effective section start.</summary>
    public double GetSectionStartTime() { ThrowIfDisposed(); return Math.Min(BoundStart(_start), CurrentAnimationLength); }
    /// <summary>Returns the effective section end.</summary>
    public double GetSectionEndTime() { ThrowIfDisposed(); return _end < 0 ? CurrentAnimationLength : Math.Clamp(_end, GetSectionStartTime(), CurrentAnimationLength); }
    /// <inheritdoc />
    internal override void AdvanceAnimation(double delta)
    {
        if (!_playing) return;
        if (!HasAnimation(_assigned)) { Pause(); return; }
        var animation = GetAnimation(_assigned); if (animation.IsDisposed) { Pause(); return; }
        var start = GetSectionStartTime(); var end = GetSectionEndTime(); var span = end - start;
        var movement = delta * GetPlayingSpeed(); Animation.Finite(movement); var position = _position + movement; Animation.Finite(position);
        var backward = movement < 0 || (movement == 0 && GetPlayingSpeed() < 0); var done = false;
        if (span <= 0) { position = start; done = true; }
        else if (animation.LoopMode == SpriteFrames.LoopMode.Linear) position = start + Mathf.PosMod(position - start, span);
        else if (animation.LoopMode == SpriteFrames.LoopMode.PingPong)
        {
            var phase = Mathf.PosMod(position - start, span * 2); position = start + (phase <= span ? phase : (span * 2) - phase);
            if (phase > span) { _pingDirection = -_pingDirection; backward = !backward; }
        }
        else { done = movement != 0 && (backward ? position <= start : position >= end); position = Math.Clamp(position, start, end); }
        var previousPosition = _position;
        var name = _assigned; var revision = _playRevision;
        try
        {
            var initial = _methodSeekPending; var external = _methodSeekExternal; _methodSeekPending = false;
            if (_blendClips.Count != 0 || NeedsBlending || animation.HasEventTracks()) { _position = position; MixPlayback(animation, position, backward, previousPosition, delta, done, initial, external, primaryMovement: movement); }
            else if (animation.HasDiscreteTracks() && movement != 0 && span > 0)
            {
                var remaining = movement; var cursor = previousPosition;
                while (remaining != 0 && !IsDisposed && revision == _playRevision && !animation.IsDisposed)
                {
                    var reverse = remaining < 0; var endpoint = reverse ? start : end;
                    var travel = Math.Min(Math.Abs(remaining), Math.Abs(endpoint - cursor));
                    var nextPosition = cursor + (reverse ? -travel : travel);
                    _position = nextPosition; ApplyAnimation(animation, nextPosition, reverse, cursor);
                    var nextRemaining = remaining + (reverse ? travel : -travel);
                    if (travel > 0 && nextRemaining == remaining) throw new InvalidOperationException("The discrete timeline delta cannot make representable progress.");
                    remaining = nextRemaining; cursor = nextPosition;
                    if (IsDisposed || revision != _playRevision || animation.IsDisposed) break;
                    if (cursor != endpoint) break;
                    if (animation.LoopMode == SpriteFrames.LoopMode.None) break;
                    if (animation.LoopMode == SpriteFrames.LoopMode.PingPong) remaining = -remaining;
                    else { cursor = reverse ? end : start; _position = cursor; ApplyAnimation(animation, cursor, reverse); }
                    if (travel == 0 && remaining == 0) break;
                }
            }
            else { _position = position; ApplyAnimation(animation, position, backward, previousPosition); }
        }
        catch (Exception failure)
        {
            if (!IsDisposed && revision == _playRevision)
            { try { Pause(); } catch (Exception cleanup) { throw new AggregateException("Animation evaluation and cleanup failed.", failure, cleanup); } }
            throw;
        }
        if (IsDisposed || revision != _playRevision) return;
        _position = position;
        if (!done) return;
        _playing = false; _playRevision++; var completedRevision = _playRevision;
        Finished(name); if (IsDisposed || completedRevision != _playRevision) return;
        var next = _queue.Count > 0 ? _queue.Dequeue() : _next.GetValueOrDefault(name, "");
        if (next.Length == 0 || !HasAnimation(next)) return;
        var queuedNames = _queue.ToArray(); Play(next); foreach (var queued in queuedNames) _queue.Enqueue(queued);
        if (!IsDisposed) AnimationChanged?.Invoke(name, next);
    }
    private sealed class BlendClip(string name, double position, double speed, int direction, double start, double end, double duration, double left)
    { internal readonly string Name = name; internal double Position = position, Left = left; internal readonly double Speed = speed, Start = start, End = end, Duration = duration; internal int Direction = direction; }
    private Animation RequireAnimation(string name) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); if (!HasAnimation(name)) throw new KeyNotFoundException($"Animation '{name}' was not found."); return GetAnimation(name); }
    private static double BoundStart(double start) => Math.Max(0, start);
    private static double BoundEnd(Animation animation, double end) => end < 0 ? animation.Length : Math.Min(end, animation.Length);
    private static void ValidateSection(Animation animation, double start, double end) { Animation.Finite(start); Animation.Finite(end); if (BoundStart(start) >= BoundEnd(animation, end)) throw new ArgumentException("Section start must precede its end within the animation."); }
    /// <inheritdoc />
    protected override void OnNotification(int what) { base.OnNotification(what); if (!IsDisposed && what == NotificationReady && _autoplay.Length != 0 && HasAnimation(_autoplay)) Play(_autoplay); }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _queue.Clear(); _next.Clear(); _blendClips.Clear(); _mixFrames.Clear(); _blendTimes.Clear(); AnimationChanged = null; CurrentAnimationChanged = null; } base.Dispose(disposing); }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(AnimationProperties);

}
