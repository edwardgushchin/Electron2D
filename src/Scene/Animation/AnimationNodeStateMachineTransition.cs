namespace Electron2D;

/// <summary>Selects when a state-machine edge activates its destination timeline.</summary>
public enum AnimationSwitchMode
{
    /// <summary>Switch as soon as the edge is selected.</summary>
    Immediate = 0,
    /// <summary>Seek the destination to the source position before continuing.</summary>
    Sync = 1,
    /// <summary>Wait until the source has at most the crossfade duration remaining.</summary>
    AtEnd = 2,
}
/// <summary>Selects whether an edge participates in travel and automatic advancement.</summary>
public enum AnimationAdvanceMode
{
    /// <summary>Exclude the edge from travel and automatic advancement.</summary>
    Disabled = 0,
    /// <summary>Allow explicit travel along the edge.</summary>
    Enabled = 1,
    /// <summary>Also allow automatic advancement when all typed conditions pass.</summary>
    Auto = 2,
}
/// <summary>A borrowed state-machine edge policy with typed conditions and optional unit fade curve.</summary>
/// <remarks>Condition keys are declared on each owning machine. The predicate receives the current tree;
/// captured state and callback allocations remain caller-owned. Changes invalidate attached graphs.</remarks>
public sealed class AnimationNodeStateMachineTransition : Resource
{
    private AnimationSwitchMode _switch = AnimationSwitchMode.Immediate;
    /// <summary>Selects immediate, synchronized or end-of-cycle switching.</summary>
    /// <value>Initially AnimationSwitchMode.Immediate.</value>
    /// <exception cref="ObjectDisposedException">This resource or a supplied curve is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite, negative or an undefined enumeration.</exception>
    public AnimationSwitchMode SwitchMode { get { ThrowIfDisposed(); return _switch; } set { ThrowIfDisposed(); Animation.Valid(value); _switch = value; Notify(); } }
    private AnimationAdvanceMode _advance = AnimationAdvanceMode.Enabled;
    /// <summary>Selects disabled, travel-enabled or automatic advancement.</summary>
    /// <value>Initially AnimationAdvanceMode.Enabled.</value>
    /// <exception cref="ObjectDisposedException">This resource or a supplied curve is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite, negative or an undefined enumeration.</exception>
    public AnimationAdvanceMode AdvanceMode { get { ThrowIfDisposed(); return _advance; } set { ThrowIfDisposed(); Animation.Valid(value); _advance = value; Notify(); } }
    private AnimationParameter<bool>? _condition = null;
    /// <summary>Gets or sets the borrowed typed flag; null imposes no flag condition.</summary>
    /// <value>Initially null.</value>
    /// <exception cref="ObjectDisposedException">This resource or a supplied curve is disposed.</exception>
    public AnimationParameter<bool>? AdvanceCondition { get { ThrowIfDisposed(); return _condition; } set { ThrowIfDisposed(); _condition = value; Notify(true); } }
    private Func<AnimationTree, bool>? _expression = null;
    /// <summary>Gets or sets the executable predicate; null imposes no expression condition.</summary>
    /// <value>Initially null.</value>
    /// <exception cref="ObjectDisposedException">This resource or a supplied curve is disposed.</exception>
    public Func<AnimationTree, bool>? AdvanceExpression { get { ThrowIfDisposed(); return _expression; } set { ThrowIfDisposed(); _expression = value; Notify(); } }
    private double _fade = 0;
    /// <summary>Gets or sets the nonnegative finite crossfade duration in seconds.</summary>
    /// <value>Initially 0.</value>
    /// <exception cref="ObjectDisposedException">This resource or a supplied curve is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite, negative or an undefined enumeration.</exception>
    public double XFadeTime { get { ThrowIfDisposed(); return _fade; } set { ThrowIfDisposed(); Animation.Finite(value); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _fade = value; Notify(); } }
    private Curve? _curve = null;
    /// <summary>Gets or sets the borrowed curve sampling the destination fade weight; null is linear.</summary>
    /// <value>Initially null.</value>
    /// <exception cref="ObjectDisposedException">This resource or a supplied curve is disposed.</exception>
    public Curve? XFadeCurve { get { ThrowIfDisposed(); return _curve; } set { ThrowIfDisposed(); if (value?.IsDisposed == true) throw new ObjectDisposedException(nameof(value)); _curve = value; Notify(); } }
    private bool _break = false;
    /// <summary>Gets or sets whether AtEnd may leave a looping clip at its predicted cycle endpoint.</summary>
    /// <value>Initially false.</value>
    /// <exception cref="ObjectDisposedException">This resource or a supplied curve is disposed.</exception>
    public bool BreakLoopAtEnd { get { ThrowIfDisposed(); return _break; } set { ThrowIfDisposed(); _break = value; Notify(); } }
    private bool _reset = true;
    /// <summary>Gets or sets whether the destination timeline restarts when the edge activates.</summary>
    /// <value>Initially true.</value>
    /// <exception cref="ObjectDisposedException">This resource or a supplied curve is disposed.</exception>
    public bool Reset { get { ThrowIfDisposed(); return _reset; } set { ThrowIfDisposed(); _reset = value; Notify(); } }
    private int _priority = 1;
    /// <summary>Gets or sets the nonnegative route-cost multiplier and automatic priority; smaller wins.</summary>
    /// <value>Initially 1.</value>
    /// <exception cref="ObjectDisposedException">This resource or a supplied curve is disposed.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The value is nonfinite, negative or an undefined enumeration.</exception>
    public int Priority { get { ThrowIfDisposed(); return _priority; } set { ThrowIfDisposed(); if (value < 0) throw new ArgumentOutOfRangeException(nameof(value)); _priority = value; Notify(); } }
    internal event Action? PolicyUpdated;
    private void Notify(bool condition = false)
    {
        var handlers = PolicyUpdated; List<Exception>? errors = null;
        try { EmitChanged(); } catch (Exception error) { AnimationNode.CollectException(ref errors, error); }
        if (handlers is not null) foreach (Action handler in handlers.GetInvocationList()) try { handler(); } catch (Exception error) { AnimationNode.CollectException(ref errors, error); }
        if (condition && !IsDisposed) try { AdvanceConditionChanged?.Invoke(); } catch (Exception error) { AnimationNode.CollectException(ref errors, error); }
        AnimationNode.ThrowCollected("State transition notification failed.", errors);
    }
    /// <summary>Occurs after assigning the typed condition key, including an equal assignment.</summary>
    public event Action? AdvanceConditionChanged;
    /// <summary>Creates an enabled immediate edge with zero fade, reset enabled and priority one.</summary>
    public AnimationNodeStateMachineTransition() { }
    private static readonly PropertyDescriptor[] Properties =
    [
        new PropertyDescriptor<AnimationNodeStateMachineTransition, AnimationSwitchMode>(nameof(SwitchMode), n => n.SwitchMode, (n, v) => n.SwitchMode = v, _ => AnimationSwitchMode.Immediate),
        new PropertyDescriptor<AnimationNodeStateMachineTransition, AnimationAdvanceMode>(nameof(AdvanceMode), n => n.AdvanceMode, (n, v) => n.AdvanceMode = v, _ => AnimationAdvanceMode.Enabled),
        new PropertyDescriptor<AnimationNodeStateMachineTransition, AnimationParameter<bool>?>(nameof(AdvanceCondition), n => n.AdvanceCondition, (n, v) => n.AdvanceCondition = v, _ => null),
        new PropertyDescriptor<AnimationNodeStateMachineTransition, Func<AnimationTree, bool>?>(nameof(AdvanceExpression), n => n.AdvanceExpression, (n, v) => n.AdvanceExpression = v, _ => null),
        new PropertyDescriptor<AnimationNodeStateMachineTransition, double>(nameof(XFadeTime), n => n.XFadeTime, (n, v) => n.XFadeTime = v, _ => 0),
        new PropertyDescriptor<AnimationNodeStateMachineTransition, Curve?>(nameof(XFadeCurve), n => n.XFadeCurve, (n, v) => n.XFadeCurve = v, _ => null),
        new PropertyDescriptor<AnimationNodeStateMachineTransition, bool>(nameof(BreakLoopAtEnd), n => n.BreakLoopAtEnd, (n, v) => n.BreakLoopAtEnd = v, _ => false),
        new PropertyDescriptor<AnimationNodeStateMachineTransition, bool>(nameof(Reset), n => n.Reset, (n, v) => n.Reset = v, _ => true),
        new PropertyDescriptor<AnimationNodeStateMachineTransition, int>(nameof(Priority), n => n.Priority, (n, v) => n.Priority = v, _ => 1),
    ];
    /// <inheritdoc />
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Concat(Properties);
    /// <inheritdoc />
    protected override Resource CreateDuplicateInstance() => new AnimationNodeStateMachineTransition();
    /// <inheritdoc />
    protected override void CopyCustomStateTo(Resource target, bool deep, DeepDuplicateMode mode, Func<Resource?, Resource?> duplicate, Func<Resource?, Resource?> force)
    {
        var copy = (AnimationNodeStateMachineTransition)target;
        copy._switch = _switch;
        copy._advance = _advance;
        copy._condition = _condition;
        copy._expression = _expression;
        copy._fade = _fade;
        copy._curve = deep ? (Curve?)duplicate(_curve) : _curve;
        copy._break = _break;
        copy._reset = _reset;
        copy._priority = _priority;
    }
    /// <inheritdoc />
    protected override void Dispose(bool disposing) { if (disposing) { _condition = null; _expression = null; _curve = null; AdvanceConditionChanged = null; PolicyUpdated = null; } base.Dispose(disposing); }
}
