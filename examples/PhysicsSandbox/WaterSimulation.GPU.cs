using System.Runtime.InteropServices;
using Float4 = System.Numerics.Vector4;

namespace Electron2D.Examples.PhysicsSandbox;

internal sealed partial class WaterSimulation
{
    private RenderingDevice? _device;
    private RID _shader, _pipeline, _uniform, _parameters;
    private readonly RID[] _buffers = new RID[8], _gpuSets = new RID[2];
    private Float4[] _summaries = [];
    private int _gpuSource;

    internal void SetUseGPU(bool enabled, RenderingDevice? device = null)
    {
        if (enabled && _device is null)
        {
            _device = device ?? throw new InvalidOperationException("A local rendering device is required for GPU mode.");
            using var source = typeof(WaterSimulation).Assembly.GetManifestResourceStream("Electron2D.Compute.Water.comp.spv")
                ?? throw new InvalidOperationException("The compiled water shader is missing.");
            using var bytes = new MemoryStream(); source.CopyTo(bytes);
            using var module = new RDShaderSPIRV { BytecodeCompute = bytes.ToArray() };
            _shader = _device.ShaderCreateFromSPIRV(module, "Water density constraints"); _pipeline = _device.ComputePipelineCreate(_shader);
            _uniform = _device.UniformBufferCreate((uint)Marshal.SizeOf<Settings>());
            using var descriptor = new RDUniform { Binding = 0, UniformType = RenderingDevice.UniformType.UniformBuffer }; descriptor.AddID(_uniform);
            _parameters = _device.UniformSetCreate([descriptor], _shader, 1); CreateBuffers();
        }
        if (enabled && !UseGPU)
        { _gpuSource = 0; _device!.BufferUpdate(_buffers[0], 0, (uint)(Count * 16), MemoryMarshal.AsBytes(_state.AsSpan())); }
        UseGPU = enabled;
    }
    private void CreateBuffers()
    {
        if (_device is null) return;
        for (var i = 0; i < _gpuSets.Length; i++) if (_gpuSets[i].IsValid()) { _device.FreeRID(_gpuSets[i]); _gpuSets[i] = default; }
        for (var i = 0; i < _buffers.Length; i++) if (_buffers[i].IsValid()) { _device.FreeRID(_buffers[i]); _buffers[i] = default; }
        var groups = (Count + 127) / 128; _summaries = new Float4[groups * 2];
        int[] sizes = [Count * 16, Count * 16, _heads.Length * 4, Count * 4, Count * 8, Count * 32, groups * 32, Count * 16];
        for (var i = 0; i < _buffers.Length; i++) _buffers[i] = _device.StorageBufferCreate((uint)sizes[i]);
        for (var flip = 0; flip < 2; flip++)
        {
            var bindings = new RDUniform[8];
            try
            {
                for (var i = 0; i < bindings.Length; i++)
                { bindings[i] = new RDUniform { Binding = i, UniformType = RenderingDevice.UniformType.StorageBuffer }; bindings[i].AddID(_buffers[i < 2 ? i ^ flip : i]); }
                _gpuSets[flip] = _device.UniformSetCreate(bindings, _shader, 0);
            }
            finally { foreach (var binding in bindings) binding?.Dispose(); }
        }
        _gpuSource = 0; _device.BufferUpdate(_buffers[0], 0, (uint)(Count * 16), MemoryMarshal.AsBytes(_state.AsSpan()));
    }
    private void StepGPU(bool capture)
    {
        var device = _device!; var list = device.ComputeListBegin();
        device.ComputeListBindComputePipeline(list, _pipeline); device.ComputeListBindUniformSet(list, _parameters, 1);
        device.ComputeListBindUniformSet(list, _gpuSets[_gpuSource], 0);
        Dispatch(5, Count); _gpuSource ^= 1;
        for (var iteration = 0; iteration < 6; iteration++)
        {
            device.ComputeListBindUniformSet(list, _gpuSets[_gpuSource], 0);
            Dispatch(0, Math.Max(Count, _heads.Length)); Dispatch(1, Count); Dispatch(2, Count); Dispatch(3, Count);
            _gpuSource ^= 1;
        }
        device.ComputeListBindUniformSet(list, _gpuSets[_gpuSource], 0);
        Dispatch(0, Math.Max(Count, _heads.Length)); Dispatch(1, Count); Dispatch(6, Count); _gpuSource ^= 1;
        Dispatch(4, Count); device.ComputeListEnd();
        if (capture) device.BufferGetData(_buffers[_gpuSource], MemoryMarshal.AsBytes(_state.AsSpan()));
        device.BufferGetData(_buffers[6], MemoryMarshal.AsBytes(_summaries.AsSpan()));
        var duck = Float4.Zero; var boat = Float4.Zero;
        for (var i = 0; i < _summaries.Length; i += 2) { duck += _summaries[i]; boat += _summaries[i + 1]; }
        React(Duck, duck); React(Boat, boat);
        void Dispatch(int operation, int count)
        {
            _settings.Meta.Y = operation;
            device.BufferUpdate(_uniform, 0, (uint)Marshal.SizeOf<Settings>(), MemoryMarshal.AsBytes(MemoryMarshal.CreateReadOnlySpan(ref _settings, 1)));
            device.ComputeListDispatch(list, (uint)((count + 127) / 128), 1, 1);
        }
    }
    private void FreeGPU()
    {
        if (_device is null) return;
        foreach (var set in _gpuSets) if (set.IsValid()) _device.FreeRID(set);
        foreach (var buffer in _buffers) if (buffer.IsValid()) _device.FreeRID(buffer);
        foreach (var resource in (ReadOnlySpan<RID>)[_parameters, _uniform, _pipeline, _shader]) if (resource.IsValid()) _device.FreeRID(resource);
        _device = null;
    }
}
