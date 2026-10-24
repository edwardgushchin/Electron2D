using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private ComputePipeline? _particleVertices;
    private readonly List<ParticleStream> _particleStreams = [];
    private readonly HashSet<ParticleGeometry> _particleGeometries = [], _usedParticleGeometries = [];
    private readonly List<ParticleGeometry> _particleGeometryRetire = [];
    private int _usedParticleStreams;
    private sealed class ParticleStream : IDisposable
    {
        internal RenderHandle? Vertices, Draws;
        internal int Bytes, Amount;
        public void Dispose() { Vertices?.Dispose(); Draws?.Dispose(); }
    }
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct ParticleVertexParameters(Vector4 X, Vector4 Y, Color Color, Vector4i Counts, Vector4i History, Vector4i Options, Vector4 Timing, Vector4 Sheet);
    private void PrepareParticleVertices(nint command, ReadOnlySpan<CanvasBatch> batches, int lightingBytes, Vector2i size)
    {
        if (sizeof(CanvasVertex) != 168) throw new NotSupportedException("The particle vertex kernel requires the current canvas vertex layout.");
        var ordinal = 0; var inputs = stackalloc nint[7]; var outputs = stackalloc SDL.GPUStorageBufferReadWriteBinding[2];
        foreach (var batch in batches)
        {
            if (batch.Particles is not { } drawing || drawing.Source.GPU is not { Initialized: true } gpu) continue;
            _particleVertices ??= ParticleKernel("ParticleVertices");
            while (_particleStreams.Count <= ordinal) _particleStreams.Add(new()); var stream = _particleStreams[ordinal++];
            var count = checked(batch.Count * drawing.Source.Amount); var bytes = checked(count * sizeof(CanvasVertex));
            if (stream.Bytes != bytes)
            {
                var replacement = InstanceBuffer((uint)bytes, SDL.GPUBufferUsageFlags.Vertex | SDL.GPUBufferUsageFlags.ComputeStorageWrite);
                stream.Vertices?.Dispose(); stream.Vertices = replacement; stream.Bytes = bytes;
            }
            if (stream.Amount != drawing.Source.Amount)
            {
                var replacement = InstanceBuffer(checked((uint)drawing.Source.Amount * 16), SDL.GPUBufferUsageFlags.Indirect | SDL.GPUBufferUsageFlags.ComputeStorageWrite);
                stream.Draws?.Dispose(); stream.Draws = replacement; stream.Amount = drawing.Source.Amount;
            }
            _particleGeometries.Add(drawing.Geometry); _usedParticleGeometries.Add(drawing.Geometry);
            inputs[0] = gpu.State.DangerousGetHandle(); inputs[1] = gpu.Previous.DangerousGetHandle(); inputs[2] = gpu.History.DangerousGetHandle(); inputs[3] = gpu.Poses.DangerousGetHandle(); inputs[4] = _vertexBuffer!.DangerousGetHandle(); inputs[5] = drawing.Geometry.Prepare(gpu, _device, command); inputs[6] = gpu.Order.DangerousGetHandle();
            outputs[0] = new() { Buffer = stream.Vertices!.DangerousGetHandle() }; outputs[1] = new() { Buffer = stream.Draws!.DangerousGetHandle() };
            var source = drawing.Source; var canvas = drawing.Canvas; var groups = ParticleDispatch(count);
            var frequency = source.FixedFPS > 0 ? source.FixedFPS : 60;
            var fraction = source.Interpolate && (source.FixedFPS > 0 || source.Trails && source.BindPoses.Length > 1) ? (float)Math.Clamp(source.Remainder * frequency, 0, 1) : 1;
            var parameters = new ParticleVertexParameters(new(canvas.X.X, canvas.Y.X, 0, canvas.Origin.X), new(canvas.X.Y, canvas.Y.Y, 0, canvas.Origin.Y), drawing.Color,
                new(source.Amount, batch.Count, checked(lightingBytes / 4 + batch.First * 42), (batch.LinearColors ? 1 : 0) | (batch.Procedural ? 2 : 0) | (source.Trails && drawing.Geometry.HasSkin ? 4 : 0) | (batch.GroupShader ? 8 : 0)),
                new(gpu.HistoryFrames, gpu.HistoryNext, gpu.HistoryWritten, source.BindPoses.Length), new((int)source.Align, (int)source.AlignAxis, size.X, checked((int)groups.Width * 64)), new(fraction, frequency, source.TrailLength, size.Y), drawing.Sheet);
            SDL.PushGPUComputeUniformData(command, 0, (nint)(&parameters), (uint)sizeof(ParticleVertexParameters));
            var pass = SDL.BeginGPUComputePass(command, 0, 0, (nint)outputs, 2); if (pass == 0) throw Failure("begin particle canvas conversion");
            SDL.BindGPUComputePipeline(pass, _particleVertices.DangerousGetHandle()); SDL.BindGPUComputeStorageBuffers(pass, 0, (nint)inputs, 7); SDL.DispatchGPUCompute(pass, groups.Width, groups.Height, 1); SDL.EndGPUComputePass(pass);
        }
        _usedParticleStreams = Math.Max(_usedParticleStreams, ordinal);
    }
    private void RetireParticleVertices()
    {
        foreach (var geometry in _particleGeometries) if (!_usedParticleGeometries.Contains(geometry)) { geometry.Buffer?.Dispose(); geometry.Buffer = null; _particleGeometryRetire.Add(geometry); }
        foreach (var geometry in _particleGeometryRetire) _particleGeometries.Remove(geometry);
        _particleGeometryRetire.Clear(); _usedParticleGeometries.Clear();
        while (_particleStreams.Count > _usedParticleStreams) { _particleStreams[^1].Dispose(); _particleStreams.RemoveAt(_particleStreams.Count - 1); }
        _usedParticleStreams = 0;
    }
    private void ReleaseParticleVertices()
    { foreach (var stream in _particleStreams) stream.Dispose(); _particleStreams.Clear(); foreach (var geometry in _particleGeometries) { geometry.Buffer?.Dispose(); geometry.Buffer = null; } _particleGeometries.Clear(); _particleVertices?.Dispose(); }
}
