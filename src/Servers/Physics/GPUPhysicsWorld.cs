using System.Runtime.InteropServices;
using Box2D.NET;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

// GPU integration is the first executing stage of the GPU world. Collision and constraints
// still use the compatibility solver until their GPU kernels are connected.
internal sealed unsafe class GPUPhysicsWorld : IDisposable
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

    private readonly RenderHandle _device, _integrate;
    private RenderHandle? _bodies, _upload, _download;
    private Body[] _data = [];
    private readonly int _owner = Environment.CurrentManagedThreadId;
    private bool _disposed;
    internal string Driver { get; }
    internal long DispatchCount { get; private set; }

    internal GPUPhysicsWorld()
    {
        if (!SDL.InitSubSystem(SDL.InitFlags.Video)) throw Failure("initialize GPU video support");
        RenderHandle? device = null, integrate = null;
        try
        {
            device = RenderingServer.Service?.RetainComputeDevice() ??
                new RenderHandle(SDL.CreateGPUDevice(ShaderCompiler.GetFormats(), false, null), SDL.DestroyGPUDevice);
            _device = device;
            Driver = SDL.GetGPUDeviceDriver(Device) ?? "unknown";
            using var source = typeof(GPUPhysicsWorld).Assembly.GetManifestResourceStream("Electron2D.PhysicsShaders.PhysicsIntegrate.comp.spv")
                ?? throw new InvalidOperationException("The physics integration shader is missing.");
            using var bytes = new MemoryStream();
            source.CopyTo(bytes);
            integrate = new RenderHandle(ShaderCompiler.CreateComputePipeline(Device, bytes.ToArray()),
                handle => SDL.ReleaseGPUComputePipeline(Device, handle), _device);
            _integrate = integrate;
        }
        catch
        {
            integrate?.Dispose(); device?.Dispose(); SDL.QuitSubSystem(SDL.InitFlags.Video);
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

    private void Reserve(int count)
    {
        if (count <= _data.Length) return;
        var capacity = checked((int)System.Numerics.BitOperations.RoundUpToPowerOf2((uint)Math.Max(64, count)));
        var size = checked((uint)(capacity * sizeof(Body)));
        var info = new SDL.GPUBufferCreateInfo { Size = size, Usage = SDL.GPUBufferUsageFlags.ComputeStorageRead | SDL.GPUBufferUsageFlags.ComputeStorageWrite };
        using var replacement = new BufferReplacement();
        replacement.Bodies = new(SDL.CreateGPUBuffer(Device, in info), handle => SDL.ReleaseGPUBuffer(Device, handle), _device);
        var transfer = new SDL.GPUTransferBufferCreateInfo { Size = size, Usage = SDL.GPUTransferBufferUsage.Upload };
        replacement.Upload = new(SDL.CreateGPUTransferBuffer(Device, in transfer), handle => SDL.ReleaseGPUTransferBuffer(Device, handle), _device);
        transfer.Usage = SDL.GPUTransferBufferUsage.Download;
        replacement.Download = new(SDL.CreateGPUTransferBuffer(Device, in transfer), handle => SDL.ReleaseGPUTransferBuffer(Device, handle), _device);
        var data = new Body[capacity];
        _bodies?.Dispose(); _upload?.Dispose(); _download?.Dispose();
        (_bodies, _upload, _download, _data) = (replacement.Bodies, replacement.Upload, replacement.Download, data);
        replacement.Bodies = replacement.Upload = replacement.Download = null;
    }

    private sealed class BufferReplacement : IDisposable
    {
        internal RenderHandle? Bodies, Upload, Download;
        public void Dispose() { Bodies?.Dispose(); Upload?.Dispose(); Download?.Dispose(); }
    }

    internal void Integrate(B2SolverStageType stage, B2StepContext context)
    {
        EnsureOwner();
        var count = context.world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodyStates.count;
        if (count == 0) return;
        if (stage is not (B2SolverStageType.b2_stageIntegrateVelocities or B2SolverStageType.b2_stageIntegratePositions))
            throw new ArgumentOutOfRangeException(nameof(stage));
        Reserve(count);
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
        var settings = new Step
        {
            Values = new(context.world.gravity.X, context.world.gravity.Y, context.h, context.maxLinearVelocity),
            Control = new(B2Constants.B2_MAX_ROTATION * context.inv_dt,
                stage == B2SolverStageType.b2_stageIntegrateVelocities ? 0 : 1, count, 0)
        };
        Dispatch(count, in settings);
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
        var size = checked((uint)(count * sizeof(Body)));
        var mapped = SDL.MapGPUTransferBuffer(Device, _upload!.DangerousGetHandle(), true);
        if (mapped == 0) throw Failure("map the body upload buffer");
        try { fixed (Body* source = _data) Buffer.MemoryCopy(source, (void*)mapped, size, size); }
        finally { SDL.UnmapGPUTransferBuffer(Device, _upload.DangerousGetHandle()); }
        var command = SDL.AcquireGPUCommandBuffer(Device);
        if (command == 0) throw Failure("acquire integration commands");
        nint fence = 0;
        try
        {
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin the body upload");
            SDL.UploadToGPUBuffer(copy, new SDL.GPUTransferBufferLocation { TransferBuffer = _upload.DangerousGetHandle() },
                new SDL.GPUBufferRegion { Buffer = _bodies!.DangerousGetHandle(), Size = size }, true);
            SDL.EndGPUCopyPass(copy);
            Span<SDL.GPUStorageBufferReadWriteBinding> binding = stackalloc SDL.GPUStorageBufferReadWriteBinding[1];
            binding[0] = new() { Buffer = _bodies.DangerousGetHandle() };
            var compute = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0, binding, 1);
            if (compute == 0) throw Failure("begin body integration");
            SDL.BindGPUComputePipeline(compute, _integrate.DangerousGetHandle());
            fixed (Step* uniform = &settings) SDL.PushGPUComputeUniformData(command, 0, (nint)uniform, (uint)sizeof(Step));
            SDL.DispatchGPUCompute(compute, checked((uint)(count + 63) / 64), 1, 1);
            SDL.EndGPUComputePass(compute);
            copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin the body readback");
            SDL.DownloadFromGPUBuffer(copy, new SDL.GPUBufferRegion { Buffer = _bodies.DangerousGetHandle(), Size = size },
                new SDL.GPUTransferBufferLocation { TransferBuffer = _download!.DangerousGetHandle() });
            SDL.EndGPUCopyPass(copy);
            var submitted = command; command = 0;
            fence = SDL.SubmitGPUCommandBufferAndAcquireFence(submitted);
            if (fence == 0) throw Failure("submit body integration");
            Check(SDL.WaitForGPUFences(Device, true, new ReadOnlySpan<nint>(&fence, 1), 1), "wait for body integration");
            mapped = SDL.MapGPUTransferBuffer(Device, _download!.DangerousGetHandle(), false);
            if (mapped == 0) throw Failure("map integrated bodies");
            try { fixed (Body* destination = _data) Buffer.MemoryCopy((void*)mapped, destination, size, size); }
            finally { SDL.UnmapGPUTransferBuffer(Device, _download.DangerousGetHandle()); }
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
        _disposed = true;
        _bodies?.Dispose(); _upload?.Dispose(); _download?.Dispose(); _integrate.Dispose(); _device.Dispose();
        SDL.QuitSubSystem(SDL.InitFlags.Video);
    }
}
