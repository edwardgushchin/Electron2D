using System.Runtime.CompilerServices;
using Electron2D;

public static partial class BackendChecks
{
    public static void RunAreaCallbackTransactions(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("AreaCallbackTransactions", () => solver = new());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("AreaCallbackTransactions");
            using var root = new SubViewport { World = world };
            using var small = new CircleShape { Radius = 2 }; using var large = new CircleShape { Radius = 8 };
            using var identity = new Node();
            var receiver = new Area { CollisionLayer = 2, CollisionMask = 1 };
            receiver.AddChild(new CollisionShape { Shape = small, Position = new(-4, 0) });
            receiver.AddChild(new CollisionShape { Shape = small, Position = new(100, 0), Disabled = true });
            receiver.AddChild(new CollisionShape { Shape = small, Position = new(4, 0) }); root.AddChild(receiver);
            var peer = new StaticBody { CollisionMask = 0 }; peer.AddChild(new CollisionShape { Shape = large }); root.AddChild(peer);
            var peerArea = new Area { Monitoring = false, CollisionMask = 0 }; peerArea.AddChild(new CollisionShape { Shape = large }); root.AddChild(peerArea);
            var moving = new RigidBody
            {
                Position = new(0, 100),
                GravityScale = 0,
                CanSleep = false,
                CollisionLayer = 0,
                LinearVelocity = new(6, 0),
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            moving.AddChild(new CollisionShape { Shape = small }); root.AddChild(moving);
            using var tree = new SceneTree(root);
            var raw = PhysicsServer.AreaCreate(); var rawBody = PhysicsServer.BodyCreate();
            var receivers = new[] { receiver.GetRID(), raw }; var counts = new int[16]; var active = new[] { -1, -1, -1, -1 };
            var handlers = new Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int>[8];
            for (var i = 0; i < handlers.Length; i++)
            {
                var index = i;
                handlers[i] = (status, rid, instance, remote, local) =>
                {
                    Check(remote == 0 && local is 0 or 2 && (index % 2 == 0
                        ? rid == peer.GetRID() && instance == peer.InstanceID || rid == rawBody && instance == identity.InstanceID
                        : rid == peerArea.GetRID() && instance == peerArea.InstanceID), "Committed callbacks retain exact body/Area and logical slot identities");
                    counts[index * 2 + (status == PhysicsServer.AreaBodyStatus.Added ? 0 : 1)]++;
                };
            }
            var failures = 0; var callbacks = 0;
            try
            {
                PhysicsServer.AreaSetCollisionLayer(raw, 2); PhysicsServer.AreaSetCollisionMask(raw, 1);
                PhysicsServer.AreaAddShape(raw, small.GetRID(), new(0, new Vector2(-4, 0)));
                PhysicsServer.AreaAddShape(raw, small.GetRID(), new(0, new Vector2(100, 0)), true);
                PhysicsServer.AreaAddShape(raw, small.GetRID(), new(0, new Vector2(4, 0))); PhysicsServer.AreaSetSpace(raw, world.Space);
                PhysicsServer.BodySetMode(rawBody, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionMask(rawBody, 0);
                PhysicsServer.BodyAddShape(rawBody, large.GetRID()); PhysicsServer.BodyAttachObject(rawBody, identity); PhysicsServer.BodySetSpace(rawBody, world.Space);
                Check(world.PhysicsBackendName == (backend?.ToString() ?? "AreaCallbackTransactions"), "The real selected implementation owns observer preparation");
                tree.PhysicsFrame(1d / 60);
                if (solver is not null)
                {
                    for (var slot = 0; slot < 4; slot++)
                    {
                        Fail(slot, handlers[slot]); tree.PhysicsFrame(1d / 60);
                        Check(counts.All(value => value == 0) && !solver.Bodies[raw].Monitoring,
                            "Failed first registration retains empty callbacks and raw monitoring policy");
                    }
                }
                Originals(); tree.PhysicsFrame(1d / 60);
                for (var slot = 0; slot < 4; slot++) Check(counts[slot * 2] == Targets(slot), "Initial registration publishes every logical pair once");
                if (solver is not null)
                {
                    for (var slot = 0; slot < 4; slot++)
                        foreach (var candidate in new Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int>?[] { null, handlers[slot], handlers[slot + 4] })
                        {
                            var before = (int[])counts.Clone(); Fail(slot, candidate); tree.PhysicsFrame(1d / 60);
                            Delta(before, -1, true);
                            MovePeers(30); tree.PhysicsFrame(1d / 60); Delta(before, -2, false);
                            before = (int[])counts.Clone(); MovePeers(0); tree.PhysicsFrame(1d / 60); Delta(before, -2, true);
                        }
                    for (var slot = 0; slot < 4; slot++)
                    {
                        var failure = new ApplicationException("weak preparation failure"); solver.MonitorCapacityHook = () => throw failure;
                        WeakReference<Node> weak;
                        try { weak = FailedAreaCallbackTarget(receivers[slot / 2], slot % 2 != 0); failures++; }
                        finally { solver.MonitorCapacityHook = null; }
                        Collect(); Check(!weak.TryGetTarget(out _), "Rejected callback publication releases the candidate target");
                    }
                }
                for (var slot = 0; slot < 4; slot++)
                    foreach (var candidate in new[] { -1, slot, slot + 4 })
                    {
                        Originals(); tree.PhysicsFrame(1d / 60);
                        var before = (int[])counts.Clone(); Assign(slot, candidate); tree.PhysicsFrame(0); Delta(before, -1, true);
                        tree.PhysicsFrame(1d / 60); Delta(before, slot / 2, true);
                        Check(receiver.OverlapsBody(peer), "External registration preserves the independent scene snapshot");
                    }
                Queued();
                Originals(); tree.PhysicsFrame(1d / 60);
                var movingRID = moving.GetRID(); PhysicsServer.BodySetForceIntegrationCallback(movingRID, _ => callbacks++);
                for (var i = 0; i < 128; i++) Frame();
                var measuredBefore = (int[])counts.Clone(); var start = moving.Position.X; var calls = callbacks;
                Collect(); var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Frame();
                owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
                Delta(measuredBefore, -2, true, 128);
                Check(owner == 0 && all == 0 && callbacks == calls + 128 && moving.GetRID() == movingRID && Math.Abs(moving.Position.X - start - 12.8f) < .01f,
                    $"Area registration/edit/query/full frames: {owner}/{all} B, force callbacks={callbacks - calls}, motion={moving.Position.X - start}");
                Console.WriteLine($"Area callback transactions {backend?.ToString() ?? "registered"}, threaded={threaded}: {failures} preparation failures; 12 replacements; queued events and weak targets; 128 full frames {owner}/{all} B.");

                void Assign(int slot, int handler) { Set(slot, handler < 0 ? null : handlers[handler]); active[slot] = handler; }
                void Set(int slot, Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int>? callback)
                {
                    if (slot % 2 == 0) PhysicsServer.AreaSetMonitorCallback(receivers[slot / 2], callback);
                    else PhysicsServer.AreaSetAreaMonitorCallback(receivers[slot / 2], callback);
                }
                void Originals() { for (var slot = 0; slot < 4; slot++) Assign(slot, slot); }
                void Fail(int slot, Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int>? candidate)
                {
                    var failure = new ApplicationException("monitor preparation failure"); var attempts = 0;
                    solver!.MonitorCapacityHook = () => { attempts++; throw failure; };
                    Exception[] errors;
                    try { errors = ReleaseErrors(() => Set(slot, candidate)); }
                    finally { solver.MonitorCapacityHook = null; }
                    Check(attempts == 1 && errors.Length == 1 && ReferenceEquals(errors[0], failure), "Preparation propagates the original error without a second fallible restoration");
                    failures++;
                }
                void Delta(int[] before, int role, bool entered, int repetitions = 1)
                {
                    for (var i = 0; i < counts.Length; i++)
                    {
                        var handler = i / 2; var slot = handler % 4;
                        var expected = (i % 2 == 0) == entered && active[slot] == handler && (role == -2 || role == slot / 2) ? Targets(slot) * repetitions : 0;
                        Check(counts[i] - before[i] == expected, "Only committed callbacks receive the exact entry/exit delta, without history replay or queued-event loss");
                    }
                }
                void MovePeers(float x)
                {
                    peer.Position = peerArea.Position = new(x, 0); PhysicsServer.BodySetTransform(rawBody, new(0, new Vector2(x, 0)));
                }
                void Queued()
                {
                    using var trigger = new Area { CollisionLayer = 2, CollisionMask = 1, Monitoring = false };
                    trigger.AddChild(new CollisionShape { Shape = small }); root.AddChild(trigger);
                    root.RemoveChild(receiver); root.AddChild(receiver);
                    var invoked = false; var failing = solver is not null;
                    PhysicsServer.AreaSetMonitorCallback(trigger.GetRID(), (status, _, _, _, _) =>
                    {
                        if (status != PhysicsServer.AreaBodyStatus.Added || invoked) return;
                        invoked = true;
                        for (var slot = 0; slot < 4; slot++)
                            if (failing) Fail(slot, handlers[slot + 4]); else Assign(slot, slot);
                    });
                    Originals(); var before = (int[])counts.Clone(); tree.PhysicsFrame(1d / 60);
                    Check(invoked, "An earlier receiver attempts registration while later receiver events are already queued");
                    Delta(before, failing ? -2 : -1, true);
                    if (failing)
                    {
                        failing = false; invoked = false; Originals();
                        PhysicsServer.AreaSetMonitorCallback(trigger.GetRID(), (status, _, _, _, _) =>
                        {
                            if (status != PhysicsServer.AreaBodyStatus.Added || invoked) return;
                            invoked = true; Originals();
                        });
                        before = (int[])counts.Clone(); tree.PhysicsFrame(1d / 60); Check(invoked, "Successful queued registration runs"); Delta(before, -1, true);
                    }
                    before = (int[])counts.Clone(); tree.PhysicsFrame(1d / 60); Delta(before, -2, true);
                    PhysicsServer.AreaSetMonitorCallback(trigger.GetRID(), null);
                }
                void Frame()
                {
                    Originals();
                    Check(receiver.OverlapsBody(peer) && PhysicsServer.AreaGetShapeCount(raw) == 3,
                        "Warm observer registration retains live geometry and scene observations");
                    tree.PhysicsFrame(1d / 60);
                }
            }
            finally
            {
                if (solver is not null) solver.MonitorCapacityHook = null;
                PhysicsServer.AreaSetMonitorCallback(receiver.GetRID(), null); PhysicsServer.AreaSetAreaMonitorCallback(receiver.GetRID(), null);
                PhysicsServer.AreaSetMonitorCallback(raw, null); PhysicsServer.AreaSetAreaMonitorCallback(raw, null);
                PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(rawBody);
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
        static int Targets(int slot) => slot % 2 == 0 ? 4 : 2;
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference<Node> FailedAreaCallbackTarget(RID receiver, bool area)
    {
        var target = new Node(); var weak = new WeakReference<Node>(target);
        Action<PhysicsServer.AreaBodyStatus, RID, ulong, int, int> callback = (_, _, _, _, _) => GC.KeepAlive(target);
        try
        {
            if (area) PhysicsServer.AreaSetAreaMonitorCallback(receiver, callback); else PhysicsServer.AreaSetMonitorCallback(receiver, callback);
            throw new InvalidOperationException("Expected observer preparation failure");
        }
        catch (ApplicationException) { }
        finally { target.Dispose(); }
        return weak;
    }
}
