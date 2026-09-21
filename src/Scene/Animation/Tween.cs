namespace Electron2D;

/// <summary>Sequences typed property interpolation, method interpolation, callbacks, waits, and nested tweens.</summary>
/// <remarks>
/// A tween is created by <see cref="SceneTree.CreateTween"/> or <see cref="Node.CreateTween"/> and is processed by
/// that tree after node callbacks and lightweight timers in the selected frame lane. Tweeners are sequential unless
/// <see cref="Parallel"/> or <see cref="SetParallel"/> groups them. A completed or killed tween is invalid and cannot
/// accept new tweeners. Tween mutation and processing use the creating tree's owner thread.
/// </remarks>
public sealed class Tween : ElectronObject
{
    /// <summary>Selects the frame lane that advances a tween.</summary>
    public enum TweenProcessMode
    {
        /// <summary>Advances after physics-frame node callbacks and timers.</summary>
        Physics = 0,

        /// <summary>Advances after process-frame node callbacks and timers.</summary>
        Idle = 1,
    }

    /// <summary>Selects how tree pause affects a tween.</summary>
    public enum TweenPauseMode
    {
        /// <summary>Uses the bound node's effective process policy, or stops with the tree when no node is bound.</summary>
        Bound = 0,

        /// <summary>Stops while the owning scene tree is paused.</summary>
        Stop = 1,

        /// <summary>Continues regardless of the owning scene tree's pause state.</summary>
        Process = 2,
    }

    /// <summary>Selects the interpolation curve family.</summary>
    public enum TransitionType
    {
        /// <summary>Uses constant interpolation speed.</summary>
        Linear = 0,

        /// <summary>Uses a sinusoidal curve.</summary>
        Sine = 1,

        /// <summary>Uses a fifth-power curve.</summary>
        Quint = 2,

        /// <summary>Uses a fourth-power curve.</summary>
        Quart = 3,

        /// <summary>Uses a quadratic curve.</summary>
        Quad = 4,

        /// <summary>Uses an exponential curve.</summary>
        Expo = 5,

        /// <summary>Uses an oscillating elastic curve.</summary>
        Elastic = 6,

        /// <summary>Uses a cubic curve.</summary>
        Cubic = 7,

        /// <summary>Uses a circular curve.</summary>
        Circ = 8,

        /// <summary>Uses a bouncing curve.</summary>
        Bounce = 9,

        /// <summary>Uses an overshooting back curve.</summary>
        Back = 10,

        /// <summary>Uses a damped spring curve.</summary>
        Spring = 11,
    }

    /// <summary>Selects where acceleration and deceleration occur within a transition.</summary>
    public enum EaseType
    {
        /// <summary>Starts slowly and accelerates.</summary>
        In = 0,

        /// <summary>Starts quickly and decelerates.</summary>
        Out = 1,

        /// <summary>Starts and ends slowly.</summary>
        InOut = 2,

        /// <summary>Starts and ends quickly.</summary>
        OutIn = 3,
    }

    private readonly List<List<Tweener>> _steps = [];
    private readonly int _ownerThreadId;
    private SceneTree? _tree;
    private Node? _boundNode;
    private int _appendStep = -1;
    private int _currentStep = -1;
    private int _loops = 1;
    private int _loopsDone;
    private double _totalElapsedTime;
    private double _speedScale = 1d;
    private bool _ignoreTimeScale;
    private bool _defaultParallel;
    private bool _parallelNext;
    private bool _started;
    private bool _running = true;
    private bool _dead;
    private bool _valid = true;
    private bool _inStep;
    private TransitionType _defaultTransition = TransitionType.Linear;
    private EaseType _defaultEase = EaseType.InOut;
    private TweenProcessMode _processMode = TweenProcessMode.Idle;
    private TweenPauseMode _pauseMode = TweenPauseMode.Bound;

    internal Tween(SceneTree tree)
    {
        _tree = tree;
        _ownerThreadId = Environment.CurrentManagedThreadId;
    }

    /// <summary>Occurs after every tweener in the final loop finishes.</summary>
    /// <remarks>The tween is stopped but remains valid during synchronous delivery; the tree invalidates it afterward. Killed tweens do not raise this event.</remarks>
    public event Action<Tween>? Finished;

    /// <summary>Occurs after a non-final loop completes.</summary>
    /// <remarks>The first argument is this tween and the second is the one-based completed-loop count.</remarks>
    public event Action<Tween, int>? LoopFinished;

    /// <summary>Occurs when one sequential step or parallel step group completes.</summary>
    /// <remarks>The first argument is this tween and the second is the zero-based step index.</remarks>
    public event Action<Tween, int>? StepFinished;

    /// <summary>Binds processing and lifetime to a node.</summary>
    /// <param name="node">The live node whose tree membership, process policy, and lifetime control this tween.</param>
    /// <returns>This tween.</returns>
    /// <remarks>A detached bound node halts the tween. Disposing the node kills and invalidates the tween.</remarks>
    /// <exception cref="ArgumentNullException"><paramref name="node"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="node"/> is currently owned by another scene tree.</exception>
    /// <exception cref="ObjectDisposedException">The tween or node is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread or the tween is invalid.</exception>
    public Tween BindNode(Node node)
    {
        ArgumentNullException.ThrowIfNull(node);
        ObjectDisposedException.ThrowIf(node.IsDisposed, node);
        EnsureValidMutation();
        if (node.Tree is not null && !ReferenceEquals(node.Tree, _tree))
            throw new ArgumentException("A tween cannot bind to a node owned by another SceneTree.", nameof(node));
        _boundNode = node;
        return this;
    }

    /// <summary>Makes the next appended tweener begin a new sequential step.</summary>
    /// <returns>This tween.</returns>
    /// <remarks>This is primarily used after <see cref="SetParallel"/> enables parallel appending.</remarks>
    /// <exception cref="InvalidOperationException">The call is off the owner thread or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public Tween Chain()
    {
        EnsureCanAppend();
        _parallelNext = false;
        return this;
    }

    /// <summary>Advances the tween manually by an elapsed duration.</summary>
    /// <param name="delta">Finite non-negative elapsed seconds before speed scaling.</param>
    /// <returns><see langword="true"/> while unfinished; otherwise <see langword="false"/>.</returns>
    /// <remarks>This advances a paused tween but still honors a bound node being detached or disposed.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delta"/> is negative, NaN, or infinite.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread, re-enters processing, or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    /// <exception cref="AggregateException">One or more parallel callbacks or completion subscribers fail.</exception>
    public bool CustomStep(double delta)
    {
        ValidateDuration(delta, nameof(delta));
        EnsureValidMutation();
        if (_inStep)
            throw new InvalidOperationException("A tween cannot be advanced recursively.");

        var wasRunning = _running;
        _running = true;
        var tree = _tree;
        try
        {
            var unfinished = Advance(delta);
            _running = _running && wasRunning;
            if (!unfinished)
                tree?.CompleteTween(this);
            return unfinished;
        }
        catch
        {
            tree?.RemoveTween(this);
            throw;
        }
    }

    /// <summary>Gets the number of remaining sequence executions.</summary>
    /// <returns><c>-1</c> for an infinite tween, zero after completion, or the positive remaining count.</returns>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public int GetLoopsLeft()
    {
        ThrowIfDisposed();
        return _loops == 0 ? -1 : Math.Max(0, _loops - _loopsDone);
    }

    /// <summary>Gets accumulated scaled processing time.</summary>
    /// <returns>Seconds accumulated while the tween was actively advanced, including final-frame overshoot.</returns>
    /// <remarks><see cref="Stop"/> resets this value.</remarks>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public double GetTotalElapsedTime()
    {
        ThrowIfDisposed();
        return _totalElapsedTime;
    }

    /// <summary>Gets whether this tween contains at least one tweener.</summary>
    /// <returns><see langword="true"/> when at least one tweener was appended, including after invalidation.</returns>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public bool HasTweeners()
    {
        ThrowIfDisposed();
        return _steps.Count != 0;
    }

    /// <summary>Interpolates a typed value from an initial value by a delta.</summary>
    /// <typeparam name="TValue">A supported numeric or Electron2D math value type.</typeparam>
    /// <param name="initialValue">The starting value.</param>
    /// <param name="deltaValue">The change from the start to the final value.</param>
    /// <param name="elapsedTime">Finite elapsed seconds; values outside the duration extrapolate.</param>
    /// <param name="duration">Finite non-negative total duration.</param>
    /// <param name="transition">The transition curve.</param>
    /// <param name="ease">The easing direction.</param>
    /// <returns>The interpolated value; a zero duration always returns the final value.</returns>
    /// <exception cref="ArgumentOutOfRangeException">A time is non-finite, duration is negative, or an enum value is undefined.</exception>
    /// <exception cref="NotSupportedException"><typeparamref name="TValue"/> has no built-in interpolation and addition contract.</exception>
    /// <exception cref="OverflowException">An integer result is outside its destination type.</exception>
    public static TValue InterpolateValue<TValue>(
        TValue initialValue,
        TValue deltaValue,
        double elapsedTime,
        double duration,
        TransitionType transition,
        EaseType ease)
    {
        if (!double.IsFinite(elapsedTime))
            throw new ArgumentOutOfRangeException(nameof(elapsedTime), elapsedTime, "Elapsed time must be finite.");
        ValidateDuration(duration, nameof(duration));
        TweenMath.Validate(transition, ease);
        var add = TweenValue<TValue>.Add ?? throw TweenValue<TValue>.Unsupported();
        var interpolate = TweenValue<TValue>.Interpolate ?? throw TweenValue<TValue>.Unsupported();
        var finalValue = add(initialValue, deltaValue);
        var weight = duration == 0d ? 1d : TweenMath.Ease(elapsedTime / duration, transition, ease);
        return interpolate(initialValue, finalValue, weight);
    }

    /// <summary>Gets whether the tween is currently playing.</summary>
    /// <returns><see langword="true"/> while running, including before its first frame.</returns>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public bool IsRunning()
    {
        ThrowIfDisposed();
        return _running && !_dead;
    }

    /// <summary>Gets whether this tween remains registered for scene-tree processing.</summary>
    /// <returns><see langword="true"/> before completion or killing; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public bool IsValid()
    {
        ThrowIfDisposed();
        return _valid;
    }

    /// <summary>Aborts all tweening operations and invalidates this tween.</summary>
    /// <remarks>No completion events are raised. Nested tweens are killed as well.</remarks>
    /// <exception cref="InvalidOperationException">The call is off the owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public void Kill()
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        KillCore(removeFromTree: true);
    }

    /// <summary>Makes only the next appended tweener join the preceding step.</summary>
    /// <returns>This tween.</returns>
    /// <exception cref="InvalidOperationException">The call is off the owner thread, processing has started, or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public Tween Parallel()
    {
        EnsureCanAppend();
        _parallelNext = true;
        return this;
    }

    /// <summary>Pauses progression without resetting current tweener state.</summary>
    /// <exception cref="InvalidOperationException">The call is off the owner thread or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public void Pause()
    {
        EnsureValidMutation();
        _running = false;
    }

    /// <summary>Resumes a paused or stopped tween.</summary>
    /// <remarks>A stopped tween restarts from step zero and property tweeners recapture their configured starting state.</remarks>
    /// <exception cref="InvalidOperationException">The call is off the owner thread, the tween is invalid, or it already completed without being stopped first.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public void Play()
    {
        EnsureValidMutation();
        if (_dead)
            throw new InvalidOperationException("A completed tween must be stopped before it can be played again.");
        _running = true;
    }

    /// <summary>Sets the default easing for property and method tweeners appended afterward.</summary>
    /// <param name="ease">The default easing direction.</param>
    /// <returns>This tween.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="ease"/> is undefined.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public Tween SetEase(EaseType ease)
    {
        EnsureValidMutation();
        TweenMath.Validate(_defaultTransition, ease);
        _defaultEase = ease;
        return this;
    }

    /// <summary>Sets whether Engine time scaling is bypassed.</summary>
    /// <param name="ignore">Whether to use the original host-supplied delta.</param>
    /// <returns>This tween.</returns>
    /// <exception cref="InvalidOperationException">The call is off the owner thread or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public Tween SetIgnoreTimeScale(bool ignore = true)
    {
        EnsureValidMutation();
        _ignoreTimeScale = ignore;
        return this;
    }

    /// <summary>Sets how many times the complete sequence runs.</summary>
    /// <param name="loops">Zero for infinite repetition, or a positive total execution count.</param>
    /// <returns>This tween.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="loops"/> is negative.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public Tween SetLoops(int loops = 0)
    {
        EnsureValidMutation();
        if (loops < 0)
            throw new ArgumentOutOfRangeException(nameof(loops), loops, "Loop count must be zero or positive.");
        _loops = loops;
        return this;
    }

    /// <summary>Sets whether subsequently appended tweeners are parallel by default.</summary>
    /// <param name="parallel">Whether appends join the preceding step.</param>
    /// <returns>This tween.</returns>
    /// <remarks>The tweener immediately preceding this call joins the same parallel step as the next append.</remarks>
    /// <exception cref="InvalidOperationException">The call is off the owner thread, processing has started, or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public Tween SetParallel(bool parallel = true)
    {
        EnsureCanAppend();
        _defaultParallel = parallel;
        _parallelNext = parallel;
        return this;
    }

    /// <summary>Sets the behavior while the owning tree is paused.</summary>
    /// <param name="mode">The pause policy.</param>
    /// <returns>This tween.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is undefined.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public Tween SetPauseMode(TweenPauseMode mode)
    {
        EnsureValidMutation();
        if (!Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "The tween pause mode is undefined.");
        _pauseMode = mode;
        return this;
    }

    /// <summary>Sets the frame lane that advances this tween.</summary>
    /// <param name="mode">The process or physics lane.</param>
    /// <returns>This tween.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="mode"/> is undefined.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public Tween SetProcessMode(TweenProcessMode mode)
    {
        EnsureValidMutation();
        if (!Enum.IsDefined(mode))
            throw new ArgumentOutOfRangeException(nameof(mode), mode, "The tween process mode is undefined.");
        _processMode = mode;
        return this;
    }

    /// <summary>Sets a multiplier applied to time delivered to every tweener and delay.</summary>
    /// <param name="speed">A finite non-negative multiplier. Zero freezes progression without changing running state.</param>
    /// <returns>This tween.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="speed"/> is negative, NaN, or infinite.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public Tween SetSpeedScale(double speed)
    {
        EnsureValidMutation();
        ValidateDuration(speed, nameof(speed));
        _speedScale = speed;
        return this;
    }

    /// <summary>Sets the default transition for property and method tweeners appended afterward.</summary>
    /// <param name="transition">The default curve family.</param>
    /// <returns>This tween.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="transition"/> is undefined.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public Tween SetTrans(TransitionType transition)
    {
        EnsureValidMutation();
        TweenMath.Validate(transition, _defaultEase);
        _defaultTransition = transition;
        return this;
    }

    /// <summary>Stops progression and resets the sequence cursor and elapsed time.</summary>
    /// <remarks>Appended tweeners remain. Animated targets are not restored. Call <see cref="Play"/> to restart.</remarks>
    /// <exception cref="InvalidOperationException">The call is off the owner thread or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public void Stop()
    {
        EnsureValidMutation();
        StopCore();
    }

    /// <summary>Appends a callback that runs once after an optional delay.</summary>
    /// <param name="callback">The callback to invoke synchronously on the owner thread.</param>
    /// <returns>The appended tweener.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="callback"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread, processing started, or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public CallbackTweener TweenCallback(Action callback)
    {
        ArgumentNullException.ThrowIfNull(callback);
        return Append(new CallbackTweener(callback));
    }

    /// <summary>Appends a duration that changes no value.</summary>
    /// <param name="time">Finite non-negative seconds.</param>
    /// <returns>The appended tweener.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="time"/> is negative, NaN, or infinite.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread, processing started, or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public IntervalTweener TweenInterval(double time)
    {
        ValidateDuration(time, nameof(time));
        return Append(new IntervalTweener(time));
    }

    /// <summary>Appends typed interpolation delivered to a callback.</summary>
    /// <typeparam name="TValue">The value type.</typeparam>
    /// <param name="method">Receives the current value each active frame and the exact final value at completion.</param>
    /// <param name="from">The starting value.</param>
    /// <param name="to">The final value.</param>
    /// <param name="duration">Finite non-negative interpolation seconds.</param>
    /// <param name="interpolator">Optional typed interpolation. Omit it for a supported built-in value type.</param>
    /// <returns>The appended typed method tweener.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="method"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is negative, NaN, or infinite.</exception>
    /// <exception cref="NotSupportedException">No interpolator is supplied for an unsupported type.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread, processing started, or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween is disposing or disposed.</exception>
    public MethodTweener<TValue> TweenMethod<TValue>(
        Action<TValue> method,
        TValue from,
        TValue to,
        double duration,
        Func<TValue, TValue, double, TValue>? interpolator = null)
    {
        ArgumentNullException.ThrowIfNull(method);
        ValidateDuration(duration, nameof(duration));
        interpolator ??= TweenValue<TValue>.Interpolate ?? throw TweenValue<TValue>.Unsupported();
        return Append(new MethodTweener<TValue>(method, from, to, duration, interpolator));
    }

    /// <summary>Appends typed interpolation of an engine object's property through explicit accessors.</summary>
    /// <typeparam name="TTarget">The engine-object type containing the property.</typeparam>
    /// <typeparam name="TValue">The property value type.</typeparam>
    /// <param name="target">The live target object.</param>
    /// <param name="getter">Reads the property from the target.</param>
    /// <param name="setter">Writes the property on the target.</param>
    /// <param name="finalValue">The final absolute value, or relative delta after <see cref="PropertyTweener{TValue}.AsRelative"/>.</param>
    /// <param name="duration">Finite non-negative interpolation seconds.</param>
    /// <param name="interpolator">Optional typed interpolation. Omit it for a supported built-in value type.</param>
    /// <returns>The appended typed property tweener.</returns>
    /// <remarks>The getter is called immediately to capture the value used by <see cref="PropertyTweener{TValue}.FromCurrent"/>.</remarks>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="duration"/> is negative, NaN, or infinite.</exception>
    /// <exception cref="NotSupportedException">No interpolator is supplied for an unsupported type.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread, processing started, or the tween is invalid.</exception>
    /// <exception cref="ObjectDisposedException">The tween or target is disposing or disposed.</exception>
    /// <exception cref="Exception">The supplied getter throws.</exception>
    public PropertyTweener<TValue> TweenProperty<TTarget, TValue>(
        TTarget target,
        Func<TTarget, TValue> getter,
        Action<TTarget, TValue> setter,
        TValue finalValue,
        double duration,
        Func<TValue, TValue, double, TValue>? interpolator = null)
        where TTarget : ElectronObject
    {
        ArgumentNullException.ThrowIfNull(target);
        ArgumentNullException.ThrowIfNull(getter);
        ArgumentNullException.ThrowIfNull(setter);
        ObjectDisposedException.ThrowIf(target.IsDisposed, target);
        ValidateDuration(duration, nameof(duration));
        EnsureCanAppend();
        interpolator ??= TweenValue<TValue>.Interpolate ?? throw TweenValue<TValue>.Unsupported();
        var current = getter(target);
        return Append(new PropertyTweener<TValue>(
            target,
            () => getter(target),
            value => setter(target, value),
            current,
            finalValue,
            duration,
            interpolator,
            TweenValue<TValue>.Add));
    }

    /// <summary>Appends a child tween as one step and removes it from independent tree processing.</summary>
    /// <param name="subtween">A valid tween owned by the same scene tree.</param>
    /// <returns>The appended nested-tween step.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="subtween"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">The tween is nested into itself, already nested, or would create a cycle.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread, parent processing has started, a tween is invalid or currently processing, or the tweens belong to different trees.</exception>
    /// <exception cref="ObjectDisposedException">Either tween is disposing or disposed.</exception>
    public SubtweenTweener TweenSubtween(Tween subtween)
    {
        ArgumentNullException.ThrowIfNull(subtween);
        EnsureCanAppend();
        ObjectDisposedException.ThrowIf(subtween.IsDisposed, subtween);
        subtween.EnsureOwnerThread();
        if (ReferenceEquals(this, subtween))
            throw new ArgumentException("A tween cannot contain itself.", nameof(subtween));
        if (!subtween._valid)
            throw new InvalidOperationException("Only a valid tween can be nested.");
        if (subtween._inStep)
            throw new InvalidOperationException("A tween cannot be nested while it is being processed.");
        if (subtween._parentTween is not null)
            throw new ArgumentException("A tween can be nested by only one parent.", nameof(subtween));
        if (!ReferenceEquals(_tree, subtween._tree))
            throw new InvalidOperationException("A nested tween must belong to the same SceneTree.");
        for (var ancestor = this; ancestor is not null; ancestor = ancestor._parentTween)
        {
            if (ReferenceEquals(ancestor, subtween))
                throw new ArgumentException("Nesting would create a tween cycle.", nameof(subtween));
        }

        _tree!.DetachTween(subtween);
        subtween._parentTween = this;
        return Append(new SubtweenTweener(subtween));
    }

    /// <summary>Appends a wait for a parameterless typed C# event.</summary>
    /// <param name="source">The event publisher whose disposal also completes the wait.</param>
    /// <param name="subscribe">Adds the supplied handler to the event.</param>
    /// <param name="unsubscribe">Removes the supplied handler from the event.</param>
    /// <returns>The appended event-wait tweener.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The tween or source is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread, processing started, or the tween is invalid.</exception>
    /// <exception cref="Exception">A supplied event accessor throws.</exception>
    public AwaitTweener TweenAwait(ElectronObject source, Action<Action> subscribe, Action<Action> unsubscribe)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(subscribe);
        ArgumentNullException.ThrowIfNull(unsubscribe);
        ObjectDisposedException.ThrowIf(source.IsDisposed, source);
        EnsureCanAppend();
        return Append(new AwaitTweener(
            source,
            handler => EventConnection.Subscribe(subscribe, unsubscribe, handler)));
    }

    /// <summary>Appends a wait for a one-argument typed C# event.</summary>
    /// <typeparam name="T">The event argument type.</typeparam>
    /// <param name="source">The event publisher whose disposal also completes the wait.</param>
    /// <param name="subscribe">Adds the supplied handler to the event.</param>
    /// <param name="unsubscribe">Removes the supplied handler from the event.</param>
    /// <returns>The appended event-wait tweener.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The tween or source is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread, processing started, or the tween is invalid.</exception>
    /// <exception cref="Exception">A supplied event accessor throws.</exception>
    public AwaitTweener TweenAwait<T>(ElectronObject source, Action<Action<T>> subscribe, Action<Action<T>> unsubscribe)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(subscribe);
        ArgumentNullException.ThrowIfNull(unsubscribe);
        ObjectDisposedException.ThrowIf(source.IsDisposed, source);
        EnsureCanAppend();
        return Append(new AwaitTweener(
            source,
            handler => EventConnection.Subscribe<T>(subscribe, unsubscribe, _ => handler())));
    }

    /// <summary>Appends a wait for a two-argument typed C# event.</summary>
    /// <typeparam name="T1">The first event argument type.</typeparam>
    /// <typeparam name="T2">The second event argument type.</typeparam>
    /// <param name="source">The event publisher whose disposal also completes the wait.</param>
    /// <param name="subscribe">Adds the supplied handler to the event.</param>
    /// <param name="unsubscribe">Removes the supplied handler from the event.</param>
    /// <returns>The appended event-wait tweener.</returns>
    /// <exception cref="ArgumentNullException">A required argument is <see langword="null"/>.</exception>
    /// <exception cref="ObjectDisposedException">The tween or source is disposing or disposed.</exception>
    /// <exception cref="InvalidOperationException">The call is off the owner thread, processing started, or the tween is invalid.</exception>
    /// <exception cref="Exception">A supplied event accessor throws.</exception>
    public AwaitTweener TweenAwait<T1, T2>(
        ElectronObject source,
        Action<Action<T1, T2>> subscribe,
        Action<Action<T1, T2>> unsubscribe)
    {
        ArgumentNullException.ThrowIfNull(source);
        ArgumentNullException.ThrowIfNull(subscribe);
        ArgumentNullException.ThrowIfNull(unsubscribe);
        ObjectDisposedException.ThrowIf(source.IsDisposed, source);
        EnsureCanAppend();
        return Append(new AwaitTweener(
            source,
            handler => EventConnection.Subscribe<T1, T2>(subscribe, unsubscribe, (_, _) => handler())));
    }

    /// <inheritdoc />
    /// <remarks>Rejects disposal during processing and requires the original scene-tree owner thread.</remarks>
    /// <exception cref="InvalidOperationException">The call is off the owner thread or occurs inside a tween callback.</exception>
    protected override void ValidateDisposal()
    {
        EnsureOwnerThread();
        if (_inStep)
            throw new InvalidOperationException("A tween cannot be disposed from one of its processing callbacks.");
        base.ValidateDisposal();
    }

    /// <inheritdoc />
    /// <remarks>Invalidates nested tweens, disposes owned tweener objects, clears subscribers, and calls the base implementation. Nested tween objects remain managed and inspectable until separately disposed.</remarks>
    protected override void Dispose(bool disposing)
    {
        List<Exception>? errors = null;
        if (disposing)
        {
            try
            {
                KillCore(removeFromTree: true);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
            foreach (var step in _steps)
            {
                foreach (var tweener in step)
                {
                    try
                    {
                        tweener.Dispose();
                    }
                    catch (Exception error)
                    {
                        CollectException(ref errors, error);
                    }
                }
            }

            _steps.Clear();
            Finished = null;
            LoopFinished = null;
            StepFinished = null;
            _boundNode = null;
        }

        try
        {
            base.Dispose(disposing);
        }
        catch (Exception error)
        {
            CollectException(ref errors, error);
        }

        if (errors is not null)
            throw new AggregateException("One or more tween teardown operations failed.", errors);
    }

    private Tween? _parentTween;

    internal TweenProcessMode ProcessMode => _processMode;

    internal bool IgnoreTimeScale => _ignoreTimeScale;

    internal bool IsNested => _parentTween is not null;

    internal bool CanProcess(bool treePaused)
    {
        if (!_valid)
            return true;
        if (_boundNode is not null && _pauseMode == TweenPauseMode.Bound)
            return _boundNode.IsDisposed || ReferenceEquals(_boundNode.Tree, _tree) && _boundNode.CanProcess();
        return !treePaused || _pauseMode == TweenPauseMode.Process;
    }

    internal bool Advance(double delta)
    {
        if (!_valid || _dead)
            return false;
        if (_boundNode is not null)
        {
            if (_boundNode.IsDisposed)
            {
                KillCore(removeFromTree: false);
                return false;
            }
            if (!ReferenceEquals(_boundNode.Tree, _tree))
                return true;
        }
        if (!_running)
            return true;
        if (_inStep)
            throw new InvalidOperationException("A tween cannot be advanced recursively.");

        _inStep = true;
        try
        {
            if (!_started)
            {
                if (_steps.Count == 0)
                    throw new InvalidOperationException("A tween cannot start without tweeners.");
                _currentStep = 0;
                _loopsDone = 0;
                _totalElapsedTime = 0d;
                StartCurrentStep();
                _started = true;
            }

            var remaining = delta * _speedScale;
            var initialRemaining = remaining;
            _totalElapsedTime += remaining;
            var unchangedInfiniteLoops = 0;

            while (_running && remaining > 0d)
            {
                var stepRemaining = remaining;
                var active = false;
                List<Exception>? errors = null;

                foreach (var tweener in _steps[_currentStep])
                {
                    var candidateRemaining = remaining;
                    try
                    {
                        active |= tweener.Advance(ref candidateRemaining);
                    }
                    catch (Exception error)
                    {
                        CollectException(ref errors, error);
                    }
                    stepRemaining = Math.Min(stepRemaining, candidateRemaining);
                }

                if (errors is not null)
                {
                    KillCore(removeFromTree: false);
                    throw new AggregateException("One or more parallel tweeners failed.", errors);
                }
                if (!_valid || !_started)
                    return _valid;

                remaining = stepRemaining;
                if (active)
                    continue;

                StepFinished?.Invoke(this, _currentStep);
                if (!_valid || !_started)
                    return _valid;

                _currentStep++;
                if (_currentStep < _steps.Count)
                {
                    StartCurrentStep();
                    continue;
                }

                _loopsDone++;
                if (_loops != 0 && _loopsDone >= _loops)
                {
                    _running = false;
                    _dead = true;
                    Finished?.Invoke(this);
                    return false;
                }

                LoopFinished?.Invoke(this, _loopsDone);
                if (!_valid || !_started)
                    return _valid;
                _currentStep = 0;
                StartCurrentStep();

                if (_loops == 0 && remaining == initialRemaining)
                {
                    unchangedInfiniteLoops++;
                    if (unchangedInfiniteLoops >= 2)
                    {
                        KillCore(removeFromTree: false);
                        throw new InvalidOperationException("An infinite tween loop must contain a positive duration or delay.");
                    }
                }
                else
                {
                    unchangedInfiniteLoops = 0;
                }
            }

            return true;
        }
        catch (Exception processingError)
        {
            if (_valid)
            {
                try
                {
                    KillCore(removeFromTree: false);
                }
                catch (Exception cleanupError)
                {
                    throw new AggregateException("Tween processing and cleanup both failed.", processingError, cleanupError);
                }
            }
            throw;
        }
        finally
        {
            _inStep = false;
        }
    }

    internal void InvalidateFromTree()
    {
        _valid = false;
        _running = false;
        _dead = true;
        List<Exception>? errors = null;
        foreach (var step in _steps)
        {
            foreach (var tweener in step)
            {
                try
                {
                    tweener.Cancel();
                }
                catch (Exception error)
                {
                    CollectException(ref errors, error);
                }
            }
        }
        _tree = null;
        if (errors is not null)
            throw new AggregateException("One or more tweeners failed to cancel.", errors);
    }

    internal void EnsureOwnerThreadForTweener() => EnsureOwnerThread();

    private TTweener Append<TTweener>(TTweener tweener)
        where TTweener : Tweener
    {
        EnsureCanAppend();
        tweener.Attach(this, _defaultTransition, _defaultEase);
        if (_parallelNext)
            _appendStep = Math.Max(_appendStep, 0);
        else
            _appendStep++;
        _parallelNext = _defaultParallel;
        while (_steps.Count <= _appendStep)
            _steps.Add([]);
        _steps[_appendStep].Add(tweener);
        return tweener;
    }

    private void StartCurrentStep()
    {
        List<Exception>? errors = null;
        foreach (var tweener in _steps[_currentStep])
        {
            try
            {
                tweener.Start();
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }
        if (errors is not null)
            throw new AggregateException("One or more parallel tweeners failed to start.", errors);
    }

    private void StopCore()
    {
        _running = false;
        _started = false;
        _dead = false;
        _currentStep = -1;
        _loopsDone = 0;
        _totalElapsedTime = 0d;
    }

    private void KillCore(bool removeFromTree)
    {
        if (!_valid && _dead)
            return;
        _running = false;
        _valid = false;
        _dead = true;
        List<Exception>? errors = null;
        foreach (var step in _steps)
        {
            foreach (var tweener in step)
            {
                try
                {
                    tweener.Cancel();
                }
                catch (Exception error)
                {
                    CollectException(ref errors, error);
                }
            }
        }
        if (removeFromTree)
        {
            try
            {
                _tree?.RemoveTween(this);
            }
            catch (Exception error)
            {
                CollectException(ref errors, error);
            }
        }
        _tree = null;
        if (errors is not null)
            throw new AggregateException("One or more tween cancellation operations failed.", errors);
    }

    private void EnsureCanAppend()
    {
        EnsureValidMutation();
        if (_started)
            throw new InvalidOperationException("Tweeners cannot be appended after processing starts. Stop the tween first.");
    }

    private void EnsureValidMutation()
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        if (!_valid)
            throw new InvalidOperationException("The tween is no longer valid.");
    }

    private void EnsureOwnerThread()
    {
        if (Environment.CurrentManagedThreadId != _ownerThreadId)
            throw new InvalidOperationException("Tween mutation and processing must run on its SceneTree owner thread.");
    }

    internal static void ValidateDuration(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value < 0d)
            throw new ArgumentOutOfRangeException(parameterName, value, "Time must be finite and non-negative.");
    }

    private static void CollectException(ref List<Exception>? errors, Exception error)
    {
        errors ??= [];
        if (error is AggregateException aggregate)
            errors.AddRange(aggregate.Flatten().InnerExceptions);
        else
            errors.Add(error);
    }
}
