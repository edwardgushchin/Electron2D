using Electron2D;
using System.Runtime.CompilerServices;

public static partial class BackendChecks
{
    private static void IntegrationOperations(PhysicsServer.Backend? backend)
    {
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("IntegrationOperations", () => solver = new());
        using var world = backend is { } selected ? new World(selected) : new World("IntegrationOperations");
        using var root = new SubViewport { World = world }; using var circle = new CircleShape { Radius = 1 };
        using var tree = new SceneTree(root);
        Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "Integration operations use the exact requested backend");
        var lifecycle = 0;
        PhysicsBody[] retired = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone()];
        var released = PhysicsServer.BodyCreate(); var releasedAlive = true;
        try
        {
            foreach (var node in retired) root.AddChild(node);
            foreach (var id in retired.Select(n => n.GetRID()).Append(released))
            {
                PhysicsServer.BodySetMode(id, PhysicsServer.BodyMode.Rigid); PhysicsServer.BodySetGravityScale(id, 0); PhysicsServer.BodySetCanSleep(id, false);
                PhysicsServer.BodySetSpace(id, world.Space);
                var oldView = PhysicsServer.BodyGetDirectState(id)!; var cleared = InstallIntegrationPayload(id);
                PhysicsServer.BodySetSpace(id, default); Collect(); Check(cleared.IsAlive, "Detachment retains registered callback data");
                Reject<ObjectDisposedException>(() => _ = oldView.Transform);
                PhysicsServer.BodySetSpace(id, world.Space); var view = PhysicsServer.BodyGetDirectState(id)!;
                Check(!ReferenceEquals(oldView, view), "Reentry gives callbacks a fresh attachment view"); tree.PhysicsFrame(.01);
                Check(IntegrationPayloadInvoked(cleared), "Retained callbacks execute after reentry");
                PhysicsServer.BodySetForceIntegrationCallback(id, null); Collect(); Check(cleared.IsAlive, "The remaining sync callback owns its data");
                PhysicsServer.BodySetStateSyncCallback(id, null); Collect(); Check(!cleared.IsAlive, "Clearing both callbacks releases their data");
                var payload = InstallIntegrationPayload(id);
                var node = retired.FirstOrDefault(n => !n.IsDisposed && n.GetRID() == id);
                if (node is null) { releasedAlive = false; PhysicsServer.FreeRID(id); } else node.Dispose();
                Collect();
                if (payload.IsAlive) throw new InvalidOperationException($"Released body retains callback data through a stale view: {node?.GetType().Name ?? "raw"}.");
                Reject<ObjectDisposedException>(() => _ = view.Transform); RejectIntegrationID(id); lifecycle += 3;
                GC.KeepAlive(oldView); GC.KeepAlive(view);
            }
            if (solver is not null)
            {
                var doomed = PhysicsServer.BodyCreate(); PhysicsServer.BodySetSpace(doomed, world.Space);
                var view = PhysicsServer.BodyGetDirectState(doomed)!; var payload = InstallIntegrationPayload(doomed);
                solver.FailBodyDetach = true;
                Exception? failure = null;
                try { PhysicsServer.FreeRID(doomed); }
                catch (Exception error) { failure = error; }
                finally { solver.FailBodyDetach = false; }
                Check(failure is not null, "Injected detach error is reported");
                Collect(); Check(!payload.IsAlive, "Failed retirement still releases callback data"); RejectIntegrationID(doomed); GC.KeepAlive(view); lifecycle++;
            }
            using var area = new Area(); RejectIntegrationID(area.GetRID()); RejectIntegrationID(circle.GetRID()); RejectIntegrationID(default);
        }
        finally { foreach (var node in retired) node.Dispose(); if (releasedAlive) PhysicsServer.FreeRID(released); }

        PhysicsBody[] nodes = [new StaticBody(), new AnimatableBody(), new CharacterBody(), new RigidBody(), new PhysicalBone()];
        var raw = PhysicsServer.BodyCreate(); RID[] ids = [.. nodes.Select(n => n.GetRID()), raw];
        var forces = 0; var syncs = 0; var queries = 0; var frames = 0; var owner = Thread.CurrentThread;
        var mode = PhysicsServer.BodyMode.Static; var omitted = false; var stages = new int[6];
        using var point = new PhysicsPointQueryParameters(); var hits = new PhysicsPointResult[1];
        try
        {
            for (var i = 0; i < nodes.Length; i++)
            {
                nodes[i].Position = new(i * 80, 0); nodes[i].AddChild(new CollisionShape { Shape = circle, Disabled = true }); nodes[i].AddChild(new CollisionShape { Shape = circle }); root.AddChild(nodes[i]);
            }
            PhysicsServer.BodyAddShape(raw, circle.GetRID(), disabled: true); PhysicsServer.BodyAddShape(raw, circle.GetRID()); PhysicsServer.BodySetSpace(raw, world.Space);
            foreach (var id in ids)
            {
                PhysicsServer.BodySetMass(id, 2); PhysicsServer.BodySetInertia(id, 4); PhysicsServer.BodySetGravityScale(id, 0); PhysicsServer.BodySetCanSleep(id, false);
                PhysicsServer.BodySetLinearDampMode(id, RigidBody.DampMode.Replace); PhysicsServer.BodySetAngularDampMode(id, RigidBody.DampMode.Replace);
                PhysicsServer.BodySetLinearDamp(id, 0); PhysicsServer.BodySetAngularDamp(id, 0); PhysicsServer.BodySetConstantForce(id, new(4, 0)); PhysicsServer.BodySetConstantTorque(id, 8);
                PhysicsServer.BodySetMaxContactsReported(id, 4095); Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetMaxContactsReported(id, -1)); Reject<ArgumentOutOfRangeException>(() => PhysicsServer.BodySetMaxContactsReported(id, 4096));
                Check(PhysicsServer.BodyGetMaxContactsReported(id) == 4095, "Invalid contact caps preserve the accepted boundary"); PhysicsServer.BodySetMaxContactsReported(id, 0);
                var index = Array.IndexOf(ids, id);
                PhysicsServer.BodySetForceIntegrationCallback(id, (state, data) =>
                {
                    Check(data == index && stages[data] == 0 && Thread.CurrentThread == owner && PhysicsServer.SpaceIsFlushingQueries(world.Space), "Typed value userdata and force callback retain owner and notification phase");
                    var dynamic = mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
                    var expected = new Vector2(3 + (dynamic && !omitted ? .05f : 0), 1);
                    var tolerance = mode == PhysicsServer.BodyMode.Kinematic ? .002f + 4 * (MathF.BitIncrement(data * 80f) - data * 80f) / .01f : .002f;
                    Check(state.LinearVelocity.DistanceTo(expected) < tolerance && MathF.Abs(state.AngularVelocity - (mode == PhysicsServer.BodyMode.RigidLinear ? 0 : .2f + (dynamic && !omitted ? .05f : 0))) < .002f, "Force callback observes actual selected integration before its edits");
                    Check(state.GetContactCount() == 0 && PhysicsServer.BodyIsOmittingForceIntegration(id) == omitted, "Callback reads current omission and bounded contacts");
                    point.Position = state.Transform.Origin; Check(world.DirectSpaceState.IntersectPoint(point, hits) == 1 && hits[0].ColliderRID == id && hits[0].ShapeIndex == 1, "Callback queries retain collider and logical shape identity"); queries++;
                    if (dynamic) { state.LinearVelocity = new(7 + data, 2); state.AngularVelocity = mode == PhysicsServer.BodyMode.RigidLinear ? 0 : .7f; }
                    stages[data] = 1; forces++;
                }, index);
                PhysicsServer.BodySetStateSyncCallback(id, state =>
                {
                    Check(stages[index] == 1 && Thread.CurrentThread == owner && PhysicsServer.SpaceIsFlushingQueries(world.Space), "Sync observes the completed force callback on the owner");
                    if (mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear && state.LinearVelocity.DistanceTo(new Vector2(7 + index, 2)) > .002f) throw new InvalidOperationException($"Sync writes: frame {frames}, role {mode}, owner {index}, actual {state.LinearVelocity}, expected {new Vector2(7 + index, 2)}.");
                    stages[index] = 0; syncs++;
                });
            }
            for (var i = 0; i < 64; i++) Frame(); Collect();
            var beforeForces = forces; var beforeSyncs = syncs; var beforeQueries = queries; var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 128; i++) Frame();
            own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(own == 0 && all == 0 && forces - beforeForces == 576 && syncs - beforeSyncs == 576 && queries - beforeQueries == 576, "Changed integration frames retain every callback/query and zero owner/all-thread managed bytes");
            Console.WriteLine($"Integration operations {backend?.ToString() ?? "registered"}: 6 owners, 4 roles, {lifecycle} lifecycle cases; 128 changed full frames, 576 force callbacks, 576 sync callbacks, 576 queries 0/0 B passed.");
            void Frame()
            {
                var index = frames++; mode = (PhysicsServer.BodyMode)(index & 3); omitted = (index & 4) != 0; var cap = index % 3;
                foreach (var id in ids)
                {
                    var i = Array.IndexOf(ids, id); PhysicsServer.BodySetMode(id, mode); PhysicsServer.BodySetOmitForceIntegration(id, omitted); PhysicsServer.BodySetMaxContactsReported(id, cap);
                    Check(PhysicsServer.BodyIsOmittingForceIntegration(id) == omitted && PhysicsServer.BodyGetMaxContactsReported(id) == cap, "All integration policies retain changed authoring");
                    PhysicsServer.BodySetTransform(id, new(.2f, new Vector2(i * 80, 0))); PhysicsServer.BodySetLinearVelocity(id, mode == PhysicsServer.BodyMode.Kinematic ? Vector2.Zero : new(3, 1)); PhysicsServer.BodySetAngularVelocity(id, mode is PhysicsServer.BodyMode.Kinematic or PhysicsServer.BodyMode.RigidLinear ? 0 : .2f);
                    if (mode == PhysicsServer.BodyMode.Kinematic) PhysicsServer.BodySetTransform(id, new(.202f, new Vector2(i * 80 + .03f, .01f)));
                    else if (mode != PhysicsServer.BodyMode.Static) { PhysicsServer.BodyApplyCentralForce(id, new(6, 0)); PhysicsServer.BodyApplyTorque(id, 12); }
                }
                var tick = PhysicsServer.SpaceGetTick(world.Space); tree.PhysicsFrame(.01); Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1, "Integration operations advance exactly one tick");
                for (var i = 0; i < ids.Length; i++)
                {
                    var pose = PhysicsServer.BodyGetTransform(ids[i]); var moving = mode != PhysicsServer.BodyMode.Static;
                    var accelerated = !omitted && mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.RigidLinear;
                    var x = i * 80 + (moving ? .03f : 0);
                    var angle = mode is PhysicsServer.BodyMode.Rigid or PhysicsServer.BodyMode.Kinematic ? .202f : .2f;
                    // Substeps integrate acceleration at different instants within the same fixed interval.
                    if (!(pose.Origin.X >= x - .0002f && pose.Origin.X <= x + (accelerated ? .0005f : 0) + .0002f &&
                        MathF.Abs(pose.Origin.Y - (moving ? .01f : 0)) < .0002f && pose.Rotation >= angle - .0002f &&
                        pose.Rotation <= angle + (accelerated && mode == PhysicsServer.BodyMode.Rigid ? .0005f : 0) + .0002f && stages[i] == 0))
                        throw new InvalidOperationException($"Completed integration: frame {index}, role {mode}, omitted {omitted}, owner {i}, pose {pose.Origin}/{pose.Rotation}, base {x}/{angle}, stage {stages[i]}.");
                }
            }
        }
        finally { PhysicsServer.FreeRID(raw); }
    }

    [MethodImpl(MethodImplOptions.NoInlining)]
    private static WeakReference InstallIntegrationPayload(RID body)
    {
        var payload = new IntegrationPayload();
        PhysicsServer.BodySetForceIntegrationCallback(body, static (_, data) => data.Forces++, payload);
        PhysicsServer.BodySetStateSyncCallback(body, _ => payload.Syncs++);
        return new WeakReference(payload);
    }
    [MethodImpl(MethodImplOptions.NoInlining)]
    private static bool IntegrationPayloadInvoked(WeakReference weak) => weak.Target is IntegrationPayload { Forces: 1, Syncs: 1 };
    private sealed class IntegrationPayload { internal int Forces, Syncs; }
    private static void RejectIntegrationID(RID id)
    {
        Reject<ArgumentException>(() => PhysicsServer.BodyGetMaxContactsReported(id)); Reject<ArgumentException>(() => PhysicsServer.BodySetMaxContactsReported(id, 0));
        Reject<ArgumentException>(() => PhysicsServer.BodyIsOmittingForceIntegration(id)); Reject<ArgumentException>(() => PhysicsServer.BodySetOmitForceIntegration(id, false));
        Reject<ArgumentException>(() => PhysicsServer.BodySetForceIntegrationCallback(id, null)); Reject<ArgumentException>(() => PhysicsServer.BodySetForceIntegrationCallback<int>(id, null, 7)); Reject<ArgumentException>(() => PhysicsServer.BodySetStateSyncCallback(id, null));
    }
}
