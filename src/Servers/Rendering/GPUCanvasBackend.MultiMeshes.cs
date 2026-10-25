using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private sealed class InstanceStream : IDisposable
    {
        internal RenderHandle? Instances, Commands, Models;
        internal int ModelCapacity;
        internal int Capacity, CommandCapacity;
        public void Dispose() { Instances?.Dispose(); Commands?.Dispose(); Models?.Dispose(); }
    }
    private RenderHandle? _instancePack, _instancePackModels, _instanceIndices, _instanceIndexUpload, _instanceLODs, _instanceLODUpload;
    private int _instanceIndexCapacity, _instanceLODCapacity;
    private readonly List<InstanceStream> _instanceStreams = [];
    private int[] _streamByBatch = [], _indexByBatch = [], _lodByBatch = [];
    [StructLayout(LayoutKind.Sequential)]
    private struct InstanceParameters
    {
        internal Vector4 Canvas, Origin;
        internal Color Modulation;
        internal Vector4i Layout, Geometry, Levels;
        internal Vector4 Physics;
    }
    private RenderHandle InstanceBuffer(uint bytes, SDL.GPUBufferUsageFlags usage) => new(SDL.CreateGPUBuffer(Device, new() { Size = bytes, Usage = usage }), h => SDL.ReleaseGPUBuffer(Device, h), _device);
    private void PrepareInstanceStreams(nint command, ReadOnlySpan<CanvasBatch> batches)
    {
        var indexCount = 0; var streamCount = 0; var lodCount = 0;
        if (_streamByBatch.Length < batches.Length) { Array.Resize(ref _streamByBatch, batches.Length); Array.Resize(ref _indexByBatch, batches.Length); Array.Resize(ref _lodByBatch, batches.Length); }
        for (var i = 0; i < batches.Length; i++)
        {
            _streamByBatch[i] = -1; _indexByBatch[i] = indexCount; _lodByBatch[i] = lodCount;
            if (batches[i].GPU.State is null || batches[i].Operation == CanvasOperation.GroupBegin) continue;
            _streamByBatch[i] = streamCount++; indexCount = checked(indexCount + (batches[i].GPU.Indices?.Length ?? 0)); lodCount = checked(lodCount + (batches[i].GPU.LODs?.Length ?? 0));
        }
        if (streamCount == 0) return;
        if (_instancePack is null)
        {
            using var source = typeof(GPUCanvasBackend).Assembly.GetManifestResourceStream("Electron2D.Shaders.InstancePack.comp.spv")!; using var bytes = new MemoryStream(); source.CopyTo(bytes);
            _instancePack = new(ShaderCompiler.CreateComputePipeline(Device, bytes.ToArray()), h => SDL.ReleaseGPUComputePipeline(Device, h), _device);
        }
        if (indexCount != 0)
        {
            if (_instanceIndexCapacity < indexCount)
            {
                var bytes = checked((uint)indexCount * 4); var indices = InstanceBuffer(bytes, SDL.GPUBufferUsageFlags.Index);
                RenderHandle transfer; try { transfer = new(SDL.CreateGPUTransferBuffer(Device, new() { Size = bytes, Usage = SDL.GPUTransferBufferUsage.Upload }), h => SDL.ReleaseGPUTransferBuffer(Device, h), _device); } catch { indices.Dispose(); throw; }
                _instanceIndices?.Dispose(); _instanceIndexUpload?.Dispose(); _instanceIndices = indices; _instanceIndexUpload = transfer; _instanceIndexCapacity = indexCount;
            }
            var mapped = SDL.MapGPUTransferBuffer(Device, _instanceIndexUpload!.DangerousGetHandle(), true); if (mapped == 0) throw Failure("map instance indices");
            try { var values = new Span<int>((void*)mapped, indexCount); for (var i = 0; i < batches.Length; i++) if (_streamByBatch[i] >= 0 && batches[i].GPU.Indices is { } indices) indices.CopyTo(values[_indexByBatch[i]..]); }
            finally { SDL.UnmapGPUTransferBuffer(Device, _instanceIndexUpload.DangerousGetHandle()); }
            var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw Failure("upload instance indices");
            SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = _instanceIndexUpload.DangerousGetHandle() }, new() { Buffer = _instanceIndices!.DangerousGetHandle(), Size = checked((uint)indexCount * 4) }, false); SDL.EndGPUCopyPass(copy);
        }
        if (_instanceLODCapacity < Math.Max(1, lodCount))
        {
            var capacity = Math.Max(1, lodCount); var bytes = checked((uint)capacity * 16); var levels = InstanceBuffer(bytes, SDL.GPUBufferUsageFlags.ComputeStorageRead);
            RenderHandle transfer; try { transfer = new(SDL.CreateGPUTransferBuffer(Device, new() { Size = bytes, Usage = SDL.GPUTransferBufferUsage.Upload }), h => SDL.ReleaseGPUTransferBuffer(Device, h), _device); } catch { levels.Dispose(); throw; }
            _instanceLODs?.Dispose(); _instanceLODUpload?.Dispose(); _instanceLODs = levels; _instanceLODUpload = transfer; _instanceLODCapacity = capacity;
        }
        if (lodCount != 0)
        {
            var mapped = SDL.MapGPUTransferBuffer(Device, _instanceLODUpload!.DangerousGetHandle(), true); if (mapped == 0) throw Failure("map instance LODs");
            try
            {
                var values = new Span<Vector4i>((void*)mapped, lodCount);
                for (var i = 0; i < batches.Length; i++) if (_streamByBatch[i] >= 0 && batches[i].GPU.LODs is { } lods)
                    { var offset = batches[i].GPU.IndexCount; for (var l = 0; l < lods.Length; l++) { values[_lodByBatch[i] + l] = new(BitConverter.SingleToInt32Bits(lods[l].Error), offset, lods[l].Data.Indices.Length, 0); offset += lods[l].Data.Indices.Length; } }
            }
            finally { SDL.UnmapGPUTransferBuffer(Device, _instanceLODUpload.DangerousGetHandle()); }
            var copy = SDL.BeginGPUCopyPass(command); if (copy == 0) throw Failure("upload instance LODs");
            SDL.UploadToGPUBuffer(copy, new() { TransferBuffer = _instanceLODUpload.DangerousGetHandle() }, new() { Buffer = _instanceLODs!.DangerousGetHandle(), Size = checked((uint)lodCount * 16) }, false); SDL.EndGPUCopyPass(copy);
        }
        var output = stackalloc SDL.GPUStorageBufferReadWriteBinding[3]; var input = stackalloc nint[4];
        for (var i = 0; i < batches.Length; i++)
        {
            var ordinal = _streamByBatch[i]; if (ordinal < 0) continue; var batch = batches[i]; var gpu = batch.GPU; var models = batch.UsesModelPoseTable;
            while (_instanceStreams.Count <= ordinal) _instanceStreams.Add(new()); var stream = _instanceStreams[ordinal];
            if (stream.Capacity < gpu.Capacity)
            {
                var replacement = InstanceBuffer(checked((uint)gpu.Capacity * 64), SDL.GPUBufferUsageFlags.Vertex | SDL.GPUBufferUsageFlags.ComputeStorageWrite);
                stream.Instances?.Dispose(); stream.Instances = replacement; stream.Capacity = gpu.Capacity;
            }
            if (models && stream.ModelCapacity < gpu.Capacity)
            {
                var replacement = InstanceBuffer(checked((uint)gpu.Capacity * 32), SDL.GPUBufferUsageFlags.GraphicsStorageRead | SDL.GPUBufferUsageFlags.ComputeStorageWrite);
                stream.Models?.Dispose(); stream.Models = replacement; stream.ModelCapacity = gpu.Capacity;
            }
            if (models && _instancePackModels is null)
            {
                _instancePackModels = new(ShaderCompiler.CreateComputePipeline(Device, BuiltInShaders.Read("InstancePackModels.comp.spv")), h => SDL.ReleaseGPUComputePipeline(Device, h), _device);
            }
            var commands = gpu.LODs is { Length: > 0 } ? gpu.Capacity : 1;
            if (stream.CommandCapacity < commands) { var replacement = InstanceBuffer(checked((uint)commands * 20), SDL.GPUBufferUsageFlags.Indirect | SDL.GPUBufferUsageFlags.ComputeStorageWrite); stream.Commands?.Dispose(); stream.Commands = replacement; stream.CommandCapacity = commands; }
            output[0] = new() { Buffer = stream.Instances!.DangerousGetHandle() }; output[1] = new() { Buffer = stream.Commands!.DangerousGetHandle() };
            if (models) output[2] = new() { Buffer = stream.Models!.DangerousGetHandle() };
            input[0] = gpu.State!.Handle(gpu.State.Current, Device); input[1] = gpu.State.Handle(gpu.State.Previous, Device); input[2] = gpu.State.Handle(gpu.Indirect ? gpu.State.Commands : gpu.State.Current, Device); input[3] = _instanceLODs!.DangerousGetHandle();
            var groups = ((uint)gpu.Capacity + 63) / 64; var width = Math.Min(65535u, groups);
            var parameters = new InstanceParameters
            {
                Canvas = new(gpu.Canvas.X.X, gpu.Canvas.X.Y, gpu.Canvas.Y.X, gpu.Canvas.Y.Y),
                Origin = new(gpu.Canvas.Origin.X, gpu.Canvas.Origin.Y, 0, 0),
                Modulation = gpu.Modulation,
                Layout = new(gpu.Capacity, gpu.Stride, (gpu.Colors ? 1 : 0) | (gpu.Custom ? 2 : 0) | (batch.LinearColors ? 4 : 0) | (batch.GroupShader ? 8 : 0) | (gpu.Indirect ? 16 : 0) | (gpu.Indices is not null ? 32 : 0), gpu.Visible),
                Geometry = new(gpu.Surface, batch.First, _indexByBatch[i], gpu.Indices is null ? batch.Count : gpu.IndexCount),
                Levels = new(_lodByBatch[i], gpu.LODs?.Length ?? 0, _relaxedAndroidDevice ? 1 : 0, 0),
                Physics = new(gpu.Fraction, (float)gpu.Quality, width * 64, 0)
            };
            SDL.PushGPUComputeUniformData(command, 0, (nint)(&parameters), (uint)sizeof(InstanceParameters));
            var pass = SDL.BeginGPUComputePass(command, 0, 0, (nint)output, models ? 3u : 2u); if (pass == 0) throw Failure("begin instance conversion");
            SDL.BindGPUComputePipeline(pass, (models ? _instancePackModels! : _instancePack).DangerousGetHandle()); SDL.BindGPUComputeStorageBuffers(pass, 0, (nint)input, 4); SDL.DispatchGPUCompute(pass, width, (groups + width - 1) / width, 1); SDL.EndGPUComputePass(pass);
        }
    }
    private void ReleaseInstanceStreams()
    {
        foreach (var stream in _instanceStreams) stream.Dispose(); _instanceStreams.Clear(); _instancePack?.Dispose(); _instancePackModels?.Dispose(); _instanceIndices?.Dispose(); _instanceIndexUpload?.Dispose(); _instanceLODs?.Dispose(); _instanceLODUpload?.Dispose();
    }
}
