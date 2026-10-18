using Electron2D;

public static partial class BackendChecks
{
    private sealed class PropertyListBody : RigidBody
    {
        internal bool ShowValue = true, GroupPublishing;
        internal int Value;
        internal static readonly PropertyDescriptor<PropertyListBody, int> ValueProperty = new("Value", n => n.Value, (n, v) => n.Value = v, _ => 0);
        protected override IEnumerable<PropertyDescriptor> GetPropertyDescriptors() => base.GetPropertyDescriptors().Append(ValueProperty);
        protected override PropertyDescriptor? ValidateProperty(PropertyDescriptor property) => ReferenceEquals(property, ValueProperty) && !ShowValue ? null : base.ValidateProperty(property);
    }
    private sealed class PropertyListLifetimeNode : Node
    {
        internal Action? DuringDelete;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == 1) DuringDelete?.Invoke(); }
    }
    public static void RunPropertyList(PhysicsServer.Backend? backend, bool threaded, bool poolOwner = false)
    {
        var previousThreads = ProjectSettings.Get(ProjectSettings.WorkerPoolMaxThreads); var previousSolver = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, 2); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("PropertyList", () => new Solver());
        try { if (poolOwner) { var task = WorkerThreadPool.AddTask(Run); WorkerThreadPool.WaitForTaskCompletion(task); } else Run(); }
        finally { ProjectSettings.Set(ProjectSettings.WorkerPoolMaxThreads, previousThreads); ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previousSolver); }
        void Run()
        {
            foreach (var value in new ElectronObject[] { new Node(), new RigidBody(), new Area(), new CircleShape(), new PhysicsMaterial() })
            {
                var calls = 0; var changes = 0; Action<ElectronObject> listen = sender => { Check(ReferenceEquals(sender, value), "Public metadata sender identity"); calls++; };
                value.PropertyListChanged += listen; if (value is Resource resource) resource.Changed += _ => changes++;
                value.NotifyPropertyListChanged(); value.SetBlockSignals(true); value.NotifyPropertyListChanged(); Check(calls == 1, "Blocking suppresses the public metadata publisher");
                value.SetBlockSignals(false); value.PropertyListChanged += listen; value.NotifyPropertyListChanged(); Check(calls == 3, "Repeated typed subscriptions retain their ordinary delivery");
                value.PropertyListChanged -= listen; value.NotifyPropertyListChanged(); Check(calls == 4 && changes == 0, "Metadata notification does not emit resource Changed");
                value.Dispose(); Reject<ObjectDisposedException>(value.NotifyPropertyListChanged);
            }
            using (var lifetime = new PropertyListLifetimeNode())
            {
                var delivered = 0; var observedThread = 0; lifetime.PropertyListChanged += _ => { delivered++; observedThread = Environment.CurrentManagedThreadId; };
                var caller = 0; Task.Run(() => { caller = Environment.CurrentManagedThreadId; lifetime.NotifyPropertyListChanged(); }).GetAwaiter().GetResult();
                Check(delivered == 1 && observedThread == caller, "Pure public metadata emission stays on the notifying thread");
                lifetime.DuringDelete = () => { lifetime.NotifyPropertyListChanged(); Task.Run(() => Reject<ObjectDisposedException>(lifetime.NotifyPropertyListChanged)).GetAwaiter().GetResult(); };
                lifetime.Dispose(); Check(delivered == 2, "Exact teardown-thread metadata inspection remains available");
            }
            using var world = backend is { } selected ? new World(selected) : new World("PropertyList"); using var root = new SubViewport { World = world };
            using var shape = new CircleShape { Radius = 2 }; var body = new PropertyListBody
            {
                CanSleep = false,
                GravityScale = 0,
                LinearDampMode = RigidBody.DampMode.Replace,
                LinearDamp = 0,
                LinearVelocity = new(1, 0),
                CollisionMask = 0,
                ProcessThreadGroup = Node.ProcessThreadGroupMode.SubThread,
                ProcessThreadMessages = Node.ProcessThreadMessageFlags.Physics
            };
            body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body); using var tree = new SceneTree(root);
            var original = body.GetPropertyList(); var notifications = 0; var grouped = 0;
            Action forbidden = () => body.LinearVelocity = Vector2.Zero; var cold = true;
            body.PropertyListChanged += sender => { Check(ReferenceEquals(sender, body), "Physical publisher keeps object identity"); notifications++; if (body.GroupPublishing) { grouped++; if (cold) Reject<InvalidOperationException>(forbidden); } };
            body.ShowValue = false; var hidden = false;
            Action<ElectronObject> inspect = _ => hidden = !body.GetPropertyList().Contains(PropertyListBody.ValueProperty);
            body.PropertyListChanged += inspect; body.NotifyPropertyListChanged(); body.PropertyListChanged -= inspect;
            Check(hidden && original.Contains(PropertyListBody.ValueProperty), "A metadata observer discovers the changed list without mutating its earlier snapshot");
            var pending = new Queue<Action>(); var deferred = 0;
            using (var once = EventConnection.Subscribe<ElectronObject>(h => body.PropertyListChanged += h, h => body.PropertyListChanged -= h, _ => deferred++, oneShot: true, defer: pending.Enqueue))
            {
                body.NotifyPropertyListChanged(); body.SetBlockSignals(true); body.NotifyPropertyListChanged();
                Check(!once.IsConnected && deferred == 0 && pending.Count == 1, "One-shot metadata delivery captures one accepted emission");
                pending.Dequeue()(); Check(deferred == 1, "Later blocking preserves an already accepted deferred metadata event"); body.SetBlockSignals(false);
            }
            var error = new IOException("property-list-callback"); var rid = body.GetRID(); var tick = PhysicsServer.SpaceGetTick(world.Space); var pose = body.GlobalTransform;
            Action<ElectronObject> fail = _ => throw error; body.PropertyListChanged += fail;
            Check(ReleaseErrors(body.NotifyPropertyListChanged).Any(e => ReferenceEquals(e, error)), "Public metadata callback preserves its original error"); body.PropertyListChanged -= fail;
            Check(body.GetRID() == rid && body.GlobalTransform == pose && PhysicsServer.SpaceGetTick(world.Space) == tick, "A failed metadata callback does not alter the numerical world");
            using var query = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
            Frame(); Check(grouped == 1, "The public publisher executes in the queued physical group without numerical authoring rights"); cold = false;
            for (var i = 0; i < 128; i++) Frame(); Collect(); for (var i = 0; i < 128; i++) Frame();
            var startPosition = body.GlobalPosition; tick = PhysicsServer.SpaceGetTick(world.Space); var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame(); own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && notifications > 768,
                $"Public property-list metadata plus 128 full physics/query frames allocated {own}/{all} B");
            var travel = body.GlobalPosition - startPosition; Check(MathF.Abs(travel.X - 1.28f) < .001f && MathF.Abs(travel.Y) < .001f, $"128 undamped 0.01-second physical steps preserve one-unit-per-second motion through metadata delivery: travel={travel}, velocity={body.LinearVelocity}");
            Check(world.RequestedPhysicsBackendName == world.PhysicsBackendName && world.PhysicsFallbackReason is null, "Metadata events retain the selected real numerical backend");
            Console.WriteLine($"Property-list publisher {backend?.ToString() ?? "registered"}/{threaded}/{poolOwner}: public lifetime, discovery, events and 128 moving full frames {own}/{all} B passed. Travel={travel}.");
            void Frame()
            {
                body.CallDeferredThreadGroup<PropertyListBody>(static n => { n.GroupPublishing = true; try { n.NotifyPropertyListChanged(); } finally { n.GroupPublishing = false; } }); tree.PhysicsFrame(.01); body.NotifyPropertyListChanged(); shape.NotifyPropertyListChanged();
                query.Position = body.GlobalPosition; Check(world.DirectSpaceState.IntersectPoint(query, hits) == 1 && hits[0].ColliderRID == rid, "Metadata publication retains the real moving collider and query identity");
            }
        }
    }
}
