using System.Runtime.InteropServices;

namespace Electron2D;

public partial class AnimationPlayer
{
    private readonly Dictionary<(string From, string To), double> _blendTimes = [];
    private readonly List<BlendClip> _blendClips = [];
    private readonly List<AnimationMixFrame> _mixFrames = [];
    private double _defaultBlendTime, _autoCaptureDuration = -1;
    private bool _autoCapture = true, _hasPlayback;
    private Tween.TransitionType _autoCaptureTransition = Tween.TransitionType.Linear;
    private Tween.EaseType _autoCaptureEase = Tween.EaseType.In;
    /// <summary>Gets or sets the finite nonnegative default crossfade duration in seconds.</summary>
    public double PlaybackDefaultBlendTime { get { ThrowIfDisposed(); return _defaultBlendTime; } set { EnsureAnimationMutable(); Nonnegative(value); _defaultBlendTime = value; } }
    /// <summary>Gets or sets automatic capture before Play, initially true.</summary>
    public bool PlaybackAutoCapture { get { ThrowIfDisposed(); return _autoCapture; } set { EnsureAnimationMutable(); _autoCapture = value; } }
    /// <summary>Gets or sets capture duration; negative values derive it from the first or last capture keys.</summary>
    public double PlaybackAutoCaptureDuration { get { ThrowIfDisposed(); return _autoCaptureDuration; } set { EnsureAnimationMutable(); Animation.Finite(value); _autoCaptureDuration = value; } }
    /// <summary>Gets or sets automatic capture's curve, initially Linear.</summary>
    public Tween.TransitionType PlaybackAutoCaptureTransitionType { get { ThrowIfDisposed(); return _autoCaptureTransition; } set { EnsureAnimationMutable(); TweenMath.Validate(value, _autoCaptureEase); _autoCaptureTransition = value; } }
    /// <summary>Gets or sets automatic capture's easing, initially In.</summary>
    public Tween.EaseType PlaybackAutoCaptureEaseType { get { ThrowIfDisposed(); return _autoCaptureEase; } set { EnsureAnimationMutable(); TweenMath.Validate(_autoCaptureTransition, value); _autoCaptureEase = value; } }
    /// <summary>Sets a nonnegative named transition duration; zero removes the entry.</summary>
    /// <param name="animationFrom">The existing source animation.</param>
    /// <param name="animationTo">The existing destination animation.</param>
    /// <param name="seconds">The finite nonnegative duration.</param>
    public void SetBlendTime(string animationFrom, string animationTo, double seconds)
    { EnsureAnimationMutable(); RequireAnimation(animationFrom); RequireAnimation(animationTo); Nonnegative(seconds); if (seconds == 0) _blendTimes.Remove((animationFrom, animationTo)); else _blendTimes[(animationFrom, animationTo)] = seconds; }
    /// <summary>Returns an explicitly configured transition duration, or zero.</summary>
    /// <param name="animationFrom">The exact source name.</param>
    /// <param name="animationTo">The exact destination name.</param>
    /// <returns>The stored duration, excluding wildcard/default fallback.</returns>
    public double GetBlendTime(string animationFrom, string animationTo) { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(animationFrom); ArgumentNullException.ThrowIfNull(animationTo); return _blendTimes.GetValueOrDefault((animationFrom, animationTo)); }
    /// <summary>Captures current target values and starts or resumes the selected clip.</summary>
    /// <param name="name">The qualified name, or empty for AssignedAnimation.</param>
    /// <param name="duration">Finite capture duration; negative derives the key interval, zero skips capture.</param>
    /// <param name="customBlend">Finite transition duration, or negative for configured/default fallback.</param>
    /// <param name="customSpeed">Finite signed clip speed, independent of the capture clock.</param>
    /// <param name="fromEnd">Whether to start or capture backwards from the section end.</param>
    /// <param name="transitionType">The capture fade curve.</param>
    /// <param name="easeType">The capture fade easing.</param>
    public void PlayWithCapture(string name = "", double duration = -1, double customBlend = -1, double customSpeed = 1, bool fromEnd = false,
        Tween.TransitionType transitionType = Tween.TransitionType.Linear, Tween.EaseType easeType = Tween.EaseType.In)
    { ValidatePlay(name, customBlend, customSpeed); Animation.Finite(duration); TweenMath.Validate(transitionType, easeType); PrepareCapture(name, fromEnd, duration, transitionType, easeType); PlaySection(name, -1, -1, customBlend, customSpeed, fromEnd); }
    private void ValidatePlay(string name, double blend, double speed)
    { EnsureAnimationMutable(); ArgumentNullException.ThrowIfNull(name); Animation.Finite(blend); Animation.Finite(speed); Animation.Finite(SpeedScale * speed); var animation = RequireAnimation(name.Length == 0 ? _assigned : name); ObjectDisposedException.ThrowIf(animation.IsDisposed, animation); }
    private void PrepareCapture(string name, bool fromEnd, double duration, Tween.TransitionType transition, Tween.EaseType ease)
    {
        if (name.Length == 0) name = _assigned; var animation = RequireAnimation(name); if (!animation.CaptureIncluded) return;
        if (duration < 0)
        {
            var position = name == _assigned ? _position : fromEnd ? animation.Length : 0; duration = 0;
            for (var i = 0; i < animation.GetTrackCount(); i++) { var track = animation.Get(i); if (track.Update != Animation.UpdateMode.Capture || track.Times.Count == 0) continue; duration = Math.Max(duration, fromEnd ? position - track.Times[^1] : track.Times[0] - position); }
        }
        if (duration > 1e-5) Capture(name, duration, transition, ease);
    }
    private double BlendDuration(string to, double requested)
    {
        if (requested >= 0) return requested;
        if (_blendTimes.TryGetValue((_assigned, to), out var duration) || _blendTimes.TryGetValue(("*", to), out duration) || _blendTimes.TryGetValue((_assigned, "*"), out duration)) return duration;
        return _defaultBlendTime;
    }
    private double CurrentBlendAmount() { var weight = 1d; foreach (var clip in _blendClips) weight -= clip.Left; return Math.Max(0, weight); }
    private void BeginTransition(double duration)
    {
        if (_hasPlayback && HasAnimation(_assigned) && !GetAnimation(_assigned).IsDisposed && duration > 0)
            _blendClips.Add(new(_assigned, _position, _customSpeed, _pingDirection, _start, _end, duration, CurrentBlendAmount()));
        else _blendClips.Clear();
    }
    private void MixPlayback(Animation animation, double position, bool backward, double? previous, double delta, bool done)
    {
        _mixFrames.Clear(); _mixFrames.Add(new(animation, position, backward, previous, CurrentBlendAmount(), GetSectionStartTime(), GetSectionEndTime(), delta * GetPlayingSpeed()));
        if (done) { _blendClips.Clear(); _mixFrames[0] = new(animation, position, backward, previous, 1, GetSectionStartTime(), GetSectionEndTime(), delta * GetPlayingSpeed()); }
        else for (var i = 0; i < _blendClips.Count; i++)
            {
                var clip = _blendClips[i]; clip.Left = Math.Max(0, clip.Left - Math.Abs(_speedScale * delta) / clip.Duration);
                if (HasAnimation(clip.Name))
                {
                    var old = GetAnimation(clip.Name);
                    if (!old.IsDisposed)
                    {
                        var start = Math.Min(Math.Max(0, clip.Start), old.Length); var end = clip.End < 0 ? old.Length : Math.Clamp(clip.End, start, old.Length);
                        var prior = clip.Position; var movement = delta * _speedScale * clip.Speed * clip.Direction; Animation.Finite(movement);
                        clip.Position = StepPosition(prior, movement, start, end, old.LoopMode, ref clip.Direction);
                        _mixFrames.Add(new(old, clip.Position, movement < 0, prior, clip.Left <= 1e-5 ? 1e-5 : clip.Left, start, end, movement));
                    }
                }
            }
        var revision = _playRevision; ApplyBlend(CollectionsMarshal.AsSpan(_mixFrames), delta * Math.Abs(_speedScale));
        if (IsDisposed || revision != _playRevision) return;
        for (var i = _blendClips.Count - 1; i >= 0; i--) if (_blendClips[i].Left <= 1e-5) _blendClips.RemoveAt(i);
    }
    private static double StepPosition(double position, double movement, double start, double end, SpriteFrames.LoopMode loop, ref int direction)
    {
        var result = position + movement; Animation.Finite(result); var span = end - start; if (span <= 0) return start;
        if (loop == SpriteFrames.LoopMode.Linear) return start + Mathf.PosMod(result - start, span);
        if (loop == SpriteFrames.LoopMode.PingPong) { var phase = Mathf.PosMod(result - start, span * 2); if (phase > span) direction = -direction; return start + (phase <= span ? phase : span * 2 - phase); }
        return Math.Clamp(result, start, end);
    }
    private static void Nonnegative(double value) { Animation.Finite(value); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); }
}
