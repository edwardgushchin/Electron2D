using Box2D.NET;
using Electron2D;

internal static partial class GPUPhysicsTests
{
    private static void VerifyConstraints(GPUPhysicsWorld gpu)
    {
        for (var sample = 0; sample < 64; sample++)
        {
            var expected = ConstraintContext(sample); var actual = ConstraintContext(sample);
            SolveCPU(expected); gpu.Solve(actual);
            try { Compare(expected, actual); }
            catch (Exception error) { throw new InvalidOperationException($"GPU raw constraint batch {sample} differs.", error); }
            for (var color = 0; color < B2Constants.B2_GRAPH_COLOR_COUNT; color++)
            {
                var e = expected.graph.colors[color]; var a = actual.graph.colors[color];
                for (var i = 0; i < e.contactSims.count; i++)
                {
                    var em = e.contactSims.data[i].manifold; var am = a.contactSims.data[i].manifold;
                    Near(em.rollingImpulse, am.rollingImpulse);
                    for (var point = 0; point < em.pointCount; point++)
                    {
                        var ep = em.points[point]; var ap = am.points[point];
                        Near(ep.normalImpulse, ap.normalImpulse); Near(ep.tangentImpulse, ap.tangentImpulse);
                        Near(ep.totalNormalImpulse, ap.totalNormalImpulse); Near(ep.normalVelocity, ap.normalVelocity);
                    }
                }
                for (var i = 0; i < e.jointSims.count; i++)
                {
                    var ej = e.jointSims.data[i]; var aj = a.jointSims.data[i];
                    Near(ej.constraintSoftness.biasRate, aj.constraintSoftness.biasRate);
                    Near(ej.constraintSoftness.massScale, aj.constraintSoftness.massScale);
                    Near(ej.constraintSoftness.impulseScale, aj.constraintSoftness.impulseScale);
                    if (ej.type == B2JointType.b2_revoluteJoint)
                    {
                        var eq = ej.uj.revoluteJoint; var aq = aj.uj.revoluteJoint;
                        ComparePreparedJoint(eq.frameA, aq.frameA, eq.frameB, aq.frameB, eq.deltaCenter, aq.deltaCenter, eq.springSoftness, aq.springSoftness);
                        Near(eq.axialMass, aq.axialMass);
                        Near(eq.linearImpulse.X, aq.linearImpulse.X); Near(eq.linearImpulse.Y, aq.linearImpulse.Y); Near(eq.springImpulse, aq.springImpulse);
                        Near(eq.motorImpulse, aq.motorImpulse); Near(eq.lowerImpulse, aq.lowerImpulse); Near(eq.upperImpulse, aq.upperImpulse);
                    }
                    else if (ej.type == B2JointType.b2_wheelJoint)
                    {
                        var eq = ej.uj.wheelJoint; var aq = aj.uj.wheelJoint;
                        ComparePreparedJoint(eq.frameA, aq.frameA, eq.frameB, aq.frameB, eq.deltaCenter, aq.deltaCenter, eq.springSoftness, aq.springSoftness);
                        Near(eq.axialMass, aq.axialMass); Near(eq.perpMass, aq.perpMass); Near(eq.motorMass, aq.motorMass);
                        Near(eq.perpImpulse, aq.perpImpulse); Near(eq.springImpulse, aq.springImpulse); Near(eq.motorImpulse, aq.motorImpulse);
                        try { Near(eq.lowerImpulse, aq.lowerImpulse); Near(eq.upperImpulse, aq.upperImpulse); }
                        catch (Exception error)
                        {
                            throw new InvalidOperationException($"Wheel batch {sample}, color {color}, joint {i}: lower {eq.lowerImpulse:R}/{aq.lowerImpulse:R}, upper {eq.upperImpulse:R}/{aq.upperImpulse:R}, axial {eq.axialMass:R}/{aq.axialMass:R}, perpendicular {eq.perpMass:R}/{aq.perpMass:R}.", error);
                        }
                    }
                }
            }
        }
        foreach (var count in new[] { 1, 61, 62, 63, 64, 65, 1025 })
        {
            var expected = ConstraintContext(79, count); var actual = ConstraintContext(79, count);
            SolveCPU(expected); gpu.Solve(actual); Compare(expected, actual);
        }
        var warmed = ConstraintContext(712); gpu.Solve(warmed);
        var before = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 8; i++) gpu.Solve(warmed);
        var bytes = GC.GetTotalAllocatedBytes(true) - before;
        if (bytes != 0) throw new InvalidOperationException($"Prepared GPU constraints allocated {bytes} managed bytes.");
        Console.WriteLine("GPU raw contact/joint preparation, four-substep solve, colored/overflow ordering and warmed allocation passed.");
    }

    private static void ComparePreparedJoint(B2Transform ea, B2Transform aa, B2Transform eb, B2Transform ab,
        B2Vec2 ed, B2Vec2 ad, B2Softness es, B2Softness a)
    {
        Near(ea.p.X, aa.p.X); Near(ea.p.Y, aa.p.Y); Near(ea.q.c, aa.q.c); Near(ea.q.s, aa.q.s);
        Near(eb.p.X, ab.p.X); Near(eb.p.Y, ab.p.Y); Near(eb.q.c, ab.q.c); Near(eb.q.s, ab.q.s);
        Near(ed.X, ad.X); Near(ed.Y, ad.Y); Near(es.biasRate, a.biasRate); Near(es.massScale, a.massScale); Near(es.impulseScale, a.impulseScale);
    }

    private static B2StepContext ConstraintContext(int seed, int lanes = 8)
    {
        var c = Context(4 * lanes); var world = c.world;
        world.contactSpeed = 3; world.restitutionThreshold = .1f;
        world.contactHertz = seed % 7 == 0 ? 0 : 60; world.contactDampingRatio = seed % 5 == 0 ? 0 : 1.5f;
        world.enableWarmStarting = seed % 2 != 0; c.enableWarmStarting = seed % 3 != 0;
        world.enableContactSoftening = seed % 4 < 2;
        c.graph.colors = new B2GraphColor[B2Constants.B2_GRAPH_COLOR_COUNT]; world.constraintGraph = c.graph;
        world.bodies.data = new B2Body[c.states.Length + 1]; world.bodies.count = world.bodies.capacity = world.bodies.data.Length;
        world.solverSets.data[2].bodySims.count = world.solverSets.data[2].bodySims.capacity = c.sims.Length;
        world.solverSets.data[2].bodyStates.capacity = c.states.Length;
        var random = new Random(seed + 714);
        float Number(float range) => (float)(random.NextDouble() * 2 - 1) * range;
        for (var i = 0; i < c.states.Length; i++)
        {
            var kinematic = i == 1;
            world.bodies.data[i] = new() { setIndex = 2, localIndex = i };
            c.states[i] = new()
            {
                flags = kinematic ? 0 : (uint)B2BodyFlags.b2_dynamicFlag,
                deltaRotation = B2MathFunction.b2MakeRot(Number(.1f)),
                linearVelocity = new(Number(3), Number(3)),
                angularVelocity = Number(1),
                deltaPosition = new(Number(.02f), Number(.02f))
            };
            c.sims[i] = new()
            {
                invMass = kinematic ? 0 : (i % 3 == 0 ? .8f : .2f),
                invInertia = kinematic || seed % 11 == 0 ? 0 : .2f,
                force = new(Number(3), Number(3)),
                torque = Number(1),
                gravityScale = 1,
                linearDamping = .1f,
                angularDamping = .2f,
                transform = new(new(Number(.3f), Number(.3f)), B2MathFunction.b2MakeRot(Number(.5f))),
                localCenter = new(Number(.1f), Number(.1f)),
                center = new(Number(.2f), Number(.2f))
            };
        }
        world.bodies.data[^1] = new() { setIndex = 0, localIndex = 0 };
        world.solverSets.data[0].bodySims.data = [new() { transform = new(new(.3f, .1f), B2MathFunction.b2MakeRot(.2f)), localCenter = new(.01f, .02f), center = new(.3f, .1f) }];
        world.solverSets.data[0].bodySims.count = world.solverSets.data[0].bodySims.capacity = 1;
        ref var contacts = ref c.graph.colors[0]; contacts.contactSims.count = lanes;
        contacts.contactSims.data = new B2ContactSim[(lanes + 7) / 8 * 8];
        contacts.simdConstraints = new B2ContactConstraintSIMD[(lanes + 7) / 8];
        for (var i = 0; i < lanes; i++) contacts.contactSims.data[i] = Contact(i == 0 ? -1 : i * 2, i * 2 + 1, i % 2 + 1);
        ref var joints = ref c.graph.colors[1]; joints.jointSims.count = lanes; joints.jointSims.data = new B2JointSim[lanes];
        for (var i = 0; i < lanes; i++) joints.jointSims.data[i] = Joint(i % 2 == 0, i == 0 ? c.states.Length : i == 1 ? 1 : 2 * lanes + i * 2, 2 * lanes + i * 2 + 1);
        ref var overflow = ref c.graph.colors[B2Constants.B2_GRAPH_COLOR_COUNT - 1];
        overflow.contactSims.count = 2; overflow.contactSims.data = [Contact(0, 2, 1), Contact(0, 2, 2)];
        overflow.overflowConstraints = new B2ContactConstraint[2];
        overflow.jointSims.count = 2;
        overflow.jointSims.data = [Joint(seed % 2 == 0, 0, 2), new() { type = B2JointType.b2_filterJoint, constraintHertz = 13, constraintDampingRatio = .8f, invMassA = .2f, invMassB = .4f }];
        if (seed % 2 == 0)
        {
            var surface = overflow.contactSims.data[0];
            surface.bodySimIndexA = -1; surface.invMassA = surface.invIA = 0;
            surface.surfaceLinearA = new(.3f, -.4f); surface.surfaceAngularA = .7f;
            if (lanes > 1)
            {
                surface = contacts.contactSims.data[1];
                surface.bodySimIndexB = -1; surface.invMassB = surface.invIB = 0;
                surface.surfaceLinearB = new(-.25f, .15f); surface.surfaceAngularB = -.5f;
            }
        }
        return c;

        B2ContactSim Contact(int a, int b, int points)
        {
            var normal = B2MathFunction.b2MakeRot(Number(3));
            var contact = new B2ContactSim
            {
                bodySimIndexA = a,
                bodySimIndexB = b,
                invMassA = a < 0 ? 0 : c.sims[a].invMass,
                invMassB = c.sims[b].invMass,
                invIA = a < 0 ? 0 : c.sims[a].invInertia,
                invIB = c.sims[b].invInertia,
                friction = .3f,
                restitution = .4f,
                rollingResistance = .05f,
                tangentSpeed = Number(.2f),
                manifold = new() { normal = new(normal.c, normal.s), pointCount = points, rollingImpulse = .01f }
            };
            for (var i = 0; i < points; i++) contact.manifold.points[i] = new()
            {
                anchorA = new(Number(.1f), Number(.1f)),
                anchorB = new(Number(.1f), Number(.1f)),
                separation = Number(.04f),
                normalImpulse = .05f,
                tangentImpulse = .01f
            };
            return contact;
        }
        B2JointSim Joint(bool pin, int a, int b)
        {
            var joint = new B2JointSim
            {
                bodyIdA = a,
                bodyIdB = b,
                constraintHertz = seed % 5 == 0 ? 0 : 120,
                constraintDampingRatio = 1,
                localFrameA = new(new(.05f, .02f), B2MathFunction.b2MakeRot(.1f)),
                localFrameB = new(new(-.03f, .04f), B2MathFunction.b2MakeRot(-.1f))
            };
            if (pin)
            {
                joint.type = B2JointType.b2_revoluteJoint;
                joint.uj.revoluteJoint = new()
                {
                    hertz = seed % 7 == 0 ? 0 : 3,
                    dampingRatio = .8f,
                    enableSpring = seed % 3 == 0,
                    enableMotor = seed % 2 == 0,
                    maxMotorTorque = 4,
                    motorSpeed = .4f,
                    enableLimit = true,
                    lowerAngle = -.15f,
                    upperAngle = .15f,
                    targetAngle = .02f,
                    linearImpulse = new(.01f, -.02f),
                    springImpulse = .03f,
                    motorImpulse = -.02f,
                    lowerImpulse = .01f,
                    upperImpulse = .02f
                };
            }
            else
            {
                joint.type = B2JointType.b2_wheelJoint;
                joint.uj.wheelJoint = new()
                {
                    hertz = seed % 7 == 0 ? 0 : 3,
                    dampingRatio = .8f,
                    enableSpring = seed % 3 == 0,
                    enableMotor = seed % 2 == 0,
                    maxMotorTorque = 4,
                    motorSpeed = .4f,
                    enableLimit = true,
                    lowerTranslation = -.01f,
                    upperTranslation = .01f,
                    perpImpulse = .01f,
                    springImpulse = .03f,
                    motorImpulse = -.02f,
                    lowerImpulse = .01f,
                    upperImpulse = .02f
                };
            }
            return joint;
        }
    }

    private static void SolveCPU(B2StepContext c)
    {
        var hz = MathF.Min(c.world.contactHertz, .125f * c.inv_h);
        c.contactSoftness = B2Solvers.b2MakeSoft(hz, c.world.contactDampingRatio, c.h);
        c.staticSoftness = B2Solvers.b2MakeSoft(2 * hz, c.world.contactDampingRatio, c.h);
        for (var color = 0; color < B2Constants.B2_GRAPH_COLOR_COUNT; color++)
        {
            var group = c.graph.colors[color];
            for (var i = 0; i < group.jointSims.count; i++) B2Joints.b2PrepareJoint(group.jointSims.data[i], c);
            if (color == B2Constants.B2_GRAPH_COLOR_COUNT - 1) B2ContactSolvers.b2PrepareOverflowContacts(c);
            else if (group.contactSims.count > 0)
            {
                c.contacts = group.contactSims.data; c.simdContactConstraints = group.simdConstraints;
                B2ContactSolvers.b2PrepareContactsTask(0, (group.contactSims.count + 7) / 8, c);
            }
        }
        void Constraints(bool warm, bool bias)
        {
            if (warm) { B2Joints.b2WarmStartOverflowJoints(c); B2ContactSolvers.b2WarmStartOverflowContacts(c); }
            else { B2Joints.b2SolveOverflowJoints(c, bias); B2ContactSolvers.b2SolveOverflowContacts(c, bias); }
            for (var color = 0; color < B2Constants.B2_GRAPH_COLOR_COUNT - 1; color++)
            {
                var group = c.graph.colors[color]; var count = (group.contactSims.count + 7) / 8;
                if (warm) B2ContactSolvers.b2WarmStartContactsTask(0, count, c, color);
                else B2ContactSolvers.b2SolveContactsTask(0, count, c, color, bias);
                foreach (var j in group.jointSims.data ?? [])
                    if (warm) B2Joints.b2WarmStartJoint(j, c); else B2Joints.b2SolveJoint(j, c, bias);
            }
        }
        for (var i = 0; i < c.subStepCount; i++)
        {
            B2Solvers.b2IntegrateVelocitiesTask(0, c.states.Length, c); Constraints(true, true); Constraints(false, true);
            B2Solvers.b2IntegratePositionsTask(0, c.states.Length, c); Constraints(false, false);
        }
        B2ContactSolvers.b2ApplyOverflowRestitution(c);
        for (var color = 0; color < B2Constants.B2_GRAPH_COLOR_COUNT - 1; color++)
        {
            var group = c.graph.colors[color];
            B2ContactSolvers.b2ApplyRestitutionTask(0, (group.contactSims.count + 7) / 8, c, color);
            if (group.contactSims.count == 0) continue;
            c.contacts = group.contactSims.data; c.simdContactConstraints = group.simdConstraints;
            B2ContactSolvers.b2StoreImpulsesTask(0, (group.contactSims.count + 7) / 8, c);
        }
        B2ContactSolvers.b2StoreOverflowImpulses(c);
    }
}
