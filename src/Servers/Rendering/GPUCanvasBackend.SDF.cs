using System.Runtime.InteropServices;
using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private RenderHandle? _sdfTarget, _sdfPipeline;
    private Vector2i _sdfSize;
    private void PrepareSDF(CanvasLightingFrame frame)
    {
        if (frame.SDFSize.X > 16384 || frame.SDFSize.Y > 16384) throw new NotSupportedException("The selected canvas distance-field area and resolution exceed the 16384-pixel texture axis bound.");
        _sdfPipeline ??= CreatePipeline(BuiltInShaders.SDF, BlendMode.Mix, overwrite: true, format: SDL.GPUTextureFormat.R32G32B32A32Float);
        if (_sdfTarget is not null && _sdfSize == frame.SDFSize) return;
        var info = new SDL.GPUTextureCreateInfo { Type = SDL.GPUTextureType.TextureType2D, Format = SDL.GPUTextureFormat.R32G32B32A32Float, Width = (uint)frame.SDFSize.X, Height = (uint)frame.SDFSize.Y, LayerCountOrDepth = 1, NumLevels = 1, Usage = SDL.GPUTextureUsageFlags.Sampler | SDL.GPUTextureUsageFlags.ColorTarget };
        var replacement = new RenderHandle(SDL.CreateGPUTexture(Device, in info), h => SDL.ReleaseGPUTexture(Device, h), _device);
        _sdfTarget?.Dispose(); _sdfTarget = replacement; _sdfSize = frame.SDFSize;
    }
    private static Vector4 SDFCoords(CanvasLightingFrame frame) => new(1 / frame.SDFRect.Size.X, 1 / frame.SDFRect.Size.Y, -frame.SDFRect.Position.X / frame.SDFRect.Size.X, -frame.SDFRect.Position.Y / frame.SDFRect.Size.Y);
    private static void WriteSDFVertices(Span<CanvasVertex> vertices, CanvasLightingFrame frame)
    {
        for (var i = 0; i < 6; i++)
        {
            var position = i switch { 0 or 3 => Vector2.Zero, 1 => new Vector2(frame.SDFSize.X, 0), 2 or 4 => new Vector2(frame.SDFSize.X, frame.SDFSize.Y), _ => new Vector2(0, frame.SDFSize.Y) };
            vertices[i] = new(position, Colors.White, Vector2.Zero);
        }
    }
    [StructLayout(LayoutKind.Sequential)]
    private readonly record struct SDFUniforms(Vector4 Region, Vector4 Dimensions, Vector4i Counts);
    private void DrawSDF(nint command, CanvasLightingFrame frame, int lightingBytes, int first)
    {
        var target = new SDL.GPUColorTargetInfo { Texture = _sdfTarget!.DangerousGetHandle(), LoadOp = SDL.GPULoadOp.Clear, StoreOp = SDL.GPUStoreOp.Store, Cycle = true };
        var pass = SDL.BeginGPURenderPass(command, new ReadOnlySpan<SDL.GPUColorTargetInfo>(&target, 1), 1, 0);
        if (pass == 0) throw Failure("begin canvas distance-field generation");
        try
        {
            SDL.BindGPUGraphicsPipeline(pass, _sdfPipeline!.DangerousGetHandle());
            var dimensions = new Vector2(frame.SDFSize.X, frame.SDFSize.Y); SDL.PushGPUVertexUniformData(command, 0, (nint)(&dimensions), (uint)sizeof(Vector2));
            var uniform = new SDFUniforms(new(frame.SDFRect.Position.X, frame.SDFRect.Position.Y, frame.SDFRect.Size.X, frame.SDFRect.Size.Y), new(dimensions.X, dimensions.Y, 0, 0), new(frame.LightCount, frame.SegmentCount, 0, 0));
            SDL.PushGPUFragmentUniformData(command, 0, (nint)(&uniform), (uint)sizeof(SDFUniforms));
            var binding = new SDL.GPUBufferBinding { Buffer = _vertexBuffer!.DangerousGetHandle(), Offset = (uint)lightingBytes };
            SDL.BindGPUVertexBuffers(pass, 0, new ReadOnlySpan<SDL.GPUBufferBinding>(&binding, 1), 1);
            var buffer = _vertexBuffer.DangerousGetHandle(); SDL.BindGPUFragmentStorageBuffers(pass, 0, (nint)(&buffer), 1);
            SDL.DrawGPUPrimitives(pass, 6, 1, (uint)first, 0);
        }
        finally { SDL.EndGPURenderPass(pass); }
    }
}
