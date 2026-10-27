using System.Runtime.CompilerServices;
using Electron2D;

internal static class NotificationChecks
{
    private const int Adjust = 47001, Trace = 47002, Nested = 47003, ConcurrentNormal = 47004, ConcurrentReverse = 47005;
    internal static void Run(PhysicsServer.Backend backend, bool threaded)
    {
        VerifyConstruction(); VerifyCalls(); VerifyConcurrency(); VerifyLifetime(); VerifyBuiltInGUI();
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        try { VerifyPhysicalFrames(backend); }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
        Console.WriteLine($"Notification direction: {backend}/{threaded}, nested/error/lifetime/concurrent/GUI and complete physics passed.");
    }

    private static void VerifyConstruction()
    {
        using var source = new ConstructionNode();
        var identity = source.InstanceID;
        Check(source.Constructed && source.PostInitializations == 0 && source.Initializations == 1 && source.ConstructorIdentity == identity, "Construction initializes once before any project notification override");
        source.Notify(ElectronObject.NotificationPostInitialize);
        source.Notify(ElectronObject.NotificationPostInitialize, true);
        Check(source.PostInitializations == 2 && source.LastDirection && source.InstanceID == identity,
            "Explicit zero notifications use the completed object and ordinary directional dispatch");

        using var duplicate = (ConstructionNode)source.Duplicate();
        Check(duplicate.Constructed && duplicate.PostInitializations == 0 && duplicate.InstanceID != identity && duplicate.Initializations == 1 && duplicate.ConstructorIdentity == duplicate.InstanceID,
            "Duplication constructs a fresh exact project type without synthesizing a project initialization callback");
        using var packed = new PackedScene(); packed.Pack(source);
        using var restored = (ConstructionNode)packed.Instantiate();
        Check(restored.Constructed && restored.PostInitializations == 0 && restored.InstanceID != identity && restored.Initializations == 1 && restored.ConstructorIdentity == restored.InstanceID,
            "PackedScene construction keeps the same compiled callback boundary");
        restored.Notify(ElectronObject.NotificationPostInitialize);
        Check(restored.PostInitializations == 1 && !restored.LastDirection, "A restored object's explicit callback executes normally");

        using var resource = new Probe();
        Check(resource.Constructed && resource.PostInitializations == 0, "Resource construction has the same project callback boundary");
        var error = new ApplicationException("explicit-initialization");
        resource.Callback = (_, what) => { if (what == ElectronObject.NotificationPostInitialize) throw error; };
        try { resource.Notify(ElectronObject.NotificationPostInitialize, true); throw new InvalidOperationException("Missing handler failure"); }
        catch (ApplicationException actual) { Check(ReferenceEquals(actual, error), "An explicit zero callback preserves its original exception"); }
        Check(resource.PostInitializations == 1 && !resource.Direction && !resource.IsDisposed,
            "A failed explicit callback restores context without changing object lifetime");
        resource.Callback = null;
        Console.WriteLine("Compiled construction: direct Node/Resource, duplicate, PackedScene and explicit zero callback boundaries passed.");
    }

    private static void VerifyCalls()
    {
        using var other = new Probe(); using var probe = new Probe();
        var error = new ApplicationException("notification-original");
        var calls = 0;
        probe.Callback = (self, what) =>
        {
            calls++;
            if (what == Nested) { Check(!self.Direction, "A same-object nested normal call has its own direction"); return; }
            Check(self.Direction && !other.Direction, "Only the active object has the reversed context");
            self.Notify(Nested, false);
            Check(self.Direction, "Nested normal delivery restores the outer reversed call");
            other.Callback = (target, _) => Check(!target.Direction && !self.Direction, "A nested other-object context is isolated");
            other.Notify(Trace);
            Check(self.Direction, "Another object cannot consume the outer direction");
            Task.Run(() => Check(!self.Direction, "Notification context does not flow through ExecutionContext")).GetAwaiter().GetResult();
            throw error;
        };
        try { probe.Notify(Trace, true); throw new InvalidOperationException("Missing handler failure"); }
        catch (ApplicationException actual) { Check(ReferenceEquals(actual, error), "The exact original exception escapes"); }
        Check(calls == 2 && !probe.Direction, "A failed outer call releases the context");
        probe.Callback = (self, _) => Check(!self.Direction, "The existing overload keeps its normal direction");
        probe.SetBlockSignals(true); probe.Notify(Trace); probe.SetBlockSignals(false);
        probe.Callback = null; other.Callback = null;
    }

    private static void VerifyConcurrency()
    {
        using var probe = new Probe(); using var barrier = new Barrier(2);
        var visits = 0;
        probe.Callback = (self, what) =>
        {
            Check(barrier.SignalAndWait(10000), "Concurrent notification rendezvous");
            Check(self.Direction == (what == ConcurrentReverse), "The same object's concurrent calls retain independent directions");
            Interlocked.Increment(ref visits);
            Check(barrier.SignalAndWait(10000), "Concurrent notification release");
        };
        Task.WaitAll(Task.Run(() => probe.Notify(ConcurrentNormal, false)), Task.Run(() => probe.Notify(ConcurrentReverse, true)));
        Check(visits == 2 && !probe.Direction, "Both concurrent calls finish without ambient state"); probe.Callback = null;
    }

    private static void VerifyLifetime()
    {
        var probe = new Probe(); var count = 0;
        probe.Callback = (self, what) =>
        {
            if (what != ElectronObject.NotificationPreDelete) return;
            Check(self.Direction, "Automatic pre-delete uses reversed delivery");
            if (++count == 1) self.CancelDisposal();
        };
        probe.Dispose(); Check(!probe.IsDisposed && !probe.Direction, "A cancelled pre-delete restores direction and live state");
        probe.Dispose(); Check(probe.IsDisposed && count == 2 && !probe.Direction, "A later disposal completes exactly once");
        Reject<ObjectDisposedException>(() => probe.Notify(Trace, true));
        var weak = CreateFailedCall(); Collect(); Check(!weak.IsAlive, "A completed failing call retains no object in thread context");
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference CreateFailedCall()
    {
        var target = new Probe { Callback = static (_, what) => { if (what == Trace) throw new ApplicationException("ephemeral"); } };
        Reject<ApplicationException>(() => target.Notify(Trace, true)); target.Dispose(); return new(target);
    }

    private static void VerifyBuiltInGUI()
    {
        using var font = new CountingFont(); font.LoadDynamicFont(System.IO.Path.Combine(AppContext.BaseDirectory, "Assets/IBMPlexSans-Regular.ttf"));
        using var root = new SubViewport { Size = new(240, 160) };
        var label = new Label("direction") { Size = new(120, 40) };
        label.AddThemeFontOverride("font", font); root.AddChild(label);
        using var tree = new SceneTree(root); tree.ProcessFrame(0);
        var observed = -1;
        label.Resized += () => { var before = font.SpacingCalls; _ = label.GetLineCount(); observed = font.SpacingCalls - before; };
        _ = label.GetLineCount(); font.SpacingCalls = 0;
        label.Notify(Control.NotificationResized, false);
        Check(observed == 0, "Normal Control.Resized observes the already prepared Label layout before invalidation");
        _ = label.GetLineCount(); font.SpacingCalls = 0;
        label.Notify(Control.NotificationResized, true);
        Check(observed > 0, "Reversed Label invalidation precedes the actual ancestor Control.Resized event");
    }

    private static void VerifyPhysicalFrames(PhysicsServer.Backend backend)
    {
        using var world = new World(backend); using var shape = new CircleShape { Radius = 1 };
        using var root = new SubViewport { World = world };
        var body = new LeafBody
        {
            GravityScale = 0,
            CanSleep = false,
            CollisionLayer = 0,
            CollisionMask = 0,
            LinearDampMode = RigidBody.DampMode.Replace,
            LinearDamp = 0
        };
        body.AddChild(new CollisionShape { Shape = shape }); root.AddChild(body);
        using var tree = new SceneTree(root);
        var identity = body.GetRID(); var trace = new List<int>(); body.TraceCalls = trace;
        body.Notify(Node.NotificationPhysicsProcess);
        Check(trace.SequenceEqual([0, 1, 2]), "Ordinary callbacks traverse the actual Node hook then both compiled body layers");
        trace.Clear(); body.Notify(Node.NotificationPhysicsProcess, true);
        Check(trace.SequenceEqual([2, 1, 0]), "Reversed callbacks traverse both compiled body layers then the actual Node hook");
        body.ThrowOnTrace = true;
        trace.Clear(); Reject<ApplicationException>(() => body.Notify(Node.NotificationPhysicsProcess, true));
        Check(trace.SequenceEqual([2]) && !body.Direction, "An unhandled reversed error skips unvisited ancestors and restores context");
        trace.Clear(); Reject<ApplicationException>(() => body.Notify(Node.NotificationPhysicsProcess, false));
        Check(trace.SequenceEqual([0, 1, 2]) && !body.Direction, "An ordinary error preserves already delivered ancestors");
        body.ThrowOnTrace = false; body.TraceCalls = null;
        Check(world.PhysicsBackend == backend && world.PhysicsFallbackReason is null, "Actual explicitly selected physical backend");
        for (var i = 0; i < 128; i++) Frame(i);
        Collect(); var tick = PhysicsServer.SpaceGetTick(world.Space); var calls = body.Adjustments;
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame(i);
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"Notification-active full frames allocate {owner}/{all} B");
        Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 128 && body.Adjustments == calls + 256, "All measured notifications control actual advancing physical intervals");
        Console.WriteLine($"Notification-active {backend}: 128 complete frames, owner/all={owner}/{all} B.");
        body.LinearVelocity = Vector2.Zero;
        var idlePosition = body.Position;
        for (var i = 0; i < 128; i++) IdleFrame(i);
        Collect(); tick = PhysicsServer.SpaceGetTick(world.Space);
        owner = GC.GetAllocatedBytesForCurrentThread(); all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) IdleFrame(i);
        owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
        Check(owner == 0 && all == 0, $"Idle notification full frames allocate {owner}/{all} B");
        Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 128, "All idle intervals advance the actual physical space");
        Console.WriteLine($"Notification-idle {backend}: 128 complete frames, owner/all={owner}/{all} B.");
        void IdleFrame(int i)
        {
            Check(body.GetScript() is null, "A plain compiled physical owner has no implicit source asset");
            body.Notify(Node.NotificationPhysicsProcess, (i & 1) != 0); tree.PhysicsFrame(.01);
            Check(body.GetRID() == identity && body.Position == idlePosition && !body.Direction,
                "Idle directional callbacks preserve exact RID/pose and release invocation context");
        }

        void Frame(int i)
        {
            Check(body.GetScript() is null, "Prepared source inspection requires no native or metadata discovery");
            var reversed = (i & 1) != 0; body.LinearVelocity = new(1, 0); var position = body.Position.X;
            body.Notify(Adjust, reversed); tree.PhysicsFrame(.01);
            var expected = reversed ? 4 : 3;
            Check(body.GetRID() == identity && MathF.Abs(body.LinearVelocity.X - expected) < .0001f &&
                MathF.Abs(body.Position.X - position - expected * .01f) < .0001f, "Ordered notification mutations preserve RID, velocity and complete physical motion");
            Check(!body.Direction, "A completed physical frame retains no notification context");
        }
    }

    private class BaseBody : RigidBody
    {
        internal List<int>? TraceCalls;
        internal int Adjustments;
        internal bool Direction => IsNotificationReversed;
        protected override void OnNotification(int what)
        {
            var reverse = IsNotificationReversed;
            if (!reverse) base.OnNotification(what);
            if (what == NotificationPhysicsProcess) TraceCalls?.Add(1);
            if (what == Adjust) { LinearVelocity = new(LinearVelocity.X * 2, 0); Adjustments++; }
            if (reverse) base.OnNotification(what);
        }
        protected override void OnPhysicsProcess(double delta) { base.OnPhysicsProcess(delta); TraceCalls?.Add(0); }
    }
    private sealed class LeafBody : BaseBody
    {
        internal bool ThrowOnTrace;
        protected override void OnNotification(int what)
        {
            var reverse = IsNotificationReversed;
            if (!reverse) base.OnNotification(what);
            if (what == NotificationPhysicsProcess) TraceCalls?.Add(2);
            if (what == NotificationPhysicsProcess && ThrowOnTrace) throw new ApplicationException("ordered-leaf");
            if (what == Adjust) { LinearVelocity = new(LinearVelocity.X + 1, 0); Adjustments++; }
            if (reverse) base.OnNotification(what);
        }
    }
    private sealed class ConstructionNode : Node
    {
        internal bool Constructed;
        internal readonly ulong ConstructorIdentity;
        internal int Initializations;
        internal int PostInitializations;
        internal bool LastDirection;
        public ConstructionNode() { Initializations++; ConstructorIdentity = InstanceID; Constructed = true; }
        protected override Func<Node> CreateSceneInstanceFactory() => Create;
        private static Node Create() => new ConstructionNode();
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != NotificationPostInitialize) return;
            Check(Constructed, "Project callbacks cannot observe an incomplete constructor");
            PostInitializations++; LastDirection = IsNotificationReversed;
        }
    }
    private sealed class Probe : Resource
    {
        internal bool Constructed;
        internal int PostInitializations;
        internal Action<Probe, int>? Callback;
        internal bool Direction => IsNotificationReversed;
        public Probe() { Constructed = true; }
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationPostInitialize) { Check(Constructed, "Resource callbacks require completed construction"); PostInitializations++; }
            Callback?.Invoke(this, what);
        }
    }
    private sealed class CountingFont : FontFile
    {
        internal int SpacingCalls;
        public override int GetSpacing(TextSpacingType spacing) { SpacingCalls++; return base.GetSpacing(spacing); }
    }
    private static void Collect() { GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, true, true); GC.WaitForPendingFinalizers(); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
