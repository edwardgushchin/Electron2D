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
        internal Float4 Anchors1, Params1, Impulses1, Anchors2, Params2, Impulses2, SurfaceA, SurfaceB, History1, History2;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct ContactInput
    {
        internal Float4 IDs, Mass, Material, Warm, Source, Offset, SurfaceA, SurfaceB, History1, History2;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct Joint
    {
        internal Float4 IDs, Mass, FrameA, FrameB, Geometry, Soft, Spring, Motor, Impulses, Limits, PoseA, PoseB, PolicyBias, PolicyForce;
    }
    [StructLayout(LayoutKind.Sequential)]
    private struct SolverStep
    {
        internal Float4 Values, Control, Solve, Preparation, Motion;
    }
    private readonly Storage<Contact> _contactStorage;
    private B2World? _solvedWorld;
    private readonly Storage<ContactInput> _contactInputStorage;
    private readonly Storage<ManifoldResult> _fallbackManifoldStorage;
    internal int ResidentContactCount { get; private set; }
    internal int UploadedManifoldCount { get; private set; }
    internal long ContactUploadBytes { get; private set; }
    private readonly Storage<Joint> _jointStorage;
    private readonly int[] _contactStarts = new int[B2Constants.B2_GRAPH_COLOR_COUNT], _contactCounts = new int[B2Constants.B2_GRAPH_COLOR_COUNT];
    private readonly int[] _jointStarts = new int[B2Constants.B2_GRAPH_COLOR_COUNT], _jointCounts = new int[B2Constants.B2_GRAPH_COLOR_COUNT];
    internal long SolverSubmissionCount { get; private set; }
    internal readonly double[] SolverProfileMS = new double[4];

    internal void Solve(B2StepContext context)
    {
        EnsureOwner(); _pendingFinalization = null; context.finalizedBodies = null!;
        var profileStart = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        var count = context.world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodyStates.count;
        ResidentContactCount = UploadedManifoldCount = 0; ContactUploadBytes = 0;
        if (count == 0) { context.generatedManifoldOwner = null!; _manifoldContext = null; return; }
        // A new solve can replace or overwrite the prior buffer before it succeeds.
        _solvedWorld = null;
        // Follow the world's prepared storage budget rather than its temporarily sleeping population.
        _bodyStorage.Reserve(Math.Max(count, context.world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodyStates.capacity));
        PackBodies(context, count);
        var finalize = ReferenceEquals(_bodyFinalizationWorld, context.world) && context.world.finalizeBodyStates == _consumeFinalization;
        if (finalize) PackFinalization(context, count);
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
        _contactInputStorage.Reserve(_contactStorage.Data.Length);
        _fallbackManifoldStorage.Reserve(_contactStorage.Data.Length);
        _manifoldStorage.Reserve(1); _matchedStorage.Reserve(1);
        PackConstraints(context);
        context.generatedManifoldOwner = null!; _manifoldContext = null;
        ContactUploadBytes = (long)contactCount * sizeof(ContactInput) + (long)UploadedManifoldCount * sizeof(ManifoldResult);
        var profilePacked = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire constraint commands");
        nint fence = 0;
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin constraint upload");
            _bodyStorage.Upload(copy, count); _contactInputStorage.Upload(copy, contactCount); _jointStorage.Upload(copy, jointCount);
            _fallbackManifoldStorage.Upload(copy, UploadedManifoldCount);
            if (finalize) _finalizationInputs.Upload(copy, count);
            SDL.EndGPUCopyPass(copy);
            var settings = new SolverStep
            {
                Values = new(context.world.gravity.X, context.world.gravity.Y, context.h, context.maxLinearVelocity),
                Solve = new(context.world.restitutionThreshold, context.inv_h, context.world.contactSpeed, 0),
                Preparation = new(context.world.contactHertz, context.world.contactDampingRatio, context.world.enableContactSoftening ? 1 : 0,
                    (context.world.enableWarmStarting ? 1 : 0) | (context.enableWarmStarting ? 2 : 0))
            };
            ExecuteSolverStage(command, ref settings, 9, 0, contactCount, false, count, context);
            ExecuteSolverStage(command, ref settings, 10, 0, jointCount, false, count, context);
            var sweeps = contactCount > 0 || jointCount > 0 ? context.world.solverIterations : 0;
            for (var substep = 0; substep < context.subStepCount; substep++)
            {
                settings.Motion.X = substep * context.h;
                ExecuteSolverStage(command, ref settings, 0, 0, count, false, count, context);
                ExecuteConstraints(command, ref settings, context, 2, 6, count);
                for (var iteration = 0; iteration < sweeps; iteration++)
                    ExecuteConstraints(command, ref settings, context, 3, 7, count);
                ExecuteSolverStage(command, ref settings, 1, 0, count, false, count, context);
                settings.Motion.X = (substep + 1) * context.h;
                for (var iteration = 0; iteration < sweeps; iteration++)
                    ExecuteConstraints(command, ref settings, context, 4, 8, count);
            }
            ExecuteConstraints(command, ref settings, context, 5, -1, count);
            if (finalize) RecordFinalization(command, context, count);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin solved-state readback");
            _bodyStorage.Download(copy, count); _contactStorage.Download(copy, contactCount); _jointStorage.Download(copy, jointCount);
            if (finalize) _finalizationResults.Download(copy, count);
            SDL.EndGPUCopyPass(copy);
            var profileRecorded = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            var submitted = command; command = 0; fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
            if (fence == 0) throw Failure("submit the constraint solver");
            Check(SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "wait for solved state");
            var profileFinished = PhysicsSpace.ProfilingEnabled ? System.Diagnostics.Stopwatch.GetTimestamp() : 0;
            _bodyStorage.Read(count); _contactStorage.Read(contactCount); _jointStorage.Read(jointCount);
            for (var i = 0; i < contactCount; i++)
                if (!Finite(_contactStorage.Data[i].Impulses1) || !Finite(_contactStorage.Data[i].Impulses2) || !Finite(_contactStorage.Data[i].Rolling) ||
                    !Finite(_contactStorage.Data[i].SurfaceA) || !Finite(_contactStorage.Data[i].SurfaceB) || !Finite(_contactStorage.Data[i].Params1) || !Finite(_contactStorage.Data[i].Params2) || !Finite(_contactStorage.Data[i].Soft))
                    throw new InvalidOperationException("GPU contact preparation or solving returned nonfinite state.");
            for (var i = 0; i < jointCount; i++)
                if (!Finite(_jointStorage.Data[i].Impulses) || !Finite(_jointStorage.Data[i].Limits) || !Finite(_jointStorage.Data[i].FrameA) ||
                    !Finite(_jointStorage.Data[i].FrameB) || !Finite(_jointStorage.Data[i].Geometry) || !Finite(_jointStorage.Data[i].Soft) || !Finite(_jointStorage.Data[i].Spring))
                    throw new InvalidOperationException("GPU joint preparation or solving returned nonfinite state.");
            if (finalize) { _finalizationResults.Read(count); ValidateFinalization(context, count); }
            PublishBodies(context, count); PublishConstraints(context);
            if (finalize)
            {
                _pendingFinalization = context; _finalizationCount = count; BodyFinalizationBatchCount++;
                BodyFinalizationTransferBytes += (long)count * (sizeof(FinalizationInput) + sizeof(B2StepContext.BodyFinalization));
            }
            if (PhysicsSpace.ProfilingEnabled)
            {
                SolverProfileMS[0] = System.Diagnostics.Stopwatch.GetElapsedTime(profileStart, profilePacked).TotalMilliseconds;
                SolverProfileMS[1] = System.Diagnostics.Stopwatch.GetElapsedTime(profilePacked, profileRecorded).TotalMilliseconds;
                SolverProfileMS[2] = System.Diagnostics.Stopwatch.GetElapsedTime(profileRecorded, profileFinished).TotalMilliseconds;
                SolverProfileMS[3] = System.Diagnostics.Stopwatch.GetElapsedTime(profileFinished).TotalMilliseconds;
            }
            SolverSubmissionCount++; _solvedWorld = context.world;
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
        var inputs = stackalloc nint[4] { _contactInputStorage.Handle, _manifoldStorage.Handle, _fallbackManifoldStorage.Handle, _matchedStorage.Handle };
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 4);
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
                var c = group.contactSims.data[i]; ref readonly var m = ref c.manifold;
                ref readonly var p1 = ref m.points[0]; ref readonly var p2 = ref m.points[1];
                var source = -1; var offset = default(Float4);
                if (ReferenceEquals(context.generatedManifoldOwner, this) && ReferenceEquals(_manifoldContext, context) &&
                    c.generatedManifoldVersion == CollisionSubmissionCount)
                {
                    source = c.generatedManifoldIndex; offset = _centerOffsets[source]; ResidentContactCount++;
                }
                if (source < 0)
                {
                    var fallback = UploadedManifoldCount++;
                    _fallbackManifoldStorage.Data[fallback] = new()
                    {
                        Normal = new(m.normal.X, m.normal.Y, m.pointCount, 0),
                        Anchor1 = new(p1.anchorA.X, p1.anchorA.Y, p1.separation, p1.id),
                        Point1 = new(p1.anchorB.X, p1.anchorB.Y, 0, 0),
                        Anchor2 = new(p2.anchorA.X, p2.anchorA.Y, p2.separation, p2.id),
                        Point2 = new(p2.anchorB.X, p2.anchorB.Y, 0, 0)
                    };
                    source = -fallback - 1;
                }
                _contactInputStorage.Data[_contactStarts[color] + i] = new()
                {
                    IDs = new(c.bodySimIndexA, c.bodySimIndexB, m.pointCount, color == B2Constants.B2_GRAPH_COLOR_COUNT - 1 ? 1 : 0),
                    Mass = new(c.invMassA, c.invMassB, c.invIA, c.invIB),
                    Material = new(c.friction, c.tangentSpeed, c.rollingResistance, c.restitution),
                    History1 = new(p1.localAnchorA.X, p1.localAnchorA.Y, p1.localAnchorB.X, p1.localAnchorB.Y),
                    History2 = new(p2.localAnchorA.X, p2.localAnchorA.Y, p2.localAnchorB.X, p2.localAnchorB.Y),
                    Warm = source >= 0 ? default : new(p1.normalImpulse, p1.tangentImpulse, p2.normalImpulse, p2.tangentImpulse),
                    Source = new(source, m.rollingImpulse, p1.id, p2.id),
                    SurfaceA = new(c.surfaceLinearA.X, c.surfaceLinearA.Y, c.surfaceAngularA, c.solverBias >= 0 ? B2ContactSolvers.ContactCorrection(context, c.solverBias).biasRate : -1),
                    SurfaceB = new(c.surfaceLinearB.X, c.surfaceLinearB.Y, c.surfaceAngularB, context.world.contactAllowedPenetration),
                    Offset = offset
                };
            }
            for (var i = 0; i < group.jointSims.count; i++) _jointStorage.Data[_jointStarts[color] + i] = PackJoint(context, group.jointSims.data[i]);
        }
    }

    private static Joint PackJoint(B2StepContext context, B2JointSim j)
    {
        if (j.type == B2JointType.b2_filterJoint)
            return new() { Mass = new(j.invMassA, j.invMassB, j.invIA, j.invIB), Soft = new(j.constraintHertz, j.constraintDampingRatio, 0, 0), PolicyBias = new(j.correctionBias, j.maxLinearBias, j.maxAngularBias, j.linearSoftness) };
        var world = context.world; var a = world.bodies.data[j.bodyIdA]; var b = world.bodies.data[j.bodyIdB];
        var sa = B2Bodies.b2GetBodySim(world, a); var sb = B2Bodies.b2GetBodySim(world, b);
        var p = new Joint
        {
            Mass = new(sa.invMass, sb.invMass, sa.invInertia, sb.invInertia),
            PolicyBias = new(j.correctionBias, j.maxLinearBias, j.maxAngularBias, j.linearSoftness),
            PolicyForce = new(j.maxLinearForce, j.maxAngularForce, 0, 0),
            Soft = new(j.constraintHertz, j.constraintDampingRatio, 0, 0),
            FrameA = new(j.localFrameA.p.X, j.localFrameA.p.Y, j.localFrameA.q.c, j.localFrameA.q.s),
            FrameB = new(j.localFrameB.p.X, j.localFrameB.p.Y, j.localFrameB.q.c, j.localFrameB.q.s),
            PoseA = new(sa.transform.q.c, sa.transform.q.s, sa.localCenter.X, sa.localCenter.Y),
            PoseB = new(sb.transform.q.c, sb.transform.q.s, sb.localCenter.X, sb.localCenter.Y),
            Geometry = new(sa.center.X, sa.center.Y, sb.center.X, sb.center.Y)
        };
        var indexA = a.setIndex == (int)B2SolverSetType.b2_awakeSet ? a.localIndex : -1;
        var indexB = b.setIndex == (int)B2SolverSetType.b2_awakeSet ? b.localIndex : -1;
        if (j.type == B2JointType.b2_revoluteJoint)
        {
            ref var q = ref j.uj.revoluteJoint;
            p.IDs = new(indexA, indexB, 1, (q.enableSpring ? 1 : 0) | (q.enableMotor ? 2 : 0) | (q.enableLimit ? 4 : 0));
            p.Spring = new(q.hertz, q.dampingRatio, 0, q.targetAngle);
            p.Motor = new(q.maxMotorTorque, q.motorSpeed, q.lowerAngle, q.upperAngle); p.Impulses = new(q.linearImpulse.X, q.linearImpulse.Y, q.springImpulse, q.motorImpulse); p.Limits = new(q.lowerImpulse, q.upperImpulse, 0, 0);
        }
        else if (j.type == B2JointType.b2_wheelJoint)
        {
            ref var q = ref j.uj.wheelJoint;
            p.IDs = new(indexA, indexB, 2, (q.enableSpring ? 1 : 0) | (q.enableMotor ? 2 : 0) | (q.enableLimit ? 4 : 0));
            p.Spring = new(q.hertz, q.dampingRatio, 0, 0); p.Motor = new(q.maxMotorTorque, q.motorSpeed, q.lowerTranslation, q.upperTranslation);
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
                var contact = group.contactSims.data[i];
                contact.solvedStep = context.world.stepIndex;
                contact.generatedManifoldVersion = -(SolverSubmissionCount + 1);
                contact.generatedManifoldIndex = _contactStarts[color] + i;
                ref var m = ref contact.manifold; m.rollingImpulse = packet.Rolling.Z;
                m.points[0].normalImpulse = packet.Impulses1.X; m.points[0].tangentImpulse = packet.Impulses1.Y;
                m.points[0].totalNormalImpulse = packet.Impulses1.Z; m.points[0].totalTangentImpulse = packet.SurfaceA.W; m.points[0].normalVelocity = packet.Params1.W;
                if (m.pointCount > 1)
                {
                    m.points[1].normalImpulse = packet.Impulses2.X; m.points[1].tangentImpulse = packet.Impulses2.Y;
                    m.points[1].totalNormalImpulse = packet.Impulses2.Z; m.points[1].totalTangentImpulse = packet.SurfaceB.W; m.points[1].normalVelocity = packet.Params2.W;
                }
            }
            for (var i = 0; i < group.jointSims.count; i++)
            {
                var joint = group.jointSims.data[i]; var p = _jointStorage.Data[_jointStarts[color] + i];
                joint.invMassA = p.Mass.X; joint.invMassB = p.Mass.Y; joint.invIA = p.Mass.Z; joint.invIB = p.Mass.W;
                joint.constraintSoftness = new(p.Soft.X, p.Soft.Y, p.Soft.Z);
                if (joint.type == B2JointType.b2_revoluteJoint)
                {
                    ref var q = ref joint.uj.revoluteJoint;
                    q.indexA = (int)p.IDs.X; q.indexB = (int)p.IDs.Y;
                    q.frameA = new(new(p.FrameA.X, p.FrameA.Y), new(p.FrameA.Z, p.FrameA.W));
                    q.frameB = new(new(p.FrameB.X, p.FrameB.Y), new(p.FrameB.Z, p.FrameB.W));
                    q.deltaCenter = new(p.Geometry.X, p.Geometry.Y); q.axialMass = p.Geometry.Z;
                    q.springSoftness = new(p.Spring.X, p.Spring.Y, p.Spring.Z);
                    q.linearImpulse = new(p.Impulses.X, p.Impulses.Y); q.springImpulse = p.Impulses.Z; q.motorImpulse = p.Impulses.W; q.lowerImpulse = p.Limits.X; q.upperImpulse = p.Limits.Y;
                }
                else if (joint.type == B2JointType.b2_wheelJoint)
                {
                    ref var q = ref joint.uj.wheelJoint;
                    q.indexA = (int)p.IDs.X; q.indexB = (int)p.IDs.Y;
                    q.frameA = new(new(p.FrameA.X, p.FrameA.Y), new(p.FrameA.Z, p.FrameA.W));
                    q.frameB = new(new(p.FrameB.X, p.FrameB.Y), new(p.FrameB.Z, p.FrameB.W));
                    q.deltaCenter = new(p.Geometry.X, p.Geometry.Y); q.axialMass = p.Geometry.Z; q.perpMass = p.Geometry.W;
                    q.motorMass = p.Soft.W; q.springSoftness = new(p.Spring.X, p.Spring.Y, p.Spring.Z);
                    q.perpImpulse = p.Impulses.X; q.springImpulse = p.Impulses.Z; q.motorImpulse = p.Impulses.W; q.lowerImpulse = p.Limits.X; q.upperImpulse = p.Limits.Y;
                }
            }
        }
    }
}
