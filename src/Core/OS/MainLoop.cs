using System.Runtime.ExceptionServices;

namespace Electron2D;

/// <summary>Defines the host-driven lifecycle and frame callbacks for an Electron2D application.</summary>
/// <remarks>
/// The creating thread is the owner thread. A host initializes the loop once, supplies non-negative finite frame
/// deltas, and finalizes it once. Returning <see langword="true"/> from a frame callback asks the host to stop the
/// application. The class does not create a thread, clock, window, renderer, input pump, or physics scheduler.
/// </remarks>
public abstract class MainLoop : ElectronObject
{
    /// <summary>Identifies an operating-system low-memory warning.</summary>
    public const int NotificationOsMemoryWarning = 2009;

    /// <summary>Identifies a notification that translated messages may have changed.</summary>
    public const int NotificationTranslationChanged = 2010;

    /// <summary>Identifies an operating-system request to show application information.</summary>
    public const int NotificationWmAbout = 2011;

    /// <summary>Identifies a notification delivered immediately before an unrecoverable crash.</summary>
    /// <remarks>Time-consuming work and allocation should be avoided while handling this notification.</remarks>
    public const int NotificationCrash = 2012;

    /// <summary>Identifies an input-method composition update supplied by the operating system.</summary>
    public const int NotificationOsImeUpdate = 2013;

    /// <summary>Identifies that the application resumed after suspension.</summary>
    public const int NotificationApplicationResumed = 2014;

    /// <summary>Identifies that the application is about to be suspended.</summary>
    public const int NotificationApplicationPaused = 2015;

    /// <summary>Identifies that the application received keyboard focus.</summary>
    public const int NotificationApplicationFocusIn = 2016;

    /// <summary>Identifies that the application lost keyboard focus.</summary>
    public const int NotificationApplicationFocusOut = 2017;

    /// <summary>Identifies that the active text service changed.</summary>
    public const int NotificationTextServerChanged = 2018;

    /// <summary>Identifies that the application entered picture-in-picture mode.</summary>
    public const int NotificationApplicationPipModeEntered = 2019;

    /// <summary>Identifies that the application exited picture-in-picture mode.</summary>
    public const int NotificationApplicationPipModeExited = 2020;

    private readonly int _ownerThreadId = Environment.CurrentManagedThreadId;
    private LoopState _state;

    /// <summary>Occurs when a previously requested operating-system permission receives a result.</summary>
    /// <remarks>
    /// The first argument is this loop, the second is the platform permission name, and the third indicates whether
    /// it was granted. Delivery is synchronous on the owner thread. A throwing subscriber stops later subscribers and
    /// propagates its exception to the platform integration that published the result.
    /// </remarks>
    public event Action<MainLoop, string, bool>? OnRequestPermissionsResult;

    /// <summary>Initializes this loop and invokes <see cref="OnInitialize"/> exactly once.</summary>
    /// <remarks>
    /// Successful return moves the loop to its running state. If the callback fails, initialization is terminal and
    /// the callback is not retried; an override is responsible for rolling back resources acquired before it throws.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread or the loop is not awaiting initialization.</exception>
    /// <exception cref="ObjectDisposedException">Disposal has started or finished.</exception>
    /// <exception cref="Exception"><see cref="OnInitialize"/> throws.</exception>
    public void Initialize()
    {
        ThrowIfDisposed();
        EnsureOwnerThread();

        if (_state != LoopState.Created)
            throw new InvalidOperationException("A MainLoop can be initialized exactly once.");

        _state = LoopState.Initializing;

        try
        {
            OnInitialize();
            _state = LoopState.Running;
        }
        catch
        {
            _state = LoopState.InitializationFailed;
            throw;
        }
    }

    /// <summary>Runs one variable-step frame.</summary>
    /// <param name="delta">Elapsed frame time in seconds. The value must be finite and non-negative.</param>
    /// <returns><see langword="true"/> when the host should stop the application; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delta"/> is negative, NaN, or infinite.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread, the loop is not running, or frame execution is re-entered.</exception>
    /// <exception cref="ObjectDisposedException">Disposal has started or finished.</exception>
    /// <exception cref="Exception"><see cref="OnProcess"/> throws.</exception>
    public bool Process(double delta) => RunFrame(delta, delta, physics: false);

    /// <summary>Runs one fixed-step physics frame.</summary>
    /// <param name="delta">Elapsed fixed-step time in seconds. The value must be finite and non-negative.</param>
    /// <returns><see langword="true"/> when the host should stop the application; otherwise <see langword="false"/>.</returns>
    /// <remarks>This callback defines scheduling only; it does not perform collision or rigid-body simulation.</remarks>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="delta"/> is negative, NaN, or infinite.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread, the loop is not running, or frame execution is re-entered.</exception>
    /// <exception cref="ObjectDisposedException">Disposal has started or finished.</exception>
    /// <exception cref="Exception"><see cref="OnPhysicsProcess"/> throws.</exception>
    public bool PhysicsProcess(double delta) => RunFrame(delta, delta, physics: true);

    internal double CurrentUnscaledFrameDelta { get; private set; }

    internal bool ProcessForEngine(double delta, double unscaledDelta) =>
        RunFrame(delta, unscaledDelta, physics: false);

    internal bool PhysicsProcessForEngine(double delta, double unscaledDelta) =>
        RunFrame(delta, unscaledDelta, physics: true);

    internal virtual void ValidateInputEventDispatch()
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        if (_state != LoopState.Running)
            throw new InvalidOperationException("Input can only be dispatched while the MainLoop is running outside another lifecycle or frame callback.");
    }

    internal virtual void DispatchInputEvent(InputEvent @event)
    {
    }

    /// <summary>Finalizes a successfully initialized loop and invokes <see cref="OnFinalize"/> exactly once.</summary>
    /// <remarks>
    /// The loop becomes terminal even if the callback throws. Calling <see cref="ElectronObject.Dispose()"/> on a running loop performs
    /// this finalization automatically; disposing an uninitialized or failed loop does not invoke the callback.
    /// </remarks>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread, the loop is not running, or finalization is rejected by a derived invariant.</exception>
    /// <exception cref="ObjectDisposedException">Disposal has started or finished.</exception>
    /// <exception cref="Exception"><see cref="OnFinalize"/> throws.</exception>
    public void FinalizeLoop()
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        FinalizeCore(validate: true);
    }

    /// <summary>Performs loop-specific initialization.</summary>
    /// <remarks>
    /// The callback runs once on the owner thread before any frame callback. An override that throws must release any
    /// resources it acquired because <see cref="OnFinalize"/> is not called after failed initialization.
    /// </remarks>
    protected virtual void OnInitialize()
    {
    }

    /// <summary>Performs one variable-step frame.</summary>
    /// <param name="delta">Elapsed frame time in seconds.</param>
    /// <returns><see langword="true"/> to ask the host to stop; otherwise <see langword="false"/>.</returns>
    /// <remarks>The default implementation does no work and returns <see langword="false"/>.</remarks>
    protected virtual bool OnProcess(double delta) => false;

    /// <summary>Performs one fixed-step physics frame.</summary>
    /// <param name="delta">Elapsed fixed-step time in seconds.</param>
    /// <returns><see langword="true"/> to ask the host to stop; otherwise <see langword="false"/>.</returns>
    /// <remarks>The default implementation does no work and returns <see langword="false"/>.</remarks>
    protected virtual bool OnPhysicsProcess(double delta) => false;

    /// <summary>Releases resources acquired by a successfully initialized loop.</summary>
    /// <remarks>The callback runs once on the owner thread after the last frame and before object disposal completes.</remarks>
    protected virtual void OnFinalize()
    {
    }

    /// <summary>Validates derived finalization preconditions before the loop enters its terminal state.</summary>
    /// <remarks>Overrides must be side-effect-free, throw when finalization is temporarily unsafe, and call the base implementation.</remarks>
    /// <exception cref="InvalidOperationException">A derived lifecycle invariant currently prevents finalization.</exception>
    protected virtual void ValidateFinalization()
    {
    }

    /// <summary>Synchronously publishes an operating-system permission result.</summary>
    /// <param name="permission">The non-null platform permission name.</param>
    /// <param name="granted"><see langword="true"/> when permission was granted.</param>
    /// <exception cref="ArgumentNullException"><paramref name="permission"/> is <see langword="null"/>.</exception>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread or the loop is not running.</exception>
    /// <exception cref="ObjectDisposedException">Disposal has started or finished.</exception>
    /// <exception cref="Exception">An event handler throws.</exception>
    protected void NotifyRequestPermissionsResult(string permission, bool granted)
    {
        ThrowIfDisposed();
        EnsureOwnerThread();
        ArgumentNullException.ThrowIfNull(permission);

        if (_state is not (LoopState.Running or LoopState.Processing))
            throw new InvalidOperationException("Permission results can only be published while the MainLoop is running.");

        OnRequestPermissionsResult?.Invoke(this, permission, granted);
    }

    /// <summary>Publishes terminal state for a derived instance whose construction cannot complete.</summary>
    /// <remarks>
    /// Derived constructor rollback must release its acquired resources before calling this helper. Lifecycle callbacks
    /// and public disposal events are skipped because ownership was never transferred to a constructed instance.
    /// </remarks>
    private protected void CompleteFailedLoopConstruction()
    {
        _state = LoopState.Finalized;
        OnRequestPermissionsResult = null;
        CompleteFailedConstruction();
    }

    internal void StartForEngine()
    {
        ThrowIfDisposed();
        EnsureOwnerThread();

        if (_state == LoopState.Created)
        {
            Initialize();
            return;
        }

        if (_state != LoopState.Running)
            throw new InvalidOperationException("The MainLoop cannot be attached to Engine in its current lifecycle state.");
    }

    /// <inheritdoc />
    /// <remarks>Requires the owner thread and rejects disposal from initialization, frame, or finalization callbacks.</remarks>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread or a lifecycle callback is executing.</exception>
    protected override void ValidateDisposal()
    {
        EnsureOwnerThread();

        if (_state is LoopState.Initializing or LoopState.Processing or LoopState.Finalizing)
            throw new InvalidOperationException("A MainLoop cannot be disposed from one of its lifecycle or frame callbacks.");

        base.ValidateDisposal();
    }

    /// <inheritdoc />
    /// <remarks>Finalizes a running loop, clears loop event subscribers, and then releases inherited state.</remarks>
    protected override void Dispose(bool disposing)
    {
        Exception? finalizeError = null;
        Exception? baseError = null;

        if (disposing)
        {
            if (_state == LoopState.Running)
            {
                try
                {
                    FinalizeCore(validate: false);
                }
                catch (Exception error)
                {
                    finalizeError = error;
                }
            }
            else
            {
                _state = LoopState.Finalized;
                OnRequestPermissionsResult = null;
            }
        }

        try
        {
            base.Dispose(disposing);
        }
        catch (Exception error)
        {
            baseError = error;
        }

        if (finalizeError is not null && baseError is not null)
            throw new AggregateException(finalizeError, baseError);

        if (finalizeError is not null)
            ExceptionDispatchInfo.Capture(finalizeError).Throw();

        if (baseError is not null)
            ExceptionDispatchInfo.Capture(baseError).Throw();
    }

    private bool RunFrame(double delta, double unscaledDelta, bool physics)
    {
        ThrowIfDisposed();
        EnsureOwnerThread();

        if (!double.IsFinite(delta) || delta < 0d)
            throw new ArgumentOutOfRangeException(nameof(delta), delta, "Frame delta must be finite and non-negative.");
        if (!double.IsFinite(unscaledDelta) || unscaledDelta < 0d)
            throw new ArgumentOutOfRangeException(nameof(unscaledDelta), unscaledDelta, "Unscaled frame delta must be finite and non-negative.");

        if (_state != LoopState.Running)
            throw new InvalidOperationException("A MainLoop frame can only run after initialization and before finalization.");

        _state = LoopState.Processing;
        CurrentUnscaledFrameDelta = unscaledDelta;
        Input.Instance.BeginFrame(physics);

        try
        {
            return physics ? OnPhysicsProcess(delta) : OnProcess(delta);
        }
        finally
        {
            Input.Instance.CompleteFrame(physics);
            CurrentUnscaledFrameDelta = 0d;
            _state = LoopState.Running;
        }
    }

    private void FinalizeCore(bool validate)
    {
        if (_state != LoopState.Running)
            throw new InvalidOperationException("A MainLoop can only be finalized once after successful initialization.");

        if (validate)
            ValidateFinalization();

        _state = LoopState.Finalizing;

        try
        {
            OnFinalize();
        }
        finally
        {
            OnRequestPermissionsResult = null;
            _state = LoopState.Finalized;
        }
    }

    private void EnsureOwnerThread()
    {
        if (Environment.CurrentManagedThreadId != _ownerThreadId)
            throw new InvalidOperationException("MainLoop lifecycle and frame execution must run on its owner thread.");
    }

    private enum LoopState
    {
        Created,
        Initializing,
        Running,
        Processing,
        Finalizing,
        Finalized,
        InitializationFailed
    }
}
