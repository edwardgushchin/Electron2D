using System.Runtime.CompilerServices;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunDeferredContract(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        var owner = Thread.CurrentThread;
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("DeferredContract", () => new Solver
        {
            StepHook = () => Check((Thread.CurrentThread != owner) == threaded, "Deferred frames use the requested numerical lane")
        });
        try
        {
            DeferredOrdering(); DeferredReferences(); DeferredClosing();
            using var frames = new DeferredFrames(backend);
            for (var i = 0; i < 128; i++) frames.Frame();
            Collect(); var position = frames.Body.Position; var tick = PhysicsServer.SpaceGetTick(frames.World.Space);
            var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) frames.Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && PhysicsServer.SpaceGetTick(frames.World.Space) == tick + 128 &&
                frames.Body.Position.DistanceTo(position + new Vector2(4, 1)) < .003f && frames.Nested == 256,
                $"Deferred calls/setters retain full physical motion, quiescent delivery and zero warm bytes: {own}/{all} B");
            Console.WriteLine($"Deferred contract {backend?.ToString() ?? "registered"}, threaded={threaded}: mixed FIFO, capture, failure/closure/reference lifetime and 128 moving frames at 0/0 B passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void DeferredOrdering()
    {
        using var tree = new SceneTree(new Node()); var log = new List<int>();
        var original = new InvalidOperationException("Intentional deferred call failure");
        var second = new InvalidOperationException("Intentional deferred setter failure");
        tree.Defer(() => { log.Add(1); tree.SetDeferred(log.Add, 9); throw original; });
        tree.SetDeferred<int>(_ => { log.Add(2); throw second; }, 0);
        tree.Defer(() => log.Add(3));
        Check(log.Count == 0, "Neither call nor typed setter executes at submission");
        try { tree.FlushDeferred(); throw new InvalidOperationException("Both deferred errors must be reported"); }
        catch (AggregateException error)
        {
            var errors = error.Flatten().InnerExceptions;
            Check(errors.Count == 2 && ReferenceEquals(errors[0], original) && ReferenceEquals(errors[1], second),
                "Original call/setter errors survive while later work continues");
        }
        Check(log.SequenceEqual([1, 2, 3, 9]), "Nested work must complete in the current flush after the original FIFO, including after errors");
        tree.FlushDeferred(); Check(log.SequenceEqual([1, 2, 3, 9]), "A completed nested call cannot run again on a later flush");
        log.Clear();
        Task.Run(() =>
        {
            for (var i = 0; i < 16; i++) { tree.SetDeferred(log.Add, i); tree.Defer(() => log.Add(-1)); }
        }).GetAwaiter().GetResult();
        Check(log.Count == 0, "A foreign submitter cannot execute work on itself");
        tree.FlushDeferred();
        Check(log.Count == 32 && Enumerable.Range(0, 16).All(i => log[2 * i] == i && log[2 * i + 1] == -1),
            "Each independently captured value retains foreign-producer FIFO order");
        var delivered = 0; var depth = 0; var maxDepth = 0;
        Action<int>? chain = null;
        chain = n => { depth++; maxDepth = Math.Max(maxDepth, depth); delivered++; if (n < 4096) tree.SetDeferred(chain!, n + 1); depth--; };
        tree.SetDeferred(chain, 1); tree.FlushDeferred();
        Check(delivered == 4096 && maxDepth == 1, "Long finite deferred chains drain iteratively without a hidden batch limit or callback recursion");
        var value = new Vector2(7, 11); var observed = Vector2.Zero;
        tree.SetDeferred<Vector2>(item => observed = item, value); value.X = 99; tree.FlushDeferred();
        Check(observed == new Vector2(7, 11), "Value arguments are captured independently of later local writes");
        Reject<ArgumentNullException>(() => tree.Defer(null!));
        Reject<ArgumentNullException>(() => tree.SetDeferred<int>(null!, 0));
        Task.Run(() => Reject<InvalidOperationException>(tree.FlushDeferred)).GetAwaiter().GetResult();
        var reentry = 0;
        tree.Defer(() => { Reject<InvalidOperationException>(tree.FlushDeferred); Reject<InvalidOperationException>(tree.Dispose); reentry++; });
        tree.FlushDeferred(); Check(reentry == 1 && !tree.IsDisposed, "Rejected flush/disposal reentry preserves the queue owner");
        tree.SetDeferred(log.Add, 17); tree.ProcessFrame(0);
        Check(log[^1] == 17, "The variable frame lane also owns a deferred safe point");
    }

    private static void DeferredReferences()
    {
        foreach (var exit in new[] { 0, 1, 2 })
        {
            using var tree = new SceneTree(new Node()); var weak = QueueReference(tree, exit == 1);
            Collect(); Check(weak.IsAlive, "A pending typed value remains retained until execution or discard");
            if (exit == 2) tree.Dispose();
            else if (exit == 1) Reject<AggregateException>(tree.FlushDeferred);
            else tree.FlushDeferred();
            Collect(); Check(!weak.IsAlive, "Success, callback error and tree closure release typed captured references");
            GC.KeepAlive(tree);
        }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference QueueReference(SceneTree tree, bool fail)
    {
        var payload = new DeferredPayload { Fail = fail };
        tree.SetDeferred<DeferredPayload>(static item => { if (item.Fail) throw new InvalidOperationException("Intentional reference setter failure"); }, payload);
        return new WeakReference(payload);
    }

    private static void DeferredClosing()
    {
        for (var i = 0; i < 16; i++)
        {
            using var tree = new SceneTree(new Node()); using var start = new ManualResetEventSlim();
            var attempts = 0; var accepted = 0; var rejected = 0; var calls = 0;
            Action callback = () => calls++;
            Action<int> setter = _ => calls++;
            var tasks = Enumerable.Range(0, 4).Select(_ => Task.Run(() =>
            {
                start.Wait();
                Interlocked.Increment(ref attempts);
                try { tree.Defer(callback); Interlocked.Increment(ref accepted); } catch (ObjectDisposedException) { Interlocked.Increment(ref rejected); }
                Interlocked.Increment(ref attempts);
                try { tree.SetDeferred(setter, 3); Interlocked.Increment(ref accepted); } catch (ObjectDisposedException) { Interlocked.Increment(ref rejected); }
            })).ToArray();
            start.Set(); tree.Dispose(); Task.WaitAll(tasks);
            Check(attempts == 8 && accepted + rejected == 8 && calls == 0, "Every racing call/setter is accepted for discard or rejected without execution");
            Reject<ObjectDisposedException>(() => tree.Defer(callback));
            Reject<ObjectDisposedException>(() => tree.SetDeferred(setter, 0));
        }
        using var finalized = new SceneTree(new Node()); var count = 0;
        finalized.Defer(() => count++); finalized.SetDeferred<int>(_ => count++, 1); finalized.FinalizeLoop();
        Reject<ObjectDisposedException>(() => finalized.Defer(() => count++));
        Reject<ObjectDisposedException>(() => finalized.SetDeferred<int>(_ => count++, 2));
        Check(count == 0, "Explicit finalization discards both kinds of work without waiting for Dispose");
    }

    private sealed class DeferredPayload { internal bool Fail; }

    private sealed class DeferredFrames : IDisposable
    {
        internal readonly World World;
        internal readonly RigidBody Body;
        private readonly CircleShape _shape = new() { Radius = .5f };
        private readonly PhysicsPointQueryParameters _query = new();
        private readonly PhysicsPointResult[] _hits = new PhysicsPointResult[1];
        private readonly SceneTree _tree;
        private readonly Action<Vector2> _setter;
        private readonly Action<int> _nested;
        private readonly Action _after;
        private readonly Thread _owner = Thread.CurrentThread;
        private int _sequence, _order;
        internal int Nested;
        internal DeferredFrames(PhysicsServer.Backend? backend)
        {
            World = backend is { } selected ? new World(selected) : new World("DeferredContract");
            Check(World.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && World.PhysicsFallbackReason is null,
                "Deferred physics retains its selected implementation without fallback");
            var root = new SubViewport { World = World };
            Body = new RigidBody { Mass = 1, GravityScale = 0, CanSleep = false, LinearVelocity = new(3, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
            Body.AddChild(new CollisionShape { Shape = _shape }); root.AddChild(Body); _tree = new SceneTree(root);
            _setter = value => { Check(Thread.CurrentThread == _owner, "Typed setters run on the scene owner"); Body.LinearVelocity = value; _order = _order * 10 + 2; };
            _nested = value => { Check(value == _sequence, "Nested values retain the current safe-point number"); Nested++; _order = _order * 10 + 1; };
            _after = After; _tree.PhysicsFrameStarted += Begin;
        }
        private void Begin(SceneTree tree)
        {
            _sequence++; _order = 0;
            tree.SetDeferred(_setter, new Vector2(_sequence % 2 == 0 ? 5 : 3, 0)); tree.Defer(_after);
        }
        private void After()
        {
            Check(Thread.CurrentThread == _owner && Body.LinearVelocity == new Vector2(_sequence % 2 == 0 ? 5 : 3, 0),
                "The call observes its preceding typed body setter after numerical completion");
            Body.ApplyCentralImpulse(new(0, 1)); _order = _order * 10 + 3;
            _query.Position = Body.Position;
            Check(World.DirectSpaceState.IntersectPoint(_query, _hits) == 1 && _hits[0].ColliderRID == Body.GetRID(),
                "Deferred physical edits preserve the actual published collider");
            _tree.SetDeferred(_nested, _sequence);
        }
        internal void Frame()
        {
            _tree.PhysicsFrame(1d / 128);
            Check(_order == 231 && Nested == _sequence,
                "Nested work completes in FIFO order before each physical safe point returns");
        }
        public void Dispose() { _tree.Dispose(); World.Dispose(); _shape.Dispose(); _query.Dispose(); }
    }
}
