using System.Runtime.InteropServices;
using Box2D.NET;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsWorld
{
    [StructLayout(LayoutKind.Sequential)]
    private struct FinalizationInput
    {
        internal Float4 Pose, Geometry, Sleep;
        internal int BodyID;
        internal uint Generation, Flags, Options;
    }
    private readonly Storage<FinalizationInput> _finalizationInputs;
    private readonly Storage<B2StepContext.BodyFinalization> _finalizationResults;
    private readonly Action<B2StepContext> _consumeFinalization;
    private B2World? _bodyFinalizationWorld;
    private B2StepContext? _pendingFinalization;
    private int _finalizationCount;
    internal long BodyFinalizationCount { get; private set; }
    internal long BodyFinalizationBatchCount { get; private set; }
    internal long BodyFinalizationTransferBytes { get; private set; }

    internal void EnableBodyFinalization(B2World world)
    {
        EnsureOwner(); DetachBodyFinalization();
        _bodyFinalizationWorld = world; world.finalizeBodyStates = _consumeFinalization;
    }

    private void DetachBodyFinalization()
    {
        if (_bodyFinalizationWorld is not null && _bodyFinalizationWorld.finalizeBodyStates == _consumeFinalization)
            _bodyFinalizationWorld.finalizeBodyStates = null!;
        _bodyFinalizationWorld = null; _pendingFinalization = null;
    }

    private void PackFinalization(B2StepContext context, int count)
    {
        _finalizationInputs.Reserve(_bodyStorage.Data.Length); _finalizationResults.Reserve(_bodyStorage.Data.Length);
        var world = context.world;
        for (var i = 0; i < count; i++)
        {
            var sim = context.sims[i]; var body = world.bodies.data[sim.bodyId];
            _finalizationInputs.Data[i] = new()
            {
                Pose = new(sim.center.X, sim.center.Y, sim.transform.q.c, sim.transform.q.s),
                Geometry = new(sim.localCenter.X, sim.localCenter.Y, sim.minExtent, sim.maxExtent),
                Sleep = new(body.sleepTime, body.sleepThreshold, 0, 0),
                BodyID = sim.bodyId,
                Generation = body.generation,
                Flags = body.flags,
                Options = (body.type == B2BodyType.b2_dynamicBody ? 1u : 0) | (body.enableSleep ? 2u : 0) |
                    (world.islands.data[body.islandId].constraintRemoveCount > 0 ? 4u : 0)
            };
        }
    }

    private void RecordFinalization(nint command, B2StepContext context, int count)
    {
        var binding = new SDL.GPUStorageBufferReadWriteBinding { Buffer = _finalizationResults.Handle };
        var pass = SDL.BeginGPUComputePass(command, ReadOnlySpan<SDL.GPUStorageTextureReadWriteBinding>.Empty, 0,
            new ReadOnlySpan<SDL.GPUStorageBufferReadWriteBinding>(&binding, 1), 1);
        if (pass == 0) throw Failure("begin body finalization");
        SDL.BindGPUComputePipeline(pass, _finalizationPipeline.DangerousGetHandle());
        var buffers = stackalloc nint[2] { _bodyStorage.Handle, _finalizationInputs.Handle };
        SDL.BindGPUComputeStorageBuffers(pass, 0, (nint)buffers, 2);
        var settings = stackalloc uint[8]
        {
            BitConverter.SingleToUInt32Bits(context.dt), BitConverter.SingleToUInt32Bits(context.inv_dt),
            BitConverter.SingleToUInt32Bits(B2Constants.B2_TIME_TO_SLEEP), 0, (uint)count,
            (context.world.enableSleep ? 1u : 0) | (context.world.enableContinuous ? 2u : 0), 0, 0
        };
        SDL.PushGPUComputeUniformData(command, 0, (nint)settings, 32);
        SDL.DispatchGPUCompute(pass, checked((uint)(count + 63) / 64), 1, 1); SDL.EndGPUComputePass(pass); DispatchCount++;
    }

    private void ValidateFinalization(B2StepContext context, int count)
    {
        for (var i = 0; i < count; i++)
        {
            var result = _finalizationResults.Data[i]; var input = _finalizationInputs.Data[i];
            var simFlags = (uint)_data[i].Flags.X;
            if (result.bodyId != input.BodyID || result.generation != input.Generation || result.padding != 0 || (result.state & ~7u) != 0 ||
                (result.state & 4u) != ((result.state & 2u) == 0 ? input.Options & 4u : 0) ||
                (result.sleepTime != 0 && (!context.world.enableSleep || (input.Options & 2u) == 0 || result.sleepTime != input.Sleep.X + context.dt)) ||
                (result.state & 2u) != (result.sleepTime < B2Constants.B2_TIME_TO_SLEEP ? 2u : 0) ||
                result.bodyFlags != ((input.Flags & ~104u) | (simFlags & 96u)) ||
                result.simFlags != ((simFlags & ~104u) | ((result.state & 1u) != 0 ? 8u : 0)) ||
                ((result.state & 1u) != 0 && ((input.Options & 1u) == 0 || !context.world.enableContinuous)) ||
                !Finite(new(result.linearVelocity.X, result.linearVelocity.Y, result.angularVelocity, result.sleepTime)) || result.sleepTime < 0 ||
                !Finite(new(result.center.X, result.center.Y, result.position.X, result.position.Y)) ||
                !float.IsFinite(result.rotation.c) || !float.IsFinite(result.rotation.s))
                throw new InvalidOperationException("GPU body finalization returned invalid state.");
            var length = result.rotation.c * result.rotation.c + result.rotation.s * result.rotation.s;
            if (MathF.Abs(length - 1) > .001f) throw new InvalidOperationException("GPU body finalization returned an invalid rotation.");
        }
    }

    private void ConsumeFinalization(B2StepContext context)
    {
        EnsureOwner();
        if (!ReferenceEquals(_pendingFinalization, context) || !ReferenceEquals(_bodyFinalizationWorld, context.world) ||
            context.world.solverSets.data[(int)B2SolverSetType.b2_awakeSet].bodyStates.count != _finalizationCount)
            throw new InvalidOperationException("GPU body finalization is not prepared for this step.");
        context.finalizedBodies = _finalizationResults.Data; _pendingFinalization = null;
        BodyFinalizationCount += _finalizationCount;
    }
}
