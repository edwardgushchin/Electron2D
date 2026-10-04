using System.Globalization;

namespace Electron2D;

/// <summary>Switches named graph inputs with per-tree requests, crossfades and automatic advancement.</summary>
/// <remarks>Current state changes when a fade starts. Input policies and curves are resource definitions;
/// selection, requests and timers belong to each AnimationTree/path. Curves and connected graphs are borrowed.</remarks>
public sealed class AnimationNodeTransition : AnimationNodeSync
{
    /// <summary>The writable exact input-name request; empty means no request and real processing consumes it.</summary>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public static readonly AnimationParameter<string> TransitionRequest = new("transition_request", "");
    /// <summary>The read-only selected input name.</summary>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public static readonly AnimationParameter<string> CurrentState = new("current_state", "", true);
    /// <summary>The read-only selected input index, initially minus one.</summary>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public static readonly AnimationParameter<int> CurrentIndex = new("current_index", -1, true);
    /// <summary>The writable outgoing input index; minus one disables an outgoing clip.</summary>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public static readonly AnimationParameter<int> PreviousIndex = new("prev_index", -1);
    /// <summary>The writable outgoing fade time remaining in seconds.</summary>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public static readonly AnimationParameter<double> PreviousXFading = new("prev_xfading", 0);
    private sealed class InputPolicy { internal bool AutoAdvance, BreakLoop; internal bool Reset = true; }
    private readonly record struct Selection(InputPolicy? Current, InputPolicy? Previous, int ReportedPrevious);
    private static readonly AnimationParameter<Selection> Selected = new("selection", new(null, null, -1), true);
    private static readonly AnimationParameter[] StateParameters = [TransitionRequest, CurrentState, CurrentIndex, PreviousIndex, PreviousXFading, Selected];
    private readonly List<InputPolicy> _inputData = [];
    /// <summary>Creates an empty named transition with default input policies.</summary>
    public AnimationNodeTransition() { }
    private double _fade;
    private Curve? _curve;
    private bool _self;
    /// <summary>Gets or sets the input count; growing creates state_N captions and default policies.</summary>
    /// <value>The nonnegative input count, initially zero.</value>
    /// <exception cref="ArgumentOutOfRangeException">The count is negative.</exception>
    /// <exception cref="ObjectDisposedException">The resource is disposed.</exception>
    public int InputCount
    {
        get => GetInputCount();
        set { ThrowIfDisposed(); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); while (GetInputCount() < value) AddInput("state_" + GetInputCount().ToString(CultureInfo.InvariantCulture)); while (GetInputCount() > value) RemoveInput(GetInputCount() - 1); EmitGraphChanged(); NotifyPropertyListChanged(); }
    }
    /// <summary>Gets or sets the finite crossfade duration in seconds; nonpositive values switch immediately.</summary>
    /// <value>The duration, initially zero.</value>
    /// <exception cref="ArgumentOutOfRangeException">The duration is nonfinite.</exception>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public double XFadeTime { get { ThrowIfDisposed(); return _fade; } set { ThrowIfDisposed(); Animation.Finite(value); _fade = value; EmitGraphChanged(); } }
    /// <summary>Gets or sets the borrowed unit curve sampling the outgoing fade weight; null is linear.</summary>
    /// <value>The borrowed curve, initially null.</value>
    /// <exception cref="ObjectDisposedException">This resource or the assigned curve is disposed.</exception>
    public Curve? XFadeCurve { get { ThrowIfDisposed(); return _curve; } set { ThrowIfDisposed(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value)); _curve = value; EmitGraphChanged(); } }
    /// <summary>Gets or sets whether requesting the selected input may reset it and clear its existing fade.</summary>
    /// <value>The permission, initially false.</value>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    public bool AllowTransitionToSelf { get { ThrowIfDisposed(); return _self; } set { ThrowIfDisposed(); _self = value; EmitGraphChanged(); } }
    private static readonly PropertyDescriptor[] TransitionProperties =
    [
        new PropertyDescriptor<AnimationNodeTransition, int>(nameof(InputCount), n => n.InputCount, (n, v) => n.InputCount = v, _ => 0),
        new PropertyDescriptor<AnimationNodeTransition, double>(nameof(XFadeTime), n => n.XFadeTime, (n, v) => n.XFadeTime = v, _ => 0),
        new PropertyDescriptor<AnimationNodeTransition, Curve?>(nameof(XFadeCurve), n => n.XFadeCurve, (n, v) => n.XFadeCurve = v, _ => null),
        new PropertyDescriptor<AnimationNodeTransition, bool>(nameof(AllowTransitionToSelf), n => n.AllowTransitionToSelf, (n, v) => n.AllowTransitionToSelf = v, _ => false),
    ];
    /// <inheritdoc />
    public override bool AddInput(string name)
    {
        ThrowIfDisposed(); ArgumentNullException.ThrowIfNull(name); if (name.Contains('/') || name.Contains('.')) return false;
        _inputData.Add(new()); return base.AddInput(name);
    }
    /// <inheritdoc />
    public override void RemoveInput(int index) { ThrowIfDisposed(); _inputData.RemoveAt(index); base.RemoveInput(index); }
    /// <inheritdoc />
    public override bool SetInputName(int input, string name) => base.SetInputName(input, name);
    /// <summary>Sets whether a completed input requests the following input, wrapping at the end.</summary>
    /// <param name="input">An existing input index.</param>
    /// <param name="enable">The automatic-advance policy.</param>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The input index is outside the current port list.</exception>
    public void SetInputAsAutoAdvance(int input, bool enable) { ThrowIfDisposed(); _inputData[input].AutoAdvance = enable; EmitGraphChanged(); }
    /// <summary>Tests the automatic-advance policy.</summary>
    /// <param name="input">An existing input index.</param>
    /// <returns>Whether it requests the next input near its endpoint.</returns>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The input index is outside the current port list.</exception>
    public bool IsInputSetAsAutoAdvance(int input) { ThrowIfDisposed(); return _inputData[input].AutoAdvance; }
    /// <summary>Sets whether automatic advancement uses the predicted end of a looping cycle.</summary>
    /// <param name="input">An existing input index.</param>
    /// <param name="enable">The loop-break policy.</param>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The input index is outside the current port list.</exception>
    public void SetInputBreakLoopAtEnd(int input, bool enable) { ThrowIfDisposed(); _inputData[input].BreakLoop = enable; EmitGraphChanged(); }
    /// <summary>Tests the loop-break policy.</summary>
    /// <param name="input">An existing input index.</param>
    /// <returns>Whether looping timelines may automatically advance.</returns>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The input index is outside the current port list.</exception>
    public bool IsInputLoopBrokenAtEnd(int input) { ThrowIfDisposed(); return _inputData[input].BreakLoop; }
    /// <summary>Sets whether a destination starts at zero on a requested switch or allowed self transition.</summary>
    /// <param name="input">An existing input index.</param>
    /// <param name="enable">The reset policy, initially true.</param>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The input index is outside the current port list.</exception>
    public void SetInputReset(int input, bool enable) { ThrowIfDisposed(); _inputData[input].Reset = enable; EmitGraphChanged(); }
    /// <summary>Tests the reset policy.</summary>
    /// <param name="input">An existing input index.</param>
    /// <returns>Whether requested activation resets this input.</returns>
    /// <exception cref="ObjectDisposedException">The resource has been disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The input index is outside the current port list.</exception>
    public bool IsInputReset(int input) { ThrowIfDisposed(); return _inputData[input].Reset; }
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(TransitionProperties);
    /// <inheritdoc />
    protected override IEnumerable<AnimationParameter> OnGetParameterList() => base.OnGetParameterList().Concat(StateParameters);
    /// <inheritdoc />
    protected override string OnGetCaption() => "Transition";
    /// <inheritdoc />
    protected override double OnProcess(double time, bool seek, bool isExternalSeeking, bool testOnly)
    {
        var c = Current(); var generation = c.Tree.GraphGeneration; var state = GetParameter(Selected); var request = GetParameter(TransitionRequest); ArgumentNullException.ThrowIfNull(request); SetParameter(TransitionRequest, "");
        var current = state.Current is null ? -1 : _inputData.IndexOf(state.Current); var previous = state.Previous is null ? -1 : _inputData.IndexOf(state.Previous); var fade = GetParameter(PreviousXFading);
        var previousRequest = GetParameter(PreviousIndex); if (previousRequest != state.ReportedPrevious) { if (previousRequest >= _inputData.Count) throw new ArgumentOutOfRangeException(nameof(PreviousIndex)); previous = previousRequest; }
        if (current < 0) { current = _inputData.Count == 0 ? -1 : 0; previous = -1; }
        var clear = seek && !isExternalSeeking && Math.Abs(time) < 1e-5; var switched = false; var restart = false;
        if (request.Length != 0)
        {
            var next = FindInput(request); if (next < 0) throw new ArgumentException("Transition request names no input.", nameof(TransitionRequest));
            if (next == current) { if (_self) { restart = _inputData[current].Reset; clear = true; } }
            else { switched = true; previous = current; current = next; }
        }
        if (clear) { fade = 0; previous = -1; }
        if (switched) fade = _fade;
        if (current < 0) { SetParameter(CurrentIndex, -1); SetParameter(CurrentState, ""); SetParameter(PreviousIndex, -1); SetParameter(Selected, new(null, null, -1)); c.Result = default; c.HasTime = true; return 0; }
        var selectedPolicy = _inputData[current]; var outgoingPolicy = previous < 0 ? null : _inputData[previous]; var reset = selectedPolicy.Reset; var auto = selectedPolicy.AutoAdvance; var breakLoop = selectedPolicy.BreakLoop; var count = _inputData.Count; var selectedName = GetInputName(current);
        if (restart)
        {
            var remain = BlendInput(current, 0, true, isExternalSeeking, 1, testOnly: testOnly);
            if (c.IsCurrent(generation)) Commit(selectedPolicy, null, current, -1, selectedName, fade);
            return remain;
        }
        if (Sync) for (var i = 0; i < count; i++) if (i != current && i != previous) { BlendInput(i, time, seek, isExternalSeeking, 0, testOnly: testOnly); if (!c.IsCurrent(generation)) return 0; }
        AnimationGraphTime selected;
        if (previous < 0)
        {
            BlendInput(current, time, seek, isExternalSeeking, 1, testOnly: testOnly); selected = c.Result; if (!c.IsCurrent(generation)) return 0;
            if (auto && selected.GetRemaining(breakLoop) <= _fade + 1e-5) SetParameter(TransitionRequest, GetInputName((current + 1) % count));
        }
        else
        {
            var weight = 0d; var inverse = 1d; var useBlend = Sync;
            if (_fade > 0) { useBlend = true; weight = fade / _fade; if (_curve is not null) weight = _curve.Sample((float)weight); Animation.Finite(weight); inverse = 1 - weight; weight = Math.Abs(weight) < 1e-5 ? 1e-5 : weight; inverse = Math.Abs(inverse) < 1e-5 ? 1e-5 : inverse; }
            BlendInput(current, reset && !seek && switched ? 0 : time, seek || (reset && switched), isExternalSeeking, inverse, testOnly: testOnly); selected = c.Result; if (!c.IsCurrent(generation)) return 0;
            BlendInput(previous, time, seek && useBlend, isExternalSeeking, weight, testOnly: testOnly); if (!c.IsCurrent(generation)) return 0;
            if (!seek) { if (fade <= 1e-5) { previous = -1; outgoingPolicy = null; } fade -= Math.Abs(c.Delta); }
        }
        Commit(selectedPolicy, outgoingPolicy, current, previous, selectedName, fade); c.Result = selected; c.HasTime = true; return selected.Remaining;
    }
    private void Commit(InputPolicy current, InputPolicy? previous, int currentIndex, int previousIndex, string name, double fade)
    { SetParameter(CurrentState, name); SetParameter(CurrentIndex, currentIndex); SetParameter(PreviousIndex, previousIndex); SetParameter(PreviousXFading, fade); SetParameter(Selected, new(current, previous, previousIndex)); }
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeTransition();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    {
        base.CopyCustomStateTo(target, deep, mode, duplicate, force); var copy = (AnimationNodeTransition)target; copy._inputData.Clear(); foreach (var input in _inputData) copy._inputData.Add(new() { AutoAdvance = input.AutoAdvance, BreakLoop = input.BreakLoop, Reset = input.Reset }); copy._fade = _fade; copy._self = _self; copy._curve = deep ? (Curve?)duplicate(_curve) : _curve;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _inputData.Clear(); _curve = null; } base.Dispose(disposing); }
}
