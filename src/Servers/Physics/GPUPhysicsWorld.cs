using System.Runtime.InteropServices;
using Box2D.NET;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

// GPU hierarchy/contact identity maintenance, integration, manifolds and constraints execute here;
// CPU query/CCD mirrors, user callbacks, mirror/event publication and graph mutation remain managed.
internal sealed unsafe partial class GPUPhysicsWorld : IDisposable
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Body
    {
        internal Float4 Velocity, Delta, Force, Properties, Flags;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct Step
    {
        internal Float4 Values, Control;
    }

    private readonly RenderHandle _device, _integrate, _solve, _collide, _broadPhase, _pairTablePipeline, _treePipeline, _filterPipeline, _contactCreationPipeline, _islandSplitPipeline;
    private readonly Storage<Body> _bodyStorage;
    private Body[] _data => _bodyStorage.Data;
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private bool _disposed;
    internal string Driver { get; }
    internal long DispatchCount { get; private set; }

    internal GPUPhysicsWorld()
    {
        if (!SDL.InitSubSystem(SDL.InitFlags.Video)) throw Failure("initialize GPU video support");
        RenderHandle? device = null, integrate = null, solve = null, collide = null, broadPhase = null, pairTable = null, tree = null, filters = null, creation = null, islands = null;
        try
        {
            device = RenderingServer.Service?.RetainComputeDevice() ??
                new RenderHandle(SDL.CreateGPUDevice(ShaderCompiler.GetFormats(), false, null), SDL.DestroyGPUDevice);
            _device = device;
            Driver = SDL.GetGPUDeviceDriver(Device) ?? "unknown";
            _integrate = integrate = CreatePipeline("PhysicsIntegrate.comp.spv");
            _solve = solve = CreatePipeline("PhysicsSolve.comp.spv");
            _collide = collide = CreatePipeline("PhysicsCollide.comp.spv");
            _broadPhase = broadPhase = CreatePipeline("PhysicsBroadPhase.comp.spv");
            _pairTablePipeline = pairTable = CreatePipeline("PhysicsPairTable.comp.spv");
            _treePipeline = tree = CreatePipeline("PhysicsTree.comp.spv");
            _filterPipeline = filters = CreatePipeline("PhysicsFilters.comp.spv");
            _contactCreationPipeline = creation = CreatePipeline("PhysicsContactCreate.comp.spv");
            _islandSplitPipeline = islands = CreatePipeline("PhysicsIslandSplit.comp.spv");
            _splitBodyStorage = new(this); _splitContactStorage = new(this); _splitJointStorage = new(this);
            _splitBodyResult = new(this); _splitContactResult = new(this); _splitJointResult = new(this);
            _splitGroupStorage = new(this); _splitStackStorage = new(this); _splitScanStorage = new(this); _splitStatusStorage = new(this);
            _splitIsland = SplitIsland;
            _contactSlotStorage = new(this); _contactFreeStorage = new(this); _contactScanStorage = new(this);
            _contactPoolStorage = new(this); _contactChangeStorage = new(this);
            _contactRequestStorage = new(this); _contactCreationStorage = new(this);
            _contactIDChanged = MarkContactIDChanged; _createContacts = CreateContacts;
            _contactLinksChanged = MarkContactLinksChanged;
            _contactRemovalStorage = new(this);
            _destroyDisjointContact = PublishContactRemoval; _finishContactRemovals = FinishContactRemovals;
            _contactLinkStorage = new(this); _bodyLinkStorage = new(this); _linkUpdateStorage = new(this);
            _bodyStorage = new(this); _contactStorage = new(this); _jointStorage = new(this);
            _contactInputStorage = new(this); _fallbackManifoldStorage = new(this);
            _historyStorage = new(this); _matchedStorage = new(this);
            _contactMaterialStorage = new(this);
            _publishContactTask = PublishContactTask;
            _geometryChanged = MarkGeometryChanged;
            _geometryUpdateStorage = new(this); _geometryStorage = new(this); _pairStorage = new(this); _manifoldStorage = new(this);
            _treeStorage = new(this); _treeQueryStorage = new(this); _treeCandidateStorage = new(this);
            _broadShapeStorage = new(this); _broadJointStorage = new(this); _existingPairStorage = new(this);
            _pairKeyStorage = new(this); _pairUpdateStorage = new(this); _pairStatusStorage = new(this);
            _pairChanged = MarkPairChanged;
            _shapeFilterUpdateStorage = new(this); _jointFilterUpdateStorage = new(this);
            _shapeFilterChanged = MarkShapeFilterChanged; _jointFilterChanged = MarkJointFilterChanged;
            _proxyStorage = new(this); _proxyUpdateStorage = new(this); _treeOrderStorage = new(this);
            _proxyChanged = MarkProxyChanged;
            _compareCandidateOrder = CompareCandidateOrder;
        }
        catch
        {
            islands?.Dispose(); creation?.Dispose(); filters?.Dispose(); tree?.Dispose(); pairTable?.Dispose(); broadPhase?.Dispose(); collide?.Dispose(); solve?.Dispose(); integrate?.Dispose(); device?.Dispose(); SDL.QuitSubSystem(SDL.InitFlags.Video);
            throw;
        }
    }

    private nint Device => _device.DangerousGetHandle();
    private static InvalidOperationException Failure(string operation) => new($"GPU physics failed to {operation}: {SDL.GetError()}");
    private static void Check(bool success, string operation) { if (!success) throw Failure(operation); }

    private void EnsureOwner()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_owner != Environment.CurrentManagedThreadId) throw new InvalidOperationException("GPU physics requires the world owner thread.");
    }

    private RenderHandle CreatePipeline(string name)
    {
        using var source = typeof(GPUPhysicsWorld).Assembly.GetManifestResourceStream("Electron2D.PhysicsShaders." + name)
            ?? throw new InvalidOperationException("The physics shader is missing: " + name);
        using var bytes = new MemoryStream(); source.CopyTo(bytes);
        return new(ShaderCompiler.CreateComputePipeline(Device, bytes.ToArray()), handle => SDL.ReleaseGPUComputePipeline(Device, handle), _device);
    }

    internal void Integrate(B2SolverStageType stage, B2StepContext context)
    {
        EnsureOwner();
        var count = context.world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodyStates.count;
        if (count == 0) return;
        if (stage is not (B2SolverStageType.b2_stageIntegrateVelocities or B2SolverStageType.b2_stageIntegratePositions))
            throw new ArgumentOutOfRangeException(nameof(stage));
        _bodyStorage.Reserve(count);
        PackBodies(context, count);
        var settings = new Step
        {
            Values = new(context.world.gravity.X, context.world.gravity.Y, context.h, context.maxLinearVelocity),
            Control = new(B2Constants.B2_MAX_ROTATION * context.inv_dt,
                stage == B2SolverStageType.b2_stageIntegrateVelocities ? 0 : 1, count, 0)
        };
        Dispatch(count, in settings);
        PublishBodies(context, count);
    }

    private void PackBodies(B2StepContext context, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var state = context.states[i]; var sim = context.sims[i];
            _data[i] = new Body
            {
                Velocity = new(state.linearVelocity.X, state.linearVelocity.Y, state.angularVelocity, state.flags),
                Delta = new(state.deltaPosition.X, state.deltaPosition.Y, state.deltaRotation.c, state.deltaRotation.s),
                Force = new(sim.force.X, sim.force.Y, sim.torque, sim.invMass),
                Properties = new(sim.invInertia, sim.gravityScale, sim.linearDamping, sim.angularDamping),
                Flags = new(sim.flags, 0, 0, 0)
            };
        }
    }

    private void PublishBodies(B2StepContext context, int count)
    {
        // Validate the complete result before publishing it into the live CPU query/state mirror.
        for (var i = 0; i < count; i++)
        {
            var b = _data[i];
            if (!float.IsFinite(b.Velocity.X) || !float.IsFinite(b.Velocity.Y) || !float.IsFinite(b.Velocity.Z) ||
                !float.IsFinite(b.Delta.X) || !float.IsFinite(b.Delta.Y) || !float.IsFinite(b.Delta.Z) || !float.IsFinite(b.Delta.W))
                throw new InvalidOperationException("GPU integration returned a nonfinite body state.");
        }
        for (var i = 0; i < count; i++)
        {
            var b = _data[i]; var state = context.states[i];
            state.linearVelocity = new(b.Velocity.X, b.Velocity.Y); state.angularVelocity = b.Velocity.Z;
            state.deltaPosition = new(b.Delta.X, b.Delta.Y); state.deltaRotation = new(b.Delta.Z, b.Delta.W);
            context.sims[i].flags = (uint)b.Flags.X;
        }
    }

    private void Dispatch(int count, in Step settings)
    {
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire integration commands");
        nint fence = 0;
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin the body upload");
            _bodyStorage.Upload(copy, count);
            SDL.EndGPUCopyPass(copy);
            Span<SDL.GPUStorageBufferReadWriteBinding> binding = stackalloc SDL.GPUStorageBufferReadWriteBinding[1];
            binding[0] = new() { Buffer = _bodyStorage.Handle };
            var compute = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0, binding, 1);
            if (compute == 0) throw Failure("begin body integration");
            SDL.BindGPUComputePipeline(compute, _integrate.DangerousGetHandle());
            fixed (Step* uniform = &settings) SDL.PushGPUComputeUniformData(command, 0, (nint)uniform, (uint)sizeof(Step));
            SDL.DispatchGPUCompute(compute, checked((uint)(count + 63) / 64), 1, 1);
            SDL.EndGPUComputePass(compute);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin the body readback");
            _bodyStorage.Download(copy, count);
            SDL.EndGPUCopyPass(copy);
            var submitted = command; command = 0;
            fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
            if (fence == 0) throw Failure("submit body integration");
            Check(SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "wait for body integration");
            _bodyStorage.Read(count);
            DispatchCount++;
        }
        finally
        {
            if (command != 0) SDL.CancelGPUCommandBuffer(command);
            if (fence != 0) SDL.ReleaseGPUFence(Device, fence);
        }
    }

    public void Dispose()
    {
        if (_disposed) return;
        EnsureOwner();
        _disposed = true; _manifoldContext = null; _solvedWorld = null;
        DetachPairTracking();
        DetachProxyTracking();
        DetachFilterTracking();
        DetachGeometryTracking();
        DetachContactPool();
        if (_splitWorld is not null && _splitWorld.splitIsland == _splitIsland) _splitWorld.splitIsland = null!;
        _splitWorld = null;
        _splitBodyStorage.Dispose(); _splitContactStorage.Dispose(); _splitJointStorage.Dispose();
        _splitBodyResult.Dispose(); _splitContactResult.Dispose(); _splitJointResult.Dispose();
        _splitGroupStorage.Dispose(); _splitStackStorage.Dispose(); _splitScanStorage.Dispose(); _splitStatusStorage.Dispose(); _islandSplitPipeline.Dispose();
        _contactSlotStorage.Dispose(); _contactFreeStorage.Dispose(); _contactScanStorage.Dispose();
        _contactPoolStorage.Dispose(); _contactChangeStorage.Dispose();
        _contactRequestStorage.Dispose(); _contactCreationStorage.Dispose(); _contactCreationPipeline.Dispose();
        _contactRemovalStorage.Dispose();
        _contactLinkStorage.Dispose(); _bodyLinkStorage.Dispose(); _linkUpdateStorage.Dispose();
        _geometryUpdateStorage.Dispose(); _contactMaterialStorage.Dispose();
        _shapeFilterUpdateStorage.Dispose(); _jointFilterUpdateStorage.Dispose(); _filterPipeline.Dispose();
        _proxyStorage.Dispose(); _proxyUpdateStorage.Dispose(); _treeOrderStorage.Dispose(); _treePipeline.Dispose();
        _pairKeyStorage.Dispose(); _pairUpdateStorage.Dispose(); _pairStatusStorage.Dispose(); _pairTablePipeline.Dispose();
        _treeStorage.Dispose(); _treeQueryStorage.Dispose(); _treeCandidateStorage.Dispose(); _broadPhase.Dispose();
        _broadShapeStorage.Dispose(); _broadJointStorage.Dispose(); _existingPairStorage.Dispose();
        _historyStorage.Dispose(); _matchedStorage.Dispose();
        _geometryStorage.Dispose(); _pairStorage.Dispose(); _manifoldStorage.Dispose(); _collide.Dispose();
        _contactInputStorage.Dispose(); _fallbackManifoldStorage.Dispose();
        _bodyStorage.Dispose(); _contactStorage.Dispose(); _jointStorage.Dispose(); _solve.Dispose(); _integrate.Dispose(); _device.Dispose();
        SDL.QuitSubSystem(SDL.InitFlags.Video);
    }
}
