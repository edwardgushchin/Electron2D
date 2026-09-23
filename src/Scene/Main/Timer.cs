namespace Electron2D;

/// <summary>Provides a reusable scene-node countdown timer.</summary>
/// <remarks>
/// The timer advances at most once in its selected frame lane, emits <see cref="Timeout"/> when its remaining time
/// becomes negative, and either stops or reloads according to <see cref="OneShot"/>. It has no clock or background thread.
/// </remarks>
public class Timer : Node
{
    private static readonly IReadOnlyList<PropertyDescriptor> TimerProperties = Array.AsReadOnly<PropertyDescriptor>(
    [
        new PropertyDescriptor<Timer, TimerProcessCallback>(
            nameof(ProcessCallback),
            timer => timer.ProcessCallback,
            (timer, value) => timer.ProcessCallback = value,
            _ => TimerProcessCallback.Idle,
            (_, value) => Enum.IsDefined(value),
            stored: true),
        new PropertyDescriptor<Timer, double>(
            nameof(WaitTime),
            timer => timer.WaitTime,
            (timer, value) => timer.WaitTime = value,
            _ => 1d,
            (_, value) => double.IsFinite(value) && value > 0d,
            stored: true),
        new PropertyDescriptor<Timer, bool>(
            nameof(OneShot),
            timer => timer.OneShot,
            (timer, value) => timer.OneShot = value,
            _ => false,
            stored: true),
        new PropertyDescriptor<Timer, bool>(
            nameof(Autostart),
            timer => timer.Autostart,
            (timer, value) => timer.Autostart = value,
            _ => false,
            stored: true),
        new PropertyDescriptor<Timer, bool>(
            nameof(Paused),
            timer => timer.Paused,
            (timer, value) => timer.Paused = value,
            _ => false),
        new PropertyDescriptor<Timer, bool>(
            nameof(IgnoreTimeScale),
            timer => timer.IgnoreTimeScale,
            (timer, value) => timer.IgnoreTimeScale = value,
            _ => false,
            stored: true),
        new PropertyDescriptor<Timer, double>(nameof(TimeLeft), timer => timer.TimeLeft)
    ]);

    private TimerProcessCallback _processCallback = TimerProcessCallback.Idle;
    private double _waitTime = 1d;
    private double _timeLeft;
    private bool _oneShot;
    private bool _autostart;
    private bool _paused;
    private bool _ignoreTimeScale;
    private bool _processing;

    /// <summary>Initializes a stopped timer with a one-second wait in the process-frame lane.</summary>
    public Timer()
    {
    }

    /// <summary>Gets or sets the frame lane that advances this timer.</summary>
    /// <value><see cref="TimerProcessCallback.Idle"/> by default.</value>
    /// <remarks>Changing the lane while running moves internal processing without resetting <see cref="TimeLeft"/>.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is undefined.</exception>
    /// <exception cref="InvalidOperationException">An attached timer is mutated off its tree's owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The timer is disposing on another thread or has finished disposing.</exception>
    public TimerProcessCallback ProcessCallback
    {
        get
        {
            ThrowIfDisposed();
            return _processCallback;
        }
        set
        {
            EnsureMutable();
            if (!Enum.IsDefined(value))
                throw new ArgumentOutOfRangeException(nameof(value), value, "Unknown timer process callback lane.");

            if (_processCallback == value)
                return;

            ApplyProcessingState(_processing, _paused, value);
            _processCallback = value;
        }
    }

    /// <summary>Gets or sets the countdown duration in seconds.</summary>
    /// <value>A finite value greater than zero; the default is one second.</value>
    /// <remarks>Changing the value does not alter the current countdown until the timer restarts or repeats.
    /// Every valid assignment commits duration before configuration-warning refresh, even when unchanged.</remarks>
    /// <exception cref="Exception">A configuration-warning subscriber fails after assignment.</exception>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is zero, negative, NaN, or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached timer is mutated off its tree's owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The timer is disposing on another thread or has finished disposing.</exception>
    public double WaitTime
    {
        get
        {
            ThrowIfDisposed();
            return _waitTime;
        }
        set
        {
            EnsureMutable();
            ValidateWaitTime(value, nameof(value));
            _waitTime = value;
            UpdateConfigurationWarnings();
        }
    }

    /// <summary>Gets or sets whether the timer stops after its next timeout.</summary>
    /// <value><see langword="false"/> by default, causing automatic restart after each timeout.</value>
    /// <exception cref="InvalidOperationException">An attached timer is mutated off its tree's owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The timer is disposing on another thread or has finished disposing.</exception>
    public bool OneShot
    {
        get
        {
            ThrowIfDisposed();
            return _oneShot;
        }
        set
        {
            EnsureMutable();
            _oneShot = value;
        }
    }

    /// <summary>Gets or sets whether ready delivery starts the timer automatically.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <remarks>
    /// A successful automatic start resets this property to <see langword="false"/>. Setting it after ready delivery
    /// does not start immediately; call <see cref="Start()"/> or request another ready cycle before reattachment.
    /// </remarks>
    /// <exception cref="InvalidOperationException">An attached timer is mutated off its tree's owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The timer is disposing on another thread or has finished disposing.</exception>
    public bool Autostart
    {
        get
        {
            ThrowIfDisposed();
            return _autostart;
        }
        set
        {
            EnsureMutable();
            _autostart = value;
        }
    }

    /// <summary>Gets or sets whether this timer's own countdown is paused.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <remarks>
    /// Pausing preserves the remaining time. Starting a paused timer resets its countdown but does not resume it.
    /// Scene-tree pause policy remains independently controlled by <see cref="Node.ProcessMode"/>.
    /// </remarks>
    /// <exception cref="InvalidOperationException">An attached timer is mutated off its tree's owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The timer is disposing on another thread or has finished disposing.</exception>
    public bool Paused
    {
        get
        {
            ThrowIfDisposed();
            return _paused;
        }
        set
        {
            EnsureMutable();
            if (_paused == value)
                return;

            ApplyProcessingState(_processing, value, _processCallback);
            _paused = value;
        }
    }

    /// <summary>Gets or sets whether the countdown ignores <see cref="Engine.TimeScale"/>.</summary>
    /// <value><see langword="false"/> by default.</value>
    /// <remarks>
    /// Engine-driven frames use the original process-frame step in both frame lanes when enabled. Direct
    /// <see cref="SceneTree"/> frame calls have no separate process step and use their supplied delta.
    /// </remarks>
    /// <exception cref="InvalidOperationException">An attached timer is mutated off its tree's owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The timer is disposing on another thread or has finished disposing.</exception>
    public bool IgnoreTimeScale
    {
        get
        {
            ThrowIfDisposed();
            return _ignoreTimeScale;
        }
        set
        {
            EnsureMutable();
            _ignoreTimeScale = value;
        }
    }

    /// <summary>Gets the remaining countdown time in seconds.</summary>
    /// <value>The non-negative remaining time, or zero while stopped or after an overshooting repeat frame.</value>
    /// <remarks>The value is read-only. Use <see cref="Start(double)"/> to change the duration and restart.</remarks>
    /// <exception cref="ObjectDisposedException">The timer is disposing on another thread or has finished disposing.</exception>
    public double TimeLeft
    {
        get
        {
            ThrowIfDisposed();
            return Math.Max(0d, _timeLeft);
        }
    }

    /// <summary>Occurs when the countdown passes below zero.</summary>
    /// <remarks>
    /// Delivery is synchronous on the scene-tree owner thread. One-shot timers stop before delivery; repeating timers
    /// reload first. At most one timeout is emitted per matching frame. Handler exceptions propagate through the frame
    /// after the remaining callback phases are attempted.
    /// </remarks>
    public event Action<Timer>? Timeout;

    /// <summary>Gets whether the timer is stopped or has not started.</summary>
    /// <returns><see langword="true"/> when no positive remaining time is observable; otherwise <see langword="false"/>.</returns>
    /// <remarks>At exact zero this returns <see langword="true"/>, although the internal lane continues until the countdown becomes negative and emits <see cref="Timeout"/>.</remarks>
    /// <exception cref="ObjectDisposedException">The timer is disposing on another thread or has finished disposing.</exception>
    public bool IsStopped()
    {
        ThrowIfDisposed();
        return TimeLeft <= 0d;
    }

    /// <summary>Starts the timer using <see cref="WaitTime"/>, or resets an already running countdown.</summary>
    /// <remarks>Calling this method does not clear <see cref="Paused"/>.</remarks>
    /// <exception cref="InvalidOperationException">The timer is detached or called off its tree's owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The timer is disposing on another thread or has finished disposing.</exception>
    public void Start()
    {
        EnsureMutable();
        EnsureInsideTree();
        StartCore();
    }

    /// <inheritdoc />
    /// <remarks>Appends a warning when WaitTime is less than 0.05 minus Mathf.Epsilon seconds, since frame
    /// cadence controls delivered timeouts. The warning is available even when detached or stopped.</remarks>
    public override string[] GetConfigurationWarnings()
    {
        var warnings = base.GetConfigurationWarnings();
        return _waitTime < 0.05 - Mathf.Epsilon
            ? [.. warnings, "Timer intervals below 0.05 seconds depend strongly on frame cadence. Consider using the process callback for very short intervals."] : warnings;
    }

    /// <summary>Sets a new wait duration and starts or resets the timer.</summary>
    /// <param name="timeSeconds">The finite positive countdown duration in seconds.</param>
    /// <remarks>This typed overload replaces sentinel duration values. Calling it does not clear <see cref="Paused"/>.
    /// WaitTime commits and requests warning refresh before countdown starts. If that callback disposes or detaches
    /// the timer, starting is abandoned. A throwing callback leaves the changed duration and previous countdown.</remarks>
    /// <exception cref="Exception">A configuration-warning subscriber fails.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="timeSeconds"/> is zero, negative, NaN, or infinite.</exception>
    /// <exception cref="InvalidOperationException">The timer is detached or called off its tree's owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The timer is disposing on another thread or has finished disposing.</exception>
    public void Start(double timeSeconds)
    {
        EnsureMutable();
        EnsureInsideTree();
        ValidateWaitTime(timeSeconds, nameof(timeSeconds));
        WaitTime = timeSeconds;
        if (IsDisposed || !IsInsideTree) return;
        StartCore();
    }

    /// <summary>Stops the timer without emitting <see cref="Timeout"/>.</summary>
    /// <remarks>The method is valid while detached and also clears <see cref="Autostart"/>.</remarks>
    /// <exception cref="InvalidOperationException">An attached timer is mutated off its tree's owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The timer is disposing on another thread or has finished disposing.</exception>
    public void Stop()
    {
        EnsureMutable();
        ApplyProcessingState(processing: false, _paused, _processCallback);
        _processing = false;
        _timeLeft = 0d;
        _autostart = false;
    }

    /// <inheritdoc />
    /// <remarks>
    /// Appends countdown configuration, runtime pause, remaining-time, and frame-lane descriptors. Runtime pause and
    /// remaining time are not stored by packed scenes.
    /// </remarks>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(TimerProperties);

    /// <inheritdoc />
    /// <remarks>Returns a static factory for exact <see cref="Timer"/> instances.</remarks>
    protected override Func<Node> CreateSceneInstanceFactory()
    {
        if (GetType() != typeof(Timer))
            return base.CreateSceneInstanceFactory();

        return CreateTimerNode;
    }

    /// <inheritdoc />
    /// <remarks>Runs autostart after inherited ready handling and consumes internal process notifications.</remarks>
    protected override void OnNotification(int what)
    {
        base.OnNotification(what);

        switch (what)
        {
            case NotificationReady when _autostart:
                Start();
                _autostart = false;
                break;
            case NotificationInternalProcess:
                AdvanceTimer(physics: false);
                break;
            case NotificationInternalPhysicsProcess:
                AdvanceTimer(physics: true);
                break;
        }
    }

    /// <inheritdoc />
    /// <remarks>Clears timeout subscribers before releasing inherited node state.</remarks>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _processing = false;
            Timeout = null;
        }

        base.Dispose(disposing);
    }

    private static Node CreateTimerNode() => new Timer();

    private static void ValidateWaitTime(double value, string parameterName)
    {
        if (!double.IsFinite(value) || value <= 0d)
            throw new ArgumentOutOfRangeException(parameterName, value, "Timer wait time must be finite and greater than zero.");
    }

    private void EnsureInsideTree()
    {
        if (!IsInsideTree)
            throw new InvalidOperationException("A timer can start only while it belongs to a SceneTree.");
    }

    private void StartCore()
    {
        ApplyProcessingState(processing: true, _paused, _processCallback);
        _timeLeft = _waitTime;
        _processing = true;
    }

    private void ApplyProcessingState(bool processing, bool paused, TimerProcessCallback callback)
    {
        var enabled = processing && !paused;
        SetInternalProcessing(
            processEnabled: enabled && callback == TimerProcessCallback.Idle,
            physicsProcessEnabled: enabled && callback == TimerProcessCallback.Physics);
    }

    private void AdvanceTimer(bool physics)
    {
        if (!_processing || _paused || (_processCallback == TimerProcessCallback.Physics) != physics)
            return;

        var delta = _ignoreTimeScale
            ? Tree?.CurrentUnscaledProcessStep ?? GetUnscaledProcessDelta(physics)
            : physics ? PhysicsProcessDeltaTime : ProcessDeltaTime;
        _timeLeft -= delta;
        if (_timeLeft >= 0d)
            return;

        if (_oneShot)
            Stop();
        else
            _timeLeft += _waitTime;

        Timeout?.Invoke(this);
    }
}
