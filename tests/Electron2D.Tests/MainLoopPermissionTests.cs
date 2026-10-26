using System.Runtime.CompilerServices;
using Electron2D;

internal static class MainLoopPermissionTests
{
    internal static void Run(PhysicsServer.Backend backend = PhysicsServer.Backend.CPU, bool threaded = false)
    {
        VerifyBatchAndFailure();
        VerifyRetirement();
        VerifyConcurrentPosting();
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { VerifyPhysicalFrames(backend); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
        Console.WriteLine($"Native permission queue on {backend}/{threaded}: ordering, failure, lifetime, concurrency and physical owner delivery passed.");
    }

    private static void VerifyBatchAndFailure()
    {
        using var loop = new Loop();
        var owner = Environment.CurrentManagedThreadId;
        var trace = new List<string>();
        var expected = new ApplicationException("permission-listener");
        loop.OnRequestPermissionsResult += Handler;
        string[] names = ["first", "second"];
        bool[] granted = [true, false];
        Task.Run(() => loop.PostPermissionResults(names, granted)).GetAwaiter().GetResult();
        names[0] = "changed";
        granted[0] = false;
        Check(trace.Count == 0, "Posting from a native thread does not invoke game handlers");
        loop.Initialize();
        try { loop.Process(0); throw new Exception("Missing original handler error"); }
        catch (ApplicationException error) { Check(ReferenceEquals(error, expected), "A frame preserves the original handler exception"); }
        Check(trace.SequenceEqual(["first:True"]) && loop.Frames == 0, "A failed event is consumed before the frame callback");
        loop.OnRequestPermissionsResult -= Handler;
        loop.OnRequestPermissionsResult += (_, permission, value) => trace.Add(permission + ":" + value);
        loop.PhysicsProcess(0);
        Check(trace.SequenceEqual(["first:True", "second:False", "nested:True"]) && loop.Frames == 1,
            "Unread and callback-posted results retain FIFO order after failure without replay");
        loop.OnRequestPermissionsResult += (_, permission, _) =>
        {
            if (permission == "outer") loop.PostPermissionResults(["later"], [true]);
        };
        loop.PostPermissionResults(["outer"], [true]);
        loop.Process(0);
        Check(trace[^1] == "outer:True", "Callback-posted results wait for another frame entry");
        loop.Process(0);
        Check(trace[^1] == "later:True", "The next frame delivers the deferred callback result");
        loop.SetBlockSignals(true);
        loop.PostPermissionResults(["blocked"], [false]);
        loop.Process(0);
        loop.SetBlockSignals(false);
        loop.Process(0);
        Check(!trace.Contains("blocked:False"), "Blocked native results are consumed without later replay");
        Reject<ArgumentException>(() => loop.PostPermissionResults(["bad"], []));
        Reject<ArgumentException>(() => loop.PostPermissionResults(["valid", ""], [true, true]));
        var count = trace.Count;
        loop.Process(0);
        Check(trace.Count == count, "Malformed native batches reject before partially appending results");

        void Handler(MainLoop sender, string permission, bool value)
        {
            Check(ReferenceEquals(sender, loop) && Environment.CurrentManagedThreadId == owner && value && permission == "first",
                "The immutable result reaches its original owner on the owner thread");
            trace.Add(permission + ":" + value);
            Reject<InvalidOperationException>(() => loop.Process(0));
            Reject<InvalidOperationException>(loop.FinalizeLoop);
            Reject<InvalidOperationException>(loop.Dispose);
            loop.PostPermissionResults(["nested"], [true]);
            throw expected;
        }
    }

    private static void VerifyRetirement()
    {
        using var finalized = new Loop();
        finalized.Initialize();
        var queued = PostEphemeral(finalized);
        finalized.FinalizeLoop();
        var late = PostEphemeral(finalized);
        AllocationWarmupCheck.Collect();
        Check(!queued.IsAlive && !late.IsAlive, "Finalization drops queued names and rejects late native results");
        using var failed = new Loop { FailInitialization = true };
        var abandoned = PostEphemeral(failed);
        Reject<ApplicationException>(failed.Initialize);
        AllocationWarmupCheck.Collect();
        Check(!abandoned.IsAlive, "Failed initialization drops pending native results");
        var disposed = new Loop();
        var beforeDisposal = PostEphemeral(disposed);
        disposed.Dispose();
        var afterDisposal = PostEphemeral(disposed);
        AllocationWarmupCheck.Collect();
        Check(!beforeDisposal.IsAlive && !afterDisposal.IsAlive, "Disposal before initialization retires pending and late native data");
        using var successor = new Loop();
        successor.Initialize();
        var delivered = false;
        successor.OnRequestPermissionsResult += (_, _, _) => delivered = true;
        successor.Process(0);
        Check(!delivered, "A new loop cannot inherit a retired loop's results");
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference PostEphemeral(MainLoop loop)
    {
        var name = new string('p', 101);
        loop.PostPermissionResults([name], [true]);
        return new(name);
    }

    private static void VerifyConcurrentPosting()
    {
        using var loop = new Loop();
        loop.Initialize();
        var last = new int[4];
        Array.Fill(last, -1);
        var delivered = 0;
        loop.OnRequestPermissionsResult += (_, name, granted) =>
        {
            var split = name.IndexOf(':');
            var producer = int.Parse(name.AsSpan(0, split));
            var sequence = int.Parse(name.AsSpan(split + 1));
            Check(sequence == last[producer] + 1 && granted == (sequence % 2 == 0), "Concurrent batches retain each producer's FIFO and values");
            last[producer] = sequence;
            delivered++;
        };
        var producers = Enumerable.Range(0, 4).Select(producer => Task.Run(() =>
        {
            for (var i = 0; i < 64; i++) loop.PostPermissionResults([$"{producer}:{i * 2}", $"{producer}:{i * 2 + 1}"], [true, false]);
        })).ToArray();
        var deadline = Environment.TickCount64 + 10000;
        while (delivered != 512)
        {
            Check(Environment.TickCount64 < deadline, "Concurrent permission delivery deadline");
            loop.Process(0);
            Thread.Yield();
        }
        Task.WaitAll(producers);
        Check(last.All(value => value == 127), "Every accepted concurrent result arrives exactly once");
    }

    private static void VerifyPhysicalFrames(PhysicsServer.Backend backend)
    {
        using var world = new World(backend);
        using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 1 };
        var body = new RigidBody
        {
            GravityScale = 0,
            CanSleep = false,
            CollisionLayer = 0,
            CollisionMask = 0,
            LinearDampMode = RigidBody.DampMode.Replace,
            LinearDamp = 0
        };
        body.AddChild(new CollisionShape { Shape = shape });
        root.AddChild(body);
        using var tree = new SceneTree(root);
        Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Actual requested physics without fallback");
        var owner = Environment.CurrentManagedThreadId;
        var delivered = 0;
        var identity = body.GetRID();
        string[] names = ["android.permission.RECORD_AUDIO"];
        bool[] granted = [true];
        tree.OnRequestPermissionsResult += (sender, permission, allowed) =>
        {
            Check(ReferenceEquals(sender, tree) && Environment.CurrentManagedThreadId == owner && permission == names[0], "Physical permission callback retains its loop and owner");
            body.LinearVelocity = new(allowed ? 2 : 1, 0);
            Check(body.GetRID() == identity, "A permission callback can safely edit physics before the numerical step");
            delivered++;
        };
        for (var i = 0; i < 128; i++) Frame(i);
        AllocationWarmupCheck.Collect();
        var beforeTick = PhysicsServer.SpaceGetTick(world.Space);
        var beforeCalls = delivered;
        var beforePosition = body.GlobalPosition.X;
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread();
        var allBytes = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(i);
        ownerBytes = GC.GetAllocatedBytesForCurrentThread() - ownerBytes;
        allBytes = GC.GetTotalAllocatedBytes(true) - allBytes;
        Check(ownerBytes == 0 && allBytes == 0, $"Permission-active full frames allocated {ownerBytes}/{allBytes} B");
        Check(delivered == beforeCalls + 128 && PhysicsServer.SpaceGetTick(world.Space) == beforeTick + 128 && body.GlobalPosition.X > beforePosition,
            "All measured callbacks precede real advancing physical frames");
        Console.WriteLine($"Permission-active {backend}: 128 complete frames, owner/all={ownerBytes}/{allBytes} B.");

        void Frame(int frame)
        {
            granted[0] = frame % 2 == 0;
            var calls = delivered;
            tree.PostPermissionResults(names, granted);
            tree.PhysicsFrame(.01);
            Check(delivered == calls + 1 && body.LinearVelocity.X == (granted[0] ? 2 : 1), "Exactly one native result controls this complete physical step");
        }
    }

    private sealed class Loop : MainLoop
    {
        internal bool FailInitialization;
        internal int Frames;
        protected override void OnInitialize() { if (FailInitialization) throw new ApplicationException("initialization"); }
        protected override bool OnProcess(double delta) { Frames++; return false; }
        protected override bool OnPhysicsProcess(double delta) { Frames++; return false; }
    }
    private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
