using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2MathFunction;

internal static partial class GPUPhysicsTests
{
    internal static void RunContactRecycling()
    {
        B2Contacts.b2InitializeContactRegisters();
        using var gpu = new GPUPhysicsWorld(); var c = ResidentContext(1); GenerateAndUpdate(gpu, c);
        var index = 0; while (index < c.contacts.Count && c.contacts[index].manifold.pointCount != 2) index++;
        if (index == c.contacts.Count) throw new Exception("Recycling fixture requires two fresh contacts.");
        var contact = c.contacts[index]; var source = contact.manifold;
        var old = source; old.pointCount = 1; old.points[0].id = ushort.MaxValue;
        old.points[0].normalImpulse = 7; old.points[0].tangentImpulse = 2;
        old.points[0].localAnchorA = .5f * (source.points[0].localAnchorA + source.points[1].localAnchorA);
        old.points[0].localAnchorB = .5f * (source.points[0].localAnchorB + source.points[1].localAnchorB);
        var a = c.world.shapes.data[contact.shapeIdA]; var b = c.world.shapes.data[contact.shapeIdB];
        var sa = B2Bodies.b2GetBodySim(c.world, c.world.bodies.data[a.bodyId]); var sb = B2Bodies.b2GetBodySim(c.world, c.world.bodies.data[b.bodyId]);
        foreach (var variant in new[] { 0, 1, 2 })
        {
            var cached = old;
            if (variant == 2) cached.points[0].localAnchorB += b2InvRotateVector(sb.transform.q, 100 * old.normal);
            c.world.contactRecycleRadius = variant == 1 ? 0 : 1000; c.world.contactMaxSeparation = 10;
            contact.manifold = cached; contact.generatedManifoldVersion = 0;
            var oracle = new B2ContactSim { manifold = cached };
            B2Contacts.b2UpdateContact(c.world, oracle, a, sa.transform, b2RotateVector(sa.transform.q, sa.localCenter),
                b, sb.transform, b2RotateVector(sb.transform.q, sb.localCenter));
            gpu.GenerateManifolds(c, c.contacts.Count); var result = c.generatedManifolds[index]; CompareWarmStart(oracle.manifold, result);
            var reused = 0; var impulse = 0f;
            for (var i = 0; i < result.pointCount; i++) { if (result.points[i].persisted) reused++; impulse += result.points[i].normalImpulse; }
            if (reused != (variant == 0 ? 1 : 0) || MathF.Abs(impulse - (variant == 0 ? 7 : 0)) > .0001f)
                throw new Exception($"Geometric history fallback/one-use/separation failed: variant={variant}, reused={reused}, impulse={impulse}.");
        }
        Console.WriteLine("CPU/GPU stage recycling: changed-feature fallback, one-use impulses, zero radius and stale separation passed.");
    }

    private static void VerifyWarmHistory(GPUPhysicsWorld gpu)
    {
        var c = ResidentContext(65);
        var originals = new B2Manifold[c.contacts.Count];
        for (var cycle = 0; cycle < 6; cycle++)
        {
            if (cycle == 2) c.contacts[0].generatedManifoldVersion = 0;
            if (cycle == 3) c.world.contacts.capacity = 2049; // Grow destinations without discarding the prior solve before collision consumes it.
            for (var i = 0; i < originals.Length; i++) originals[i] = c.contacts[i].manifold;
            gpu.GenerateManifolds(c, c.contacts.Count);
            var expectedResident = cycle == 0 ? 0 : cycle == 2 ? 66 : 67;
            CheckHistory(gpu, expectedResident, 67 - expectedResident);
            for (var i = 0; i < originals.Length; i++)
            {
                var contact = c.contacts[i]; var a = c.world.shapes.data[contact.shapeIdA]; var b = c.world.shapes.data[contact.shapeIdB];
                var sa = B2Bodies.b2GetBodySim(c.world, c.world.bodies.data[a.bodyId]);
                var sb = B2Bodies.b2GetBodySim(c.world, c.world.bodies.data[b.bodyId]);
                var oracle = new B2ContactSim { manifold = originals[i] };
                B2Contacts.b2UpdateContact(c.world, oracle, a, sa.transform, b2RotateVector(sa.transform.q, sa.localCenter),
                    b, sb.transform, b2RotateVector(sb.transform.q, sb.localCenter));
                CompareWarmStart(oracle.manifold, c.generatedManifolds[i]);
            }
            UpdateGeneratedContacts(c);
            c.world.enableWarmStarting = cycle % 2 == 0;
            gpu.Solve(c);
        }
        var broken = ResidentContext(65);
        broken.world.contacts.capacity = 8193;
        broken.graph.colors[1].jointSims.data[0].type = B2JointType.b2_distanceJoint;
        try { gpu.Solve(broken); throw new Exception("Unsupported raw constraint did not fail."); }
        catch (NotSupportedException) { }
        gpu.GenerateManifolds(c, c.contacts.Count); CheckHistory(gpu, 0, 67);
        UpdateGeneratedContacts(c); gpu.Solve(c);
        // A sleeping/unprocessed contact keeps its CPU snapshot after the solver buffer is reused.
        var oldCount = c.graph.colors[0].contactSims.count;
        c.graph.colors[0].contactSims.count--;
        gpu.Solve(c);
        c.graph.colors[0].contactSims.count = oldCount;
        gpu.GenerateManifolds(c, c.contacts.Count); CheckHistory(gpu, 66, 1);
        UpdateGeneratedContacts(c); gpu.Solve(c);
        // The GPU object's last solver buffer may belong to another synthetic world.
        var other = ResidentContext(65); CopyManifolds(c, other);
        gpu.GenerateManifolds(other, other.contacts.Count); CheckHistory(gpu, 0, 67);
        // Empty contact history must not revive cached impulses after a veto or separation.
        var cleared = c.contacts[0]; cleared.manifold = default; cleared.generatedManifoldVersion = 0;
        gpu.GenerateManifolds(c, c.contacts.Count); CheckHistory(gpu, 66, 0);
        if (c.generatedManifolds[0].rollingImpulse != 0 || c.generatedManifolds[0].points[0].persisted)
            throw new Exception("An empty contact revived old GPU impulses.");
        UpdateGeneratedContacts(c); gpu.Solve(c);
        for (var i = 0; i < 8; i++) { GenerateAndUpdate(gpu, c); gpu.Solve(c); }
        var before = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 8; i++) { GenerateAndUpdate(gpu, c); gpu.Solve(c); }
        var bytes = GC.GetTotalAllocatedBytes(true) - before;
        if (bytes != 0) throw new Exception($"Resident GPU warm-start matching allocated {bytes} managed bytes.");
        CheckHistory(gpu, 67, 0);
        Console.WriteLine("GPU feature matching passed: CPU oracle, resident solved impulses, cold/stale/empty/world history, growth, warm-start toggles and zero warmed bytes.");
    }

    private static void CheckHistory(GPUPhysicsWorld gpu, int resident, int uploaded)
    {
        if (gpu.ResidentHistoryCount != resident || gpu.UploadedHistoryCount != uploaded || gpu.HistoryUploadBytes != 80L * uploaded)
            throw new Exception($"GPU history differs: resident {gpu.ResidentHistoryCount}/{resident}, uploaded {gpu.UploadedHistoryCount}/{uploaded}, {gpu.HistoryUploadBytes} bytes.");
    }

    private static void CompareWarmStart(B2Manifold expected, B2Manifold actual)
    {
        Near(expected.rollingImpulse, actual.rollingImpulse);
        if (expected.pointCount != actual.pointCount) throw new Exception("Warm-start manifold counts differ.");
        for (var i = 0; i < expected.pointCount; i++)
        {
            var e = expected.points[i]; var a = actual.points[i];
            Near(e.normalImpulse, a.normalImpulse); Near(e.tangentImpulse, a.tangentImpulse);
            if (e.persisted != a.persisted) throw new Exception("GPU contact persistence differs from CPU feature matching.");
            if (a.totalNormalImpulse != 0 || a.normalVelocity != 0) throw new Exception("A new manifold retained last-step solver totals.");
        }
    }
}
