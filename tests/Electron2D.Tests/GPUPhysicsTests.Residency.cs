using Box2D.NET;
using Electron2D;
using static Box2D.NET.B2MathFunction;

internal static partial class GPUPhysicsTests
{
    private static void VerifyResidentConstraints(GPUPhysicsWorld gpu)
    {
        foreach (var count in new[] { 1, 61, 62, 63, 64, 65, 1025 })
        {
            var actual = ResidentContext(count); var expected = ResidentContext(count);
            GenerateAndUpdate(gpu, actual);
            CopyManifolds(actual, expected);
            SolveCPU(expected); gpu.Solve(actual); Compare(expected, actual);
            CheckTransfer(gpu, count + 2, 0);
            CompareContactImpulses(expected, actual);
        }
        // Contact order, feature order and graph storage are independent of collision-batch order.
        for (var variant = 0; variant < 6; variant++)
        {
            var actual = ResidentContext(65); var expected = ResidentContext(65);
            GenerateAndUpdate(gpu, actual);
            var contact = actual.graph.colors[0].contactSims.data[0];
            if (contact.manifold.pointCount != 2) throw new Exception("Resident test requires a two-point manifold.");
            var replacement = new B2ContactSim(); replacement.CopyFrom(contact);
            actual.graph.colors[0].contactSims.data[0] = replacement;
            ref var m = ref replacement.manifold;
            var uploaded = 0;
            switch (variant)
            {
                case 0: (m.points[0], m.points[1]) = (m.points[1], m.points[0]); break;
                case 1: m.points[0] = m.points[1]; m.pointCount = 1; break;
                case 2: m.points[0].separation += .03f; replacement.generatedManifoldVersion = 0; uploaded = 1; break;
                case 3: replacement.generatedManifoldVersion--; uploaded = 1; break;
                case 4: gpu.GenerateManifolds(ResidentContext(1), 3); uploaded = 67; break;
                case 5: gpu.GenerateManifolds(actual, 0); uploaded = 67; break;
            }
            CopyManifolds(actual, expected);
            SolveCPU(expected); gpu.Solve(actual); Compare(expected, actual);
            CompareContactImpulses(expected, actual); CheckTransfer(gpu, 67 - uploaded, uploaded);
            // A consumed batch cannot be used again, even while a pooled context keeps its identity.
            gpu.Solve(actual); CheckTransfer(gpu, 0, 67);
        }
        // A pre-solve veto removes the constraint even when its geometric manifold remains on GPU.
        var vetoed = ResidentContext(65); var reference = ResidentContext(65);
        GenerateAndUpdate(gpu, vetoed);
        var rejected = vetoed.graph.colors[0].contactSims.data[0];
        rejected.simFlags |= (uint)B2ContactSimFlags.b2_simEnablePreSolveEvents;
        var callbacks = 0;
        vetoed.world.preSolveFcn = (_, _, _, _, _) => { callbacks++; return false; };
        var shapeA = vetoed.world.shapes.data[rejected.shapeIdA]; var shapeB = vetoed.world.shapes.data[rejected.shapeIdB];
        var simA = B2Bodies.b2GetBodySim(vetoed.world, vetoed.world.bodies.data[shapeA.bodyId]);
        var simB = B2Bodies.b2GetBodySim(vetoed.world, vetoed.world.bodies.data[shapeB.bodyId]);
        if (B2Contacts.b2UpdateContact(vetoed.world, rejected, shapeA, simA.transform, b2RotateVector(simA.transform.q, simA.localCenter),
                shapeB, simB.transform, b2RotateVector(simB.transform.q, simB.localCenter), vetoed.generatedManifolds, 66) || callbacks != 1 || rejected.manifold.rollingImpulse != 0)
            throw new Exception("Resident geometry bypassed the pre-solve veto.");
        vetoed.graph.colors[0].contactSims.data[0] = vetoed.graph.colors[0].contactSims.data[64];
        vetoed.graph.colors[0].contactSims.count = reference.graph.colors[0].contactSims.count = 64;
        CopyManifolds(vetoed, reference); SolveCPU(reference); gpu.Solve(vetoed);
        Compare(reference, vetoed); CompareContactImpulses(reference, vetoed); CheckTransfer(gpu, 66, 0);
        var warmed = ResidentContext(65);
        for (var i = 0; i < 8; i++) { GenerateAndUpdate(gpu, warmed); gpu.Solve(warmed); }
        var before = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 8; i++) { GenerateAndUpdate(gpu, warmed); gpu.Solve(warmed); }
        var bytes = GC.GetTotalAllocatedBytes(true) - before;
        if (bytes != 0) throw new InvalidOperationException($"Resident collision-to-solver path allocated {bytes} managed bytes.");
        CheckTransfer(gpu, 67, 0);
        warmed.generatedManifoldOwner = gpu; warmed.Reset();
        if (warmed.generatedManifoldOwner is not null) throw new Exception("Step reset retained an obsolete manifold source.");
        Console.WriteLine("Resident manifolds passed: CPU solve parity, graph/feature reordering, explicit overrides and stale geometry, 128 B/contact upload and zero warmed managed bytes.");
    }

    private static void CheckTransfer(GPUPhysicsWorld gpu, int resident, int uploaded)
    {
        if (gpu.ResidentContactCount != resident || gpu.UploadedManifoldCount != uploaded ||
            gpu.ContactUploadBytes != 128L * (resident + uploaded) + 80L * uploaded)
            throw new InvalidOperationException($"Contact transfer differs: {gpu.ResidentContactCount}/{resident} resident, {gpu.UploadedManifoldCount}/{uploaded} uploaded, {gpu.ContactUploadBytes} bytes.");
    }

    private static B2StepContext ResidentContext(int lanes)
    {
        var c = ConstraintContext(79, lanes); var world = c.world; var count = lanes + 2;
        world.shapes.data = new B2Shape[2 * count]; world.shapes.count = world.shapes.capacity = 2 * count;
        world.contacts.capacity = count + 1;
        world.frictionCallback = B2Worlds.b2DefaultFrictionCallback;
        world.restitutionCallback = B2Worlds.b2DefaultRestitutionCallback;
        world.enableSpeculative = true;
        c.contacts = new B2ContactSim[count];
        var id = 0;
        foreach (var group in c.graph.colors)
        {
            for (var i = 0; i < group.contactSims.count; i++, id++)
            {
                var contact = group.contactSims.data[i]; contact.contactId = id;
                contact.shapeIdA = 2 * id; contact.shapeIdB = 2 * id + 1;
                for (var side = 0; side < 2; side++)
                {
                    var index = side == 0 ? contact.bodySimIndexA : contact.bodySimIndexB;
                    world.shapes.data[2 * id + side] = new()
                    {
                        bodyId = index < 0 ? c.states.Length : index,
                        type = B2ShapeType.b2_polygonShape,
                        us = new() { polygon = B2Geometries.b2MakeBox(.8f, .8f) },
                        fatAABB = new() { lowerBound = new(-2, -2), upperBound = new(2, 2) },
                        material = new() { friction = .3f, restitution = .4f }
                    };
                }
                c.contacts[count - id - 1] = contact;
            }
        }
        return c;
    }

    private static void GenerateAndUpdate(GPUPhysicsWorld gpu, B2StepContext c)
    {
        gpu.GenerateManifolds(c, c.contacts.Count);
        UpdateGeneratedContacts(c);
    }

    private static void UpdateGeneratedContacts(B2StepContext c)
    {
        for (var i = 0; i < c.contacts.Count; i++)
        {
            var contact = c.contacts[i]; var a = c.world.shapes.data[contact.shapeIdA]; var b = c.world.shapes.data[contact.shapeIdB];
            var sa = B2Bodies.b2GetBodySim(c.world, c.world.bodies.data[a.bodyId]);
            var sb = B2Bodies.b2GetBodySim(c.world, c.world.bodies.data[b.bodyId]);
            if (!B2Contacts.b2UpdateContact(c.world, contact, a, sa.transform, b2RotateVector(sa.transform.q, sa.localCenter),
                    b, sb.transform, b2RotateVector(sb.transform.q, sb.localCenter), c.generatedManifolds, i))
                throw new Exception("Resident test contact must touch.");
        }
    }

    private static void CopyManifolds(B2StepContext source, B2StepContext target)
    {
        for (var color = 0; color < source.graph.colors.Length; color++)
            for (var i = 0; i < source.graph.colors[color].contactSims.count; i++)
                target.graph.colors[color].contactSims.data[i].CopyFrom(source.graph.colors[color].contactSims.data[i]);
    }

    private static void CompareContactImpulses(B2StepContext expected, B2StepContext actual)
    {
        for (var color = 0; color < expected.graph.colors.Length; color++)
            for (var i = 0; i < expected.graph.colors[color].contactSims.count; i++)
            {
                var e = expected.graph.colors[color].contactSims.data[i].manifold;
                var a = actual.graph.colors[color].contactSims.data[i].manifold;
                Near(e.rollingImpulse, a.rollingImpulse);
                for (var p = 0; p < e.pointCount; p++)
                {
                    Near(e.points[p].normalImpulse, a.points[p].normalImpulse); Near(e.points[p].tangentImpulse, a.points[p].tangentImpulse);
                    Near(e.points[p].totalNormalImpulse, a.points[p].totalNormalImpulse); Near(e.points[p].normalVelocity, a.points[p].normalVelocity);
                }
            }
    }
}
