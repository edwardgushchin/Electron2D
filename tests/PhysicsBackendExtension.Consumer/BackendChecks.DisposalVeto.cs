using Electron2D;

public static partial class BackendChecks
{
    public static void RunDisposalVeto(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var previous = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        if (backend is null) PhysicsServerManager.RegisterServer("DisposalVeto", static () => new Solver());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("DisposalVeto");
            Check(world.PhysicsBackendName == (backend?.ToString() ?? "DisposalVeto") && world.PhysicsFallbackReason is null, "Veto checks select the requested world without fallback");
            DisposalVetoCore(); DisposalVetoRegistrations(world); DisposalVetoConcurrency(); DisposalVetoHierarchy(world); DisposalVetoTreeRoot(world); DisposalVetoQueues(world); DisposalVetoFrames(world); DisposalVetoProvider(world.PhysicsBackendName);
            Console.WriteLine($"Disposal veto {world.PhysicsBackendName}/{(threaded ? "separate" : "owner")}: automatic window, identity/subscriptions, concurrency, hierarchy, queues, provider and warm full frames passed.");
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, previous); }
    }

    private sealed class VetoProbe : ElectronObject
    {
        internal bool Veto = true, FailNotification, CancelDuringCleanup;
        internal int Notifications, Cleanups;
        internal Action<VetoProbe>? DuringNotification;
        internal void Refresh() => NotifyPropertyListChanged();
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what != NotificationPreDelete) return;
            Notifications++;
            if (Veto) { CancelDisposal(); CancelDisposal(); }
            DuringNotification?.Invoke(this);
            if (FailNotification) throw new ArithmeticException("Pre-delete failure");
        }
        protected override void Dispose(bool disposing)
        {
            Cleanups++; if (CancelDuringCleanup) CancelDisposal(); base.Dispose(disposing);
        }
    }

    private sealed class VetoAsset : Resource
    {
        internal bool Veto = true;
        internal int Cleanups;
        internal Action? During;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationPreDelete) { if (Veto) CancelDisposal(); During?.Invoke(); } }
        protected override void Dispose(bool disposing) { Cleanups++; base.Dispose(disposing); }
    }

    private sealed class VetoBody : RigidBody
    {
        internal bool Veto = true, RejectValidation, FailUnparent, FailBeforeBase;
        internal int Notifications, Cleanups, Ticks, Integrations, Unparents;
        internal Action? Unparent, During;
        protected override void ValidateDisposal() { base.ValidateDisposal(); if (RejectValidation) throw new InvalidOperationException("Rejected child disposal"); }
        protected override void OnNotification(int what)
        {
            base.OnNotification(what);
            if (what == NotificationPreDelete) { Notifications++; if (Veto) CancelDisposal(); During?.Invoke(); }
            if (what == NotificationUnparented) { Unparents++; Unparent?.Invoke(); if (FailUnparent) throw new InvalidOperationException("Unparent callback failure"); }
        }
        protected override void Dispose(bool disposing) { Cleanups++; if (FailBeforeBase) throw new InvalidOperationException("Derived cleanup failure"); base.Dispose(disposing); }
        protected override void OnPhysicsProcess(double delta) { base.OnPhysicsProcess(delta); Ticks++; }
        protected override void IntegrateForces(PhysicsDirectBodyState state) { base.IntegrateForces(state); Integrations++; }
    }

    private sealed partial class Solver
    {
        internal bool VetoDisposal;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationPreDelete && VetoDisposal) CancelDisposal(); }
    }

    private static VetoBody CreateVetoBody(CircleShape circle)
    {
        var body = new VetoBody { GravityScale = 0, CanSleep = false, LinearDampMode = RigidBody.DampMode.Replace, LinearDamp = 0, LinearVelocity = new(1, 0) };
        body.AddChild(new CollisionShape { Shape = circle }); return body;
    }

    private static void DisposalVetoCore()
    {
        var probe = new VetoProbe { CanTranslateMessages = false, TranslationDomain = "veto" }; using var weak = new WeakRef<VetoProbe>(probe);
        var id = probe.InstanceID; var disposed = 0; var changes = 0;
        probe.Disposed += _ => disposed++; probe.PropertyListChanged += _ => changes++;
        WeakRef<VetoProbe>? createdDuringAttempt = null;
        probe.DuringNotification = value =>
        {
            createdDuringAttempt = new WeakRef<VetoProbe>(value);
            Check(value.IsDisposed && weak.GetRef() is null && !value.CanTranslateMessages && value.TranslationDomain == "veto", "The exact teardown thread can inspect state while identity remains closed during an active vetoed notification");
        };
        probe.Dispose(); probe.Refresh();
        Check(!probe.IsDisposed && probe.Cleanups == 0 && disposed == 0 && changes == 1 && ReferenceEquals(weak.GetRef(), probe) && probe.InstanceID == id, "A veto restores the same identity, skips cleanup and preserves subscriptions and properties");
        Check(createdDuringAttempt is not null && createdDuringAttempt.GetRef() is null, "A weak wrapper sampled while IsDisposed stays empty after the attempt is vetoed"); createdDuringAttempt!.Dispose();
        probe.DuringNotification = null; probe.Veto = false; probe.CancelDisposal(); probe.Notify(ElectronObject.NotificationPreDelete); probe.CancelDisposal();
        probe.Dispose(); probe.CancelDisposal(); probe.Dispose();
        Check(probe.IsDisposed && probe.Cleanups == 1 && disposed == 1 && weak.GetRef() is null, "Outside-window/manual calls cannot arm a future veto or revive terminal objects");
        var manual = new VetoProbe(); manual.Notify(ElectronObject.NotificationPreDelete); manual.Veto = false; manual.Dispose();
        Check(manual.IsDisposed && manual.Cleanups == 1, "A cancellation requested by manual pre-delete delivery does not affect the next automatic attempt");
        var blocked = new VetoProbe(); blocked.SetBlockSignals(true); blocked.Dispose();
        Check(!blocked.IsDisposed && blocked.IsBlockingSignals() && blocked.Cleanups == 0, "Signal blocking cannot suppress automatic pre-delete veto and its policy survives the attempt");
        blocked.Veto = false; blocked.Dispose();
        var late = new VetoProbe { Veto = false, CancelDuringCleanup = true }; late.Dispose();
        Check(late.IsDisposed && late.Cleanups == 1, "Cleanup cannot be canceled after the notification window closes");
        var failing = new VetoProbe { FailNotification = true }; var rejected = false;
        try { failing.Dispose(); } catch (ArithmeticException) { rejected = true; }
        Check(rejected && !failing.IsDisposed && failing.Cleanups == 0, "Notification failure propagates after restoring a vetoed object without forcing cleanup");
        failing.Veto = false; failing.FailNotification = false; failing.Dispose(); Check(failing.IsDisposed && failing.Cleanups == 1, "A failed vetoed attempt can be retried normally");
        using var resource = new VetoAsset(); var resourceID = resource.InstanceID; resource.Dispose();
        Check(!resource.IsDisposed && resource.InstanceID == resourceID && resource.Cleanups == 0, "Resource subclasses inherit the same veto contract"); resource.Veto = false; resource.Dispose();
    }

    private sealed class VetoButton : Button
    {
        internal bool Veto = true;
        internal Action? During;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationPreDelete) { if (Veto) CancelDisposal(); During?.Invoke(); } }
    }
    private sealed class VetoFold : FoldableContainer
    {
        internal bool Veto = true;
        internal Action? During;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationPreDelete) { if (Veto) CancelDisposal(); During?.Invoke(); } }
    }
    private sealed class VetoWriter : MovieWriter
    {
        internal bool Veto = true;
        internal Action? During;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationPreDelete) { if (Veto) CancelDisposal(); During?.Invoke(); } }
        protected override string[] OnGetSupportedExtensions() => ["vetotest"];
        protected override bool OnHandlesFile(string path) => false;
        protected override void OnWriteBegin(Vector2i movieSize, int fps, string basePath) { }
        protected override void OnWriteFrame(Image frameImage, ReadOnlySpan<int> audioFrameBlock) { }
        protected override void OnWriteEnd() { }
    }

    private static void DisposalVetoRegistrations(World world)
    {
        using (var root = new Node())
        {
            var a = new Node(); var b = new Node(); root.AddChild(a); root.AddChild(b);
            var target = new VetoRoot { Name = "Target" }; var other = new Node { Name = "Target" }; a.AddChild(target); b.AddChild(other);
            target.Owner = root; target.UniqueNameInOwner = true;
            target.During = () =>
            {
                Check(root.GetNodeOrNull("%Target") is null, "Unique lookup hides a temporarily closed target");
                other.Owner = root; other.UniqueNameInOwner = true;
                Check(!other.UniqueNameInOwner, "A temporarily closed unique-name reservation cannot be stolen");
            };
            target.Dispose();
            Check(!target.IsDisposed && ReferenceEquals(target.Owner, root) && ReferenceEquals(root.GetNodeOrNull("%Target"), target), "Vetoed identity retains forward/reverse ownership and its unique registration after another authoring write");
            target.During = null; target.Veto = false;
        }
        var owner = new VetoRoot(); var firstOwned = new Node { Name = "Reserved" }; var otherOwned = new Node { Name = "Reserved" };
        owner.AddChild(firstOwned); firstOwned.Owner = owner; firstOwned.UniqueNameInOwner = true; owner.RemoveChild(firstOwned);
        owner.AddChild(otherOwned); otherOwned.Owner = owner; owner.RemoveChild(otherOwned);
        owner.During = () => { otherOwned.UniqueNameInOwner = true; Check(!otherOwned.UniqueNameInOwner, "A temporarily closed authoring owner still reserves its existing unique namespace"); };
        owner.Dispose(); Check(ReferenceEquals(owner.GetNodeOrNull("%Reserved"), firstOwned) && ReferenceEquals(firstOwned.Owner, owner), "A vetoed detached owner restores the same borrowed unique namespace");
        owner.During = null; owner.Veto = false; owner.Dispose(); firstOwned.Dispose(); otherOwned.Dispose();
        var path = "res://disposal-veto-cache.tres"; var asset = new VetoAsset { ResourcePath = path }; using var otherAsset = new Resource();
        asset.During = () =>
        {
            Check(ResourceLoader.GetCachedRef<Resource>(path) is null, "Cache lookup hides a temporarily closed resource");
            var reserved = false; try { otherAsset.ResourcePath = path; } catch (InvalidOperationException) { reserved = true; }
            Check(reserved, "A pre-delete path reservation cannot be stolen by ordinary assignment");
        };
        asset.Dispose(); Check(ReferenceEquals(ResourceLoader.GetCachedRef<Resource>(path), asset) && asset.ResourcePath == path, "Cache identity survives temporary closure and a rejected competing assignment");
        asset.During = null; asset.Veto = false; asset.Dispose();
        using (var group = new ButtonGroup())
        {
            var button = new VetoButton { ButtonGroup = group };
            button.During = () => Check(group.GetButtons().Length == 0, "Button snapshots hide but retain a temporarily closed member");
            button.Dispose(); Check(group.GetButtons().Length == 1 && ReferenceEquals(group.GetButtons()[0], button), "Button membership returns after veto without a registration rewrite");
            button.During = null; button.Veto = false; button.Dispose();
        }
        using (var group = new FoldableGroup())
        {
            var fold = new VetoFold { FoldableGroup = group };
            fold.During = () => Check(group.GetContainers().Length == 0, "Fold snapshots hide but retain a temporarily closed member");
            fold.Dispose(); Check(group.GetContainers().Length == 1 && ReferenceEquals(group.GetContainers()[0], fold), "Fold membership returns after veto");
            fold.During = null; fold.Veto = false; fold.Dispose();
        }
        var writer = new VetoWriter(); var otherWriter = new VetoWriter(); MovieWriter.AddWriter(writer);
        writer.During = () => MovieWriter.AddWriter(otherWriter); writer.Dispose(); var duplicate = false;
        try { MovieWriter.AddWriter(writer); } catch (InvalidOperationException) { duplicate = true; }
        Check(duplicate, "Registering another writer cannot sweep a vetoed writer's registration");
        writer.During = null; writer.Veto = false; otherWriter.Veto = false; writer.Dispose(); otherWriter.Dispose();
        using var circle = new CircleShape { Radius = 1 }; using var viewport = new SubViewport { World = world }; using var tree = new SceneTree(viewport);
        var body = CreateVetoBody(circle); viewport.AddChild(body); var rid = body.GetRID();
        using var point = new PhysicsPointQueryParameters { Position = body.GlobalPosition }; var hits = new PhysicsPointResult[1];
        body.During = () =>
        {
            try { _ = world.DirectSpaceState.IntersectPoint(point, hits); }
            catch (ArgumentException) { } // A currently closed collider is unavailable; lookup must retain its binding.
            for (var i = 0; i < 256; i++) { using var temporary = new StaticBody(); _ = temporary.GetRID(); }
        };
        body.Dispose(); body.During = null;
        Check(body.GetRID() == rid && world.DirectSpaceState.IntersectPoint(point, hits) == 1 && ReferenceEquals(hits[0].Collider, body), "A public in-notification query and registration sweep cannot retire a vetoed physics identity");
        var x = body.Position.X; tree.PhysicsFrame(.01); Check(body.Position.X > x, "A body still simulates after a vetoed registry sweep"); body.Veto = false; body.Dispose();
    }

    private static void DisposalVetoConcurrency()
    {
        foreach (var ownerVeto in new[] { false, true })
        {
            using var entered = new ManualResetEventSlim(); using var resume = new ManualResetEventSlim();
            var probe = new VetoProbe { Veto = false };
            probe.DuringNotification = value => { entered.Set(); if (!resume.Wait(TimeSpan.FromSeconds(20))) throw new TimeoutException("Veto window"); if (ownerVeto) value.CancelDisposal(); };
            var task = Task.Run(probe.Dispose);
            try
            {
                Check(entered.Wait(TimeSpan.FromSeconds(20)) && probe.IsDisposed, "The disposal attempt publishes closure before automatic notification");
                probe.CancelDisposal(); probe.Dispose();
                var rejected = false; try { _ = probe.CanTranslateMessages; } catch (ObjectDisposedException) { rejected = true; }
                Check(rejected, "Foreign inspection stays rejected during the pre-delete window");
            }
            finally { resume.Set(); task.GetAwaiter().GetResult(); }
            Check(probe.Notifications == 1 && probe.IsDisposed != ownerVeto && probe.Cleanups == (ownerVeto ? 0 : 1), "Foreign cancellation and concurrent disposal cannot override the exact notification owner's decision");
            probe.DuringNotification = null; probe.Dispose();
        }
        var repeated = new VetoProbe();
        Task.WhenAll(Enumerable.Range(0, 8).Select(_ => Task.Run(() => { for (var i = 0; i < 64; i++) repeated.Dispose(); }))).GetAwaiter().GetResult();
        Check(!repeated.IsDisposed && repeated.Notifications > 0 && repeated.Cleanups == 0, "Concurrent vetoed attempts retain live state without any committed cleanup");
        repeated.Veto = false; repeated.Dispose(); Check(repeated.Cleanups == 1, "Cleanup commits once after concurrent retries");
    }

    private static void DisposalVetoHierarchy(World world)
    {
        using var circle = new CircleShape { Radius = 1 }; using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        foreach (var failure in new[] { 0, 1, 2 })
        {
            var parent = new Node(); var child = CreateVetoBody(circle); var grandchild = new Node(); child.AddChild(grandchild); var sibling = new Node();
            parent.AddChild(child, internalMode: Node.InternalMode.Front); parent.AddChild(sibling); root.AddChild(parent); var oldRID = child.GetRID(); var collision = (CollisionShape)child.GetChild(0);
            child.RejectValidation = failure == 1; child.FailBeforeBase = failure == 2; if (failure == 2) child.Veto = false;
            child.FailUnparent = true; var escapeRejected = false;
            child.Unparent = () => { try { root.AddChild(child); } catch (Exception error) when (error is InvalidOperationException or ObjectDisposedException) { escapeRejected = true; } };
            var failed = false; try { parent.Dispose(); } catch (AggregateException) { failed = true; }
            Check(failed && parent.IsDisposed && sibling.IsDisposed && child.Parent is null && child.Tree is null && escapeRejected && child.Unparents == 1,
                "Parent cleanup releases rejected/throwing children, attempts siblings and keeps unparent callbacks inside the teardown barrier");
            if (failure != 2)
            {
                Check(!child.IsDisposed && !grandchild.IsDisposed && ReferenceEquals(grandchild.Parent, child) && ReferenceEquals(((CollisionShape)child.GetChild(0)).Shape, circle), "A surviving child's subtree and borrowed shape survive parent cleanup intact");
                child.FailUnparent = false; child.Unparent = null; child.RejectValidation = false;
                root.AddChild(child); Check(child.GetRID() == oldRID && PhysicsServer.BodyGetSpace(oldRID) == world.Space, "Reattachment restores numerical space membership while preserving the public body RID");
                var x = child.Position.X; tree.PhysicsFrame(.01); Check(child.Position.X > x, "A surviving body resumes actual simulation after reattachment");
                child.Veto = false; child.Dispose(); Check(child.IsDisposed && grandchild.IsDisposed, "A later accepted disposal retires the surviving subtree");
            }
            else { Check(child.IsDisposed, "A failed derived cleanup remains terminal while its stale structural edge is still removed"); grandchild.Dispose(); collision.Dispose(); }
        }
    }

    private sealed class VetoRoot : Node
    {
        internal bool Veto = true;
        internal Action? During;
        protected override void OnNotification(int what) { base.OnNotification(what); if (what == NotificationPreDelete) { if (Veto) CancelDisposal(); During?.Invoke(); } }
    }

    private static void DisposalVetoTreeRoot(World world)
    {
        using var circle = new CircleShape { Radius = 1 }; var root = new VetoRoot();
        var viewport = new SubViewport { World = world }; var body = CreateVetoBody(circle); viewport.AddChild(body); root.AddChild(viewport);
        var tree = new SceneTree(root); var rid = body.GetRID(); tree.Dispose();
        Check(tree.IsDisposed && !root.IsDisposed && root.Tree is null && !viewport.IsDisposed && !body.IsDisposed && ReferenceEquals(body.Parent, viewport), "A root veto keeps its detached owned subtree intact while the SceneTree itself closes");
        using var replacement = new SceneTree(root); var x = body.Position.X; replacement.PhysicsFrame(.01);
        Check(body.Position.X > x && body.GetRID() == rid && PhysicsServer.BodyGetSpace(rid) == world.Space, "A surviving root can reactivate the same physical identities in a later tree");
        root.Veto = false; body.Veto = false; replacement.Dispose(); Check(root.IsDisposed && viewport.IsDisposed && body.IsDisposed, "A later accepted root disposal retires its subtree");
    }

    private static void DisposalVetoQueues(World world)
    {
        using var circle = new CircleShape { Radius = 1 }; using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        var body = CreateVetoBody(circle); root.AddChild(body); body.QueueFree(); tree.FlushDeferred();
        Check(!body.IsDisposed && body.Parent is null && !body.IsQueuedForDeletion && body.Cleanups == 0, "A queued claim is consumed after a veto while the live node remains detached");
        root.AddChild(body); body.QueueFree(); Check(body.CancelFree(), "Node queue cancellation remains a separate operation"); tree.FlushDeferred();
        Check(!body.IsDisposed && body.Notifications == 1 && ReferenceEquals(body.Parent, root), "CancelFree does not execute or arm a pre-delete veto");
        body.Veto = false; body.QueueFree(); tree.FlushDeferred(); Check(body.IsDisposed && !body.IsQueuedForDeletion, "A fresh queued generation can commit after the veto is withdrawn");
        var value = new VetoProbe(); tree.QueueDelete(value); tree.QueueDelete(value); tree.FlushDeferred();
        Check(!value.IsDisposed && value.Notifications == 2 && !value.IsQueuedForDeletion && value.Cleanups == 0, "Each independent explicit entry gets its own cancelable attempt and releases its own mark");
        value.Veto = false; tree.QueueDelete(value); tree.FlushDeferred(); Check(value.IsDisposed && value.Cleanups == 1, "A later explicit request commits cleanup once");
        var emitter = new VetoProbe(); var delivered = 0;
        using var once = EventConnection.Subscribe<ElectronObject>(h => emitter.PropertyListChanged += h, h => emitter.PropertyListChanged -= h, _ => delivered++, oneShot: true, defer: tree.Defer);
        emitter.Refresh(); emitter.Dispose(); tree.FlushDeferred();
        Check(!emitter.IsDisposed && delivered == 1 && !once.IsConnected, "A veto preserves an already accepted deferred one-shot callback and its exact consumption");
        emitter.Refresh(); tree.FlushDeferred(); Check(delivered == 1, "A veto does not replay a consumed one-shot"); emitter.Veto = false; emitter.Dispose();
    }

    private static void DisposalVetoFrames(World world)
    {
        using var circle = new CircleShape { Radius = 1 }; using var root = new SubViewport { World = world }; using var tree = new SceneTree(root);
        var body = CreateVetoBody(circle); root.AddChild(body); using var weak = new WeakRef<VetoBody>(body); var rid = body.GetRID(); var id = body.InstanceID;
        using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        for (var i = 0; i < 128; i++) Frame();
        Collect(); var ticks = body.Ticks; var integrations = body.Integrations; var notifications = body.Notifications;
        var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 128; i++) Frame();
        var ownerBytes = GC.GetAllocatedBytesForCurrentThread() - owner; var allBytes = GC.GetTotalAllocatedBytes(true) - all;
        Check(ownerBytes == 0 && allBytes == 0 && body.Ticks - ticks == 128 && body.Integrations - integrations == 128 && body.Notifications - notifications == 128, $"Prepared veto, full simulation and query frames: {ownerBytes}/{allBytes} B");
        Console.WriteLine($"Disposal veto warm frames {world.PhysicsBackendName}: 128 full frames, owner/all-thread={ownerBytes}/{allBytes} B.");
        body.Veto = false; body.Dispose();
        void Frame()
        {
            body.Dispose(); Check(!body.IsDisposed && body.Cleanups == 0 && body.InstanceID == id && body.GetRID() == rid && ReferenceEquals(weak.GetRef(), body) && ReferenceEquals(body.Parent, root), "Direct veto preserves the actual body, binding, identity and scene attachment");
            var x = body.Position.X; tree.PhysicsFrame(.01); point.Position = body.GlobalPosition;
            Check(body.Position.X > x && world.DirectSpaceState.IntersectPoint(point, hits) == 1 && ReferenceEquals(hits[0].Collider, body), "The vetoed body integrates and remains publicly queryable");
        }
    }

    private static void DisposalVetoProvider(string backendName)
    {
        var name = "OwnedDisposalVeto" + backendName;
        Solver? solver = null; PhysicsServerManager.RegisterServer(name, () => solver = new Solver { VetoDisposal = true });
        var owned = new World(name); var space = owned.Space; owned.Dispose();
        Check(owned.IsDisposed && solver is { Finished: true, IsDisposed: false } && solver.Bodies.Count == 0 && solver.Joints.Count == 0, "Vetoing provider object disposal cannot keep a finished world's numerical state alive");
        var rejected = false; try { PhysicsServer.SpaceIsActive(space); } catch (Exception) { rejected = true; }
        Check(rejected, "The owning world's retired space is inaccessible despite the surviving provider object");
        solver!.VetoDisposal = false; solver.Dispose(); Check(solver.IsDisposed, "The finished provider can later commit its own object cleanup");
    }
}
