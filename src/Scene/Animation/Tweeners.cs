namespace Electron2D;

/// <summary>Defines one task executed as part of a <see cref="Tween"/> step.</summary>
/// <remarks>
/// Tweeners are created only by the corresponding <see cref="Tween"/> append methods. They are owned by that tween,
/// can run sequentially or in a parallel group, and use the same owner-thread contract.
/// </remarks>
public abstract class Tweener : ElectronObject
{
    private bool _finished;

    internal Tweener()
    {
    }

    /// <summary>Occurs immediately after this tweener completes or its target becomes unavailable.</summary>
    /// <remarks>The source tweener is passed as the sole argument. A killed tween does not complete unfinished tweeners.</remarks>
    public event Action<Tweener>? Finished;

    internal Tween? Owner { get; private set; }

    internal double ElapsedTime { get; private set; }

    internal bool IsFinished => _finished;

    internal virtual void Attach(Tween owner, Tween.TransitionType transition, Tween.EaseType ease) => Owner = owner;

    internal virtual void Start()
    {
        ElapsedTime = 0d;
        _finished = false;
    }

    internal bool Advance(ref double remaining)
    {
        if (IsDisposed || _finished)
            return false;
        return OnAdvance(ref remaining);
    }

    internal virtual void Cancel()
    {
    }

    internal void AddElapsed(double delta) => ElapsedTime += delta;

    internal void Finish()
    {
        if (_finished)
            return;
        _finished = true;
        Finished?.Invoke(this);
    }

    internal abstract bool OnAdvance(ref double remaining);

    /// <inheritdoc />
    /// <remarks>Requires the owning tween's thread while attached.</remarks>
    /// <exception cref="InvalidOperationException">The caller is not the owning tween's thread.</exception>
    protected override void ValidateDisposal()
    {
        Owner?.EnsureOwnerThreadForTweener();
        base.ValidateDisposal();
    }

    /// <inheritdoc />
    /// <remarks>Cancels owned subscriptions, clears completion subscribers, and calls the base implementation.</remarks>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            Cancel();
            Finished = null;
            Owner = null;
        }
        base.Dispose(disposing);
    }
}

/// <summary>Interpolates a typed property through explicit getter and setter delegates.</summary>
/// <typeparam name="TValue">The property value type.</typeparam>
/// <remarks>The tweener completes without writing when its target has been disposed. Continuation captures at step start for delays with magnitude below 0.00001 second, or after the delay otherwise.</remarks>
public sealed class PropertyTweener<TValue> : Tweener
{
    private const double DelayZeroThreshold = 0.00001d;

    private readonly ElectronObject _target;
    private readonly Func<TValue> _getter;
    private readonly Action<TValue> _setter;
    private readonly Func<TValue, TValue, double, TValue> _interpolate;
    private readonly Func<TValue, TValue, TValue>? _add;
    private readonly Func<TValue, TValue, TValue>? _subtract;
    private readonly TValue _baseFinalValue;
    private readonly double _duration;
    private TValue _initialValue;
    private TValue _finalValue;
    private TValue _activeDelta = default!;
    private bool _hasStarted;
    private bool _useActiveDelta;
    private double _delay;
    private bool _continueFromStart = true;
    private bool _captureAfterDelay;
    private bool _relative;
    private Func<double, double>? _customInterpolator;
    private Tween.TransitionType _transition;
    private Tween.EaseType _ease;

    internal PropertyTweener(
        ElectronObject target,
        Func<TValue> getter,
        Action<TValue> setter,
        TValue creationValue,
        TValue finalValue,
        double duration,
        Func<TValue, TValue, double, TValue> interpolate,
        Func<TValue, TValue, TValue>? add)
    {
        _target = target;
        _getter = getter;
        _setter = setter;
        _initialValue = creationValue;
        _baseFinalValue = finalValue;
        _finalValue = finalValue;
        _duration = duration;
        _interpolate = interpolate;
        _add = add;
        _subtract = TweenValue<TValue>.Subtract;
    }

    /// <summary>Uses the supplied value as the start of every sequence execution.</summary>
    /// <param name="value">The explicit starting value.</param>
    /// <returns>This tweener.</returns>
    /// <remarks>A change during an active step shifts intermediate built-in interpolation while retaining that step's final value. An integer displacement outside its type's range keeps endpoint interpolation.</remarks>
    /// <exception cref="InvalidOperationException">The call is off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tweener is disposing or disposed.</exception>
    public PropertyTweener<TValue> From(TValue value)
    {
        EnsureMutable();
        if (_hasStarted && (!_captureAfterDelay || Math.Abs(_delay) < DelayZeroThreshold) &&
            !_useActiveDelta && _customInterpolator is null &&
            _add is not null && _subtract is not null)
        {
            try
            {
                _activeDelta = _subtract(_finalValue, _initialValue);
                _useActiveDelta = true;
            }
            catch (OverflowException)
            {
                // A full-span integer keeps the existing double-based endpoint interpolation.
            }
        }
        _initialValue = value;
        _continueFromStart = false;
        return this;
    }

    /// <summary>Disables continuation from the step-start property; the configured start initially equals the append-time value.</summary>
    /// <returns>This tweener.</returns>
    /// <remarks>A preceding <see cref="From"/> value remains configured.</remarks>
    /// <exception cref="InvalidOperationException">The call is off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tweener is disposing or disposed.</exception>
    public PropertyTweener<TValue> FromCurrent()
    {
        EnsureMutable();
        _continueFromStart = false;
        return this;
    }

    /// <summary>Interprets the configured final value as a delta from the captured start.</summary>
    /// <returns>This tweener.</returns>
    /// <remarks>For a delayed continuation, the relative final value is resolved at step start before the delay-end recapture.</remarks>
    /// <exception cref="NotSupportedException"><typeparamref name="TValue"/> has no built-in addition contract.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tweener is disposing or disposed.</exception>
    public PropertyTweener<TValue> AsRelative()
    {
        EnsureMutable();
        if (_add is null)
            throw TweenValue<TValue>.Unsupported();
        _relative = true;
        return this;
    }

    /// <summary>Sets a custom mapping applied after the configured transition and easing.</summary>
    /// <param name="interpolator">Maps the eased weight to a final interpolation weight; overshoot values are allowed.</param>
    /// <returns>This tweener.</returns>
    /// <remarks>The mapping is also called with weight one on the final step, so the final write may overshoot.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="interpolator"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tweener is disposing or disposed.</exception>
    public PropertyTweener<TValue> SetCustomInterpolator(Func<double, double> interpolator)
    {
        ArgumentNullException.ThrowIfNull(interpolator);
        EnsureMutable();
        _customInterpolator = interpolator;
        return this;
    }

    /// <summary>Sets the delay before property interpolation begins.</summary>
    /// <param name="delay">Finite seconds; a negative delay begins on the first positive step.</param>
    /// <returns>This tweener.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delay"/> is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tweener is disposing or disposed.</exception>
    public PropertyTweener<TValue> SetDelay(double delay)
    {
        EnsureMutable();
        if (!double.IsFinite(delay))
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "Property delay must be finite.");
        _delay = delay;
        return this;
    }

    /// <summary>Overrides the owning tween's easing for this property.</summary>
    /// <param name="ease">The easing direction.</param>
    /// <returns>This tweener.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ease"/> is undefined.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tweener is disposing or disposed.</exception>
    public PropertyTweener<TValue> SetEase(Tween.EaseType ease)
    {
        EnsureMutable();
        TweenMath.Validate(_transition, ease);
        _ease = ease;
        return this;
    }

    /// <summary>Overrides the owning tween's transition for this property.</summary>
    /// <param name="transition">The transition curve.</param>
    /// <returns>This tweener.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="transition"/> is undefined.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tweener is disposing or disposed.</exception>
    public PropertyTweener<TValue> SetTrans(Tween.TransitionType transition)
    {
        EnsureMutable();
        TweenMath.Validate(transition, _ease);
        _transition = transition;
        return this;
    }

    internal override void Attach(Tween owner, Tween.TransitionType transition, Tween.EaseType ease)
    {
        base.Attach(owner, transition, ease);
        _transition = transition;
        _ease = ease;
    }

    internal override void Start()
    {
        base.Start();
        _hasStarted = true;
        _useActiveDelta = false;
        _captureAfterDelay = _continueFromStart && Math.Abs(_delay) >= DelayZeroThreshold;
        if (_continueFromStart && !_captureAfterDelay && !_target.IsDisposed)
            _initialValue = _getter();
        ResolveFinalValue();
    }

    internal override bool OnAdvance(ref double remaining)
    {
        if (_target.IsDisposed)
        {
            Finish();
            return false;
        }

        AddElapsed(remaining);
        if (ElapsedTime < _delay)
        {
            remaining = 0d;
            return true;
        }

        if (_captureAfterDelay && Math.Abs(_delay) >= DelayZeroThreshold)
        {
            _initialValue = _getter();
            _captureAfterDelay = false;
        }

        var time = Math.Min(ElapsedTime - _delay, _duration);
        if (time < _duration)
        {
            var weight = TweenMath.Ease(time / _duration, _transition, _ease);
            if (_customInterpolator is not null)
                weight = _customInterpolator(weight);
            var endpoint = _customInterpolator is null && _useActiveDelta && _add is not null
                ? _add(_initialValue, _activeDelta)
                : _finalValue;
            _setter(_interpolate(_initialValue, endpoint, weight));
            remaining = 0d;
            return true;
        }

        var finalWeight = _customInterpolator?.Invoke(1d);
        _setter(finalWeight.HasValue ? _interpolate(_initialValue, _finalValue, finalWeight.Value) : _finalValue);
        remaining = ElapsedTime - _delay - _duration;
        Finish();
        return false;
    }

    private void ResolveFinalValue() =>
        _finalValue = _relative ? _add!(_initialValue, _baseFinalValue) : _baseFinalValue;

    private void EnsureMutable()
    {
        ThrowIfDisposed();
        Owner?.EnsureOwnerThreadForTweener();
    }
}

/// <summary>Interpolates a typed value and supplies it to a callback over time.</summary>
/// <typeparam name="TValue">The interpolated value type.</typeparam>
public sealed class MethodTweener<TValue> : Tweener
{
    private readonly Action<TValue> _method;
    private readonly ElectronObject? _methodTarget;
    private readonly TValue _from;
    private readonly TValue _to;
    private readonly double _duration;
    private readonly Func<TValue, TValue, double, TValue> _interpolate;
    private double _delay;
    private Tween.TransitionType _transition;
    private Tween.EaseType _ease;

    internal MethodTweener(
        Action<TValue> method,
        TValue from,
        TValue to,
        double duration,
        Func<TValue, TValue, double, TValue> interpolate)
    {
        _method = method;
        _methodTarget = method.Target as ElectronObject;
        _from = from;
        _to = to;
        _duration = duration;
        _interpolate = interpolate;
    }

    /// <summary>Sets the delay before callback interpolation begins.</summary>
    /// <param name="delay">Finite seconds; a negative delay begins on the first positive step.</param>
    /// <returns>This tweener.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delay"/> is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tweener is disposing or disposed.</exception>
    public MethodTweener<TValue> SetDelay(double delay)
    {
        EnsureMutable();
        if (!double.IsFinite(delay))
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "Method delay must be finite.");
        _delay = delay;
        return this;
    }

    /// <summary>Overrides the owning tween's easing for this callback.</summary>
    /// <param name="ease">The easing direction.</param>
    /// <returns>This tweener.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ease"/> is undefined.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tweener is disposing or disposed.</exception>
    public MethodTweener<TValue> SetEase(Tween.EaseType ease)
    {
        EnsureMutable();
        TweenMath.Validate(_transition, ease);
        _ease = ease;
        return this;
    }

    /// <summary>Overrides the owning tween's transition for this callback.</summary>
    /// <param name="transition">The transition curve.</param>
    /// <returns>This tweener.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="transition"/> is undefined.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tweener is disposing or disposed.</exception>
    public MethodTweener<TValue> SetTrans(Tween.TransitionType transition)
    {
        EnsureMutable();
        TweenMath.Validate(transition, _ease);
        _transition = transition;
        return this;
    }

    internal override void Attach(Tween owner, Tween.TransitionType transition, Tween.EaseType ease)
    {
        base.Attach(owner, transition, ease);
        _transition = transition;
        _ease = ease;
    }

    internal override bool OnAdvance(ref double remaining)
    {
        if (_methodTarget?.IsDisposed == true)
        {
            Finish();
            return false;
        }
        AddElapsed(remaining);
        if (ElapsedTime < _delay)
        {
            remaining = 0d;
            return true;
        }

        var time = Math.Min(ElapsedTime - _delay, _duration);
        var value = time < _duration
            ? _interpolate(_from, _to, TweenMath.Ease(time / _duration, _transition, _ease))
            : _to;
        _method(value);
        if (time < _duration)
        {
            remaining = 0d;
            return true;
        }
        remaining = ElapsedTime - _delay - _duration;
        Finish();
        return false;
    }

    private void EnsureMutable()
    {
        ThrowIfDisposed();
        Owner?.EnsureOwnerThreadForTweener();
    }
}

/// <summary>Invokes a parameterless callback after an optional delay.</summary>
public sealed class CallbackTweener : Tweener
{
    private readonly Action _callback;
    private readonly ElectronObject? _callbackTarget;
    private double _delay;

    internal CallbackTweener(Action callback)
    {
        _callback = callback;
        _callbackTarget = callback.Target as ElectronObject;
    }

    /// <summary>Sets the delay before callback invocation.</summary>
    /// <param name="delay">Finite seconds; a negative delay fires on the first positive step.</param>
    /// <returns>This tweener.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delay"/> is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tweener is disposing or disposed.</exception>
    public CallbackTweener SetDelay(double delay)
    {
        ThrowIfDisposed();
        Owner?.EnsureOwnerThreadForTweener();
        if (!double.IsFinite(delay))
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "Callback delay must be finite.");
        _delay = delay;
        return this;
    }

    internal override bool OnAdvance(ref double remaining)
    {
        if (_callbackTarget?.IsDisposed == true)
        {
            Finish();
            return false;
        }
        AddElapsed(remaining);
        if (ElapsedTime < _delay)
        {
            remaining = 0d;
            return true;
        }
        _callback();
        remaining = ElapsedTime - _delay;
        Finish();
        return false;
    }
}

/// <summary>Consumes a fixed duration without invoking a callback or modifying a value.</summary>
public sealed class IntervalTweener : Tweener
{
    private readonly double _duration;

    internal IntervalTweener(double duration) => _duration = duration;

    internal override bool OnAdvance(ref double remaining)
    {
        AddElapsed(remaining);
        if (ElapsedTime < _duration)
        {
            remaining = 0d;
            return true;
        }
        remaining = ElapsedTime - _duration;
        Finish();
        return false;
    }
}

/// <summary>Runs another tween as one step in a parent tween.</summary>
/// <remarks>On completion, unused child time advances the parent's next step. A disposed child releases the parent without a disposed-state query.</remarks>
public sealed class SubtweenTweener : Tweener
{
    private readonly Tween _subtween;
    private double _delay;

    internal SubtweenTweener(Tween subtween) => _subtween = subtween;

    /// <summary>Sets the delay before the nested tween begins.</summary>
    /// <param name="delay">Finite seconds; a negative delay begins on the first positive step.</param>
    /// <returns>This tweener.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delay"/> is NaN or infinite.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tweener is disposing or disposed.</exception>
    public SubtweenTweener SetDelay(double delay)
    {
        ThrowIfDisposed();
        Owner?.EnsureOwnerThreadForTweener();
        if (!double.IsFinite(delay))
            throw new ArgumentOutOfRangeException(nameof(delay), delay, "Subtween delay must be finite.");
        _delay = delay;
        return this;
    }

    internal override void Start()
    {
        base.Start();
        if (_subtween.IsDisposed)
        {
            Finish();
            return;
        }
        _subtween.Stop();
        if (_subtween.IsValid())
            _subtween.Play();
        else
            Finish();
    }

    internal override bool OnAdvance(ref double remaining)
    {
        if (IsFinished)
            return false;
        AddElapsed(remaining);
        if (ElapsedTime < _delay)
        {
            remaining = 0d;
            return true;
        }
        if (_subtween.IsDisposed)
        {
            Finish();
            return false;
        }
        if (_subtween.Advance(remaining))
        {
            remaining = 0d;
            return true;
        }
        remaining = ElapsedTime - _delay - _subtween.GetTotalElapsedTime();
        Finish();
        return false;
    }

    internal override void Cancel()
    {
        if (!_subtween.IsDisposed && _subtween.IsValid())
            _subtween.Kill();
    }
}

/// <summary>Waits for a typed C# event, an optional timeout, or disposal of the event publisher.</summary>
/// <remarks>The event subscription is established when the tweener is appended and released when the wait or its owner ends.</remarks>
public sealed class AwaitTweener : Tweener
{
    private readonly ElectronObject _source;
    private EventConnection? _connection;
    private double? _timeout;
    private int _received;

    internal AwaitTweener(ElectronObject source, Func<Action, EventConnection> connect)
    {
        _source = source;
        _connection = connect(Receive);
    }

    /// <summary>Sets the maximum time to wait for the event.</summary>
    /// <param name="timeout">Finite non-negative seconds.</param>
    /// <returns>This tweener.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeout"/> is negative, NaN, or infinite.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tweener is disposing or disposed.</exception>
    public AwaitTweener SetTimeout(double timeout)
    {
        ThrowIfDisposed();
        Owner?.EnsureOwnerThreadForTweener();
        Tween.ValidateDuration(timeout, nameof(timeout));
        _timeout = timeout;
        return this;
    }

    internal override void Start()
    {
        base.Start();
        Volatile.Write(ref _received, 0);
    }

    internal override bool OnAdvance(ref double remaining)
    {
        if (_source.IsDisposed || _connection?.IsConnected != true)
        {
            Complete();
            return false;
        }
        AddElapsed(remaining);
        if (_timeout.HasValue && ElapsedTime >= _timeout.Value)
        {
            remaining = ElapsedTime - _timeout.Value;
            Complete();
            return false;
        }
        remaining = 0d;
        if (Volatile.Read(ref _received) == 0)
            return true;
        Complete();
        return false;
    }

    internal override void Cancel()
    {
        var connection = _connection;
        _connection = null;
        connection?.Dispose();
    }

    private void Receive() => Volatile.Write(ref _received, 1);

    private void Complete()
    {
        Finish();
    }

}
