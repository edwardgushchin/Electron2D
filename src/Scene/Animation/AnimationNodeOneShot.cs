namespace Electron2D;

/// <summary>Selects replacement or additive animation contributions.</summary>
public enum AnimationMixMode
{
    /// <summary>Blend the base and action contributions.</summary>
    Blend = 0,
    /// <summary>Add the action to the complete base contribution.</summary>
    Add = 1,
}
/// <summary>Requests an action from a one-shot animation controller.</summary>
public enum AnimationOneShotRequest
{
    /// <summary>Make no request.</summary>
    None = 0,
    /// <summary>Start or restart the action.</summary>
    Fire = 1,
    /// <summary>Stop immediately and cancel automatic restart.</summary>
    Abort = 2,
    /// <summary>Fade out and cancel the pending automatic restart.</summary>
    FadeOut = 3,
}
/// <summary>Plays a requested action over a base graph with fade, filters and automatic restart.</summary>
/// <remarks>Inputs are in and shot. Parameter cells and timers belong to each AnimationTree/path;
/// definitions and fade curves are borrowed. Contributions use the existing typed property mixer.</remarks>
public sealed class AnimationNodeOneShot : AnimationNodeSync
{
    /// <summary>The writable request, consumed on the next real evaluation.</summary>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public static readonly AnimationParameter<AnimationOneShotRequest> Request = new("request", AnimationOneShotRequest.None);
    /// <summary>Read-only activity, including the fade-out period.</summary>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public static readonly AnimationParameter<bool> Active = new("active", false, true);
    /// <summary>Read-only activity before fade-out; diagnostics select the action while this is true.</summary>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public static readonly AnimationParameter<bool> InternalActive = new("internal_active", false, true);
    /// <summary>The writable remaining fade-in seconds, initially zero.</summary>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public static readonly AnimationParameter<double> FadeInRemaining = new("fade_in_remaining", 0);
    /// <summary>The writable remaining fade-out seconds, initially zero.</summary>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public static readonly AnimationParameter<double> FadeOutRemaining = new("fade_out_remaining", 0);
    /// <summary>The writable automatic-restart timer; minus one disables a pending restart.</summary>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public static readonly AnimationParameter<double> TimeToRestart = new("time_to_restart", -1);
    private static readonly AnimationParameter[] ParametersList = [Request, Active, InternalActive, FadeInRemaining, FadeOutRemaining, TimeToRestart];
    private double _fadeIn, _fadeOut, _restartDelay = 1, _randomDelay;
    private Curve? _inCurve, _outCurve;
    private bool _autorestart, _breakLoop, _abortReset;
    private AnimationMixMode _mix;
    /// <summary>Creates the standard in and shot inputs.</summary>
    public AnimationNodeOneShot() { AddInput("in"); AddInput("shot"); }
    /// <summary>Finite fade-in duration in seconds; nonpositive values select immediate blending.</summary>
    /// <value>The configured value, initially 0.</value>
    /// <exception cref="ObjectDisposedException">The controller or assigned curve is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The numeric or enum value is invalid.</exception>
    public double FadeInTime { get { ThrowIfDisposed(); return _fadeIn; } set { ThrowIfDisposed(); Animation.Finite(value); _fadeIn = value; EmitGraphChanged(); } }
    /// <summary>Finite fade-out duration in seconds; nonpositive values select immediate exit.</summary>
    /// <value>The configured value, initially 0.</value>
    /// <exception cref="ObjectDisposedException">The controller or assigned curve is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The numeric or enum value is invalid.</exception>
    public double FadeOutTime { get { ThrowIfDisposed(); return _fadeOut; } set { ThrowIfDisposed(); Animation.Finite(value); _fadeOut = value; EmitGraphChanged(); } }
    /// <summary>Borrowed unit curve for fade-in; null is linear.</summary>
    /// <value>The configured value, initially null.</value>
    /// <exception cref="ObjectDisposedException">The controller or assigned curve is disposed.</exception>
    public Curve? FadeInCurve { get { ThrowIfDisposed(); return _inCurve; } set { ThrowIfDisposed(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value)); _inCurve = value; EmitGraphChanged(); } }
    /// <summary>Borrowed unit curve for fade-out; null is linear.</summary>
    /// <value>The configured value, initially null.</value>
    /// <exception cref="ObjectDisposedException">The controller or assigned curve is disposed.</exception>
    public Curve? FadeOutCurve { get { ThrowIfDisposed(); return _outCurve; } set { ThrowIfDisposed(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value)); _outCurve = value; EmitGraphChanged(); } }
    /// <summary>Whether a completed fired action schedules another start.</summary>
    /// <value>The configured value, initially false.</value>
    /// <exception cref="ObjectDisposedException">The controller or assigned curve is disposed.</exception>
    public bool Autorestart { get { ThrowIfDisposed(); return _autorestart; } set { ThrowIfDisposed(); _autorestart = value; EmitGraphChanged(); } }
    /// <summary>Finite automatic-restart delay in seconds; negative resulting timers do not restart.</summary>
    /// <value>The configured value, initially 1.</value>
    /// <exception cref="ObjectDisposedException">The controller or assigned curve is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The numeric or enum value is invalid.</exception>
    public double AutorestartDelay { get { ThrowIfDisposed(); return _restartDelay; } set { ThrowIfDisposed(); Animation.Finite(value); _restartDelay = value; EmitGraphChanged(); } }
    /// <summary>Finite random additional delay; nonnegative values add a uniform interval from zero to this value.</summary>
    /// <value>The configured value, initially 0.</value>
    /// <exception cref="ObjectDisposedException">The controller or assigned curve is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The numeric or enum value is invalid.</exception>
    public double AutorestartRandomDelay { get { ThrowIfDisposed(); return _randomDelay; } set { ThrowIfDisposed(); Animation.Finite(value); _randomDelay = value; EmitGraphChanged(); } }
    /// <summary>Whether the action may finish on the predicted end of a loop cycle.</summary>
    /// <value>The configured value, initially false.</value>
    /// <exception cref="ObjectDisposedException">The controller or assigned curve is disposed.</exception>
    public bool BreakLoopAtEnd { get { ThrowIfDisposed(); return _breakLoop; } set { ThrowIfDisposed(); _breakLoop = value; EmitGraphChanged(); } }
    /// <summary>Whether an internal reset aborts an already active action.</summary>
    /// <value>The configured value, initially false.</value>
    /// <exception cref="ObjectDisposedException">The controller or assigned curve is disposed.</exception>
    public bool AbortOnReset { get { ThrowIfDisposed(); return _abortReset; } set { ThrowIfDisposed(); _abortReset = value; EmitGraphChanged(); } }
    /// <summary>Whether the action blends over or adds to the base graph.</summary>
    /// <value>The configured value, initially AnimationMixMode.Blend.</value>
    /// <exception cref="ObjectDisposedException">The controller or assigned curve is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The numeric or enum value is invalid.</exception>
    public AnimationMixMode MixMode { get { ThrowIfDisposed(); return _mix; } set { ThrowIfDisposed(); Animation.Valid(value); _mix = value; EmitGraphChanged(); } }
    private static readonly PropertyDescriptor[] ActionProperties =
    [
        new PropertyDescriptor<AnimationNodeOneShot, double>(nameof(FadeInTime), n => n.FadeInTime, (n, v) => n.FadeInTime = v, _ => 0),
        new PropertyDescriptor<AnimationNodeOneShot, double>(nameof(FadeOutTime), n => n.FadeOutTime, (n, v) => n.FadeOutTime = v, _ => 0),
        new PropertyDescriptor<AnimationNodeOneShot, Curve?>(nameof(FadeInCurve), n => n.FadeInCurve, (n, v) => n.FadeInCurve = v, _ => null),
        new PropertyDescriptor<AnimationNodeOneShot, Curve?>(nameof(FadeOutCurve), n => n.FadeOutCurve, (n, v) => n.FadeOutCurve = v, _ => null),
        new PropertyDescriptor<AnimationNodeOneShot, bool>(nameof(Autorestart), n => n.Autorestart, (n, v) => n.Autorestart = v, _ => false),
        new PropertyDescriptor<AnimationNodeOneShot, double>(nameof(AutorestartDelay), n => n.AutorestartDelay, (n, v) => n.AutorestartDelay = v, _ => 1),
        new PropertyDescriptor<AnimationNodeOneShot, double>(nameof(AutorestartRandomDelay), n => n.AutorestartRandomDelay, (n, v) => n.AutorestartRandomDelay = v, _ => 0),
        new PropertyDescriptor<AnimationNodeOneShot, bool>(nameof(BreakLoopAtEnd), n => n.BreakLoopAtEnd, (n, v) => n.BreakLoopAtEnd = v, _ => false),
        new PropertyDescriptor<AnimationNodeOneShot, bool>(nameof(AbortOnReset), n => n.AbortOnReset, (n, v) => n.AbortOnReset = v, _ => false),
        new PropertyDescriptor<AnimationNodeOneShot, AnimationMixMode>(nameof(MixMode), n => n.MixMode, (n, v) => n.MixMode = v, _ => AnimationMixMode.Blend),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(ActionProperties);
    /// <inheritdoc />
    protected override IEnumerable<AnimationParameter> OnGetParameterList() { if (_randomDelay != 0) _ = Random.Shared.NextDouble(); return base.OnGetParameterList().Concat(ParametersList); }
    /// <inheritdoc />
    protected override string OnGetCaption() => "OneShot";
    /// <inheritdoc />
    protected override bool OnHasFilter() => true;
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly)
    {
        var c = Current(); var generation = c.Tree.GraphGeneration; var request = GetParameter(Request); Animation.Valid(request); SetParameter(Request, AnimationOneShotRequest.None);
        var active = GetParameter(Active); var internalActive = GetParameter(InternalActive); var restart = GetParameter(TimeToRestart); var fadeIn = GetParameter(FadeInRemaining); var fadeOut = GetParameter(FadeOutRemaining);
        var delta = Math.Abs(c.Delta); var start = request == AnimationOneShotRequest.Fire; var abort = request == AnimationOneShotRequest.Abort; var fading = active && !internalActive; var shooting = true;
        if (seek && !isExternalSeeking && Math.Abs(time) < 1e-5) { if (!start && (fading || (_abortReset && active))) abort = true; if (internalActive) start = true; }
        if (abort) { active = internalActive = false; restart = -1; fadeOut = 0; fading = shooting = false; }
        else if (request == AnimationOneShotRequest.FadeOut && !fading) { if (active) { fading = true; fadeOut = _fadeOut; fadeIn = 0; } else shooting = false; internalActive = false; restart = -1; }
        else if (!start && !active) { if (restart >= 0 && !seek) { restart -= delta; if (restart < 0) start = true; } if (!start) shooting = false; }
        if (!shooting)
        {
            var remaining = BlendInput(0, time, seek, isExternalSeeking, 1, sync: Sync, testOnly: testOnly);
            if (c.IsCurrent(generation)) { SetParameter(Active, active); SetParameter(InternalActive, internalActive); SetParameter(TimeToRestart, restart); SetParameter(FadeOutRemaining, fadeOut); }
            return remaining;
        }
        var actionSeek = seek;
        if (start) { actionSeek = true; if (!internalActive) fadeIn = _fadeIn; active = internalActive = true; fading = false; fadeOut = 0; }
        var weight = 1d; var blendSeek = Sync;
        if (fadeIn > 1e-5) { if (_fadeIn > 1e-5) { blendSeek = true; weight = (_fadeIn - fadeIn) / _fadeIn; if (_inCurve is not null) weight = _inCurve.Sample((float)weight); } else weight = 0; }
        if (fading) { blendSeek = true; weight = _fadeOut > 1e-5 ? fadeOut / _fadeOut : 0; if (_fadeOut > 1e-5 && _outCurve is not null) weight = 1 - _outCurve.Sample((float)(1 - weight)); }
        Animation.Finite(weight);
        BlendInput(0, time, _mix == AnimationMixMode.Add ? seek : seek && blendSeek, isExternalSeeking, _mix == AnimationMixMode.Add ? 1 : 1 - weight, _mix == AnimationMixMode.Add ? FilterAction.Ignore : FilterAction.Blend, Sync, testOnly);
        var main = c.Result; if (!c.IsCurrent(generation)) return 0;
        BlendInput(1, start ? 0 : actionSeek ? GetParameter(CurrentPosition) : time, actionSeek, isExternalSeeking, Math.Abs(weight) < 1e-5 ? 1e-5 : weight, FilterAction.Pass, true, testOnly);
        var action = c.Result; if (!c.IsCurrent(generation)) return 0;
        if (fadeIn <= 1e-5 && !start && !fading)
        {
            var actionDelta = Math.Abs(action.Delta); var rate = delta < 1e-5 || actionDelta < 1e-5 || Math.Abs(delta - actionDelta) < 1e-5 ? 1 : delta / actionDelta;
            var remain = action.GetRemaining(_breakLoop) * rate;
            if (remain <= _fadeOut + 1e-5) { fading = true; fadeOut = remain + delta; fadeIn = 0; }
        }
        var returned = internalActive ? action : main;
        if (fading) internalActive = false;
        if (!seek)
        {
            if (action.GetRemaining(_breakLoop) <= 1e-5 || (fading && fadeOut <= 1e-5)) { active = internalActive = false; if (_autorestart) restart = _restartDelay + (testOnly || _randomDelay == 0 ? 0 : Random.Shared.NextDouble() * _randomDelay); }
            if (!start) fadeIn = Math.Max(0, fadeIn - delta);
            fadeOut = Math.Max(0, fadeOut - delta);
        }
        SetParameter(Active, active); SetParameter(InternalActive, internalActive); SetParameter(TimeToRestart, restart); SetParameter(FadeInRemaining, fadeIn); SetParameter(FadeOutRemaining, fadeOut);
        c.Result = returned; c.HasTime = true; return returned.Remaining;
    }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeOneShot();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    {
        base.CopyCustomStateTo(target, deep, mode, duplicate, force); var copy = (AnimationNodeOneShot)target; copy._fadeIn = _fadeIn; copy._fadeOut = _fadeOut; copy._restartDelay = _restartDelay; copy._randomDelay = _randomDelay; copy._autorestart = _autorestart; copy._breakLoop = _breakLoop; copy._abortReset = _abortReset; copy._mix = _mix;
        copy._inCurve = deep ? (Curve?)duplicate(_inCurve) : _inCurve; copy._outCurve = deep ? (Curve?)duplicate(_outCurve) : _outCurve;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _inCurve = null; _outCurve = null; } base.Dispose(disposing); }
}
