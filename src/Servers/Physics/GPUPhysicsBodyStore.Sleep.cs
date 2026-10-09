using System.Runtime.InteropServices;
using SDL3;
using Float4 = System.Numerics.Vector4;

namespace Electron2D;

internal sealed unsafe partial class GPUPhysicsBodyStore
{
    /// <summary>Internal linear/angular quiet thresholds in scene units/s and rad/s, followed by the quiet duration in seconds.</summary>
    internal readonly record struct SleepSettings(float LinearThreshold = 2, float AngularThreshold = 0.13962634f, float TimeToSleep = 0.5f);
    private SleepSettings _sleepSettings = new(2, 0.13962634f, 0.5f);
    private bool _wakeAllSleep, _sleepGravityKnown;
    private Vector2 _sleepGravity;
    private (int, float, float, float, float, float)? _sleepSolverPolicy;
    private long _sleepBodyVersion = -1, _sleepShapeVersion = -1, _sleepGeometryEpoch = -1;
    /// <summary>Last completed simulation count of awake dynamics and moving nondynamic surfaces; negative before first publication.</summary>
    internal int ActiveSimulationBodyCount { get; private set; } = -1;
    private RenderHandle? _sleepPipeline, _sleepGraphGPU, _sleepEdgesGPU;
    private int _sleepGraphCapacity, _sleepEdgeCapacity, _sleepGraphBodies, _sleepEdgeCount;
    [StructLayout(LayoutKind.Sequential)]
    private struct SleepUniforms
    {
        internal uint Stage, Count, Bodies, Points;
        internal uint Joints, PreviousBodies, PreviousEdges, ApplyForces;
        internal Float4 Policy, Gravity;
    }

    internal SleepSettings GetSleepSettings() { EnsureAccess(); return _sleepSettings; }
    internal void SetSleepSettings(in SleepSettings settings)
    {
        EnsureAccess();
        if (!float.IsFinite(settings.LinearThreshold) || settings.LinearThreshold < 0 ||
            !float.IsFinite(settings.AngularThreshold) || settings.AngularThreshold < 0 ||
            !float.IsFinite(settings.TimeToSleep) || settings.TimeToSleep < 0) throw new ArgumentOutOfRangeException(nameof(settings));
        if (_sleepSettings == settings) return;
        _sleepSettings = settings; if (Count > 0) _wakeAllSleep = true;
    }
    internal bool GetCanSleep(BodyHandle body) { Validate(body); return _slots[body.Index].CanSleep; }
    internal void SetCanSleep(BodyHandle body, bool value)
    {
        Validate(body);
        if (_slots[body.Index].CanSleep == value) return;
        _slots[body.Index].CanSleep = value;
        ref var command = ref Edit(body.Index); command.Mask |= 512;
        command.Padding = (command.Padding & ~8u) | (value ? 0u : 8u);
        if (!value) Wake(body.Index);
    }
    internal void SetSleeping(BodyHandle body, bool value)
    {
        Validate(body);
        if (_slots[body.Index].Mode < PhysicsServer.BodyMode.Rigid) return;
        PrepareMasses();
        if (!value) { Wake(body.Index); return; }
        ref var command = ref Edit(body.Index);
        command.Mask = (command.Mask | 256 | Velocity) & ~(128u | Impulse);
        command.Body.Velocity = default; command.Impulse = default;
    }
    private void Wake(int index, bool structural = false)
    {
        ref var command = ref Edit(index);
        if (structural && (command.Mask & Create) != 0) return;
        command.Mask = (command.Mask | 128 | (structural ? 1024u : 0u)) & ~256u;
    }
    private void RememberSleepSolver(int iterations, float margin, float penetration, float correction, float speed, float bounce)
    {
        var policy = (iterations, margin, penetration, correction, speed, bounce);
        if (_sleepSolverPolicy.HasValue && _sleepSolverPolicy.Value != policy) _wakeAllSleep = true;
        _sleepSolverPolicy = policy;
    }
    private void EnsureSleep()
    {
        _sleepPipeline ??= _context.CreatePipeline("PhysicsResidentSleep.comp.spv");
        Grow(ref _sleepGraphGPU, ref _sleepGraphCapacity, _highWater, 16, true);
        Grow(ref _sleepEdgesGPU, ref _sleepEdgeCapacity, Math.Max(1, ContactPointCount + _jointHighWater), 16, true);
    }
    private void PrepareSleep(nint command, float delta, Vector2? gravity, float dampingDelta)
    {
        var settings = SleepParameters(delta);
        settings.ApplyForces = gravity.HasValue ? 1u : 0u;
        settings.Gravity = new(gravity?.X ?? 0, gravity?.Y ?? 0, _wakeAllSleep ? 1 : 0, dampingDelta);
        SleepPass(command, settings, 0, _highWater, _spatialSummary!);
        SleepPass(command, settings, 1, _sleepEdgeCount, _spatialSummary!);
        SleepPass(command, settings, 2, _sleepGraphBodies, _spatialSummary!);
        SleepPass(command, settings, 3, _highWater, _spatialSummary!);
        SleepPass(command, settings, 4, ContactPointCount + _jointHighWater, _spatialSummary!);
        SleepPass(command, settings, 5, _highWater, _spatialSummary!);
        SleepPass(command, settings, 6, ContactPointCount + _jointHighWater, _spatialSummary!);
        SleepPass(command, settings, 7, _highWater, _spatialSummary!);
        _sleepGraphBodies = _highWater; _sleepEdgeCount = ContactPointCount + _jointHighWater; _wakeAllSleep = false;
    }
    private SleepUniforms SleepParameters(float delta) => new()
    {
        Bodies = (uint)_highWater,
        Points = (uint)ContactPointCount,
        Joints = (uint)_jointHighWater,
        PreviousBodies = (uint)_sleepGraphBodies,
        PreviousEdges = (uint)_sleepEdgeCount,
        Policy = new(_sleepSettings.LinearThreshold, _sleepSettings.AngularThreshold, _sleepSettings.TimeToSleep, delta)
    };
    private void FinishSleep(nint command, float delta)
    {
        var settings = SleepParameters(delta);
        SleepPass(command, settings, 8, _highWater, _status!);
        SleepPass(command, settings, 9, _highWater, _status!);
    }
    private void SleepPass(nint command, SleepUniforms settings, uint stage, int count, RenderHandle status)
    {
        if (count == 0) return;
        var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[4];
        outputs[0] = new() { Buffer = _bodies!.DangerousGetHandle() }; outputs[1] = new() { Buffer = _sleepGraphGPU!.DangerousGetHandle() };
        outputs[2] = new() { Buffer = _sleepEdgesGPU!.DangerousGetHandle() }; outputs[3] = new() { Buffer = status.DangerousGetHandle() };
        var compute = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, 4);
        if (compute == 0) throw GPUPhysicsDevice.Failure("begin resident sleep graph");
        SDL.BindGPUComputePipeline(compute, _sleepPipeline!.DangerousGetHandle());
        var inputs = stackalloc nint[4] { _contactsGPU!.DangerousGetHandle(), _shapesGPU!.DangerousGetHandle(),
            (_jointsGPU ?? _shapesGPU).DangerousGetHandle(), _positionCorrectionsGPU!.DangerousGetHandle() };
        SDL.BindGPUComputeStorageBuffers(compute, 0, (nint)inputs, 4);
        settings.Stage = stage; settings.Count = (uint)count;
        SDL.PushGPUComputeUniformData(command, 0, (nint)(&settings), (uint)sizeof(SleepUniforms));
        SDL.DispatchGPUCompute(compute, ((uint)count + 63) / 64, 1, 1); SDL.EndGPUComputePass(compute);
        UniformBytes += sizeof(SleepUniforms);
    }
    private void DisposeSleep() { _sleepPipeline?.Dispose(); _sleepGraphGPU?.Dispose(); _sleepEdgesGPU?.Dispose(); }
}
