using System.Runtime.CompilerServices;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunThreadMessages(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("ThreadMessages", () => new Solver());
        try { ThreadMessages(backend); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }

    private static void ThreadMessages(PhysicsServer.Backend? backend)
    {
        using var world = backend is { } selected ? new World(selected) : new World("ThreadMessages");
        using var root = new SubViewport { World = world };
        using var shape = new CircleShape { Radius = 2 };
        var body = new MessageBody
        {
            Name = "Receiver",
            GravityScale = 0,
            CanSleep = false,
            LinearDampMode = RigidBody.DampMode.Replace,
            AngularDampMode = RigidBody.DampMode.Replace
        };
        body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        using var tree = new SceneTree(root); var ownerThread = Environment.CurrentManagedThreadId;
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null,
            "Thread messages execute the explicitly selected physical backend without fallback");
        var order = new List<int>(16);
        body.Signal = what =>
        {
            Check(Environment.CurrentManagedThreadId == ownerThread, "Notifications and queued operations run on the scene owner");
            if (what == MessageNotification) order.Add(3);
            if (what == Node.NotificationPhysicsProcess)
            {
                order.Add(4); body.CallDeferredThreadGroup<MessageBody>(_ => order.Add(5));
                body.SetDeferredThreadGroup<RigidBody, Vector2>(static (node, value) => node.LinearVelocity = value, new(18, 0));
            }
        };
        body.CallThreadSafe<MessageBody>(_ => order.Add(1));
        body.SetThreadSafe<RigidBody, Vector2>(static (node, value) => node.LinearVelocity = value, new(6, 0));
        body.NotifyThreadSafe(MessageNotification);
        Check(order.SequenceEqual([1, 3]) && body.LinearVelocity == new Vector2(6, 0), "Owner-thread calls/setters/notifications execute synchronously");
        body.PhysicsProcessEnabled = false; order.Clear();
        body.CallDeferredThreadGroup<MessageBody>(node => { order.Add(1); node.PhysicsProcessEnabled = true; });
        body.SetDeferredThreadGroup<MessageBody, int>((_, value) => order.Add(value), 2);
        body.NotifyDeferredThreadGroup(MessageNotification);
        tree.FlushDeferred(); Check(order.Count == 0, "Ordinary deferred flushing does not consume a process-group queue");
        tree.PhysicsFrame(.01);
        Check(order.SequenceEqual([1, 2, 3, 4, 5]) && MathF.Abs(body.Position.X - .18f) < .00001f,
            "Pre-group messages enable same-frame membership; post-group messages precede actual physical integration");
        body.PhysicsProcessEnabled = false; body.Signal = what => { if (what == MessageNotification) order.Add(3); };
        order.Clear(); Exception? workerError = null;
        var worker = new Thread(() =>
        {
            try
            {
                body.CallThreadSafe<MessageBody>(_ => { Check(Environment.CurrentManagedThreadId == ownerThread, "Worker call is owner-delivered"); order.Add(1); });
                body.SetThreadSafe<MessageBody, int>((_, value) => order.Add(value), 2);
                body.NotifyThreadSafe(MessageNotification);
            }
            catch (Exception error) { workerError = error; }
        });
        worker.Start(); worker.Join(); Check(workerError is null && order.Count == 0, "Worker submissions defer every member without off-owner scene access");
        tree.ProcessFrame(.01); Check(order.SequenceEqual([1, 2, 3]), "Disabled nodes still receive captured FIFO group messages in idle frames");
        order.Clear(); tree.Paused = true; body.NotifyDeferredThreadGroup(MessageNotification); tree.ProcessFrame(.01);
        Check(order.SequenceEqual([3]), "Paused processing does not block default-group message delivery"); tree.Paused = false;
        order.Clear();
        var expected = new InvalidOperationException("Expected group callback failure");
        body.CallDeferredThreadGroup<MessageBody>(node =>
        {
            order.Add(1); node.CallDeferredThreadGroup<MessageBody>(_ => order.Add(3)); throw expected;
        });
        body.SetDeferredThreadGroup<MessageBody, int>((_, value) => order.Add(value), 2);
        Check(CaptureCopy(() => tree.PhysicsFrame(.01)) is AggregateException aggregate && aggregate.Flatten().InnerExceptions.Contains(expected) &&
            order.SequenceEqual([1, 2, 3]), "Group drains appended work in FIFO order and continues after failures");
        Check(CaptureCopy(() => body.CallThreadSafe<MessageBody>(_ => throw expected)) == expected, "Immediate callback failures propagate without queue wrapping");
        Reject<ArgumentNullException>(() => body.CallThreadSafe<MessageBody>(null!));
        Reject<ArgumentNullException>(() => body.SetDeferredThreadGroup<MessageBody, int>(null!, 1));
        Reject<ArgumentException>(() => root.CallThreadSafe<MessageBody>(_ => { }));
        ReceiverLifetime(root, tree);
        OwnerTransfer(root, tree);
        QueueClosure();

        using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        body.Signal = null; body.LinearVelocity = new(6, 0);
        using var send = new AutoResetEvent(false); using var sent = new AutoResetEvent(false);
        var stop = false; var measured = false; var senderBytes = 0L; Exception? senderError = null;
        var sender = new Thread(() =>
        {
            while (true)
            {
                send.WaitOne(); if (stop) return;
                var start = GC.GetAllocatedBytesForCurrentThread();
                try
                {
                    body.CallThreadSafe<RigidBody>(static node => node.LinearVelocity = new(6, 0));
                    body.SetThreadSafe<RigidBody, Vector2>(static (node, value) => node.LinearVelocity = value, new(6, 0));
                    body.NotifyThreadSafe(MessageNotification);
                    body.CallDeferredThreadGroup<RigidBody>(static node => node.LinearVelocity = new(6, 0));
                    body.SetDeferredThreadGroup<RigidBody, Vector2>(static (node, value) => node.LinearVelocity = value, new(6, 0));
                    body.NotifyDeferredThreadGroup(MessageNotification);
                }
                catch (Exception error) { senderError = error; }
                if (measured) senderBytes += GC.GetAllocatedBytesForCurrentThread() - start;
                sent.Set();
            }
        });
        sender.Start();
        try
        {
            for (var i = 0; i < 128; i++) Frame();
            Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true); measured = true;
            for (var i = 0; i < 128; i++) Frame();
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0 && senderBytes == 0, $"Thread messages full sender/owner/physics/query cycles allocate {senderBytes}/{owner}/{all} B");
            Console.WriteLine($"Thread messages {backend?.ToString() ?? "registered"}, solverThreaded={ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread)}: all six APIs, before/after/disabled/paused/recursive/error/weak/transfer/closure and 128 frames/queries {senderBytes}/{owner}/{all} B passed.");
        }
        finally { stop = true; send.Set(); sender.Join(); }

        void Frame()
        {
            body.CallThreadSafe<RigidBody>(static node => node.LinearVelocity = new(6, 0));
            body.SetThreadSafe<RigidBody, Vector2>(static (node, value) => node.LinearVelocity = value, new(6, 0));
            body.NotifyThreadSafe(MessageNotification);
            body.CallDeferredThreadGroup<RigidBody>(static node => node.LinearVelocity = new(6, 0));
            body.SetDeferredThreadGroup<RigidBody, Vector2>(static (node, value) => node.LinearVelocity = value, new(6, 0));
            body.NotifyDeferredThreadGroup(MessageNotification);
            send.Set(); sent.WaitOne(); Check(senderError is null, "Prepared worker submissions preserve owner access without sender failures");
            var x = body.Position.X; tree.PhysicsFrame(.01); query.Position = body.Position;
            Check(MathF.Abs(body.Position.X - x - .06f) < .0001f && world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == body.GetRID(),
                "Queued and immediate setters preserve real selected motion and moving-collider identity");
        }
    }

    private const int MessageNotification = 76001;
    private sealed class MessageBody : RigidBody
    {
        internal Action<int>? Signal;
        protected override void OnNotification(int what) { base.OnNotification(what); Signal?.Invoke(what); }
    }

    private static void ReceiverLifetime(Node root, SceneTree tree)
    {
        var calls = 0; var discarded = new Node { Name = "DisposedReceiver" }; root.AddChild(discarded);
        discarded.CallDeferredThreadGroup<Node>(_ => calls++); discarded.Dispose();
        var weak = UnrootedMessage(root); Collect(); tree.ProcessFrame(.01);
        Check(calls == 0 && !weak.TryGetTarget(out _), "Pending messages neither call disposed receivers nor retain detached receivers");
        using var detached = new Node(); detached.SetThreadSafe<Node, string>(static (node, value) => node.Name = value, "Detached");
        Check(detached.Name == "Detached", "Detached thread-safe calls keep ordinary detached access");
        Reject<InvalidOperationException>(() => detached.NotifyDeferredThreadGroup(MessageNotification));
        Reject<ObjectDisposedException>(() => discarded.NotifyThreadSafe(MessageNotification));
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Node> UnrootedMessage(Node root)
    {
        var node = new Node { Name = "UnrootedReceiver" }; root.AddChild(node);
        node.CallDeferredThreadGroup<Node>(static _ => throw new InvalidOperationException("Collected receiver was called"));
        root.RemoveChild(node); return new(node);
    }

    private static void OwnerTransfer(Node root, SceneTree tree)
    {
        using var receiver = new Node { Name = "TransferredReceiver" }; root.AddChild(receiver);
        var executed = 0; var receivingThread = 0;
        receiver.CallDeferredThreadGroup<Node>(_ => executed = Environment.CurrentManagedThreadId);
        root.RemoveChild(receiver);
        using var ready = new ManualResetEventSlim(); using var advance = new ManualResetEventSlim(); Exception? failure = null;
        var worker = new Thread(() =>
        {
            try
            {
                using var host = new Node(); host.AddChild(receiver); using var other = new SceneTree(host);
                receivingThread = Environment.CurrentManagedThreadId; ready.Set(); advance.Wait(); other.ProcessFrame(.01);
                host.RemoveChild(receiver);
            }
            catch (Exception error) { failure = error; ready.Set(); }
        });
        worker.Start(); ready.Wait(); tree.ProcessFrame(.01); Check(executed == 0, "Transferred messages cannot execute on the old owner");
        advance.Set(); worker.Join(); Check(failure is null && executed == receivingThread && executed != Environment.CurrentManagedThreadId,
            "A queued immutable receiver follows its new tree owner exactly once");
    }

    private static void QueueClosure()
    {
        var receiver = new Node(); var host = new Node(); host.AddChild(receiver); var tree = new SceneTree(host); var calls = 0;
        receiver.CallDeferredThreadGroup<Node>(_ => calls++); tree.Dispose();
        Check(calls == 0 && receiver.IsDisposed, "Finalization discards queued messages and retains normal receiver cleanup");
        Reject<ObjectDisposedException>(() => receiver.NotifyDeferredThreadGroup(MessageNotification));
    }
}
