using Clip = Electron2D.Animation;

namespace Electron2D;

/// <summary>Selects forward or reversed sampling of a graph animation clip.</summary>
public enum AnimationPlayMode
{
    /// <summary>Sample from the clip start toward its end.</summary>
    Forward = 0,
    /// <summary>Sample from the clip end toward its start.</summary>
    Backward = 1,
}
/// <summary>A graph leaf that samples a named clip through a per-tree timeline.</summary>
/// <remarks>Custom timelines can offset, trim, stretch and loop a clip without modifying its resource.</remarks>
public sealed class AnimationNodeAnimation : AnimationRootNode
{
    private static readonly PropertyDescriptor[] GraphProperties =
    [
        new PropertyDescriptor<AnimationNodeAnimation, string>(nameof(Animation), n => n.Animation, (n, v) => n.Animation = v, _ => ""),
        new PropertyDescriptor<AnimationNodeAnimation, bool>(nameof(AdvanceOnStart), n => n.AdvanceOnStart, (n, v) => n.AdvanceOnStart = v, _ => false),
        new PropertyDescriptor<AnimationNodeAnimation, AnimationPlayMode>(nameof(PlayMode), n => n.PlayMode, (n, v) => n.PlayMode = v, _ => AnimationPlayMode.Forward),
        new PropertyDescriptor<AnimationNodeAnimation, bool>(nameof(UseCustomTimeline), n => n.UseCustomTimeline, (n, v) => n.UseCustomTimeline = v, _ => false),
        new PropertyDescriptor<AnimationNodeAnimation, double>(nameof(TimelineLength), n => n.TimelineLength, (n, v) => n.TimelineLength = v, _ => 1),
        new PropertyDescriptor<AnimationNodeAnimation, double>(nameof(StartOffset), n => n.StartOffset, (n, v) => n.StartOffset = v, _ => 0),
        new PropertyDescriptor<AnimationNodeAnimation, SpriteFrames.LoopMode>(nameof(LoopMode), n => n.LoopMode, (n, v) => n.LoopMode = v, _ => SpriteFrames.LoopMode.None),
        new PropertyDescriptor<AnimationNodeAnimation, bool>(nameof(StretchTimeScale), n => n.StretchTimeScale, (n, v) => n.StretchTimeScale = v, _ => false),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(GraphProperties);

    private string _animation = "";
    private bool _advanceOnStart, _customTimeline, _stretch;
    private double _length = 1, _offset;
    private SpriteFrames.LoopMode _loop;
    private AnimationPlayMode _playMode;
    private static readonly AnimationParameter<bool> Backward = new("backward", false, true);
    /// <summary>Gets or sets the qualified animation name.</summary>
    /// <value>The typed value described in the summary.</value>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public string Animation { get { ThrowIfDisposed(); return _animation; } set { ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(value); _animation = value; EmitGraphChanged(); } }
    /// <summary>Gets or sets whether startup consumes the first supplied delta; defaults to false.</summary>
    /// <value>The typed value described in the summary.</value>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public bool AdvanceOnStart { get { ThrowIfDisposed(); return _advanceOnStart; } set { ThrowIfDisposed(); _advanceOnStart = value; EmitGraphChanged(); } }
    /// <summary>Gets or sets forward/reversed clip sampling.</summary>
    /// <value>The typed value described in the summary.</value>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public AnimationPlayMode PlayMode { get { ThrowIfDisposed(); return _playMode; } set { ThrowIfDisposed(); Clip.Valid(value); _playMode = value; EmitGraphChanged(); } }
    /// <summary>Gets or sets whether this resource overrides the clip timeline.</summary>
    /// <value>The typed value described in the summary.</value>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public bool UseCustomTimeline { get { ThrowIfDisposed(); return _customTimeline; } set { ThrowIfDisposed(); _customTimeline = value; EmitGraphChanged(); } }
    /// <summary>Gets or sets the positive finite custom length in seconds, initially one.</summary>
    /// <value>The typed value described in the summary.</value>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A numeric value is nonfinite or outside the stated domain.</exception>
    public double TimelineLength { get { ThrowIfDisposed(); return _length; } set { ThrowIfDisposed(); Clip.Finite(value); if (value <= 0) throw new ArgumentOutOfRangeException(nameof(value)); _length = value; EmitGraphChanged(); } }
    /// <summary>Gets or sets a finite custom start offset in seconds.</summary>
    /// <value>The typed value described in the summary.</value>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">A numeric value is nonfinite or outside the stated domain.</exception>
    public double StartOffset { get { ThrowIfDisposed(); return _offset; } set { ThrowIfDisposed(); Clip.Finite(value); _offset = value; EmitGraphChanged(); } }
    /// <summary>Gets or sets the custom timeline endpoint loop mode.</summary>
    /// <value>The typed value described in the summary.</value>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public SpriteFrames.LoopMode LoopMode { get { ThrowIfDisposed(); return _loop; } set { ThrowIfDisposed(); Clip.Valid(value); _loop = value; EmitGraphChanged(); } }
    /// <summary>Gets or sets whether the custom timeline scales the entire clip period.</summary>
    /// <value>The typed value described in the summary.</value>
    /// <exception cref="ObjectDisposedException">This resource/controller or a required borrowed resource has been disposed.</exception>
    public bool StretchTimeScale { get { ThrowIfDisposed(); return _stretch; } set { ThrowIfDisposed(); _stretch = value; EmitGraphChanged(); } }
    /// <inheritdoc />
    protected override IEnumerable<AnimationParameter> OnGetParameterList() => base.OnGetParameterList().Append(Backward);
    /// <inheritdoc />
    protected override string OnGetCaption() => "Animation";
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly)
    {
        var c = Current(); var clip = c.Tree.GetAnimation(_animation); ObjectDisposedException.ThrowIf(clip.IsDisposed, clip);
        var clipLength = clip.Length; var length = _customTimeline ? _length : clipLength; var loop = _customTimeline ? _loop : clip.LoopMode;
        var prior = GetParameter(CurrentPosition); var backward = GetParameter(Backward); var delta = c.Delta;
        var position = seek ? time : prior + (backward ? -delta : delta); var started = seek && !isExternalSeeking && time == 0;
        if (started) position = _advanceOnStart ? delta : 0;
        if (loop == SpriteFrames.LoopMode.Linear) { position = Mathf.PosMod(position, length); backward = false; }
        else if (loop == SpriteFrames.LoopMode.PingPong)
        { var phase = Mathf.PosMod(position, length * 2); if (position < 0 || position > length) backward = !backward; position = phase <= length ? phase : length * 2 - phase; }
        else { if (position < 0) { delta += position; position = 0; } else if (position > length) { delta += position - length; position = length; } backward = false; if ((_playMode == AnimationPlayMode.Forward && prior >= length) || (_playMode == AnimationPlayMode.Backward && prior <= 0)) delta = 0; }
        var timelineDelta = delta;
        var start = 0d; var end = clipLength; var playback = position; var previousPlayback = prior;
        if (_customTimeline)
        {
            playback += _offset; previousPlayback += _offset;
            if (_stretch) { var factor = clipLength / length; playback *= factor; previousPlayback *= factor; delta *= factor; }
            if (!_stretch && loop == SpriteFrames.LoopMode.None) { if (_playMode == AnimationPlayMode.Forward) { start = _offset; end = start + length; } else { end = clipLength - _offset; start = end - length; } }
        }
        if (loop == SpriteFrames.LoopMode.Linear) { playback = Mathf.PosMod(playback, clipLength); previousPlayback = Mathf.PosMod(previousPlayback, clipLength); }
        else if (loop == SpriteFrames.LoopMode.PingPong) { var phase = Mathf.PosMod(playback, clipLength * 2); playback = phase <= clipLength ? phase : clipLength * 2 - phase; }
        else playback = Math.Clamp(playback, 0, clipLength);
        if (_playMode == AnimationPlayMode.Backward) { playback = clipLength - playback; previousPlayback = clipLength - previousPlayback; delta = -delta; }
        if (backward) delta = -delta;
        c.Result = new(length, position, timelineDelta, loop); c.HasTime = true;
        if (!testOnly)
        {
            c.Tree.AddClip(c.Instance, _animation, playback, delta, seek, 1, start, end);
            SetParameter(Backward, backward);
            if (started) c.Tree.GraphStarted(_animation);
            if (loop == SpriteFrames.LoopMode.None && prior < length && position >= length) c.Tree.GraphFinished(_animation);
        }
        return c.Result.Remaining;
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeAnimation();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    { var copy = (AnimationNodeAnimation)target; CopyNodeState(copy); copy._animation = _animation; copy._advanceOnStart = _advanceOnStart; copy._customTimeline = _customTimeline; copy._stretch = _stretch; copy._length = _length; copy._offset = _offset; copy._loop = _loop; copy._playMode = _playMode; }
}
