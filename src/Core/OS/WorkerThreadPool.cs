namespace Electron2D;

/// <summary>Schedules typed regular and indexed group callbacks on process-owned background workers.</summary>
/// <remarks>Wait every accepted ID to release retained callback state. Callbacks must synchronize shared data and
/// publish scene mutations through SceneTree.Defer. Startup settings prepare bounded storage; successful repeated
/// submission, dispatch and waiting reuse it. Browser submissions require a threaded runtime and reject explicitly.</remarks>
public sealed class WorkerThreadPool : ElectronObject
{
    internal static WorkerThreadPool Service { get; } = new();
    private readonly Lazy<WorkerPoolRuntime> _runtime = new(CreateRuntime);
    private WorkerThreadPool() { }
    private WorkerPoolRuntime Runtime { get { ThrowIfDisposed(); return _runtime.Value; } }
    private static WorkerPoolRuntime CreateRuntime()
    {
        if (OperatingSystem.IsBrowser()) throw new PlatformNotSupportedException("Worker callbacks require a threaded browser runtime.");
        var workers = ProjectSettings.GetWithOverride(ProjectSettings.WorkerPoolMaxThreads);
        return new(workers < 0 ? Math.Max(1, Environment.ProcessorCount - 1) : workers,
            ProjectSettings.GetWithOverride(ProjectSettings.WorkerPoolLowPriorityThreadRatio), ProjectSettings.GetWithOverride(ProjectSettings.WorkerPoolMaxPendingTasks));
    }
    /// <summary>Queues one callback and returns its unique task ID.</summary>
    /// <param name="action">Callback executed once on a worker.</param><param name="highPriority">Prefers this task over queued ordinary work without preemption.</param><param name="description">Retained debugger description until the ID is retired.</param>
    /// <returns>A positive task ID requiring a completion wait.</returns>
    /// <exception cref="ArgumentNullException">Action or description is null.</exception>
    /// <exception cref="InvalidOperationException">Prepared pending capacity is exhausted.</exception>
    /// <exception cref="PlatformNotSupportedException">This runtime cannot start background workers.</exception>
    public static long AddTask(Action action, bool highPriority = false, string description = "") => Service.Runtime.AddTask(action, highPriority, description);
    /// <summary>Queues exactly one callback attempt for each index from zero to elements minus one.</summary>
    /// <param name="action">Indexed callback. Failed indices do not prevent remaining work.</param><param name="elements">Nonnegative element count; zero creates an already completed group.</param><param name="tasksNeeded">Any negative value uses all workers; positive values cap runners. Zero requires an empty group.</param><param name="highPriority">Prefers queued runners over ordinary work without preemption.</param><param name="description">Retained debugger description.</param>
    /// <returns>A positive group ID requiring a completion wait.</returns>
    /// <exception cref="ArgumentNullException">Action or description is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Elements is negative, or a nonempty group has zero runners.</exception>
    /// <exception cref="InvalidOperationException">Prepared pending capacity is exhausted.</exception>
    /// <exception cref="PlatformNotSupportedException">This runtime cannot start background workers.</exception>
    public static long AddGroupTask(Action<int> action, int elements, int tasksNeeded = -1, bool highPriority = false, string description = "") => Service.Runtime.AddGroup(action, elements, tasksNeeded, highPriority, description);
    /// <summary>Returns the current regular callback's ID, or -1 outside a regular pool callback.</summary>
    /// <returns>The regular task ID; group callbacks and other threads receive -1.</returns>
    public static long GetCallerTaskID() => Service._runtime.IsValueCreated ? Service.Runtime.CallerTaskID : -1;
    /// <summary>Returns the current indexed callback's group ID, or -1 outside a group pool callback.</summary>
    /// <returns>The group ID; regular callbacks and other threads receive -1.</returns>
    public static long GetCallerGroupID() => Service._runtime.IsValueCreated ? Service.Runtime.CallerGroupID : -1;
    /// <summary>Returns the number of group callback attempts that have returned or thrown.</summary>
    /// <param name="groupID">Pending group ID.</param><returns>A count from zero through elements.</returns>
    /// <exception cref="ArgumentException">The ID is invalid, retired, or identifies a regular task.</exception>
    public static int GetGroupProcessedElementCount(long groupID) => Service.Runtime.Processed(groupID);
    /// <summary>Reports whether a regular callback has finished; querying does not retire the task.</summary>
    /// <param name="taskID">Pending regular task ID.</param><returns>True after callback completion and context restoration.</returns>
    /// <exception cref="ArgumentException">The ID is invalid, retired, or identifies a group.</exception>
    public static bool IsTaskCompleted(long taskID) => Service.Runtime.IsCompleted(taskID, false);
    /// <summary>Reports whether every group runner has finished; querying does not retire the group.</summary>
    /// <param name="groupID">Pending group ID.</param><returns>True after every callback attempt and runner completes.</returns>
    /// <exception cref="ArgumentException">The ID is invalid, retired, or identifies a regular task.</exception>
    public static bool IsGroupTaskCompleted(long groupID) => Service.Runtime.IsCompleted(groupID, true);
    /// <summary>Waits for a regular callback, retires its ID after the final registered waiter, and rethrows callback failure.</summary>
    /// <param name="taskID">Pending regular task ID.</param>
    /// <remarks>Outside callers block; workers help newer queued work. Completed older tasks remain waitable.</remarks>
    /// <exception cref="ArgumentException">The ID is invalid, retired, or identifies a group.</exception>
    /// <exception cref="InvalidOperationException">A worker attempts to wait for unfinished self or older work.</exception>
    public static void WaitForTaskCompletion(long taskID) => Service.Runtime.Wait(taskID, false);
    /// <summary>Waits for all indexed attempts, retires the group after the final waiter, and rethrows its lowest-index failure.</summary>
    /// <param name="groupID">Pending group ID.</param>
    /// <remarks>Waiting does not cancel remaining indices after failure. Worker waits help newer queued work.</remarks>
    /// <exception cref="ArgumentException">The ID is invalid, retired, or identifies a regular task.</exception>
    /// <exception cref="InvalidOperationException">A worker attempts to wait for unfinished self or older work.</exception>
    public static void WaitForGroupTaskCompletion(long groupID) => Service.Runtime.Wait(groupID, true);
    /// <summary>Rejects disposal of the process-owned retained service.</summary>
    /// <exception cref="InvalidOperationException">The permanent worker service cannot be disposed by a caller.</exception>
    protected override void ValidateDisposal() => throw new InvalidOperationException("The process-owned worker service cannot be disposed.");
}
