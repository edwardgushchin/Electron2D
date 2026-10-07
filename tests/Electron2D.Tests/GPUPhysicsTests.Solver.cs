using Box2D.NET;
using Electron2D;

internal static partial class GPUPhysicsTests
{
    private static void VerifyConstraints(GPUPhysicsWorld gpu)
    {
        for (var sample = 0; sample < 64; sample++)
        {
            var expected = ConstraintContext(sample); var actual = ConstraintContext(sample);
            SolveCPU(expected); gpu.Solve(actual); Compare(expected, actual);
            for (var color = 0; color < B2Constants.B2_GRAPH_COLOR_COUNT; color++)
            {
                var e = expected.graph.colors[color]; var a = actual.graph.colors[color];
                for (var i = 0; i < e.contactSims.count; i++)
                {
                    if (color == B2Constants.B2_GRAPH_COLOR_COUNT - 1)
                    {
                        for (var point = 0; point < e.overflowConstraints[i].pointCount; point++)
                        {
                            Near(e.overflowConstraints[i].points[point].normalImpulse, a.overflowConstraints[i].points[point].normalImpulse);
                            Near(e.overflowConstraints[i].points[point].tangentImpulse, a.overflowConstraints[i].points[point].tangentImpulse);
                            Near(e.overflowConstraints[i].points[point].totalNormalImpulse, a.overflowConstraints[i].points[point].totalNormalImpulse);
                        }
                    }
                    else
                    {
                        var ec = e.simdConstraints[i / 8]; var ac = a.simdConstraints[i / 8]; var lane = i % 8;
                        Near(ec.normalImpulse1[lane], ac.normalImpulse1[lane]); Near(ec.normalImpulse2[lane], ac.normalImpulse2[lane]);
                        Near(ec.tangentImpulse1[lane], ac.tangentImpulse1[lane]); Near(ec.tangentImpulse2[lane], ac.tangentImpulse2[lane]);
                        Near(ec.totalNormalImpulse1[lane], ac.totalNormalImpulse1[lane]); Near(ec.totalNormalImpulse2[lane], ac.totalNormalImpulse2[lane]);
                    }
                }
                for (var i = 0; i < e.jointSims.count; i++)
                {
                    var ej = e.jointSims.data[i]; var aj = a.jointSims.data[i];
                    if (ej.type == B2JointType.b2_revoluteJoint)
                    {
                        var eq = ej.uj.revoluteJoint; var aq = aj.uj.revoluteJoint;
                        Near(eq.linearImpulse.X, aq.linearImpulse.X); Near(eq.linearImpulse.Y, aq.linearImpulse.Y); Near(eq.springImpulse, aq.springImpulse);
                        Near(eq.motorImpulse, aq.motorImpulse); Near(eq.lowerImpulse, aq.lowerImpulse); Near(eq.upperImpulse, aq.upperImpulse);
                    }
                    else
                    {
                        var eq = ej.uj.wheelJoint; var aq = aj.uj.wheelJoint;
                        Near(eq.perpImpulse, aq.perpImpulse); Near(eq.springImpulse, aq.springImpulse); Near(eq.motorImpulse, aq.motorImpulse);
                        Near(eq.lowerImpulse, aq.lowerImpulse); Near(eq.upperImpulse, aq.upperImpulse);
                    }
                }
            }
        }
        var warmed = ConstraintContext(712); gpu.Solve(warmed);
        var before = GC.GetTotalAllocatedBytes(true);
        for (var i = 0; i < 8; i++) gpu.Solve(warmed);
        var bytes = GC.GetTotalAllocatedBytes(true) - before;
        if (bytes != 0) throw new InvalidOperationException($"Prepared GPU constraints allocated {bytes} managed bytes.");
        Console.WriteLine("GPU contact/joint arithmetic, colored/overflow ordering and warmed allocation passed.");
    }

    private static B2StepContext ConstraintContext(int seed)
    {
        var c = Context(32); c.world.contactSpeed = 3; c.world.restitutionThreshold = .1f;
        c.graph.colors = new B2GraphColor[B2Constants.B2_GRAPH_COLOR_COUNT]; c.world.constraintGraph = c.graph;
        var random = new Random(seed + 714);
        float Number(float range) => (float)(random.NextDouble() * 2 - 1) * range;
        for (var i = 0; i < c.states.Length; i++)
        {
            var kinematic = i == 1;
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
                invMass = kinematic ? 0 : .4f,
                invInertia = kinematic ? 0 : .2f,
                force = new(Number(3), Number(3)),
                torque = Number(1),
                gravityScale = 1,
                linearDamping = .1f,
                angularDamping = .2f
            };
        }
        ref var contacts = ref c.graph.colors[0]; contacts.contactSims.count = 8; contacts.contactSims.data = new B2ContactSim[8];
        var packed = new B2ContactConstraintSIMD[1]; contacts.simdConstraints = packed;
        for (var lane = 0; lane < 8; lane++)
        {
            contacts.contactSims.data[lane] = new() { manifold = new() { pointCount = lane % 2 + 1 } };
            ref var p = ref packed[0]; var a = lane == 0 ? -1 : lane * 2; var b = lane * 2 + 1;
            p.indexA[lane] = a; p.indexB[lane] = b;
            p.invMassA[lane] = a < 0 ? 0 : c.sims[a].invMass; p.invIA[lane] = a < 0 ? 0 : c.sims[a].invInertia;
            p.invMassB[lane] = c.sims[b].invMass; p.invIB[lane] = c.sims[b].invInertia;
            var n = B2MathFunction.b2MakeRot(Number(3)); p.normal.X[lane] = n.c; p.normal.Y[lane] = n.s;
            p.friction[lane] = .3f; p.tangentSpeed[lane] = Number(.2f); p.rollingResistance[lane] = .05f; p.rollingMass[lane] = 1;
            p.restitution[lane] = lane % 3 == 0 ? .6f : 0; p.biasRate[lane] = 8; p.massScale[lane] = .7f; p.impulseScale[lane] = .3f;
            p.anchorA1.X[lane] = Number(.1f); p.anchorA1.Y[lane] = Number(.1f); p.anchorB1.X[lane] = Number(.1f); p.anchorB1.Y[lane] = Number(.1f);
            p.baseSeparation1[lane] = Number(.04f); p.normalMass1[lane] = 1; p.tangentMass1[lane] = .8f;
            p.normalImpulse1[lane] = .05f; p.relativeVelocity1[lane] = Number(2);
            if (lane % 2 != 0)
            {
                p.anchorA2.X[lane] = Number(.1f); p.anchorA2.Y[lane] = Number(.1f); p.anchorB2.X[lane] = Number(.1f); p.anchorB2.Y[lane] = Number(.1f);
                p.baseSeparation2[lane] = Number(.04f); p.normalMass2[lane] = 1; p.tangentMass2[lane] = .8f; p.normalImpulse2[lane] = .05f; p.relativeVelocity2[lane] = Number(2);
            }
        }
        ref var joints = ref c.graph.colors[1]; joints.jointSims.count = 8; joints.jointSims.data = new B2JointSim[8];
        for (var i = 0; i < 8; i++)
        {
            var j = new B2JointSim { invMassA = .4f, invMassB = .4f, invIA = .2f, invIB = .2f, constraintSoftness = new() { biasRate = 8, massScale = .7f, impulseScale = .3f } };
            var frameA = new B2Transform(new(.05f, .02f), B2MathFunction.b2MakeRot(.1f)); var frameB = new B2Transform(new(-.03f, .04f), B2MathFunction.b2MakeRot(-.1f));
            var spring = new B2Softness { biasRate = 3, massScale = .8f, impulseScale = .2f };
            if (i % 2 == 0)
            {
                j.type = B2JointType.b2_revoluteJoint;
                j.uj.revoluteJoint = new()
                {
                    indexA = 16 + i * 2,
                    indexB = 17 + i * 2,
                    frameA = frameA,
                    frameB = frameB,
                    deltaCenter = new(.02f, -.01f),
                    axialMass = 2.5f,
                    springSoftness = spring,
                    enableSpring = seed % 3 == 0,
                    enableMotor = seed % 2 == 0,
                    maxMotorTorque = 4,
                    motorSpeed = .4f,
                    enableLimit = true,
                    lowerAngle = -.15f,
                    upperAngle = .15f
                };
            }
            else
            {
                j.type = B2JointType.b2_wheelJoint;
                j.uj.wheelJoint = new()
                {
                    indexA = 16 + i * 2,
                    indexB = 17 + i * 2,
                    frameA = frameA,
                    frameB = frameB,
                    deltaCenter = new(.02f, -.01f),
                    axialMass = 1.2f,
                    perpMass = 1.2f,
                    motorMass = 2.5f,
                    springSoftness = spring,
                    enableSpring = seed % 3 == 0,
                    enableMotor = seed % 2 == 0,
                    maxMotorTorque = 4,
                    motorSpeed = .4f,
                    enableLimit = true,
                    lowerTranslation = -.01f,
                    upperTranslation = .01f
                };
            }
            joints.jointSims.data[i] = j;
        }
        ref var overflow = ref c.graph.colors[B2Constants.B2_GRAPH_COLOR_COUNT - 1]; overflow.contactSims.count = 2;
        overflow.contactSims.data = [new(), new()];
        var scalar = new B2ContactConstraint[2]; overflow.overflowConstraints = scalar;
        for (var i = 0; i < 2; i++)
        {
            scalar[i] = new()
            {
                indexA = 0,
                indexB = 2,
                invMassA = .4f,
                invMassB = .4f,
                invIA = .2f,
                invIB = .2f,
                normal = new(0, 1),
                pointCount = 1,
                friction = .3f,
                restitution = .4f,
                rollingMass = 2.5f,
                rollingResistance = .05f,
                softness = new() { biasRate = 8, massScale = .7f, impulseScale = .3f }
            };
            scalar[i].points[0] = new() { anchorA = new(.05f, 0), anchorB = new(.03f, 0), normalMass = 1.2f, tangentMass = 1.1f, baseSeparation = -.01f, relativeVelocity = -1 };
        }
        return c;
    }

    private static void SolveCPU(B2StepContext c)
    {
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
            B2ContactSolvers.b2ApplyRestitutionTask(0, (c.graph.colors[color].contactSims.count + 7) / 8, c, color);
    }
}
