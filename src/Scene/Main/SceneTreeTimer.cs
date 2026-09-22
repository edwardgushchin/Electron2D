using System.Runtime.ExceptionServices;

namespace Electron2D;

/// <summary>Provides a lightweight one-shot timer processed by a <see cref="SceneTree"/>.</summary>
/// <remarks>
/// The owning tree updates the timer after node callbacks in the selected frame lane. The timer is automatically
/// disposed after timeout delivery. Keeping a managed reference does not extend its lifetime. Time advances only from
/// delivered frame deltas; <see cref="Engine"/> scales those deltas when it drives the tree. The timer has no internal
/// clock or independent time-scale bypass.
/// </remarks>
public sealed class SceneTreeTimer : ElectronObject
{
    private static readonly IReadOnlyList<PropertyDescriptor> TimerProperties = Array.AsReadOnly<PropertyDescriptor>(
    [
        new PropertyDescriptor<SceneTreeTimer, double>(
            nameof(TimeLeft),
            timer => timer.TimeLeft,
            (timer, value) => timer.TimeLeft = value,
            _ => 0d,
            (_, value) => double.IsFinite(value) && value >= 0d),
        new PropertyDescriptor<SceneTreeTimer, bool>(nameof(ProcessAlways), timer => timer.ProcessAlways),
        new PropertyDescriptor<SceneTreeTimer, bool>(nameof(ProcessInPhysics), timer => timer.ProcessInPhysics)
    ]);

    private SceneTree? _tree;
    private double _timeLeft;

    internal SceneTreeTimer(SceneTree tree, double timeLeft, bool processAlways, bool processInPhysics)
    {
        _tree = tree;
        _timeLeft = timeLeft;
        ProcessAlways = processAlways;
        ProcessInPhysics = processInPhysics;
    }

    /// <summary>Gets or sets the remaining delay in seconds.</summary>
    /// <value>A finite non-negative duration. It reaches zero before <see cref="Timeout"/> is raised.</value>
    /// <remarks>Changing the value does not change the timer's selected frame lane or pause policy. Zero expires on the next matching frame.</remarks>
    /// <exception cref="ArgumentOutOfRangeException">The assigned value is negative, NaN, or infinite.</exception>
    /// <exception cref="InvalidOperationException">An attached timer is mutated off its tree's owner thread.</exception>
    /// <exception cref="ObjectDisposedException">The timer is disposing on another thread or has finished disposing.</exception>
    public double TimeLeft
    {
        get
        {
            ThrowIfDisposed();
            return _timeLeft;
        }
        set
        {
            ThrowIfDisposed();
            _tree?.EnsureOwnerThread();
            ValidateTime(value, nameof(value));
            _timeLeft = value;
        }
    }

    /// <summary>Gets whether this timer continues while its tree is paused.</summary>
    /// <value><see langword="true"/> to ignore tree pause; otherwise <see langword="false"/>.</value>
    internal bool ProcessAlways { get; }

    /// <summary>Gets whether this timer advances in physics frames instead of process frames.</summary>
    /// <value><see langword="true"/> for physics frames; <see langword="false"/> for process frames.</value>
    internal bool ProcessInPhysics { get; }

    /// <summary>Occurs once when the remaining delay reaches zero.</summary>
    /// <remarks>
    /// Delivery is synchronous on the tree owner thread after node callbacks and before deferred work. The timer is
    /// disposed after the event invocation returns or throws. A handler failure propagates through the owning frame as part of an
    /// <see cref="AggregateException"/>.
    /// </remarks>
    public event Action<SceneTreeTimer>? Timeout;

    /// <inheritdoc />
    /// <remarks>Appends this class's remaining-time and processing-policy descriptors.</remarks>
    protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() =>
        base.GetPropertyDescriptors().Concat(TimerProperties);

    /// <inheritdoc />
    /// <remarks>Requires the owner thread while this timer remains owned by a scene tree.</remarks>
    /// <exception cref="InvalidOperationException">The caller is not the owner thread.</exception>
    protected override void ValidateDisposal()
    {
        _tree?.EnsureOwnerThread();
        base.ValidateDisposal();
    }

    /// <inheritdoc />
    /// <remarks>Removes the timer from its tree, clears timeout subscribers, and calls the base implementation.</remarks>
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            var tree = _tree;
            _tree = null;
            tree?.RemoveTimer(this);
            Timeout = null;
        }

        base.Dispose(disposing);
    }

    internal bool Advance(double delta, bool paused)
    {
        if (IsDisposed || !ProcessAlways && paused)
            return false;

        _timeLeft = Math.Max(0d, _timeLeft - delta);
        return _timeLeft == 0d;
    }

    internal void Expire()
    {
        Exception? timeoutError = null;
        Exception? disposalError = null;

        try
        {
            Timeout?.Invoke(this);
        }
        catch (Exception error)
        {
            timeoutError = error;
        }

        try
        {
            Dispose();
        }
        catch (Exception error)
        {
            disposalError = error;
        }

        if (timeoutError is not null && disposalError is not null)
            throw new AggregateException(timeoutError, disposalError);

        if (timeoutError is not null)
            ExceptionDispatchInfo.Capture(timeoutError).Throw();

        if (disposalError is not null)
            ExceptionDispatchInfo.Capture(disposalError).Throw();
    }

    internal static void ValidateTime(double time, string parameterName)
    {
        if (!double.IsFinite(time) || time < 0d)
            throw new ArgumentOutOfRangeException(parameterName, time, "Timer duration must be finite and non-negative.");
    }
}
