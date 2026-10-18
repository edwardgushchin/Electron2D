using Electron2D;

internal static class PhysicsNodeTweenTests
{
    internal static void Run(Func<World> create, string label, bool threaded)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try
        {
            using var detached = new Node();
            Reject<InvalidOperationException>(() => detached.CreateTween());
            using (var loop = new EmptyLoop())
            {
                Engine.Start(loop);
                try { Reject<InvalidOperationException>(() => detached.CreateTween()); }
                finally { Engine.Stop(); }
            }
            using var world = create(); using var root = new SubViewport { World = world };
            using var shape = new CircleShape { Radius = 1 }; using var tree = new SceneTree(root);
            Reject<InvalidOperationException>(() => detached.CreateTween());
            Engine.Start(tree);
            try
            {
                DetachedBinding(tree, root, shape); TreeSelection(tree, root);
                FrameOrder(tree, root, world, shape); Failures(tree, root); WarmFrames(tree, root, world, shape);
                using var pending = detached.CreateTween(); pending.TweenInterval(1);
                Engine.Stop();
                Check(!pending.IsValid() && !pending.HasTweeners(), "Stopping the host invalidates detached-node work");
                Reject<InvalidOperationException>(() => detached.CreateTween());
            }
            finally { if (ReferenceEquals(Engine.MainLoop, tree)) Engine.Stop(); }
            Check(world.PhysicsBackendName == world.RequestedPhysicsBackendName && world.PhysicsFallbackReason is null,
                "The requested physics implementation executes without fallback");
            Console.WriteLine($"Node tween physics {label}/{threaded}: detached host selection, binding, ordering, errors and 128 full frames 0/0 B passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private static void DetachedBinding(SceneTree tree, SubViewport root, CircleShape shape)
    {
        foreach (var node in new Node[] { new Node(), new Entity(), new RigidBody { GravityScale = 0 } })
        {
            using (node)
            {
                if (node is RigidBody body) body.AddChild(new CollisionShape { Shape = shape });
                var value = 0d; var finished = 0;
                using var tween = node.CreateTween().SetProcessMode(ProcessPhase.Physics);
                tween.TweenMethod(v => value = v, 0d, 1d, 1d); tween.Finished += _ => finished++;
                Check(tween.IsValid() && tween.IsRunning() && tree.GetProcessedTweens().Contains(tween), "Detached creation registers running bound work");
                Engine.AdvanceFrame(.02); tree.PhysicsFrame(.25); Check(value == 0 && tween.GetTotalElapsedTime() == 0, "Detached binding suspends host and direct-frame elapsed time");
                root.AddChild(node); tree.ProcessFrame(.25); Check(value == 0, "Physics work ignores idle frames");
                tree.PhysicsFrame(.25); Near(value, .25, "Attachment resumes the bound physics tween");
                tree.Paused = true; tree.PhysicsFrame(.25); Near(value, .25, "Bound policy inherits pause");
                node.ProcessMode = ProcessMode.Always; tree.PhysicsFrame(.25); Near(value, .5, "Bound policy follows the node's Always mode");
                tree.Paused = false; root.RemoveChild(node); tree.PhysicsFrame(.25); Near(value, .5, "Detachment suspends rather than completes");
                root.AddChild(node); tree.PhysicsFrame(.25); Near(value, .75, "Reattachment retains progress");
                node.Dispose(); tree.PhysicsFrame(.25);
                Check(!tween.IsValid() && !tween.HasTweeners() && finished == 0, "Node disposal retires the bound tween without completion");
            }
        }
    }

    private static void TreeSelection(SceneTree main, SubViewport root)
    {
        using var foreignRoot = new Node(); using var foreign = new SceneTree(foreignRoot);
        using var node = new Node(); foreignRoot.AddChild(node); var value = 0;
        using var attached = node.CreateTween(); attached.TweenCallback(() => value++);
        Check(foreign.GetProcessedTweens().Contains(attached) && !main.GetProcessedTweens().Contains(attached), "An attached tree takes priority over the host loop");
        main.ProcessFrame(.1); Check(value == 0, "The main tree cannot advance another tree's tween");
        foreign.ProcessFrame(.1); Check(value == 1, "The actual owning tree advances its tween");
        foreignRoot.RemoveChild(node);
        using var migrated = node.CreateTween(); migrated.TweenCallback(() => value++);
        foreignRoot.AddChild(node); main.ProcessFrame(.1); Check(value == 1, "Foreign attachment does not transfer tween ownership");
        foreignRoot.RemoveChild(node); root.AddChild(node); main.ProcessFrame(.1);
        Check(value == 2, "A host-created tween resumes only in its selected tree");
        using var detached = new Entity(); var count = main.GetProcessedTweens().Count;
        Reject<InvalidOperationException>(() => Task.Run(() => detached.CreateTween()).GetAwaiter().GetResult());
        Reject<InvalidOperationException>(() => Task.Run(() => node.CreateTween()).GetAwaiter().GetResult());
        Check(main.GetProcessedTweens().Count == count, "Rejected foreign-thread calls publish no empty tween");
        detached.Dispose(); Reject<ObjectDisposedException>(() => detached.CreateTween());
    }

    private static void FrameOrder(SceneTree tree, SubViewport root, World world, CircleShape shape)
    {
        using var body = NewBody(shape); root.AddChild(body);
        var tick = PhysicsServer.SpaceGetTick(world.Space); var rid = body.GetRID(); var id = body.InstanceID;
        var order = new List<string>(); body.Frame = () => order.Add("node");
        using var timer = tree.CreateTimer(.001, processInPhysics: true); timer.Timeout += _ => order.Add("timer");
        using var tween = body.CreateTween().SetProcessMode(ProcessPhase.Physics);
        tween.TweenCallback(() =>
        {
            Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1 && body.Position.X > 0 && body.GetRID() == rid && body.InstanceID == id,
                "Tween callbacks see committed physical motion and stable identities");
            order.Add("tween"); tree.Defer(() => order.Add("deferred"));
        });
        tree.PhysicsFrame(.01);
        Check(order.SequenceEqual(new[] { "node", "timer", "tween", "deferred" }), "Physics nodes, world, timers, tweens and deferred work retain phase order");
        body.Frame = null; var childCalls = 0; Tween? child = null;
        using var parent = body.CreateTween(); parent.TweenCallback(() => { child = body.CreateTween(); child.TweenCallback(() => childCalls++); });
        tree.ProcessFrame(.01); Check(child is not null && childCalls == 0, "Tween-created work waits for the next captured batch");
        tree.ProcessFrame(.01); Check(childCalls == 1, "The next idle frame runs the newly registered tween"); child!.Dispose();
        var finished = 0;
        using var retiring = body.CreateTween().SetProcessMode(ProcessPhase.Physics); retiring.TweenInterval(100); retiring.Finished += _ => finished++;
        body.QueueFree(); tree.PhysicsFrame(.01); Check(body.IsDisposed, "Queued physical deletion commits after tween processing");
        tree.PhysicsFrame(.01); Check(!retiring.IsValid() && !retiring.HasTweeners() && finished == 0, "The next physics sweep retires deleted-node work");
    }

    private static void Failures(SceneTree tree, SubViewport root)
    {
        using var node = new Node(); root.AddChild(node);
        using var empty = node.CreateTween(); Reject<AggregateException>(() => tree.ProcessFrame(0));
        Check(!empty.IsValid() && !tree.GetProcessedTweens().Contains(empty), "An empty matching frame fails and retires the tween");
        var later = 0; var deferred = 0;
        using var failed = node.CreateTween(); failed.TweenCallback(static () => throw new InvalidOperationException("expected tween failure"));
        using var survivor = node.CreateTween(); survivor.TweenCallback(() => later++);
        tree.Defer(() => deferred++); Reject<AggregateException>(() => tree.ProcessFrame(.01));
        Check(!failed.IsValid() && later == 1 && deferred == 1, "Callback failure invalidates its tween while later phases finish");
    }

    private static void WarmFrames(SceneTree tree, SubViewport root, World world, CircleShape shape)
    {
        using var body = NewBody(shape); root.AddChild(body);
        var calls = 0; var ticks = PhysicsServer.SpaceGetTick(world.Space); var rid = body.GetRID(); var id = body.InstanceID;
        using var tween = body.CreateTween().SetProcessMode(ProcessPhase.Physics).SetLoops();
        tween.TweenMethod(value =>
        {
            body.LinearVelocity = new(value, 0); calls++;
            Check(PhysicsServer.SpaceGetTick(world.Space) == ++ticks && body.GetRID() == rid && body.InstanceID == id,
                "Every tween update follows exactly one complete world step");
        }, 2f, 4f, 1000d);
        for (var i = 0; i < 256; i++) { tree.PhysicsFrame(.01); tree.ProcessFrame(.01); }
        GC.Collect(); GC.WaitForPendingFinalizers(); GC.Collect(); GC.WaitForPendingFinalizers();
        for (var i = 0; i < 128; i++) { tree.PhysicsFrame(.01); tree.ProcessFrame(.01); }
        var before = calls; var position = body.Position.X;
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) { tree.PhysicsFrame(.01); tree.ProcessFrame(.01); }
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(calls == before + 128 && body.Position.X > position && owner == 0 && all == 0, $"Warmed moving physics/tween frames allocate {owner}/{all} B");
        Console.WriteLine($"Node tween full frames: 128 measured physics+idle pairs, {owner}/{all} B.");
    }

    private static Body NewBody(CircleShape shape)
    {
        var body = new Body { GravityScale = 0, CanSleep = false, LinearVelocity = new(2, 0), LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0 };
        body.AddChild(new CollisionShape { Shape = shape }); return body;
    }
    private sealed class Body : RigidBody
    {
        internal Action? Frame;
        protected override void OnPhysicsProcess(double delta) => Frame?.Invoke();
    }
    private sealed class EmptyLoop : MainLoop { }
    private static void Near(double actual, double expected, string message) => Check(Math.Abs(actual - expected) < 1e-10, message);
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception
    {
        try { action(); } catch (T) { return; }
        throw new InvalidOperationException($"Expected {typeof(T).Name}");
    }
}
