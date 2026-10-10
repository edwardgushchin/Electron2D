using System.Diagnostics;
using System.Runtime.ExceptionServices;

namespace Electron2D;

internal sealed class WorkerPoolRuntime : IDisposable
{
    [DebuggerDisplay("{ID}: {Description}")]
    private sealed class Job
    {
        internal long ID, NextIndex;
        internal Action? Action;
        internal Action<int>? GroupAction;
        internal string Description = "";
        internal bool Group, HighPriority, Completed;
        internal int Elements, Processed, Runners, Waiters, FailureIndex;
        internal ExceptionDispatchInfo? Failure;
    }
    private sealed class Worker(WorkerPoolRuntime pool)
    {
        internal readonly WorkerPoolRuntime Pool = pool;
        internal System.Threading.Thread Thread = null!;
        internal Job? Current;
        internal long WaitFloor = -1;
        internal int LowDepth;
        internal long AllocatedBytes, AllocationStart;
        internal void Run()
        {
            CurrentWorker = this;
            AllocationStart = GC.GetAllocatedBytesForCurrentThread();
            try
            {
                while (true)
                {
                    Job? job;
                    lock (Pool._gate)
                    {
                        while ((job = Pool.Take(this)) is null)
                        {
                            if (Pool._stopping && Pool._high.Count == 0 && Pool._low.Count == 0) return;
                            System.Threading.Monitor.Wait(Pool._gate);
                        }
                    }
                    Pool.Execute(this, job);
                }
            }
            finally { CurrentWorker = null; }
        }
    }
    [ThreadStatic] private static Worker? CurrentWorker;
    private readonly object _gate = new();
    private readonly Dictionary<long, Job> _jobs;
    private readonly Stack<Job> _free;
    private readonly Queue<Job> _high, _low;
    private readonly Worker[] _workers;
    private readonly int _maxLow;
    private int _activeLow;
    private long _lastID;
    private bool _stopping;

    internal WorkerPoolRuntime(int workers, float lowRatio, int capacity)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(workers, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(capacity, 1);
        if (!float.IsFinite(lowRatio) || lowRatio < 0 || lowRatio > 1) throw new ArgumentOutOfRangeException(nameof(lowRatio));
        var tickets = checked(workers * capacity);
        _jobs = new(capacity); _free = new(capacity); _high = new(tickets); _low = new(tickets);
        for (var i = 0; i < capacity; i++) _free.Push(new());
        _maxLow = Math.Clamp((int)(workers * lowRatio), 1, Math.Max(1, workers - 1)); _workers = new Worker[workers];
        var started = 0;
        try
        {
            using var flow = System.Threading.ExecutionContext.IsFlowSuppressed() ? default : System.Threading.ExecutionContext.SuppressFlow();
            for (var i = 0; i < workers; i++)
            {
                var worker = new Worker(this);
                worker.Thread = new(worker.Run) { IsBackground = true, Name = "Electron2D worker " + i };
                _workers[i] = worker; worker.Thread.Start(); started++;
            }
        }
        catch
        {
            lock (_gate) { _stopping = true; System.Threading.Monitor.PulseAll(_gate); }
            for (var i = 0; i < started; i++) _workers[i].Thread.Join();
            throw;
        }
    }
    internal long CallerTaskID => CurrentWorker is { } w && w.Pool == this && w.Current is { Group: false } job ? job.ID : -1;
    internal long CallerGroupID => CurrentWorker is { } w && w.Pool == this && w.Current is { Group: true } job ? job.ID : -1;
    internal long WorkerAllocatedBytes { get { long result = 0; foreach (var w in _workers) result += Interlocked.Read(ref w.AllocatedBytes); return result; } }
    internal int PendingCount { get { lock (_gate) return _jobs.Count; } }
    internal int WaitingCount(long id, bool group) { lock (_gate) return Find(id, group).Waiters; }
    internal long AddTask(Action action, bool highPriority = false, string description = "") => Add(action, null, 0, 1, highPriority, description);
    internal long AddGroup(Action<int> action, int elements, int tasksNeeded = -1, bool highPriority = false, string description = "")
    {
        ArgumentNullException.ThrowIfNull(action);
        ArgumentOutOfRangeException.ThrowIfNegative(elements);
        if (elements != 0 && tasksNeeded == 0) throw new ArgumentOutOfRangeException(nameof(tasksNeeded));
        return Add(null, action, elements, elements == 0 ? 0 : Math.Min(elements, tasksNeeded < 0 ? _workers.Length : Math.Min(tasksNeeded, _workers.Length)), highPriority, description);
    }
    private long Add(Action? action, Action<int>? groupAction, int elements, int runners, bool high, string description)
    {
        if (action is null && groupAction is null) throw new ArgumentNullException(nameof(action));
        ArgumentNullException.ThrowIfNull(description);
        lock (_gate)
        {
            ObjectDisposedException.ThrowIf(_stopping, this);
            if (_free.Count == 0) throw new InvalidOperationException("Worker pool pending capacity is exhausted. Wait for accepted jobs before submitting more.");
            var id = checked(_lastID + 1);
            var job = _free.Pop(); job.ID = id; job.Action = action; job.GroupAction = groupAction; job.Group = groupAction is not null;
            job.Description = description; job.HighPriority = high; job.Elements = elements; job.Runners = runners; job.Completed = runners == 0; job.FailureIndex = int.MaxValue;
            _jobs.Add(id, job); _lastID = id;
            var queue = high ? _high : _low;
            for (var i = 0; i < runners; i++) queue.Enqueue(job);
            System.Threading.Monitor.PulseAll(_gate);
            return id;
        }
    }
    private Job Find(long id, bool group) => _jobs.TryGetValue(id, out var job) && job.Group == group ? job : throw new ArgumentException("The ID does not identify a pending job of the requested kind.", nameof(id));
    internal bool IsCompleted(long id, bool group) { lock (_gate) return Find(id, group).Completed; }
    internal int Processed(long id) { lock (_gate) return Find(id, true).Processed; }
    private Job? Take(Worker worker)
    {
        var job = TakeEligible(_high, worker.WaitFloor);
        if (job is not null) return job;
        if (worker.LowDepth == 0 && _activeLow >= _maxLow) return null;
        job = TakeEligible(_low, worker.WaitFloor);
        if (job is not null && worker.LowDepth++ == 0) _activeLow++;
        return job;
    }
    private static Job? TakeEligible(Queue<Job> queue, long floor)
    {
        if (queue.TryPeek(out var head) && head.ID > floor) return queue.Dequeue();
        Job? selected = null;
        for (var count = queue.Count; count > 0; count--)
        {
            var job = queue.Dequeue();
            if (selected is null && job.ID > floor) selected = job;
            else queue.Enqueue(job);
        }
        return selected;
    }
    private void Execute(Worker worker, Job job)
    {
        var previous = worker.Current; worker.Current = job;
        if (job.Group)
        {
            while (true)
            {
                var index = Interlocked.Increment(ref job.NextIndex) - 1;
                if (index >= job.Elements) break;
                Invoke(job, (int)index);
                lock (_gate) job.Processed++;
            }
        }
        else Invoke(job, 0);
        worker.Current = previous;
        lock (_gate)
        {
            if (!job.HighPriority && --worker.LowDepth == 0) _activeLow--;
            if (previous is null)
            {
                var allocated = GC.GetAllocatedBytesForCurrentThread();
                Interlocked.Add(ref worker.AllocatedBytes, allocated - worker.AllocationStart); worker.AllocationStart = allocated;
            }
            if (--job.Runners == 0) job.Completed = true;
            System.Threading.Monitor.PulseAll(_gate);
        }
    }
    private void Invoke(Job job, int index)
    {
        try { if (job.Group) job.GroupAction!(index); else job.Action!(); }
        catch (Exception error)
        {
            lock (_gate) if (index <= job.FailureIndex) { job.FailureIndex = index; job.Failure = ExceptionDispatchInfo.Capture(error); }
        }
    }
    internal void Wait(long id, bool group)
    {
        Job job; var worker = CurrentWorker is { } w && w.Pool == this ? w : null;
        var oldFloor = worker?.WaitFloor ?? -1;
        lock (_gate)
        {
            job = Find(id, group);
            if (worker?.Current is { } current && !job.Completed && id <= Math.Max(current.ID, oldFloor)) throw new InvalidOperationException("A worker cannot wait for unfinished self or older work.");
            job.Waiters++;
            if (worker is not null) worker.WaitFloor = Math.Max(oldFloor, worker.Current!.ID);
        }
        ExceptionDispatchInfo? failure;
        try
        {
            while (true)
            {
                Job? help = null;
                lock (_gate)
                {
                    if (job.Completed) { failure = job.Failure; break; }
                    if (worker is not null) help = Take(worker);
                    if (help is null) { System.Threading.Monitor.Wait(_gate); continue; }
                }
                Execute(worker!, help);
            }
        }
        finally
        {
            if (worker is not null) worker.WaitFloor = oldFloor;
            lock (_gate)
            {
                if (--job.Waiters == 0 && job.Completed)
                {
                    _jobs.Remove(id); job.Action = null; job.GroupAction = null; job.Description = ""; job.Failure = null;
                    job.NextIndex = 0; job.Processed = 0; _free.Push(job);
                }
            }
        }
        failure?.Throw();
    }
    public void Dispose()
    {
        if (CurrentWorker?.Pool == this) throw new InvalidOperationException("A worker cannot dispose its own pool.");
        lock (_gate) { _stopping = true; System.Threading.Monitor.PulseAll(_gate); }
        foreach (var worker in _workers) worker.Thread.Join();
        lock (_gate) { _jobs.Clear(); _free.Clear(); }
    }
}
