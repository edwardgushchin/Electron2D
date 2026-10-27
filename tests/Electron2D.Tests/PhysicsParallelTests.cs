using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2Types;
using static Box2D.NET.B2Worlds;

internal static class PhysicsParallelTests
{
    internal static void Run()
    {
        RunScheduler();
        var owner = Environment.CurrentManagedThreadId;
        using var circle = new CircleShape { Radius = 6 };
        using var floorShape = new RectangleShape { Size = new(800, 18) };
        var root = new Node();
        var floor = new StaticBody { Position = new(400, 600) };
        floor.AddChild(new CollisionShape { Shape = floorShape, OneWayCollision = true });
        root.AddChild(floor);
        var bodies = new List<ProbeBody>();
        for (var i = 0; i < 288; i++)
        {
            var body = new ProbeBody(owner) { Name = "Particle" + i, CanSleep = false, Position = new(20 + i % 24 * 30, 310 + i / 24 * 18), ContactMonitor = true, MaxContactsReported = 4 };
            body.AddChild(new CollisionShape { Shape = circle });
            body.BodyEntered += _ => { if (Environment.CurrentManagedThreadId != owner) throw new Exception("Contact event left its owner thread."); };
            root.AddChild(body); bodies.Add(body);
        }
        using var tree = new SceneTree(root);
        for (var i = 0; i < 256; i++) tree.PhysicsFrame(1d / 144);
        var world = b2GetWorldFromId(bodies[0].Space!.WorldID);
        Check(world.workerCount == (OperatingSystem.IsBrowser() ? 1 : Math.Min(4, Environment.ProcessorCount)), "Large world selects retained workers.");
        Check(bodies.Any(body => body.ContactSamples > 0), "Parallel collectors publish contacts before owner integration.");
        Check(bodies.All(body => body.Position.IsFinite() && body.Position.Y < 610 && body.Calls == 256), "Parallel contacts and owner integration remain valid.");
        for (var i = bodies.Count - 1; i >= 32; i--) bodies[i].Freeze = true;
        tree.PhysicsFrame(1d / 144);
        Check(world.workerCount == 1, "Small awake population returns to the serial solver.");
        foreach (var body in bodies) body.Freeze = false;
        tree.PhysicsFrame(1d / 144);
        Check(world.workerCount == (OperatingSystem.IsBrowser() ? 1 : Math.Min(4, Environment.ProcessorCount)), "Wakeup restores parallel solving.");
    }

    internal static void RunScheduler()
    {
        if (OperatingSystem.IsBrowser()) return;
        VerifyLateQueueContention(); VerifyQueueCapacity(); VerifyScheduler(); VerifyUnboundRanges();
    }

    private static void VerifyQueueCapacity()
    {
        using var scheduler = new PhysicsTaskScheduler(3); using var release = new ManualResetEvent(false);
        int started = 0, completed = 0;
        b2TaskCallback callback = (_, _, _, _) => { Interlocked.Increment(ref started); release.WaitOne(); Interlocked.Increment(ref completed); };
        var jobs = new object[64]; jobs[0] = scheduler.Enqueue(callback, 3, 1, scheduler, null!);
        var threads = (Thread[])typeof(PhysicsTaskScheduler).GetField("_threads", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.GetValue(scheduler)!;
        try
        {
            Check(SpinWait.SpinUntil(() => Volatile.Read(ref started) == 3 && threads.All(thread => (thread.ThreadState & System.Threading.ThreadState.WaitSleepJoin) != 0), 10000), "Each worker is held outside its queue gate before saturation.");
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 1; i < jobs.Length; i++) jobs[i] = scheduler.Enqueue(callback, 3, 1, scheduler, null!);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, $"A full retained task graph allocates {owner}/{all} bytes while enqueueing.");
            try { scheduler.Enqueue(callback, 3, 1, scheduler, null!); throw new Exception("Task exhaustion was ignored."); }
            catch (InvalidOperationException error) when (error.Message == "Physics task storage exhausted.") { }
            release.Set(); foreach (var job in jobs) scheduler.Finish(job, null!);
            Check(completed == 192, "Every saturated range completes once after the queue is released.");
            scheduler.Finish(scheduler.Enqueue(callback, 3, 1, scheduler, null!), null!);
            Check(completed == 195, "Exhausted task storage remains reusable after the complete join.");
            Console.WriteLine("All 64 retained task slots enqueue without queue growth or managed allocation.");
        }
        finally { release.Set(); scheduler.Drain(); }
    }

    private static void VerifyLateQueueContention()
    {
        _ = AppContext.BaseDirectory;
        using var scheduler = new PhysicsTaskScheduler(3);
        const System.Reflection.BindingFlags fields = System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic;
        typeof(PhysicsTaskScheduler).GetMethod("StartWorkers", fields)!.Invoke(scheduler, null);
        typeof(PhysicsTaskScheduler).GetField("_nextQueue", fields)!.SetValue(scheduler, 1);
        var gates = (SpinLock[])typeof(PhysicsTaskScheduler).GetField("_queueGates", fields)!.GetValue(scheduler)!;
        var threads = (Thread[])typeof(PhysicsTaskScheduler).GetField("_threads", fields)!.GetValue(scheduler)!;
        b2TaskCallback callback = static (_, _, _, state) => Interlocked.Increment(ref ((int[])state)[0]);
        var values = new int[1];
        // Prepare callback state without touching the first queue's gate.
        for (var i = 0; i < 2; i++) scheduler.Finish(scheduler.Enqueue(callback, 1, 1, values, null!), null!);
        Check(SpinWait.SpinUntil(() => threads.All(thread => (thread.ThreadState & System.Threading.ThreadState.WaitSleepJoin) != 0), 10000), "Scheduler workers park before delayed contention.");
        var ownerThread = Thread.CurrentThread; int held = 0, armed = 0, finished = 0, timedOut = 0;
        var holder = new Thread(() =>
        {
            var limit = System.Diagnostics.Stopwatch.GetTimestamp() + System.Diagnostics.Stopwatch.Frequency * 10;
            bool taken = false;
            try
            {
                gates[0].Enter(ref taken); Volatile.Write(ref held, 1);
                while (Volatile.Read(ref finished) == 0 && (Volatile.Read(ref armed) == 0 || (ownerThread.ThreadState & System.Threading.ThreadState.WaitSleepJoin) == 0))
                {
                    if (System.Diagnostics.Stopwatch.GetTimestamp() >= limit) { Volatile.Write(ref timedOut, 1); break; }
                    Thread.SpinWait(1);
                }
            }
            finally { if (taken) gates[0].Exit(); }
            while (Volatile.Read(ref finished) == 0) Thread.SpinWait(1);
        });
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        long owner = 0, all = 0; holder.Start();
        try
        {
            Check(SpinWait.SpinUntil(() => Volatile.Read(ref held) != 0, 10000), "A delayed worker owns the fresh queue gate.");
            owner = GC.GetAllocatedBytesForCurrentThread(); all = GC.GetTotalAllocatedBytes(true);
            Volatile.Write(ref armed, 1); scheduler.Finish(scheduler.Enqueue(callback, 1, 1, values, null!), null!);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        }
        finally { Volatile.Write(ref finished, 1); holder.Join(); }
        Console.WriteLine($"First contended scheduler enqueue: {owner}/{all} B.");
        Check(timedOut == 0 && values[0] == 3 && owner == 0 && all == 0, "A preempted worker causes no first-contention allocation and all queued work still completes.");
    }

    private static void VerifyScheduler()
    {
        using var scheduler = new PhysicsTaskScheduler(3);
        var definition = b2DefaultWorldDef();
        definition.workerCount = 3; definition.enqueueTask = scheduler.Enqueue; definition.finishTask = scheduler.Finish;
        var id = b2CreateWorld(definition);
        scheduler.Bind(b2GetWorldFromId(id));
        try
        {
            var values = new int[97];
            static void Fill(int start, int end, uint worker, object context) { var values = (int[])context; for (var i = start; i < end; i++) values[i]++; }
            for (var i = 0; i < 160; i++) scheduler.Finish(scheduler.Enqueue(Fill, values.Length, 16, values, null!), null!);
            Check(values.All(value => value == 160), "Non-divisible ranges and recycled jobs cover every item once.");
            var worker = new B2WorkerContext { context = new B2StepContext(), workerIndex = 1 };
            var failed = scheduler.Enqueue(static (_, _, _, _) => throw new ArithmeticException("probe"), 3, 1, worker, null!);
            try { scheduler.Finish(failed, null!); throw new Exception("Worker failure was ignored."); }
            catch (InvalidOperationException ex) when (ex.InnerException is ArithmeticException) { }
            Check(worker.context.workerFailure is ArithmeticException && Volatile.Read(ref worker.context.atomicSyncBits) == long.MaxValue,
                "Solver failure publishes cancellation to waiting workers.");
            scheduler.Finish(scheduler.Enqueue(Fill, values.Length, 16, values, null!), null!);
            Check(values.All(value => value == 161), "All failed task ranges finish and storage remains reusable.");
        }
        finally { b2DestroyWorld(id); }
    }

    private sealed class RangeState
    {
        internal readonly int[] Values = new int[769];
        internal int Completed;
        internal bool Fail;
    }
    private static void FillRange(int start, int end, uint worker, object context)
    {
        var state = (RangeState)context;
        for (var i = start; i < end; i++) state.Values[i]++;
        Interlocked.Add(ref state.Completed, end - start);
        if (state.Fail && start == 0) throw new ArithmeticException("worker");
        if (state.Fail && end == state.Values.Length) throw new FormatException("owner");
    }
    private static void VerifyUnboundRanges()
    {
        using var scheduler = new PhysicsTaskScheduler(3); var state = new RangeState();
        for (var i = 0; i < 64; i++) scheduler.RunRanges(FillRange, state.Values.Length, 3, state);
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) scheduler.RunRanges(FillRange, state.Values.Length, 3, state);
        Check(GC.GetAllocatedBytesForCurrentThread() == owner && GC.GetTotalAllocatedBytes(true) == all,
            "Unbound range dispatch allocates zero owner/all-thread managed bytes after warmup.");
        Check(state.Values.All(x => x == 192), "Unbound ranges cover every non-divisible item exactly once.");
        state.Completed = 0; state.Fail = true;
        try { scheduler.RunRanges(FillRange, state.Values.Length, 3, state); throw new Exception("Range failures were ignored."); }
        catch (AggregateException error)
        {
            Check(error.InnerExceptions[0] is FormatException && error.InnerExceptions[1].InnerException is ArithmeticException &&
                state.Completed == state.Values.Length, "Owner and worker failures survive only after every range finishes.");
        }
        state.Fail = false; scheduler.RunRanges(FillRange, state.Values.Length, 3, state);
        Check(state.Values.All(x => x == 194), "Failed range storage remains reusable after the complete join.");
    }

    internal static void RunGPU(bool threaded = false)
    {
        if (!OperatingSystem.IsBrowser()) VerifyUnboundRanges();
        var setting = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { VerifyGPUContacts(); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, setting); }
    }
    private static void VerifyGPUContacts()
    {
        var owner = Environment.CurrentManagedThreadId;
        using var world = new World(PhysicsServer.Backend.GPU); using var root = new SubViewport { World = world };
        using var circle = new CircleShape { Radius = 6 }; using var floorShape = new RectangleShape { Size = new(800, 18) };
        var floor = new StaticBody { Position = new(400, 600) }; floor.AddChild(new CollisionShape { Shape = floorShape }); root.AddChild(floor);
        var bodies = new ProbeBody[288];
        for (var i = 0; i < bodies.Length; i++)
        {
            var body = new ProbeBody(owner) { Name = "Particle" + i, CanSleep = false, Position = new(20 + i % 24 * 30, 310 + i / 24 * 18), ContactMonitor = true, MaxContactsReported = 4 };
            body.AddChild(new CollisionShape { Shape = circle });
            body.BodyEntered += _ => Check(Environment.CurrentManagedThreadId == owner, "GPU contact events remain on the owner after worker publication.");
            root.AddChild(body); bodies[i] = body;
        }
        using var tree = new SceneTree(root);
        for (var i = 0; i < 256; i++) tree.PhysicsFrame(1d / 144);
        Check(world.PhysicsBackend == PhysicsServer.Backend.GPU && bodies.All(x => x.Calls == 256) && bodies.Any(x => x.ContactSamples > 0),
            "The independent GPU world publishes complete contacts before owner integration.");
        var before = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 64; i++) tree.PhysicsFrame(1d / 144);
        Check(GC.GetAllocatedBytesForCurrentThread() == before && GC.GetTotalAllocatedBytes(true) == all,
            "Warmed GPU frames with owner callbacks and contact workers allocate zero managed bytes.");
        Check(bodies.All(x => x.Calls == 320 && x.Position.IsFinite() && x.Position.Y < 610), "Every GPU receiver remains simulated and published.");
        Console.WriteLine("GPU contact workers and owner integration passed: 288 bodies, 64 measured full frames, 0/0 managed bytes.");
    }

    private sealed class ProbeBody(int owner) : RigidBody
    {
        internal int Calls, ContactSamples;
        public override bool Equals(object? other)
        {
            if (Environment.CurrentManagedThreadId != owner) throw new Exception("User equality left its owner thread.");
            return ReferenceEquals(this, other);
        }
        public override int GetHashCode()
        {
            if (Environment.CurrentManagedThreadId != owner) throw new Exception("User hashing left its owner thread.");
            return System.Runtime.CompilerServices.RuntimeHelpers.GetHashCode(this);
        }
        protected override void IntegrateForces(PhysicsDirectBodyState state)
        {
            if (Environment.CurrentManagedThreadId != owner) throw new Exception("Force integration left its owner thread.");
            Calls++;
            Check(state.GetContactCount() == GetContactCount(), "Owner integration sees the complete shared contact snapshot.");
            if (state.GetContactCount() > 0)
            {
                ContactSamples++;
                Check(state.GetContactLocalNormal(0).IsFinite(), "Parallel contact normal is finite.");
            }
        }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
}
