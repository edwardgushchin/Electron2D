using Electron2D;

public static partial class BackendChecks
{
    public static void RunAreaOverlapPublication(PhysicsServer.Backend? backend = null, bool threaded = false)
    {
        var old = ProjectSettings.Get(ProjectSettings.Physics2DRunOnSeparateThread);
        ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, threaded);
        Solver? solver = null;
        if (backend is null) PhysicsServerManager.RegisterServer("AreaOverlapPublication", () => solver = new());
        try
        {
            using var world = backend is { } selected ? new World(selected) : new World("AreaOverlapPublication");
            using var root = new SubViewport { World = world }; using var small = new CircleShape { Radius = 2 };
            using var compound = new ConcavePolygonShape { Segments = [new(-8, 0), new(8, 0), new(0, -8), new(0, 8)] };
            using var wide = new CircleShape { Radius = 8 }; using var identity = new Node(); using var replacement = new Node();
            var receiver = new Area { CollisionLayer = 2, CollisionMask = 1 };
            receiver.AddChild(new CollisionShape { Shape = small, Position = new(-1, 0) });
            receiver.AddChild(new CollisionShape { Shape = small, Disabled = true });
            receiver.AddChild(new CollisionShape { Shape = small, Position = new(1, 0) }); root.AddChild(receiver);
            var body = new StaticBody { CollisionMask = 0 }; var area = new Area { CollisionMask = 0, Monitoring = false };
            foreach (var peer in new CollisionObject[] { body, area })
            {
                peer.AddChild(new CollisionShape { Shape = compound }); peer.AddChild(new CollisionShape { Shape = small, Disabled = true });
                peer.AddChild(new CollisionShape { Shape = wide }); root.AddChild(peer);
            }
            var moving = new RigidBody
            {
                Position = new(0, 1000),
                GravityScale = 0,
                CanSleep = false,
                CollisionLayer = 0,
                LinearVelocity = Vector2.Right,
                LinearDampMode = RigidBody.DampMode.Replace,
                AngularDampMode = RigidBody.DampMode.Replace
            };
            moving.AddChild(new CollisionShape { Shape = small }); root.AddChild(moving); using var tree = new SceneTree(root);
            var raw = PhysicsServer.AreaCreate(); var rawBody = PhysicsServer.BodyCreate(); var rawArea = PhysicsServer.AreaCreate();
            RID[] receivers = [receiver.GetRID(), raw]; RID[] peers = [body.GetRID(), rawBody, area.GetRID(), rawArea];
            ulong[] ids = [body.InstanceID, identity.InstanceID, area.InstanceID, 0]; var removedIDs = (ulong[])ids.Clone();
            var counts = new int[144]; var owner = Thread.CurrentThread; var callbacks = 0; var rejected = 0; var policyFailures = 0; var custom = 0; var throwNext = false;
            var original = new ApplicationException("Area pair observer failure");
            using var point = new PhysicsPointQueryParameters { CollisionMask = 2, CollideWithAreas = true, CollideWithBodies = false, Position = new(-1.5f, 0) };
            var hits = new PhysicsPointResult[2];
            try
            {
                PhysicsServer.AreaSetCollisionLayer(raw, 2); PhysicsServer.AreaSetCollisionMask(raw, 1);
                PhysicsServer.AreaAddShape(raw, small.GetRID(), new(0, new Vector2(-1, 0)));
                PhysicsServer.AreaAddShape(raw, small.GetRID(), disabled: true);
                PhysicsServer.AreaAddShape(raw, small.GetRID(), new(0, new Vector2(1, 0))); PhysicsServer.AreaSetSpace(raw, world.Space);
                PhysicsServer.BodySetMode(rawBody, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionMask(rawBody, 0);
                PhysicsServer.BodyAttachObject(rawBody, identity);
                PhysicsServer.BodyAddShape(rawBody, compound.GetRID()); PhysicsServer.BodyAddShape(rawBody, small.GetRID(), disabled: true); PhysicsServer.BodyAddShape(rawBody, wide.GetRID());
                PhysicsServer.BodySetSpace(rawBody, world.Space);
                PhysicsServer.AreaSetCollisionMask(rawArea, 0); PhysicsServer.AreaSetMonitorable(rawArea, true);
                PhysicsServer.AreaAddShape(rawArea, compound.GetRID()); PhysicsServer.AreaAddShape(rawArea, small.GetRID(), disabled: true); PhysicsServer.AreaAddShape(rawArea, wide.GetRID());
                PhysicsServer.AreaSetSpace(rawArea, world.Space);
                for (var r = 0; r < 2; r++)
                {
                    var slot = r;
                    PhysicsServer.AreaSetMonitorCallback(receivers[r], (status, rid, id, remote, local) => Record(slot, false, status, rid, id, remote, local));
                    PhysicsServer.AreaSetAreaMonitorCallback(receivers[r], (status, rid, id, remote, local) => Record(slot, true, status, rid, id, remote, local));
                }
                Check(world.PhysicsBackend == (backend ?? PhysicsServer.Backend.Extension) && world.PhysicsFallbackReason is null, "Overlap publication uses the selected owner without fallback");
                var before = (int[])counts.Clone(); tree.PhysicsFrame(0); Delta(before, 0); tree.PhysicsFrame(.01); Delta(before);
                Check(receiver.OverlapsBody(body) && receiver.OverlapsArea(area), "Compound physical pairs project independent scene snapshots");
                before = (int[])counts.Clone(); tree.PhysicsFrame(.01); Delta(before, 0);
                PhysicsServer.AreaSetMonitorable(rawArea, false); tree.PhysicsFrame(.01); Delta(before, 8, added: false);
                before = (int[])counts.Clone(); PhysicsServer.AreaSetMonitorable(rawArea, true); tree.PhysicsFrame(.01); Delta(before, 8);
                before = (int[])counts.Clone(); PhysicsServer.AreaSetMonitorable(raw, true); PhysicsServer.AreaSetMonitorable(raw, false); tree.PhysicsFrame(.01); Delta(before, 0);
                before = (int[])counts.Clone(); PhysicsServer.BodySetShapeDisabled(rawBody, 0, true); tree.PhysicsFrame(.01); Delta(before, 2, added: false, remote: 0);
                before = (int[])counts.Clone(); PhysicsServer.BodySetShapeDisabled(rawBody, 0, false); tree.PhysicsFrame(.01); Delta(before, 2, remote: 0);
                before = (int[])counts.Clone(); ids[1] = replacement.InstanceID; PhysicsServer.BodyAttachObject(rawBody, replacement); tree.PhysicsFrame(.01); Delta(before, 2, added: null); removedIDs[1] = ids[1];
                before = (int[])counts.Clone(); PhysicsServer.AreaSetSpace(raw, default); Delta(before, 0); PhysicsServer.AreaSetSpace(raw, world.Space); tree.PhysicsFrame(.01); Delta(before, receiversMask: 2);
                if (solver is not null) { RejectedResults(); custom = CustomAreaOverlapCases(world, tree); }
                Move(30); tree.PhysicsFrame(.01); Move(0); tree.PhysicsFrame(.01);
                before = (int[])counts.Clone(); throwNext = true; Move(30);
                var errors = ReleaseErrors(() => tree.PhysicsFrame(.01)); Check(errors.Contains(original), "Original callback errors survive after all pair deliveries"); Delta(before, added: false);
                before = (int[])counts.Clone(); tree.PhysicsFrame(.01); Delta(before, 0); Move(0); tree.PhysicsFrame(.01); Delta(before);
                PhysicsServer.BodySetStateSyncCallback(moving.GetRID(), _ => { Check(Thread.CurrentThread == owner, "Body callbacks remain on the owner"); callbacks++; });
                if (solver is not null) solver.StepHook = () =>
                {
                    Check((Thread.CurrentThread != owner) == threaded && solver.Bodies[receivers[0]].Monitoring && solver.Bodies[receivers[1]].Monitoring &&
                        !solver.Bodies[receivers[1]].Monitorable && !solver.Bodies[peers[2]].Monitoring && !solver.Bodies[peers[3]].Monitoring && solver.Bodies[peers[3]].Monitorable,
                        "Selected numerical preparation retains independent monitorability and effective scene/raw callback lanes");
                };
                for (var i = 0; i < 128; i++) Cycle(); before = (int[])counts.Clone(); var start = moving.Position.X; var calls = callbacks; Collect();
                var own = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
                for (var i = 0; i < 128; i++) Cycle();
                own = GC.GetAllocatedBytesForCurrentThread() - own; all = GC.GetTotalAllocatedBytes(true) - all;
                Delta(before, added: null, repeats: 128);
                Check(own == 0 && all == 0 && callbacks == calls + 256 && MathF.Abs(moving.Position.X - start - 2.56f) < .001f,
                    $"Complete overlap frames allocated {own}/{all} B, callbacks={callbacks - calls}, motion={moving.Position.X - start}");
                before = (int[])counts.Clone(); throwNext = true; errors = ReleaseErrors(() => PhysicsServer.FreeRID(rawBody));
                Check(errors.Contains(original), "Collider retirement preserves a throwing pair observer and completes registry cleanup"); Delta(before, 2, added: false);
                Reject<ArgumentException>(() => PhysicsServer.BodyGetSpace(rawBody)); rawBody = default;
                Console.WriteLine($"Area overlap publication {backend?.ToString() ?? "registered"}/{(threaded ? "separate" : "owner")}: {rejected} rejected publications, {policyFailures} policy failures, {custom} exact custom cases, 256 complete frames, 8192 pair callbacks and 256 body callbacks 0/0 B passed.");
            }
            finally
            {
                if (solver is not null) { solver.AreaCountOverride = null; solver.AreaReadOverride = null; solver.StepHook = null; solver.AreaMonitoringHook = null; }
                foreach (var id in receivers) { PhysicsServer.AreaSetMonitorCallback(id, null); PhysicsServer.AreaSetAreaMonitorCallback(id, null); }
                PhysicsServer.FreeRID(raw); PhysicsServer.FreeRID(rawArea); if (rawBody != default) PhysicsServer.FreeRID(rawBody);
            }
            void Record(int r, bool isArea, PhysicsServer.AreaBodyStatus status, RID rid, ulong id, int remote, int local)
            {
                var p = Array.IndexOf(peers, rid); var entered = status == PhysicsServer.AreaBodyStatus.Added;
                Check(Thread.CurrentThread == owner && p >= 0 && isArea == (p >= 2) && remote is 0 or 2 && local is 0 or 2 && id == (entered ? ids[p] : removedIDs[p]),
                    "Compound callbacks retain selected owner, RID, sampled identity and global logical indices without fixture-piece duplicates");
                counts[((r * 2 + (entered ? 0 : 1)) * 4 + p) * 9 + remote * 3 + local]++;
                if (throwNext) { throwNext = false; throw original; }
            }
            void Delta(int[] prior, int peersMask = 15, int receiversMask = 3, bool? added = true, int repeats = 1, int remote = -1)
            {
                for (var r = 0; r < 2; r++) for (var s = 0; s < 2; s++) for (var p = 0; p < 4; p++) for (var j = 0; j < 3; j++) for (var k = 0; k < 3; k++)
                                {
                                    var expected = (receiversMask & 1 << r) != 0 && (peersMask & 1 << p) != 0 && (added is null || added == (s == 0)) && j != 1 && k != 1 && (remote < 0 || j == remote) ? repeats : 0;
                                    var index = ((r * 2 + s) * 4 + p) * 9 + j * 3 + k; Check(counts[index] - prior[index] == expected, $"Pair delta r={r} s={s} peer={p} remote={j} local={k}: {counts[index] - prior[index]} != {expected}");
                                }
            }
            void Move(float x) { body.Position = area.Position = new(x, 0); PhysicsServer.BodySetTransform(rawBody, new(0, new Vector2(x, 0))); PhysicsServer.AreaSetTransform(rawArea, new(0, new Vector2(x, 0))); }
            void Cycle() { Move(30); Sample(); Move(0); Sample(); }
            void Sample()
            {
                var tick = PhysicsServer.SpaceGetTick(world.Space); tree.PhysicsFrame(.01); Check(PhysicsServer.SpaceGetTick(world.Space) == tick + 1, "Every complete overlap frame advances one tick");
                Check(world.DirectSpaceState.IntersectPoint(point, hits) == 2 && hits[0].ColliderRID != hits[1].ColliderRID &&
                    (hits[0].ColliderRID == receivers[0] || hits[0].ColliderRID == receivers[1]) && (hits[1].ColliderRID == receivers[0] || hits[1].ColliderRID == receivers[1]) && hits[0].ShapeIndex == 0 && hits[1].ShapeIndex == 0,
                    "Real Area queries retain both scene/raw receivers and exact logical slots");
            }
            void RejectedResults()
            {
                using var foreignWorld = new World(PhysicsServer.Backend.CPU); var foreign = PhysicsServer.BodyCreate(); var forbidden = PhysicsServer.BodyCreate(); var hidden = PhysicsServer.AreaCreate();
                var stale = PhysicsServer.BodyCreate(); PhysicsServer.FreeRID(stale);
                try
                {
                    PhysicsServer.BodyAddShape(foreign, small.GetRID()); PhysicsServer.BodySetSpace(foreign, foreignWorld.Space);
                    PhysicsServer.BodyAddShape(forbidden, small.GetRID()); PhysicsServer.BodySetCollisionLayer(forbidden, 4); PhysicsServer.BodySetSpace(forbidden, world.Space);
                    PhysicsServer.AreaAddShape(hidden, small.GetRID()); PhysicsServer.AreaSetSpace(hidden, world.Space);
                    foreach (var target in receivers)
                    {
                        PhysicsBackendOverlap[] invalid = [new(target, 0, 0), new(foreign, 0, 0), new(stale, 0, 0), new(small.GetRID(), 0, 0), new(forbidden, 0, 0), new(hidden, 0, 0),
                            new(peers[0], -1, 0), new(peers[0], 0, -1), new(peers[0], 1, 0), new(peers[0], 0, 1), new(peers[0], 3, 0), new(peers[0], 0, 3), new(default, 0, 0)];
                        for (var fault = 0; fault < invalid.Length + 5; fault++)
                        {
                            var failure = new ApplicationException("Overlap provider failure"); var prior = (int[])counts.Clone();
                            solver!.AreaCountOverride = id => id != target ? null : fault == 4 ? throw failure : fault == 0 ? -1 : 2;
                            solver.AreaReadOverride = (id, output) =>
                            {
                                if (id != target) return null;
                                if (fault == 3) throw failure; if (fault == 1) return -1; if (fault == 2) return 3;
                                output[0] = new(peers[0], 0, 0); output[1] = invalid[fault - 5]; return 2;
                            };
                            Exception[] errors;
                            try { errors = ReleaseErrors(() => tree.PhysicsFrame(.01)); }
                            finally { solver.AreaCountOverride = null; solver.AreaReadOverride = null; }
                            Check(errors.Length > 0 && (fault is 3 or 4 ? errors.Contains(failure) : errors.All(error => error is InvalidOperationException or ArgumentException)), "Malformed overlap count, identity, geometry or filters reject before snapshot publication");
                            Delta(prior, 0); Check(receiver.OverlapsBody(body) && receiver.OverlapsArea(area), "Rejected results retain the last complete scene snapshot");
                            tree.PhysicsFrame(.01); Delta(prior, 0); rejected++;
                        }
                        var priorCounts = (int[])counts.Clone(); var valid = new PhysicsBackendOverlap[19]; var count = 0;
                        foreach (var peer in peers) foreach (var remote in new[] { 0, 2 }) foreach (var local in new[] { 0, 2 }) valid[count++] = new(peer, remote, local);
                        valid[16] = valid[17] = valid[18] = valid[0]; var probed = false;
                        solver!.AreaCountOverride = id => id == target ? valid.Length : null;
                        solver.AreaReadOverride = (id, output) =>
                        {
                            if (id != target) return null;
                            if (!probed) { Reject<InvalidOperationException>(() => PhysicsServer.AreaSetMonitorable(target, true)); Reject<InvalidOperationException>(() => PhysicsServer.AreaSetMonitorCallback(target, null)); probed = true; }
                            valid.CopyTo(output); return valid.Length;
                        };
                        try { tree.PhysicsFrame(.01); } finally { solver.AreaCountOverride = null; solver.AreaReadOverride = null; }
                        Delta(priorCounts, 0); Check(probed, "Valid publication is borrowed and duplicate numerical pieces collapse into one logical pair");
                    }
                    var provider = solver!;
                    foreach (var target in new[] { receivers[0], receivers[1], peers[2], peers[3] })
                    {
                        var failure = new ApplicationException("Area policy preparation failure"); var prior = (int[])counts.Clone(); var tick = PhysicsServer.SpaceGetTick(world.Space);
                        provider.AreaMonitoringHook = id =>
                        {
                            if (id != target) return;
                            var partial = provider.Bodies[id]; partial.Monitoring = !partial.Monitoring; partial.Monitorable = !partial.Monitorable; throw failure;
                        };
                        Exception[] errors;
                        try { errors = ReleaseErrors(() => tree.PhysicsFrame(.01)); } finally { provider.AreaMonitoringHook = null; }
                        Check(errors.Contains(failure) && PhysicsServer.SpaceGetTick(world.Space) == tick, "Failed selected Area policy preparation preserves the original error and unstarted tick");
                        Delta(prior, 0); tree.PhysicsFrame(.01); Delta(prior, 0);
                        var actual = provider.Bodies[target];
                        Check(actual.Monitoring == (target == receivers[0] || target == receivers[1]) && actual.Monitorable == (target != receivers[1]),
                            "The next complete preparation repairs both effective Area policy flags without resetting committed histories"); policyFailures++;
                    }
                }
                finally { PhysicsServer.FreeRID(foreign); PhysicsServer.FreeRID(forbidden); PhysicsServer.FreeRID(hidden); }
            }
        }
        finally { ProjectSettings.Set(ProjectSettings.Physics2DRunOnSeparateThread, old); }
    }

    private static int CustomAreaOverlapCases(World world, SceneTree tree)
    {
        var shape = PhysicsServer.CustomShapeCreate(Annulus.Create(5, 10)); using var circle = new CircleShape { Radius = 1 };
        var area = PhysicsServer.AreaCreate(); var body = PhysicsServer.BodyCreate(); var enters = 0; var exits = 0; var cases = 0;
        try
        {
            PhysicsServer.AreaSetCollisionLayer(area, 8); PhysicsServer.AreaSetCollisionMask(area, 8); PhysicsServer.AreaSetTransform(area, new(0, new Vector2(100, 0)));
            PhysicsServer.AreaAddShape(area, shape); PhysicsServer.AreaAddShape(area, circle.GetRID(), disabled: true); PhysicsServer.AreaAddShape(area, shape); PhysicsServer.AreaSetSpace(area, world.Space);
            PhysicsServer.BodySetMode(body, PhysicsServer.BodyMode.Static); PhysicsServer.BodySetCollisionLayer(body, 8); PhysicsServer.BodySetCollisionMask(body, 0);
            PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodyAddShape(body, circle.GetRID(), disabled: true); PhysicsServer.BodyAddShape(body, circle.GetRID()); PhysicsServer.BodySetSpace(body, world.Space);
            PhysicsServer.AreaSetMonitorCallback(area, (status, rid, id, remote, local) => { Check(rid == body && id == 0 && remote is 0 or 2 && local is 0 or 2, "Custom annulus callbacks preserve exact raw logical pairs"); if (status == PhysicsServer.AreaBodyStatus.Added) enters++; else exits++; });
            At(100); Check(enters == 0 && exits == 0, "An actual custom Area hole remains empty despite broad bounds"); cases++;
            At(105); Check(enters == 4 && exits == 0, "All four custom logical pairs enter the inner wall once"); cases++;
            using (var checkpoint = PhysicsServer.SpaceCreateCheckpoint(world.Space))
            {
                At(100); Check(exits == 4, "Returning into the hole removes every logical pair"); checkpoint.Restore(); var before = enters + exits; tree.PhysicsFrame(.01);
                Check(enters + exits == before && PhysicsServer.BodyGetTransform(body).Origin.X == 105, "Silent private correction restores custom Area pair history without replay"); cases++;
            }
            At(112); Check(exits == 8, "The outer annulus boundary has an exact departure"); cases++;
            using var changed = new CustomShape(Annulus.Create(2, 4)); PhysicsServer.ShapeSetData(shape, changed); At(102.5f); Check(enters == 8, "Canonical custom geometry edits rebuild selected monitoring"); cases++;
            using var restored = new CustomShape(Annulus.Create(5, 10)); PhysicsServer.ShapeSetData(shape, restored); tree.PhysicsFrame(.01); Check(exits == 12, "Restoring the larger hole removes the four edited pairs"); cases++;
            return cases;
        }
        finally { PhysicsServer.AreaSetMonitorCallback(area, null); PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(area); PhysicsServer.FreeRID(shape); }
        void At(float x) { PhysicsServer.BodySetTransform(body, new(0, new Vector2(x, 0))); tree.PhysicsFrame(.01); }
    }
}
