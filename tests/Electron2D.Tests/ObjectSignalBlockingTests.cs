using Electron2D;

internal static class ObjectSignalBlockingTests
{
    public static void Run()
    {
        using (var resource = new Resource())
        {
            var changed = 0; resource.Changed += _ => changed++;
            Check(!resource.IsBlockingSignals(), "Ordinary emissions are enabled initially");
            resource.SetBlockSignals(true); resource.EmitChanged();
            Check(changed == 0 && resource.IsBlockingSignals(), "Blocked resources publish no Changed signal");
            resource.SetBlockSignals(false);
            Check(changed == 0, "Unblocking does not replay dropped emissions");
            resource.EmitChanged(); Check(changed == 1, "An existing subscription resumes after unblocking");
        }
        using (var publisher = new Publisher())
        {
            var calls = 0; var pending = new Queue<Action>();
            using var once = EventConnection.Subscribe<Publisher, int>(h => publisher.Pulse += h, h => publisher.Pulse -= h, (_, v) => calls += v, oneShot: true);
            using var deferred = EventConnection.Subscribe<Publisher, int>(h => publisher.Pulse += h, h => publisher.Pulse -= h, (_, v) => calls += v * 10, defer: pending.Enqueue);
            publisher.SetBlockSignals(true); publisher.Raise(2);
            Check(calls == 0 && pending.Count == 0 && once.IsConnected && deferred.IsConnected, "Blocked emissions neither consume nor schedule connections");
            publisher.SetBlockSignals(false); publisher.Raise(3); publisher.SetBlockSignals(true);
            Check(calls == 3 && !once.IsConnected && pending.Count == 1, "One-shot consumption belongs to the accepted emission");
            pending.Dequeue()(); Check(calls == 33, "An already accepted deferred callback survives later blocking");
        }
        using (var publisher = new Publisher())
        {
            var calls = 0;
            publisher.Pulse += (_, _) => { calls++; publisher.SetBlockSignals(true); };
            publisher.Pulse += (_, _) => calls++;
            publisher.Raise(1); publisher.Raise(2);
            Check(calls == 2, "A block change affects the next emission and preserves the selected multicast snapshot");
            publisher.RejectMutation = true;
            Throws<InvalidOperationException>(() => publisher.SetBlockSignals(true));
            Throws<InvalidOperationException>(() => publisher.SetBlockSignals(false));
            Check(publisher.IsBlockingSignals(), "Rejected unchanged writes retain the previous delivery policy");
        }
        var disposing = new Publisher(); var disposed = 0; disposing.Disposed += _ => disposed++;
        disposing.SetBlockSignals(true); disposing.Dispose();
        Check(disposed == 1 && disposing.PreDeleteCalls == 1, "Deterministic disposal and numeric notifications remain active while blocked");
        Throws<ObjectDisposedException>(() => disposing.SetBlockSignals(false));
        Throws<ObjectDisposedException>(() => disposing.IsBlockingSignals());
        using (var publisher = new Publisher())
        {
            var sum = 0; publisher.Packet += (_, data) => sum += data[0];
            publisher.EmitPacket([7]); publisher.SetBlockSignals(true); publisher.EmitPacket([9]);
            Check(sum == 7, "The typed gateway preserves custom borrowed-span signatures");
        }
        BufferedSnapshot(); ResourceBatches(); RetainedService(); PatternPublishers();
        Console.WriteLine("Object signal blocking: publication, snapshots, connections, deferred acceptance, spans and disposal passed.");
    }

    private static void PatternPublishers()
    {
        using var viewport = new SubViewport(); var sizes = 0; viewport.SizeChanged += () => sizes++;
        viewport.SetBlockSignals(true); viewport.Size = new(64, 64);
        Check(sizes == 0 && viewport.Size == new Vector2i(64, 64), "A pattern-captured viewport event still commits its size while blocked");
        viewport.SetBlockSignals(false); viewport.Size = new(96, 96); Check(sizes == 1, "A viewport resumes its captured subscriber");
        using var graph = new AnimationNode(); var graphEvents = 0;
        graph.TreeChanged += () => graphEvents++; graph.AnimationNodeRemoved += (_, _) => graphEvents++;
        graph.AnimationNodeRenamed += (_, _, _) => graphEvents++; graph.NodeUpdated += _ => graphEvents++;
        graph.SetBlockSignals(true); PublishGraph(); Check(graphEvents == 0, "All graph pattern-captured event families are blocked");
        graph.SetBlockSignals(false); PublishGraph(); Check(graphEvents == 4, "All graph pattern-captured event families resume");
        void PublishGraph() { graph.EmitGraphChanged(); graph.Removed(1, "node"); graph.Renamed(1, "old", "new"); graph.Updated(1); }
        using var agent = new NavigationAgent(); var velocities = 0; agent.VelocityComputed += _ => velocities++;
        agent.SetBlockSignals(true); agent.DeliverVelocity(Vector2.One); Check(velocities == 0, "Pattern-captured avoidance delivery is blocked");
        agent.SetBlockSignals(false); agent.DeliverVelocity(Vector2.One); Check(velocities == 1, "Avoidance delivery resumes");
        using var window = new NotificationWindow(); var windowEvents = 0;
        window.FocusEntered += () => windowEvents++; window.FocusExited += () => windowEvents++;
        window.CloseRequested += () => windowEvents++; window.DpiChanged += () => windowEvents++;
        var dpi = typeof(Window).GetMethod("HandleDPIChanged", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic)!.CreateDelegate<Action>(window);
        window.SetBlockSignals(true); PublishWindow();
        Check(windowEvents == 0 && window.Notifications == 4, "Switch-selected window signals are blocked while all numeric notifications continue");
        window.SetBlockSignals(false); PublishWindow(); Check(windowEvents == 4 && window.Notifications == 8, "All switch-selected window families resume");
        void PublishWindow() { window.EmitEmbeddedFocus(true); window.EmitEmbeddedFocus(false); window.RequestEmbeddedClose(); dpi(); }
    }

    private static void BufferedSnapshot()
    {
        using var body = new RigidBody(); var calls = 0;
        body.SleepingStateChanged += _ => { calls++; body.SetBlockSignals(true); };
        body.SleepingStateChanged += _ => calls++;
        body.RaiseSleepingStateChanged(); body.RaiseSleepingStateChanged();
        Check(calls == 2, "A buffered event also selects once, before its first listener");
        body.SetBlockSignals(false);
        body.SleepingStateChanged += _ => throw new ArithmeticException();
        Throws<ArithmeticException>(body.RaiseSleepingStateChanged);
        body.RaiseSleepingStateChanged();
        Check(calls == 4, "A failed accepted event releases its lease and later blocked emissions remain silent");
    }

    private static void ResourceBatches()
    {
        using var source = new PhysicsMaterial { Friction = .25f, Bounce = .5f };
        using var target = new PhysicsMaterial(); var count = 0; target.Changed += _ => count++;
        var revision = target.ChangeRevision;
        target.SetBlockSignals(true); target.CopyFromResource(source);
        Check(count == 0 && target.Friction == .25f && target.Bounce == .5f && target.ChangeRevision > revision,
            "Blocked batched copies still commit state and advance resource revisions");
        target.SetBlockSignals(false); target.CopyFromResource(source);
        Check(count == 1, "An enabled resource copy still coalesces its Changed emission");
        using var parent = new Publisher(); using var child = new Publisher();
        var calls = 0; parent.Pulse += (_, _) => calls++; child.Pulse += (_, _) => calls++;
        parent.SetBlockSignals(true); parent.Raise(1); child.Raise(1);
        Check(calls == 1 && !child.IsBlockingSignals(), "Delivery policies belong to each publisher independently");
        Parallel.For(0, 256, i => { child.SetBlockSignals(i % 2 == 0); _ = child.IsBlockingSignals(); });
        child.SetBlockSignals(false); child.Raise(1); Check(calls == 2, "Atomic policy reads and writes remain usable after concurrent metadata access");
    }

    private static void RetainedService()
    {
        var service = Engine.GetSingleton("NavigationServer"); var old = service.IsBlockingSignals();
        var map = NavigationServer.MapCreate(); var count = 0;
        void Changed(RID rid) { if (rid == map) count++; }
        NavigationServer.MapChanged += Changed;
        try
        {
            service.SetBlockSignals(true); NavigationServer.MapSetEdgeConnectionMargin(map, 2); NavigationServer.Synchronize();
            Check(count == 0 && NavigationServer.MapGetEdgeConnectionMargin(map) == 2, "The retained service suppresses its static facade event after committing state");
            service.SetBlockSignals(false); NavigationServer.MapSetEdgeConnectionMargin(map, 3); NavigationServer.Synchronize();
            Check(count == 1, "A retained service resumes its existing static subscription without replay");
            var second = NavigationServer.MapCreate(); var batch = 0;
            void BlockNext(RID rid) { if (rid == map || rid == second) { batch++; service.SetBlockSignals(true); } }
            NavigationServer.MapChanged += BlockNext;
            try
            {
                NavigationServer.MapSetEdgeConnectionMargin(map, 4); NavigationServer.MapSetEdgeConnectionMargin(second, 4); NavigationServer.Synchronize();
                Check(batch == 1 && NavigationServer.MapGetEdgeConnectionMargin(map) == 4 && NavigationServer.MapGetEdgeConnectionMargin(second) == 4,
                    "A policy change in one map event suppresses the next emission in the same committed batch");
            }
            finally { NavigationServer.MapChanged -= BlockNext; NavigationServer.FreeRID(second); }
        }
        finally { service.SetBlockSignals(old); NavigationServer.MapChanged -= Changed; NavigationServer.FreeRID(map); }
    }

    private sealed class NotificationWindow : Window
    {
        internal int Notifications;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what is NotificationWmWindowFocusIn or NotificationWmWindowFocusOut or NotificationWmCloseRequest or NotificationWmDpiChange) Notifications++; }
    }

    private sealed class Publisher : ElectronObject
    {
        internal delegate void PacketHandler(Publisher source, ReadOnlySpan<byte> data);
        internal event Action<Publisher, int>? Pulse;
        internal event PacketHandler? Packet;
        internal bool RejectMutation;
        internal int PreDeleteCalls;
        internal void Raise(int value) { ThrowIfDisposed(); BeginEventEmission(Pulse)?.Invoke(this, value); }
        internal void EmitPacket(ReadOnlySpan<byte> value) { ThrowIfDisposed(); BeginEventEmission(Packet)?.Invoke(this, value); }
        protected override void ValidateMutation() { base.ValidateMutation(); if (RejectMutation) throw new InvalidOperationException("Fixture mutation rejected."); }
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationPreDelete) PreDeleteCalls++; }
    }

    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Throws<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}.");
    }
}
