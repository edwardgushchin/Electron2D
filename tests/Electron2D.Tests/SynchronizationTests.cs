using Electron2D;
using Mutex = Electron2D.Mutex;
using Semaphore = Electron2D.Semaphore;

internal static partial class SynchronizationTests
{
    internal static void Run()
    {
        MutexContract(); SemaphoreContract(); InterruptedWaits(); ContendedWrites(); WarmOperations(); WorkerPipeline();
        Console.WriteLine("Mutex recursion, ownership, blocking, semaphore counts, interruption, disposal and actual worker pipeline passed.");
    }
    private static void MutexContract()
    {
        using var mutex = new Mutex(); using var entered = new ManualResetEventSlim(); using var acquired = new ManualResetEventSlim();
        Exception? failure = null;
        mutex.Lock(); Check(mutex.TryLock(), "Owner recursively acquires without blocking."); var depth = 2;
        var worker = new Thread(() =>
        {
            try { Check(!mutex.TryLock(), "Other thread's TryLock fails immediately."); Reject<SynchronizationLockException>(mutex.Unlock); entered.Set(); mutex.Lock(); try { acquired.Set(); } finally { mutex.Unlock(); } }
            catch (Exception error) { failure = error; }
        })
        { IsBackground = true };
        worker.Start();
        try
        {
            Check(entered.Wait(5000), "Mutex contender starts."); Blocked(worker);
            Reject<InvalidOperationException>(mutex.Dispose); Check(!mutex.IsDisposed, "Held mutex disposal rejects before lifetime mutation.");
            mutex.Unlock(); depth--; Check(!acquired.IsSet, "One release preserves recursive ownership.");
        }
        finally { while (depth-- > 0) mutex.Unlock(); Join(worker); }
        if (failure is not null) throw failure;
        Check(acquired.IsSet, "Balanced release permits the blocked contender."); Reject<SynchronizationLockException>(mutex.Unlock);
        mutex.Dispose(); mutex.Dispose(); Reject<ObjectDisposedException>(mutex.Lock); Reject<ObjectDisposedException>(() => mutex.TryLock()); Reject<ObjectDisposedException>(mutex.Unlock);
    }
    private static void SemaphoreContract()
    {
        using var semaphore = new Semaphore();
        Check(!semaphore.TryWait(), "Initial count zero."); Reject<ArgumentOutOfRangeException>(() => semaphore.Post(0)); Reject<ArgumentOutOfRangeException>(() => semaphore.Post(-1));
        semaphore.Post(3); semaphore.Wait(); Check(semaphore.TryWait() && semaphore.TryWait() && !semaphore.TryWait(), "Counted posts each consumed exactly once.");
        using var entered = new CountdownEvent(3); var completed = 0; var failures = new Exception?[3];
        var workers = Enumerable.Range(0, 3).Select(index => new Thread(() =>
        { try { entered.Signal(); semaphore.Wait(); Interlocked.Increment(ref completed); } catch (Exception error) { failures[index] = error; } })
        { IsBackground = true }).ToArray();
        foreach (var worker in workers) worker.Start();
        try
        {
            Check(entered.Wait(5000), "Three semaphore contenders start."); foreach (var worker in workers) Blocked(worker);
            Reject<InvalidOperationException>(semaphore.Dispose); Check(!semaphore.IsDisposed, "Waiting semaphore disposal rejects without losing waits.");
            semaphore.Post(2); Check(SpinWait.SpinUntil(() => Volatile.Read(ref completed) == 2, 5000), "Two tokens wake exactly two waiters.");
            Check(!semaphore.TryWait(), "No phantom token remains."); semaphore.Post();
        }
        finally { semaphore.Post(3); foreach (var worker in workers) Join(worker); }
        Check(completed == 3 && failures.All(error => error is null), "All waiters finish once.");
        using var large = new Semaphore(); large.Post(int.MaxValue); large.Post(int.MaxValue); large.Post();
        Reject<InvalidOperationException>(() => large.Post()); Check(large.TryWait(), "Overflow preserves available count."); large.Post(); Reject<InvalidOperationException>(() => large.Post());
        semaphore.Dispose(); semaphore.Dispose(); Reject<ObjectDisposedException>(() => semaphore.Post()); Reject<ObjectDisposedException>(semaphore.Wait); Reject<ObjectDisposedException>(() => semaphore.TryWait());
    }
    private static void InterruptedWaits()
    {
        using var mutex = new Mutex(); using var semaphore = new Semaphore();
        var interrupted = 0;
        mutex.Lock();
        var lockWaiter = new Thread(() => { try { mutex.Lock(); mutex.Unlock(); } catch (ThreadInterruptedException) { Interlocked.Increment(ref interrupted); } }) { IsBackground = true };
        lockWaiter.Start();
        try { Blocked(lockWaiter); lockWaiter.Interrupt(); Join(lockWaiter); }
        finally { mutex.Unlock(); }
        var signalWaiter = new Thread(() => { try { semaphore.Wait(); } catch (ThreadInterruptedException) { Interlocked.Increment(ref interrupted); } }) { IsBackground = true };
        signalWaiter.Start(); Blocked(signalWaiter); signalWaiter.Interrupt(); Join(signalWaiter);
        Check(interrupted == 2, "Interrupted waits release their lifecycle bookkeeping.");
        semaphore.Post(); Check(semaphore.TryWait(), "Interrupted semaphore wait consumes no future token.");
        mutex.Dispose(); semaphore.Dispose();
    }
    private static void ContendedWrites()
    {
        using var mutex = new Mutex(); var value = 0; var failures = new Exception?[4];
        var workers = Enumerable.Range(0, 4).Select(index => new Thread(() =>
        {
            try { for (var i = 0; i < 2000; i++) { mutex.Lock(); try { var previous = value; Thread.SpinWait(8); value = previous + 1; } finally { mutex.Unlock(); } } }
            catch (Exception error) { failures[index] = error; }
        })
        { IsBackground = true }).ToArray();
        foreach (var worker in workers) worker.Start(); foreach (var worker in workers) Join(worker);
        Check(value == 8000 && failures.All(error => error is null), "Four concurrent writers preserve every critical-section update.");
    }
    private static void WarmOperations()
    {
        using var mutex = new Mutex(); using var semaphore = new Semaphore();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 128; i++) Warm(mutex, semaphore);
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) Warm(mutex, semaphore);
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(bytes == 0, "1024 warm recursive lock and signal cycles allocate zero owner bytes: " + bytes);
    }
    private static void Warm(Mutex mutex, Semaphore semaphore)
    {
        mutex.Lock(); if (!mutex.TryLock()) throw new InvalidOperationException(); mutex.Unlock(); mutex.Unlock();
        semaphore.Post(2); semaphore.Wait(); if (!semaphore.TryWait() || semaphore.TryWait()) throw new InvalidOperationException();
    }
    private sealed class Pipeline : IDisposable
    {
        internal readonly Mutex Gate = new(); internal readonly Semaphore Work = new(), Done = new();
        internal int Value; internal bool Measure; internal long WorkerBytes;
        internal readonly Action Callback;
        internal Pipeline() { Callback = Execute; }
        private void Execute()
        {
            var before = Measure ? GC.GetAllocatedBytesForCurrentThread() : 0;
            try { Work.Wait(); Gate.Lock(); try { Value++; } finally { Gate.Unlock(); } }
            finally { Done.Post(); if (Measure) Interlocked.Add(ref WorkerBytes, GC.GetAllocatedBytesForCurrentThread() - before); }
        }
        internal void Cycle()
        {
            var id = WorkerThreadPool.AddTask(Callback); Work.Post(); Done.Wait();
            WorkerThreadPool.WaitForTaskCompletion(id);
        }
        public void Dispose() { Gate.Dispose(); Work.Dispose(); Done.Dispose(); }
    }
    private static void WorkerPipeline()
    {
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2);
        using var pipeline = new Pipeline();
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 128; i++) pipeline.Cycle(); pipeline.Measure = true;
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 1024; i++) pipeline.Cycle();
        var bytes = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(pipeline.Value == 1152 && bytes == 0 && pipeline.WorkerBytes == 0, "Actual warm pool/signal/mutex pipeline preserves updates with zero sender/worker bytes: " + bytes + "/" + pipeline.WorkerBytes);
        Console.WriteLine("Synchronization 1024-cycle worker pipeline: owner=" + bytes + " B, worker=" + pipeline.WorkerBytes + " B.");
    }
    private static void Blocked(Thread worker) => Check(SpinWait.SpinUntil(() => (worker.ThreadState & System.Threading.ThreadState.WaitSleepJoin) != 0, 5000), "Expected a concrete blocked worker.");
    private static void Join(Thread worker) => Check(worker.Join(5000), "Worker did not finish.");
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
