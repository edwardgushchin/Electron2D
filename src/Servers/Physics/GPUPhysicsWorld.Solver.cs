using System.Runtime.InteropServices;
using Box2D.NET;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Contact
    {
        internal Float4 IDs, Mass, Normal, Rolling, Soft;
        internal Float4 Anchors1, Params1, Impulses1, Anchors2, Params2, Impulses2;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Joint
    {
        internal Float4 IDs, Mass, FrameA, FrameB, Geometry, Soft, Spring, Motor, Impulses, Limits;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct SolverStep
    {
        internal Float4 Values, Control, Solve;
    }
    private readonly Storage<Contact> _contactStorage;
    private readonly Storage<Joint> _jointStorage;
    private readonly int[] _contactStarts = new int[B2Constants.B2_GRAPH_COLOR_COUNT], _contactCounts = new int[B2Constants.B2_GRAPH_COLOR_COUNT];
    private readonly int[] _jointStarts = new int[B2Constants.B2_GRAPH_COLOR_COUNT], _jointCounts = new int[B2Constants.B2_GRAPH_COLOR_COUNT];
    internal long SolverSubmissionCount { get; private set; }
    internal readonly double[] SolverProfileMS = new double[4];

    internal void Solve(B2StepContext context)
    {
        EnsureOwner();
        var profileStart = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        var count = context.world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodyStates.count;
        if (count == 0) return;
        // Follow the world's prepared storage budget rather than its temporarily sleeping population.
        _bodyStorage.Reserve(Math.Max(count, context.world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodyStates.capacity));
        PackBodies(context, count);
        var contactCount = 0; var jointCount = 0;
        for (var color = 0; color < B2Constants.B2_GRAPH_COLOR_COUNT; color++)
        {
            _contactStarts[color] = contactCount; _jointStarts[color] = jointCount;
            _contactCounts[color] = context.graph.colors[color].contactSims.count;
            _jointCounts[color] = context.graph.colors[color].jointSims.count;
            contactCount += _contactCounts[color]; jointCount += _jointCounts[color];
        }
        _contactStorage.Reserve(Math.Max(Math.Max(1, contactCount), context.world.contacts.capacity));
        _jointStorage.Reserve(Math.Max(Math.Max(1, jointCount), context.world.joints.capacity));
        PackConstraints(context);
        var profilePacked = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire constraint commands");
        nint fence = 0;
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin constraint upload");
            _bodyStorage.Upload(copy, count); _contactStorage.Upload(copy, contactCount); _jointStorage.Upload(copy, jointCount);
            SDL.EndGPUCopyPass(copy);
            var settings = new SolverStep
            {
                Values = new(context.world.gravity.X, context.world.gravity.Y, context.h, context.maxLinearVelocity),
                Solve = new(context.world.restitutionThreshold, context.inv_h, context.world.contactSpeed, 0)
            };
            for (var substep = 0; substep < context.subStepCount; substep++)
            {
                ExecuteSolverStage(command, ref settings, 0, 0, count, false, count, context);
                ExecuteConstraints(command, ref settings, context, 2, 6, count);
                for (var iteration = 0; iteration < B2Solvers.ITERATIONS; iteration++)
                    ExecuteConstraints(command, ref settings, context, 3, 7, count);
                ExecuteSolverStage(command, ref settings, 1, 0, count, false, count, context);
                for (var iteration = 0; iteration < B2Solvers.RELAX_ITERATIONS; iteration++)
                    ExecuteConstraints(command, ref settings, context, 4, 8, count);
            }
            ExecuteConstraints(command, ref settings, context, 5, -1, count);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin solved-state readback");
            _bodyStorage.Download(copy, count); _contactStorage.Download(copy, contactCount); _jointStorage.Download(copy, jointCount);
            SDL.EndGPUCopyPass(copy);
            var profileRecorded = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            var submitted = command; command = 0; fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
            if (fence == 0) throw Failure("submit the constraint solver");
            Check(SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "wait for solved state");
            var profileFinished = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            _bodyStorage.Read(count); _contactStorage.Read(contactCount); _jointStorage.Read(jointCount);
            for (var i = 0; i < contactCount; i++)
                if (!Finite(_contactStorage.Data[i].Impulses1) || !Finite(_contactStorage.Data[i].Impulses2) || !Finite(_contactStorage.Data[i].Rolling))
                    throw new InvalidOperationException("GPU contact solving returned nonfinite impulses.");
            for (var i = 0; i < jointCount; i++)
                if (!Finite(_jointStorage.Data[i].Impulses) || !Finite(_jointStorage.Data[i].Limits))
                    throw new InvalidOperationException("GPU joint solving returned nonfinite impulses.");
            PublishBodies(context, count); PublishConstraints(context);
            if (PhysicsSpace.ProfilingEnabled)
            {
                SolverProfileMS[0] = System.Diagnostics.Stopwatch.GetElapsedTime(profileStart, profilePacked).TotalMilliseconds;
                SolverProfileMS[1] = System.Diagnostics.Stopwatch.GetElapsedTime(profilePacked, profileRecorded).TotalMilliseconds;
                SolverProfileMS[2] = System.Diagnostics.Stopwatch.GetElapsedTime(profileRecorded, profileFinished).TotalMilliseconds;
                SolverProfileMS[3] = System.Diagnostics.Stopwatch.GetElapsedTime(profileFinished).TotalMilliseconds;
            }
            SolverSubmissionCount++;
        }
        finally
        {
            if (command != 0) SDL.CancelGPUCommandBuffer(command);
            if (fence != 0) SDL.ReleaseGPUFence(Device, fence);
        }
    }

    private static bool Finite(Float4 v) => float.IsFinite(v.X) && float.IsFinite(v.Y) && float.IsFinite(v.Z) && float.IsFinite(v.W);

    private void ExecuteConstraints(nint command, ref SolverStep settings, B2StepContext context, int contactStage, int jointStage, int bodies)
    {
        const int overflow = B2Constants.B2_GRAPH_COLOR_COUNT - 1;
        if (jointStage >= 0) ExecuteSolverStage(command, ref settings, jointStage, _jointStarts[overflow], _jointCounts[overflow], true, bodies, context);
        ExecuteSolverStage(command, ref settings, contactStage, _contactStarts[overflow], _contactCounts[overflow], true, bodies, context);
        for (var color = 0; color < overflow; color++)
        {
            ExecuteSolverStage(command, ref settings, contactStage, _contactStarts[color], _contactCounts[color], false, bodies, context);
            if (jointStage >= 0) ExecuteSolverStage(command, ref settings, jointStage, _jointStarts[color], _jointCounts[color], false, bodies, context);
        }
    }

    private void ExecuteSolverStage(nint command, ref SolverStep settings, int stage, int start, int count, bool serial, int bodies, B2StepContext context)
    {
        if (count == 0) return;
        settings.Control = new(stage < 2 ? B2Constants.B2_MAX_ROTATION * context.inv_dt : start, stage, bodies, count);
        settings.Solve.W = serial ? 1 : 0;
        Span<SDL.GPUStorageBufferReadWriteBinding> binding = stackalloc SDL.GPUStorageBufferReadWriteBinding[3];
        binding[0] = new() { Buffer = _bodyStorage.Handle }; binding[1] = new() { Buffer = _contactStorage.Handle }; binding[2] = new() { Buffer = _jointStorage.Handle };
        var compute = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0, binding, 3);
        if (compute == 0) throw Failure("begin a constraint stage");
        SDL.BindGPUComputePipeline(compute, _solve.DangerousGetHandle());
        fixed (SolverStep* uniform = &settings) SDL.PushGPUComputeUniformData(command, 0, (nint)uniform, (uint)sizeof(SolverStep));
        SDL.DispatchGPUCompute(compute, serial ? 1 : checked((uint)(count + 63) / 64), 1, 1);
        SDL.EndGPUComputePass(compute); DispatchCount++;
    }

    private void PackConstraints(B2StepContext context)
    {
        for (var color = 0; color < B2Constants.B2_GRAPH_COLOR_COUNT; color++)
        {
            ref var group = ref context.graph.colors[color];
            for (var i = 0; i < group.contactSims.count; i++)
            {
                Contact packet;
                if (color == B2Constants.B2_GRAPH_COLOR_COUNT - 1)
                {
                    ref var c = ref group.overflowConstraints.AsSpan()[i]; var p1 = c.points[0]; var p2 = c.points[1];
                    packet = new()
                    {
                        IDs = new(c.indexA, c.indexB, c.pointCount, 1),
                        Mass = new(c.invMassA, c.invMassB, c.invIA, c.invIB),
                        Normal = new(c.normal.X, c.normal.Y, c.friction, c.tangentSpeed),
                        Rolling = new(c.rollingResistance, c.rollingMass, c.rollingImpulse, c.restitution),
                        Soft = new(c.softness.biasRate, c.softness.massScale, c.softness.impulseScale, 0),
                        Anchors1 = new(p1.anchorA.X, p1.anchorA.Y, p1.anchorB.X, p1.anchorB.Y),
                        Params1 = new(p1.normalMass, p1.tangentMass, p1.baseSeparation, p1.relativeVelocity),
                        Impulses1 = new(p1.normalImpulse, p1.tangentImpulse, p1.totalNormalImpulse, 0),
                        Anchors2 = new(p2.anchorA.X, p2.anchorA.Y, p2.anchorB.X, p2.anchorB.Y),
                        Params2 = new(p2.normalMass, p2.tangentMass, p2.baseSeparation, p2.relativeVelocity),
                        Impulses2 = new(p2.normalImpulse, p2.tangentImpulse, p2.totalNormalImpulse, 0)
                    };
                }
                else
                {
                    ref var c = ref group.simdConstraints.AsSpan()[i / B2Cores.B2_SIMD_WIDTH]; var lane = i % B2Cores.B2_SIMD_WIDTH;
                    packet = new()
                    {
                        IDs = new(c.indexA[lane], c.indexB[lane], group.contactSims.data[i].manifold.pointCount, 0),
                        Mass = new(c.invMassA[lane], c.invMassB[lane], c.invIA[lane], c.invIB[lane]),
                        Normal = new(c.normal.X[lane], c.normal.Y[lane], c.friction[lane], c.tangentSpeed[lane]),
                        Rolling = new(c.rollingResistance[lane], c.rollingMass[lane], c.rollingImpulse[lane], c.restitution[lane]),
                        Soft = new(c.biasRate[lane], c.massScale[lane], c.impulseScale[lane], 0),
                        Anchors1 = new(c.anchorA1.X[lane], c.anchorA1.Y[lane], c.anchorB1.X[lane], c.anchorB1.Y[lane]),
                        Params1 = new(c.normalMass1[lane], c.tangentMass1[lane], c.baseSeparation1[lane], c.relativeVelocity1[lane]),
                        Impulses1 = new(c.normalImpulse1[lane], c.tangentImpulse1[lane], c.totalNormalImpulse1[lane], 0),
                        Anchors2 = new(c.anchorA2.X[lane], c.anchorA2.Y[lane], c.anchorB2.X[lane], c.anchorB2.Y[lane]),
                        Params2 = new(c.normalMass2[lane], c.tangentMass2[lane], c.baseSeparation2[lane], c.relativeVelocity2[lane]),
                        Impulses2 = new(c.normalImpulse2[lane], c.tangentImpulse2[lane], c.totalNormalImpulse2[lane], 0)
                    };
                }
                _contactStorage.Data[_contactStarts[color] + i] = packet;
            }
            for (var i = 0; i < group.jointSims.count; i++) _jointStorage.Data[_jointStarts[color] + i] = PackJoint(group.jointSims.data[i]);
        }
    }

    private static Joint PackJoint(B2JointSim j)
    {
        if (j.type == B2JointType.b2_filterJoint) return default;
        var p = new Joint { Mass = new(j.invMassA, j.invMassB, j.invIA, j.invIB), Soft = new(j.constraintSoftness.biasRate, j.constraintSoftness.massScale, j.constraintSoftness.impulseScale, 0) };
        if (j.type == B2JointType.b2_revoluteJoint)
        {
            ref var q = ref j.uj.revoluteJoint;
            p.IDs = new(q.indexA, q.indexB, 1, (q.enableSpring ? 1 : 0) | (q.enableMotor ? 2 : 0) | (q.enableLimit ? 4 : 0));
            p.FrameA = new(q.frameA.p.X, q.frameA.p.Y, q.frameA.q.c, q.frameA.q.s); p.FrameB = new(q.frameB.p.X, q.frameB.p.Y, q.frameB.q.c, q.frameB.q.s);
            p.Geometry = new(q.deltaCenter.X, q.deltaCenter.Y, q.axialMass, 0); p.Spring = new(q.springSoftness.biasRate, q.springSoftness.massScale, q.springSoftness.impulseScale, q.targetAngle);
            p.Motor = new(q.maxMotorTorque, q.motorSpeed, q.lowerAngle, q.upperAngle); p.Impulses = new(q.linearImpulse.X, q.linearImpulse.Y, q.springImpulse, q.motorImpulse); p.Limits = new(q.lowerImpulse, q.upperImpulse, 0, 0);
        }
        else if (j.type == B2JointType.b2_wheelJoint)
        {
            ref var q = ref j.uj.wheelJoint;
            p.IDs = new(q.indexA, q.indexB, 2, (q.enableSpring ? 1 : 0) | (q.enableMotor ? 2 : 0) | (q.enableLimit ? 4 : 0));
            p.FrameA = new(q.frameA.p.X, q.frameA.p.Y, q.frameA.q.c, q.frameA.q.s); p.FrameB = new(q.frameB.p.X, q.frameB.p.Y, q.frameB.q.c, q.frameB.q.s);
            p.Geometry = new(q.deltaCenter.X, q.deltaCenter.Y, q.axialMass, q.perpMass); p.Soft.W = q.motorMass;
            p.Spring = new(q.springSoftness.biasRate, q.springSoftness.massScale, q.springSoftness.impulseScale, 0); p.Motor = new(q.maxMotorTorque, q.motorSpeed, q.lowerTranslation, q.upperTranslation);
            p.Impulses = new(q.perpImpulse, 0, q.springImpulse, q.motorImpulse); p.Limits = new(q.lowerImpulse, q.upperImpulse, 0, 0);
        }
        else throw new NotSupportedException($"GPU constraints do not support the internal {j.type} joint yet.");
        return p;
    }

    private void PublishConstraints(B2StepContext context)
    {
        for (var color = 0; color < B2Constants.B2_GRAPH_COLOR_COUNT; color++)
        {
            ref var group = ref context.graph.colors[color];
            for (var i = 0; i < group.contactSims.count; i++)
            {
                var packet = _contactStorage.Data[_contactStarts[color] + i];
                if (color == B2Constants.B2_GRAPH_COLOR_COUNT - 1)
                {
                    ref var c = ref group.overflowConstraints.AsSpan()[i]; c.rollingImpulse = packet.Rolling.Z;
                    c.points[0].normalImpulse = packet.Impulses1.X; c.points[0].tangentImpulse = packet.Impulses1.Y; c.points[0].totalNormalImpulse = packet.Impulses1.Z;
                    c.points[1].normalImpulse = packet.Impulses2.X; c.points[1].tangentImpulse = packet.Impulses2.Y; c.points[1].totalNormalImpulse = packet.Impulses2.Z;
                }
                else
                {
                    ref var c = ref group.simdConstraints.AsSpan()[i / B2Cores.B2_SIMD_WIDTH]; var lane = i % B2Cores.B2_SIMD_WIDTH;
                    c.rollingImpulse[lane] = packet.Rolling.Z;
                    c.normalImpulse1[lane] = packet.Impulses1.X; c.tangentImpulse1[lane] = packet.Impulses1.Y; c.totalNormalImpulse1[lane] = packet.Impulses1.Z;
                    c.normalImpulse2[lane] = packet.Impulses2.X; c.tangentImpulse2[lane] = packet.Impulses2.Y; c.totalNormalImpulse2[lane] = packet.Impulses2.Z;
                }
            }
            for (var i = 0; i < group.jointSims.count; i++)
            {
                var joint = group.jointSims.data[i]; var p = _jointStorage.Data[_jointStarts[color] + i];
                if (joint.type == B2JointType.b2_revoluteJoint)
                {
                    ref var q = ref joint.uj.revoluteJoint; q.linearImpulse = new(p.Impulses.X, p.Impulses.Y); q.springImpulse = p.Impulses.Z; q.motorImpulse = p.Impulses.W; q.lowerImpulse = p.Limits.X; q.upperImpulse = p.Limits.Y;
                }
                else if (joint.type == B2JointType.b2_wheelJoint)
                {
                    ref var q = ref joint.uj.wheelJoint; q.perpImpulse = p.Impulses.X; q.springImpulse = p.Impulses.Z; q.motorImpulse = p.Impulses.W; q.lowerImpulse = p.Limits.X; q.upperImpulse = p.Limits.Y;
                }
            }
        }
    }
}
