using System.Collections;
using Electron2D;

internal static class ObjectIterationTests
{
    internal static void Run(PhysicsServer.Backend backend, bool threaded) => Run(() => new World(backend), backend.ToString(), threaded);
    internal static void Run(Func<World> create, string label, bool threaded)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            Protocol();
            using var world = create(); using var root = new SubViewport { World = world };
            using var shape = new CircleShape { Radius = 1 };
            var body = new IterableBody { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
            body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
            using var tree = new SceneTree(root);
            using var policy = new Sequence<Vector2>(new(1, 0), new(2, 0), new(3, 0));
            body.Policy = policy;
            PhysicsServer.BodySetStateSyncCallback(body.GetRID(), state =>
            {
                var sum = Vector2.Zero; foreach (var value in body) sum += value;
                Check(sum == new Vector2(6, 0), "A physical body keeps its own typed iteration capability");
                body.Deliveries++;
                if (body.FailCallback) throw body.Failure;
            });
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
            Collect(); for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
            var before = (body.Integrations, body.Deliveries, policy.Ends); var tick = PhysicsServer.SpaceGetTick(world.Space); var position = body.Position;
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) tree.PhysicsFrame(.01);
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0 && body.Integrations - before.Integrations == 128 && body.Deliveries - before.Deliveries == 128 &&
                policy.Ends - before.Ends == 256 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && body.Position.X > position.X,
                $"Typed iteration retains real moving full frames and callbacks at 0/0 bytes: {owner}/{all}");
            body.FailCallback = true; tick = PhysicsServer.SpaceGetTick(world.Space); var ends = policy.Ends;
            Original(() => tree.PhysicsFrame(.01), body.Failure, aggregate: true);
            Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1 && policy.Ends == ends + 2, "Callback failure follows completed physics and disposes both traversals");
            body.FailCallback = false; tree.PhysicsFrame(.01); Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 2, "Later frames remain usable without replaying the completed interval");
            PhysicsServer.BodySetStateSyncCallback(body.GetRID(), null);
            tree.Dispose(); Reject<ObjectDisposedException>(() => body.GetEnumerator());
            Check(!policy.IsDisposed, "Disposing the scene does not dispose a borrowed iterable owner");
            Console.WriteLine($"Object iteration {label}/{threaded}: typed protocol, independent/nested state, errors/lifetime and 128 full frames 0/0 owner/all-thread B.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void Protocol()
    {
        using var empty = new Sequence<int>(); foreach (var _ in empty) throw new InvalidOperationException("Empty iterable entered");
        Check(empty.Starts == 1 && empty.Moves == 1 && empty.Reads == 0 && empty.Ends == 1, "Empty iteration initializes, tests end and disposes without fetching a value");
        using var values = new Sequence<int>(1, 2, 3);
        var sum = 0; foreach (var n in values) sum += n; foreach (var n in values) sum += n;
        Check(sum == 12 && values.Ends == 2, "Repeated traversals start independently");
        sum = 0; foreach (var a in values) foreach (var b in values) sum += a * b;
        Check(sum == 36, "Nested loops keep a distinct state per traversal");
        using (var first = values.GetEnumerator())
        using (var second = values.GetEnumerator())
        {
            Reject<InvalidOperationException>(() => { _ = first.Current; });
            Check(first.MoveNext() && first.Current == 1 && first.Current == 1 && second.MoveNext() && second.Current == 1,
                "Reading Current does not advance either iterator");
            Check(first.MoveNext() && first.Current == 2 && second.Current == 1, "Independent explicit cursors do not interfere");
        }
        Check(((IEnumerable<int>)values).Sum() == 6 && ((IEnumerable)values).Cast<int>().Sum() == 6, "Ordinary BCL generic and nongeneric consumers execute the same protocol");
        var ends = values.Ends; foreach (var _ in values) break; Check(values.Ends == ends + 1, "Early exit disposes once");
        var failure = new ArithmeticException("iterator-probe");
        foreach (var stage in new[] { 1, 2, 3, 4 })
        {
            values.FailStage = stage; values.Failure = failure; ends = values.Ends;
            Original(() => { foreach (var _ in values) { } }, failure);
            Check(values.Ends == ends + (stage == 1 ? 0 : 1), "Construction failure acquires no cursor; later failures run cleanup once");
            values.FailStage = 0; Check(values.Sum() == 6, "A failed traversal does not poison a fresh traversal");
        }
        ends = values.Ends; Original(() => { foreach (var _ in values) throw failure; }, failure);
        Check(values.Ends == ends + 1, "Loop-body error still disposes its cursor");
        using var nodes = new Sequence<Node>(new Node(), new Node());
        var borrowed = nodes.ToArray(); nodes.Dispose(); Check(borrowed.All(node => !node.IsDisposed), "Iteration retains typed borrowed object identity without implicit disposal");
        foreach (var node in borrowed) node.Dispose();
        var closing = new Sequence<int>(7); var cursor = closing.GetEnumerator(); closing.Dispose();
        Reject<ObjectDisposedException>(() => closing.GetEnumerator()); Reject<ObjectDisposedException>(() => cursor.MoveNext());
        Reject<ObjectDisposedException>(() => { _ = cursor.Current; }); cursor.Dispose();
        using var guarded = new Sequence<int>(1); guarded.BeforeDelete = () =>
        {
            Check(guarded.Sum() == 1, "A caller-owned iterator can use normal same-thread teardown inspection");
            Task.Run(() => Reject<ObjectDisposedException>(() => guarded.GetEnumerator())).GetAwaiter().GetResult();
        };
        guarded.Dispose();
    }

    private sealed class Sequence<T>(params T[] items) : ElectronObject, IEnumerable<T>
    {
        internal int Starts, Moves, Reads, Ends, FailStage;
        internal Exception Failure = new ArithmeticException();
        internal Action? BeforeDelete;
        public Cursor GetEnumerator() { ThrowIfDisposed(); Starts++; if (FailStage == 1) throw Failure; return new(this); }
        IEnumerator<T> IEnumerable<T>.GetEnumerator() => GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationPreDelete) BeforeDelete?.Invoke(); }
        internal struct Cursor(Sequence<T> owner) : IEnumerator<T>
        {
            private int _index = -1;
            public T Current
            {
                get
                {
                    owner.ThrowIfDisposed(); owner.Reads++; if (owner.FailStage == 3) throw owner.Failure;
                    if ((uint)_index >= (uint)ItemsLength) throw new InvalidOperationException("Cursor is not on a value");
                    return owner.Items[_index];
                }
            }
            object? IEnumerator.Current => Current;
            private int ItemsLength => owner.Items.Length;
            public bool MoveNext() { owner.ThrowIfDisposed(); owner.Moves++; if (owner.FailStage == 2) throw owner.Failure; return ++_index < ItemsLength; }
            public void Dispose() { owner.Ends++; if (owner.FailStage == 4) throw owner.Failure; }
            public void Reset() => throw new NotSupportedException();
        }
        private T[] Items => items;
    }
    private sealed class IterableBody : RigidBody
    {
        internal Sequence<Vector2> Policy = null!;
        internal int Integrations, Deliveries;
        internal bool FailCallback;
        internal readonly Exception Failure = new ArithmeticException("iteration-callback");
        public Sequence<Vector2>.Cursor GetEnumerator() { ThrowIfDisposed(); return Policy.GetEnumerator(); }
        protected override void IntegrateForces(PhysicsDirectBodyState state)
        {
            base.IntegrateForces(state); var sum = Vector2.Zero; foreach (var velocity in this) sum += velocity;
            state.LinearVelocity = sum; Integrations++;
        }
    }
    private static void Collect() { GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers(); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Original(Action action, Exception expected, bool aggregate = false)
    {
        try { action(); }
        catch (Exception error)
        {
            var actual = aggregate && error is AggregateException errors && errors.Flatten().InnerExceptions.Count == 1 ? errors.Flatten().InnerExceptions[0] : error;
            Check(ReferenceEquals(actual, expected), $"The original iterator or callback error propagates: {error}"); return;
        }
        throw new InvalidOperationException("Expected original failure");
    }
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException($"Expected {typeof(T).Name}"); }
}
