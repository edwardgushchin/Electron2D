using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private RenderHandle? _particleStep, _particleQueue, _particleOrder;
    private readonly Dictionary<ShaderProgram, RenderHandle> _particlePrograms = [];
    private HashSet<ShaderProgram> _usedParticlePrograms = [], _previousParticlePrograms = [];
    private readonly List<RenderingParticleState> _particlePrepared = [];
    private readonly List<(RenderingParticleState State, ParticleCheckpoint Before)> _particleChanges = [];
    private readonly record struct ParticleCheckpoint(double Phase, double Remainder, double InactiveTime, uint Cycle, bool Emitting, bool Clear, bool Inactive, long Frame, int HistoryNext, int HistoryWritten, bool Initialized, long SubemissionFrame, Transform PreviousEmission);
    private RenderHandle ParticleKernel(string name)
    {
        using var source = typeof(GPUCanvasBackend).Assembly.GetManifestResourceStream("Electron2D.Shaders." + name + ".comp.spv")!;
        using var bytes = new MemoryStream(); source.CopyTo(bytes);
        return new(ShaderCompiler.CreateComputePipeline(Device, bytes.ToArray()), h => SDL.ReleaseGPUComputePipeline(Device, h), _device);
    }
    private static (uint Width, uint Height) ParticleDispatch(int count)
    {
        var groups = ((uint)count + 63) / 64; var width = Math.Min(65535u, groups); return (width, (groups + width - 1) / width);
    }
    private void PrepareParticleSources(RenderingServer server)
    {
        _particlePrepared.Clear(); _particleChanges.Clear();
        if (!server.HasParticleSources) return;
        _particleStep ??= ParticleKernel("ParticleStep"); _particleQueue ??= ParticleKernel("ParticleQueue"); _particleOrder ??= ParticleKernel("ParticleOrder");
        foreach (var source in server.ParticleSchedule)
        {
            if (!source.Requested || source.LastFrame == server.ParticleFrame || source.Amount == 0 || source.Released) continue;
            source.Preparing = true;
            try
            {
                var material = source.ProcessMaterial; var state = material?.GetCanvasState();
                if (state is not null && state.Program.Mode != Shader.Mode.Particles) throw new NotSupportedException("Particle process materials require particle compute programs.");
                if (state is not null) _usedParticlePrograms.Add(state.Program);
                if (state is not null && !_particlePrograms.ContainsKey(state.Program))
                    _particlePrograms.Add(state.Program, new(ShaderCompiler.CreateComputePipeline(Device, state.Program.NativeCode), h => SDL.ReleaseGPUComputePipeline(Device, h), _device));
                if (state is { Textures.Length: > 0 }) { _usedMaterials.Add(state); PrepareTextures(state); }
                if (source.Released || material is { IsDisposed: true } || !ReferenceEquals(material, source.ProcessMaterial) || !ReferenceEquals(state, material?.GetCanvasState()))
                    throw new InvalidOperationException("Particle material callbacks require stable borrowed state.");
                source.PreparedProcess = state;
                ValidateParticleWork(source, server.ParticleDelta);
                source.GPU ??= new(source, _device);
                _particlePrepared.Add(source);
            }
            finally { source.Preparing = false; }
            if (source.Subemitter is { Released: false, Amount: > 0 } child && source.PreparedProcess?.Program.ParticleStorageBuffers == 2)
                child.Requested = true;
        }
    }
    private static void ValidateParticleWork(RenderingParticleState source, double delta)
    {
        var FPS = source.FixedFPS > 0 ? source.FixedFPS : source.Trails && source.BindPoses.Length > 1 ? 60 : 30;
        var explicitTime = (source.Clear ? source.Preprocess : 0d) + source.RequestTime + source.RequestResidual;
        if (!double.IsFinite(explicitTime * FPS) || explicitTime * FPS > int.MaxValue || !double.IsFinite(delta * source.SpeedScale) || delta * source.SpeedScale > float.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(source), "Particle work exceeds its finite step-counter budget.");
        if (source.Trails && source.BindPoses.Length > 1 && Math.Ceiling((double)source.TrailLength * FPS + 1) * source.Amount * 96 > int.MaxValue)
            throw new ArgumentOutOfRangeException(nameof(source), "Particle trail history exceeds the native byte budget.");
    }
    private void ProcessParticles(nint command, RenderingServer server, double time, CanvasLightingFrame lighting)
    {
        foreach (var source in _particlePrepared)
        {
            var gpu = source.GPU!;
            _particleChanges.Add((source, new(source.Phase, source.Remainder, source.InactiveTime, source.Cycle, source.Emitting, source.Clear, source.Inactive, source.LastFrame, gpu.HistoryNext, gpu.HistoryWritten, gpu.Initialized, source.SubemissionFrame, source.PreviousEmission)));
        }
        foreach (var source in _particlePrepared)
            source.GPU!.Prepare(source, command, source.ManualCount != 0 || server.ParticleHasIncoming(source));
        foreach (var source in _particlePrepared)
        {
            var gpu = source.GPU!;
            if (source.Emitting)
            {
                if (source.Inactive) { source.Clear = true; source.Phase = source.Remainder = 0; source.Cycle = 0; }
                source.Inactive = false; source.InactiveTime = 0;
            }
            else if (source.ManualCount == 0 && source.SubemissionFrame != server.ParticleFrame)
            {
                source.InactiveTime += server.ParticleDelta * source.SpeedScale;
                if (source.InactiveTime > source.Lifetime * 1.2) { source.Inactive = true; source.LastFrame = server.ParticleFrame; continue; }
            }
            else { source.Inactive = false; source.InactiveTime = 0; }
            if (source.Clear && source.Preprocess > 0) Seek(source, source.Preprocess);
            if (source.RequestTime > 0) Seek(source, source.RequestTime);
            if (source.RequestResidual > 0) { source.Emitting = false; Seek(source, source.RequestResidual); }
            var FPS = source.FixedFPS > 0 ? source.FixedFPS : source.Trails && source.BindPoses.Length > 1 ? 60 : 0;
            if (FPS > 0)
            {
                var step = 1d / FPS; var remaining = source.Remainder + Math.Clamp(server.ParticleDelta, 0, .1) * source.SpeedScale;
                if (remaining / step > int.MaxValue) throw new ArgumentOutOfRangeException(nameof(source), "Particle step count exceeds its counter budget.");
                while (remaining >= step || source.Clear) { Step(source, step); remaining -= step; }
                source.Remainder = remaining;
            }
            else Step(source, server.ParticleDelta * source.SpeedScale);
            SortParticles(command, source); source.LastFrame = server.ParticleFrame;
        }
        void Seek(RenderingParticleState source, double seconds)
        {
            var FPS = source.FixedFPS > 0 ? source.FixedFPS : source.Trails && source.BindPoses.Length > 1 ? 60 : 30; var step = 1d / FPS;
            while (seconds > 0) { var amount = Math.Min(step, seconds); Step(source, amount); seconds -= amount; }
        }
        void Step(RenderingParticleState source, double elapsed)
        {
            var delta = (float)elapsed;
            var gpu = source.GPU!; var reset = source.Clear; var old = reset ? 0 : source.Phase;
            if (reset) { source.Cycle = 0; gpu.HistoryNext = gpu.HistoryWritten = 0; }
            var duration = elapsed / source.Lifetime; var whole = Math.Floor(old + duration); var phase = (old + duration) % 1;
            source.Cycle = unchecked(source.Cycle + (uint)(whole % 4294967296d)); source.Phase = phase;
            if (!reset && source.OneShot && whole >= 1) source.Emitting = false;
            var groups = ParticleDispatch(source.Amount);
            var sdfCoordinates = SDFCoords(lighting);
            var toSDF = new Transform(new(sdfCoordinates.X, 0), new(0, sdfCoordinates.Y), new(sdfCoordinates.Z, sdfCoordinates.W)) * source.CanvasBasis;
            var emission = source.LocalCoordinates ? Transform.Identity : source.Emission;
            var previousEmission = reset ? emission : source.PreviousEmission;
            var frame = new ParticleFrameLayout
            {
                Counts = new(source.Amount, unchecked((int)source.Cycle), unchecked((int)source.Seed), gpu.OutgoingCapacity),
                Time = new(delta, source.Lifetime, source.Randomness, source.Explosiveness),
                Cycle = new((float)old, (float)phase, source.InterpToEnd, source.AmountRatio),
                EmissionX = new(emission.X.X, emission.Y.X, 0, emission.Origin.X),
                EmissionY = new(emission.X.Y, emission.Y.Y, 0, emission.Origin.Y),
                PreviousEmissionX = new(previousEmission.X.X, previousEmission.Y.X, 0, previousEmission.Origin.X),
                PreviousEmissionY = new(previousEmission.X.Y, previousEmission.Y.Y, 0, previousEmission.Origin.Y),
                EmitterVelocity = new(source.EmitterVelocity.X, source.EmitterVelocity.Y, 0, source.CollisionBaseSize),
                SDFRect = sdfCoordinates,
                SDFTransformX = new(toSDF.X.X, toSDF.Y.X, 0, toSDF.Origin.X),
                SDFTransformY = new(toSDF.X.Y, toSDF.Y.Y, 0, toSDF.Origin.Y),
                Options = new((source.Emitting ? 1 : 0) | (source.FractionalDelta ? 2 : 0) | (source.LocalCoordinates ? 4 : 0) | (reset ? 8 : 0) | (duration >= 1 ? 16 : 0) | (source.PreparedProcess is null ? 32 : 0), 0, 0, checked((int)groups.Width * 64))
            };
            gpu.CopyPrevious(command);
            SDL.PushGPUComputeUniformData(command, 0, (nint)(&frame), (uint)sizeof(ParticleFrameLayout));
            var writable = stackalloc SDL.GPUStorageBufferReadWriteBinding[2]; writable[0] = new() { Buffer = gpu.State.DangerousGetHandle() }; writable[1] = new() { Buffer = gpu.Incoming.DangerousGetHandle() };
            var pass = SDL.BeginGPUComputePass(command, 0, 0, (nint)writable, 2); if (pass == 0) throw Failure("begin particle step");
            SDL.BindGPUComputePipeline(pass, _particleStep!.DangerousGetHandle()); SDL.DispatchGPUCompute(pass, groups.Width, groups.Height, 1); SDL.EndGPUComputePass(pass);
            if (source.PreparedProcess is { } material)
            {
                material.PushUniforms(command, (float)time, default, SDFCoords(lighting), compute: true);
                SDL.PushGPUComputeUniformData(command, 0, (nint)(&frame), (uint)sizeof(ParticleFrameLayout));
                writable[1] = new() { Buffer = gpu.Outgoing.DangerousGetHandle() };
                pass = SDL.BeginGPUComputePass(command, 0, 0, (nint)writable, (uint)material.Program.ParticleStorageBuffers); if (pass == 0) throw Failure("begin particle process");
                SDL.BindGPUComputePipeline(pass, _particlePrograms[material.Program].DangerousGetHandle());
                if (material.Textures.Length != 0)
                {
                    var samplers = _textureBindings[material];
                    for (var i = 0; i < samplers.Length; i++) if (material.Program.Textures[i].IsSDFTexture) samplers[i] = new() { Texture = _sdfTarget!.DangerousGetHandle(), Sampler = Sampler(TextureFilter.Linear, TextureRepeat.Disabled) };
                    fixed (SDL.GPUTextureSamplerBinding* pointer = samplers) SDL.BindGPUComputeSamplers(pass, 0, (nint)pointer, (uint)samplers.Length);
                }
                SDL.DispatchGPUCompute(pass, groups.Width, groups.Height, 1); SDL.EndGPUComputePass(pass);
            }
            gpu.CaptureHistory(command); if (!gpu.Initialized || reset) gpu.CopyPrevious(command);
            gpu.Initialized = true; source.Clear = false; source.PreviousEmission = emission;
            if (source.Subemitter is { GPU: { } target, Released: false } child && source.PreparedProcess?.Program.ParticleStorageBuffers == 2)
            {
                var pose = source.LocalCoordinates ? source.Emission : Transform.Identity; if (child.LocalCoordinates) pose = child.Emission.AffineInverse() * pose;
                var queue = new ParticleQueueParameters(new(pose.X.X, pose.Y.X, 0, pose.Origin.X), new(pose.X.Y, pose.Y.Y, 0, pose.Origin.Y), new(source.Amount, 0, checked((int)groups.Width * 64), 0));
                SDL.PushGPUComputeUniformData(command, 0, (nint)(&queue), (uint)sizeof(ParticleQueueParameters));
                writable[0] = new() { Buffer = target.Incoming.DangerousGetHandle() };
                pass = SDL.BeginGPUComputePass(command, 0, 0, (nint)writable, 1); if (pass == 0) throw Failure("begin particle subemission");
                SDL.BindGPUComputePipeline(pass, _particleQueue!.DangerousGetHandle()); var input = gpu.Outgoing.DangerousGetHandle(); SDL.BindGPUComputeStorageBuffers(pass, 0, new ReadOnlySpan<nint>(&input, 1), 1); SDL.DispatchGPUCompute(pass, groups.Width, groups.Height, 1); SDL.EndGPUComputePass(pass);
                gpu.ResetOutgoing(command); if (child.Emitting) child.Clear = true; child.Emitting = false; child.SubemissionFrame = server.ParticleFrame;
            }
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct ParticleQueueParameters(Vector4 X, Vector4 Y, Vector4i Counts);
    private void SortParticles(nint command, RenderingParticleState source)
    {
        var gpu = source.GPU!; var groups = ParticleDispatch(gpu.OrderCount); var dimensions = new Vector4i(checked((int)groups.Width * 64), 0, 0, 0);
        Dispatch(0, (int)source.DrawOrder);
        if (source.DrawOrder == RenderingServer.ParticlesDrawOrder.Index) return;
        for (var level = 2; level <= gpu.OrderCount; level = checked(level * 2))
        {
            for (var stride = level / 2; stride > 0; stride /= 2) Dispatch(stride, level);
            if (level == gpu.OrderCount) break;
        }
        void Dispatch(int stride, int level)
        {
            Span<Vector4i> parameters = stackalloc Vector4i[2]; parameters[0] = new(source.Amount, gpu.OrderCount, stride, level); parameters[1] = dimensions;
            SDL.PushGPUComputeUniformData(command, 0, MemoryMarshal.AsBytes(parameters), 32);
            var output = new SDL.GPUStorageBufferReadWriteBinding { Buffer = gpu.Order.DangerousGetHandle() };
            var pass = SDL.BeginGPUComputePass(command, 0, 0, (nint)(&output), 1); if (pass == 0) throw Failure("begin particle ordering");
            SDL.BindGPUComputePipeline(pass, _particleOrder!.DangerousGetHandle()); var input = gpu.State.DangerousGetHandle(); SDL.BindGPUComputeStorageBuffers(pass, 0, new ReadOnlySpan<nint>(&input, 1), 1); SDL.DispatchGPUCompute(pass, groups.Width, groups.Height, 1); SDL.EndGPUComputePass(pass);
        }
    }
    private void CommitParticleChanges()
    {
        foreach (var (source, _) in _particleChanges) { source.Requested = false; source.ManualCount = 0; source.RequestTime = source.RequestResidual = 0; }
        _particleChanges.Clear(); _particlePrepared.Clear();
    }
    private void RollbackParticleChanges(bool committed)
    {
        if (committed) return;
        foreach (var (source, before) in _particleChanges)
        {
            source.Phase = before.Phase; source.Remainder = before.Remainder; source.InactiveTime = before.InactiveTime; source.Cycle = before.Cycle; source.Emitting = before.Emitting; source.Clear = before.Clear; source.Inactive = before.Inactive; source.LastFrame = before.Frame; source.SubemissionFrame = before.SubemissionFrame; source.PreviousEmission = before.PreviousEmission;
            if (source.GPU is { } gpu) { gpu.HistoryNext = before.HistoryNext; gpu.HistoryWritten = before.HistoryWritten; gpu.Initialized = before.Initialized; gpu.CancelPreparedUploads(); }
        }
        foreach (var geometry in _usedParticleGeometries) geometry.CancelPreparedUpload();
        _particleChanges.Clear(); _particlePrepared.Clear();
    }
    internal Rect2 ReadParticleBounds(RenderingParticleState source)
    {
        if (source.GPU is not { Initialized: true } gpu) return default;
        var state = gpu.Read(gpu.State, source.Amount);
        var inverse = source.LocalCoordinates ? Transform.Identity : source.Emission.AffineInverse(); var found = false; var bounds = default(Rect2);
        foreach (var particle in state) Include(particle);
        if (source.Trails && source.BindPoses.Length > 1 && gpu.HistoryWritten != 0)
        {
            var history = gpu.Read(gpu.History, checked(gpu.HistoryFrames * source.Amount));
            var FPS = source.FixedFPS > 0 ? source.FixedFPS : 60;
            for (var bone = 0; bone < source.BindPoses.Length; bone++)
            {
                var fraction = source.Interpolate ? Math.Clamp(source.Remainder * FPS, 0, 1) : 1;
                var ago = Math.Min((int)((double)bone / (source.BindPoses.Length - 1) * source.TrailLength * FPS + 1 - fraction), gpu.HistoryWritten - 1);
                for (var neighbor = ago; neighbor <= Math.Min(ago + 1, gpu.HistoryWritten - 1); neighbor++)
                {
                    var frame = (gpu.HistoryNext + gpu.HistoryFrames - 1 - neighbor) % gpu.HistoryFrames;
                    for (var i = 0; i < source.Amount; i++) if (history[frame * source.Amount + i].State.Y == state[i].State.Y) Include(history[frame * source.Amount + i]);
                }
            }
        }
        void Include(ParticleStateLayout particle)
        {
            if ((particle.State.X & 1) == 0) return;
            var position = inverse * new Vector2(particle.TransformX.W, particle.TransformY.W);
            if (!position.IsFinite()) throw new ArithmeticException("Native particle bounds contain nonfinite coordinates.");
            bounds = found ? bounds.Expand(position) : new(position, Vector2.Zero); found = true;
        }
        var longest = 0f; var revision = source.Revision; source.Preparing = true;
        try { foreach (var mesh in source.DrawPasses) if (mesh is not null) { ObjectDisposedException.ThrowIf(mesh.IsDisposed, mesh); var box = mesh.GetAABB(); longest = Math.Max(longest, Math.Max(box.Size.X, box.Size.Y)); } }
        finally { source.Preparing = false; }
        if (source.Released || source.Revision != revision) throw new InvalidOperationException("Mesh callbacks changed particle bounds ownership.");
        return bounds.Grow(longest);
    }
    private CanvasRenderTarget? _particleDetachedTarget;
    private readonly CanvasLightingFrame _particleDetachedLighting = new() { SDFRect = new(0, 0, 1, 1), SDFSize = Vector2i.One };
    internal void ProcessPendingParticles(RenderingServer server)
    {
        var pending = false;
        foreach (var source in server.ParticleSchedule) if (source.Requested && source.Amount != 0 && source.LastFrame != server.ParticleFrame) { pending = true; break; }
        if (!pending) return;
        if (_particleDetachedTarget is null)
        {
            var current = CreateTarget(Vector2i.One, default);
            try { _particleDetachedTarget = new(current, CreateTarget(Vector2i.One, default), Vector2i.One); }
            catch { current.Dispose(); throw; }
        }
        Draw(_particleDetachedTarget, [], [], default, true, server.CanvasTime, [], _particleDetachedLighting);
    }
    private void BeginParticleFrame()
    { (_usedParticlePrograms, _previousParticlePrograms) = (_previousParticlePrograms, _usedParticlePrograms); _usedParticlePrograms.Clear(); }
    private void RetireParticlePrograms()
    { foreach (var pair in _particlePrograms) if (!_usedParticlePrograms.Contains(pair.Key) && !_previousParticlePrograms.Contains(pair.Key)) { pair.Value.Dispose(); _particlePrograms.Remove(pair.Key); } }
    private void ReleaseParticlePrograms()
    { _particleDetachedTarget?.Dispose(); _particleStep?.Dispose(); _particleQueue?.Dispose(); _particleOrder?.Dispose(); foreach (var pipeline in _particlePrograms.Values) pipeline.Dispose(); _particlePrograms.Clear(); }
}
