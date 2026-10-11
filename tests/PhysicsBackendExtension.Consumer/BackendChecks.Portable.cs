using Electron2D;

public static partial class BackendChecks
{
    private static void PortableEpisodeNumerics(PhysicsSnapshot snapshot, CircleShape circle)
    {
        Solver? target = null; PhysicsServerManager.RegisterServer("PortableEpisodesTarget", () => target = new());
        var space = PhysicsServer.SpaceCreate("PortableEpisodesTarget"); var bodies = new RID[3];
        try
        {
            PhysicsServer.AreaSetGravity(space, 0); PhysicsServer.AreaSetLinearDamp(space, 0); PhysicsServer.AreaSetAngularDamp(space, 0);
            using var map = new PhysicsSnapshotMap(space);
            for (var i = 2; i >= 0; i--)
            {
                bodies[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodyAddShape(bodies[i], circle.GetRID());
                PhysicsServer.BodySetGravityScale(bodies[i], 0); PhysicsServer.BodySetCanSleep(bodies[i], false);
                PhysicsServer.BodySetSpace(bodies[i], space); map.Bind((ulong)(100 + i), 1, bodies[i]);
            }
            PhysicsServer.SpaceSetActive(space, true); map.Apply(snapshot);
            Check(target!.OneWays.Count == 1 && target.OneWays[0] == new PhysicsBackendOneWay(bodies[0], 0, 0, bodies[1], 0, 0, false),
                "Portable episodes map network IDs to different local IDs and creation order");
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(Math.Abs(PhysicsServer.BodyGetLinearVelocity(bodies[0]).X - 60) < .00001f && target.Pairs == 0,
                "Restored rejected episode preserves actual pass-through motion");
            map.Apply(snapshot); target.OneWays[0] = target.OneWays[0] with { Allowed = true };
            PhysicsServer.SpaceStep(space, 1d / 60);
            Check(Math.Abs(PhysicsServer.BodyGetLinearVelocity(bodies[0]).X - 30) < .00001f && target.Pairs == 1,
                "Accepted episode produces numerical contact impulse on the same restored state");
            map.Apply(snapshot); PhysicsServer.SpaceStep(space, 1d / 60);
            Check(Math.Abs(PhysicsServer.BodyGetLinearVelocity(bodies[0]).X - 60) < .00001f && target.Pairs == 0,
                "Repeated authoritative restore replaces the episode decision before solving");
        }
        finally { foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body); PhysicsServer.FreeRID(space); }
    }
    private static void PortableEpisodeChecks()
    {
        Solver? solver = null;
        PhysicsServerManager.RegisterServer("PortableEpisodes", () => solver = new());
        using var world = new World("PortableEpisodes"); using var circle = new CircleShape { Radius = 3 };
        var bodies = new RID[3];
        try
        {
            PhysicsServer.AreaSetGravity(world.Space, 0); PhysicsServer.AreaSetLinearDamp(world.Space, 0); PhysicsServer.AreaSetAngularDamp(world.Space, 0);
            using var map = new PhysicsSnapshotMap(world.Space);
            for (var i = 0; i < bodies.Length; i++)
            {
                bodies[i] = PhysicsServer.BodyCreate(); PhysicsServer.BodyAddShape(bodies[i], circle.GetRID());
                PhysicsServer.BodySetTransform(bodies[i], new(0, new(i == 2 ? 1000 : i * 5, 0)));
                PhysicsServer.BodySetGravityScale(bodies[i], 0); PhysicsServer.BodySetCanSleep(bodies[i], false);
                PhysicsServer.BodySetSpace(bodies[i], world.Space); map.Bind((ulong)(100 + i), 1, bodies[i]);
            }
            var first = new PhysicsBackendOneWay(bodies[0], 0, 0, bodies[1], 0, 0, false);
            var second = new PhysicsBackendOneWay(bodies[1], 0, 0, bodies[2], 0, 0, true);
            var third = new PhysicsBackendOneWay(bodies[0], 0, 0, bodies[2], 0, 0, true);
            PhysicsServer.BodySetLinearVelocity(bodies[0], new(60, 0));
            solver!.OneWays.AddRange([first, second, third]);
            var snapshot = new PhysicsSnapshot(65536); map.Capture(snapshot);
            solver.OneWays.Clear(); solver.OneWays.Add(first); map.Capture(snapshot);
            Check(solver.OneWaySpanLength == 1, "Portable episode output is bounded by the current count after retained storage grows");
            var bytes = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(bytes);
            void Unchanged() { var now = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(now); Check(now.AsSpan().SequenceEqual(bytes), "Rejected capture preserves the previous complete packet"); }
            solver.OneWayReturn = 2; Reject<InvalidOperationException>(() => map.Capture(snapshot)); Unchanged();
            solver.OneWayReturn = -1; solver.OneWayCapacity = 0;
            Reject<InvalidOperationException>(() => map.Capture(snapshot)); Unchanged(); solver.OneWayCapacity = -1;
            foreach (var invalid in new[] { first with { B = default }, first with { B = first.A }, first with { ShapeA = -1 }, first with { PieceA = -1 }, first with { ShapeB = 1 }, first with { PieceB = 1 } })
            {
                solver.OneWays[0] = invalid; Reject<InvalidOperationException>(() => map.Capture(snapshot)); Unchanged();
            }
            solver.OneWays[0] = first; solver.OneWays.Add(first with { A = first.B, B = first.A });
            Reject<InvalidOperationException>(() => map.Capture(snapshot)); Unchanged();
            solver.OneWays.RemoveAt(1); PhysicsServer.BodySetShapeDisabled(bodies[1], 0, true);
            Reject<InvalidOperationException>(() => map.Capture(snapshot)); Unchanged();
            PhysicsServer.BodySetShapeDisabled(bodies[1], 0, false); map.Capture(snapshot);
            solver.OneWays.Clear(); map.Apply(snapshot);
            Check(solver.OneWays.Count == 1 && solver.OneWays[0] == first, "Portable restore preserves local identities and rejected one-way continuation");
            var packet = new byte[snapshot.GetEncodedSize()]; snapshot.WriteTo(packet);
            var received = new PhysicsSnapshot(65536); received.ReadFrom(packet); PortableEpisodeNumerics(received, circle);
            for (var i = 0; i < 64; i++) { map.Capture(snapshot); map.Apply(snapshot); }
            var owner = GC.GetAllocatedBytesForCurrentThread(); var all = GC.GetTotalAllocatedBytes(true);
            for (var i = 0; i < 64; i++) { map.Capture(snapshot); map.Apply(snapshot); }
            owner = GC.GetAllocatedBytesForCurrentThread() - owner; all = GC.GetTotalAllocatedBytes(true) - all;
            Check(owner == 0 && all == 0, "Portable episode capture/restore stays allocation-free after preparation");
            Console.WriteLine($"Registered portable one-way episodes: capacity, identity, invalid-output preservation and 64 warm capture/restore cycles {owner}/{all} B passed.");
        }
        finally { foreach (var body in bodies) if (body.IsValid()) PhysicsServer.FreeRID(body); }
    }
}
