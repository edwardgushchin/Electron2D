namespace Electron2D;

/// <summary>Immutable typed input for one custom graph evaluation.</summary>
/// <remarks>Time is the requested absolute position; Delta is signed elapsed time. An end of minus one denotes an
/// unbounded first evaluation. Subsequent bounds and crossing flags use the prior successfully reported timeline.
/// Weight is the signed scalar contribution before track-path filtering.</remarks>
public readonly struct AnimationNodePlaybackInfo
{
    /// <summary>Creates validated playback input.</summary>
    /// <param name="time">Finite requested absolute seconds.</param><param name="delta">Finite signed elapsed seconds.</param>
    /// <param name="start">Finite section start.</param><param name="end">Finite section end, or minus one when unbounded.</param>
    /// <param name="seeked">Whether the position is an absolute seek request.</param><param name="isExternalSeeking">Whether the seek originated outside graph startup.</param>
    /// <param name="loopedFlag">The crossed boundary.</param><param name="weight">Finite signed contribution.</param>
    /// <exception cref="ArgumentOutOfRangeException">A value is nonfinite or a boundary flag is undefined.</exception>
    public AnimationNodePlaybackInfo(double time, double delta, double start, double end, bool seeked, bool isExternalSeeking, Animation.LoopedFlag loopedFlag, double weight)
    {
        Animation.Finite(time); Animation.Finite(delta); Animation.Finite(start); Animation.Finite(end); Animation.Finite(weight); Animation.Valid(loopedFlag);
        Time = time; Delta = delta; Start = start; End = end; Seeked = seeked; IsExternalSeeking = isExternalSeeking; LoopedFlag = loopedFlag; Weight = weight;
    }
    /// <summary>Gets requested absolute seconds.</summary><value>The finite timeline position.</value>
    public double Time { get; }
    /// <summary>Gets signed elapsed seconds.</summary><value>The finite delta.</value>
    public double Delta { get; }
    /// <summary>Gets the section start.</summary><value>Zero for the ordinary unsectioned graph domain.</value>
    public double Start { get; }
    /// <summary>Gets the section end.</summary><value>The prior reported length, or minus one before the first successful report.</value>
    public double End { get; }
    /// <summary>Gets whether this update seeks.</summary><value>The seek request flag.</value>
    public bool Seeked { get; }
    /// <summary>Gets whether the seek is external.</summary><value>False for internal startup seeks.</value>
    public bool IsExternalSeeking { get; }
    /// <summary>Gets the crossed boundary.</summary><value>The prior timeline's crossing, or None without known looping bounds.</value>
    public Animation.LoopedFlag LoopedFlag { get; }
    /// <summary>Gets the scalar contribution.</summary><value>The signed incoming weight before path filtering.</value>
    public double Weight { get; }
}

/// <summary>Immutable timeline output from a custom graph node.</summary>
public readonly struct AnimationNodeTimeInfo
{
    /// <summary>Creates validated timeline output.</summary>
    /// <param name="length">Finite nonnegative duration.</param><param name="position">Finite current position.</param><param name="delta">Finite signed reported delta.</param>
    /// <param name="loopMode">Defined loop policy.</param><param name="willEnd">Whether a looping timeline just reached its break boundary.</param>
    /// <param name="isInfinite">Whether an end cannot be predicted.</param>
    /// <exception cref="ArgumentOutOfRangeException">A value is nonfinite, the length is negative, or the loop mode is undefined.</exception>
    public AnimationNodeTimeInfo(double length, double position, double delta, SpriteFrames.LoopMode loopMode = SpriteFrames.LoopMode.None, bool willEnd = false, bool isInfinite = false)
    {
        Animation.Finite(length); Animation.Finite(position); Animation.Finite(delta); Animation.Valid(loopMode);
        if (length < 0) throw new ArgumentOutOfRangeException(nameof(length));
        Length = length; Position = position; Delta = delta; LoopMode = loopMode; WillEnd = willEnd; IsInfinite = isInfinite;
    }
    /// <summary>Gets finite nonnegative duration.</summary><value>Timeline seconds.</value>
    public double Length { get; }
    /// <summary>Gets finite position.</summary><value>Timeline seconds.</value>
    public double Position { get; }
    /// <summary>Gets finite signed delta.</summary><value>Elapsed seconds.</value>
    public double Delta { get; }
    /// <summary>Gets loop policy.</summary><value>The defined endpoint behavior.</value>
    public SpriteFrames.LoopMode LoopMode { get; }
    /// <summary>Gets whether the loop just reached its break boundary.</summary><value>The predictive end flag.</value>
    public bool WillEnd { get; }
    /// <summary>Gets whether an end is unpredictable.</summary><value>The infinite-duration flag.</value>
    public bool IsInfinite { get; }
}

/// <summary>Provides the typed callback for application-defined animation graph resources.</summary>
/// <remarks>Derive an application class and register its exact static resource factory for files. Inherited graph
/// helpers operate only during evaluation. Return complete timeline output; test-only evaluation suppresses engine
/// state/output commits. Application callback side effects remain application-owned.</remarks>
public abstract class AnimationNodeExtension : AnimationNode
{
    /// <summary>Initializes an application graph definition.</summary>
    protected AnimationNodeExtension() { }
    /// <summary>Processes one graph update and returns complete timeline output.</summary>
    /// <param name="playbackInfo">Typed time, bounds, seek, crossing and weight input.</param><param name="testOnly">Whether engine state/output commits are suppressed.</param>
    /// <returns>Finite timeline values and loop/end/infinite policy.</returns>
    protected abstract AnimationNodeTimeInfo OnProcessAnimationNode(in AnimationNodePlaybackInfo playbackInfo, bool testOnly);
    /// <summary>Returns whether timeline output loops.</summary>
    /// <param name="nodeInfo">The complete timeline output.</param><returns>True for a non-None loop mode.</returns>
    public static bool IsLooping(AnimationNodeTimeInfo nodeInfo) => nodeInfo.LoopMode != SpriteFrames.LoopMode.None;
    /// <summary>Returns remaining seconds or the finite long-duration sentinel.</summary>
    /// <param name="nodeInfo">The complete timeline output.</param><param name="breakLoop">Whether to permit stopping at a loop boundary.</param>
    /// <returns>31540000 for infinite or unbroken looping output; zero at a reported break boundary; otherwise length minus position with near-zero normalization.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Subtracting finite length and position overflows.</exception>
    public static double GetRemainingTime(AnimationNodeTimeInfo nodeInfo, bool breakLoop = false)
    {
        if (nodeInfo.IsInfinite || IsLooping(nodeInfo) && !breakLoop) return 31540000;
        if (IsLooping(nodeInfo) && nodeInfo.WillEnd) return 0;
        var remaining = nodeInfo.Length - nodeInfo.Position; Animation.Finite(remaining); return Mathf.IsZeroApprox(remaining) ? 0 : remaining;
    }
    internal sealed override double ProcessGraph(double time, bool seek, bool external, bool testOnly)
    {
        var context = Current(); var prior = context.Instance.PreviousTime; var flag = Animation.LoopedFlag.None;
        if (!seek && context.Instance.HasPreviousTime && prior.Loop != SpriteFrames.LoopMode.None && prior.Length > 0)
            flag = context.Position >= prior.Length ? Animation.LoopedFlag.End : context.Position < 0 ? Animation.LoopedFlag.Start : Animation.LoopedFlag.None;
        var input = new AnimationNodePlaybackInfo(context.Position, context.Delta, 0, context.Instance.HasPreviousTime ? prior.Length : -1, seek, external, flag, context.Weight);
        var info = OnProcessAnimationNode(in input, testOnly);
        context.Result = new(info.Length, info.Position, info.Delta, info.LoopMode, info.WillEnd, info.IsInfinite); context.HasTime = true;
        return GetRemainingTime(info);
    }
}
