using Box2D.NET;

namespace Electron2D;

internal sealed class PhysicsTaskScheduler : IDisposable
{
    private sealed class Job
    {
        internal readonly ManualResetEvent Done = new(true);
        internal int Remaining;
        internal bool InUse;
        internal Exception? Failure;
    }
    private readonly record struct Work(b2TaskCallback Callback, object Context, int Start, int End, Job Parent);
    private Job[] _jobs = [];
    private Queue<Work>[] _queues = [];
    private AutoResetEvent[] _wake = [];
    private Thread[] _threads = [];
    private B2World? _world;
    private bool _stopped;
    private int _nextJob, _nextQueue;

    internal int WorkerCount { get; }
    internal PhysicsTaskScheduler(int count)
    {
        if (count is < 1 or > 8) throw new ArgumentOutOfRangeException(nameof(count));
        WorkerCount = count;
    }
    internal void Bind(B2World world) => _world = world;

    private void StartWorkers()
    {
        if (_threads.Length != 0) return;
        // ponytail: 64 retained handles cover the backend task graph; increase after a measured exhaustion.
        _jobs = Enumerable.Range(0, 64).Select(_ => new Job()).ToArray();
        _queues = Enumerable.Range(0, WorkerCount).Select(_ => new Queue<Work>(16)).ToArray();
        _wake = Enumerable.Range(0, WorkerCount).Select(_ => new AutoResetEvent(false)).ToArray();
        _threads = new Thread[WorkerCount];
        for (int i = 0; i < WorkerCount; ++i)
        {
            int index = i;
            _threads[i] = new Thread(() => Run(index)) { IsBackground = true, Name = "Electron2D Physics" };
            _threads[i].Start();
        }
    }

    internal object Enqueue(b2TaskCallback task, int itemCount, int minRange, object taskContext, object userContext)
    {
        if (_world?.workerCount == 1)
        {
            task(0, itemCount, 0, taskContext);
            return null!;
        }
        StartWorkers();
        var job = _jobs[_nextJob];
        int queue = _nextQueue;
        _nextJob = (_nextJob + 1) % _jobs.Length;
        _nextQueue = (_nextQueue + 1) % WorkerCount;
        if (job.InUse) throw new InvalidOperationException("Physics task storage exhausted.");
        job.InUse = true; job.Done.Reset(); job.Failure = null;
        int parts = Math.Min(WorkerCount, Math.Max(1, itemCount / minRange));
        job.Remaining = parts;
        for (int i = 0; i < parts; ++i)
        {
            int index = (queue + i) % WorkerCount;
            var work = new Work(task, taskContext, itemCount * i / parts, itemCount * (i + 1) / parts, job);
            lock (_queues[index]) _queues[index].Enqueue(work);
            _wake[index].Set();
        }
        return job;
    }

    internal void Finish(object task, object userContext)
    {
        var job = (Job)task;
        job.Done.WaitOne();
        job.InUse = false;
        if (job.Failure is { } failure) throw new InvalidOperationException("Physics worker failed.", failure);
    }

    private void Run(int index)
    {
        while (true)
        {
            _wake[index].WaitOne();
            while (true)
            {
                Work work;
                lock (_queues[index]) if (!_queues[index].TryDequeue(out work)) break;
                try { work.Callback(work.Start, work.End, (uint)index, work.Context); }
                catch (Exception ex)
                {
                    Interlocked.CompareExchange(ref work.Parent.Failure, ex, null);
                    if (work.Context is B2WorkerContext worker)
                    {
                        Interlocked.CompareExchange(ref worker.context.workerFailure, ex, null);
                        B2Atomics.b2AtomicStoreU32(ref worker.context.atomicSyncBits, uint.MaxValue);
                    }
                }
                finally { if (Interlocked.Decrement(ref work.Parent.Remaining) == 0) work.Parent.Done.Set(); }
            }
            if (Volatile.Read(ref _stopped)) return;
        }
    }

    public void Dispose()
    {
        Volatile.Write(ref _stopped, true);
        foreach (var wake in _wake) wake.Set();
        foreach (var thread in _threads) thread.Join();
        foreach (var wake in _wake) wake.Dispose();
        foreach (var job in _jobs) job.Done.Dispose();
    }
}
