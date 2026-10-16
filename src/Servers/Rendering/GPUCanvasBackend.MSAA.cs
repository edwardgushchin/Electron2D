using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private nint BeginCanvasPass(nint command, CanvasRenderTarget output, bool backBuffer, SDL.GPULoadOp load, Color clear)
    {
        var resolved = backBuffer ? output.BackBuffer! : output.Next;
        var multisample = backBuffer ? output.MultisampleBackBuffer : output.Multisample;
        if (multisample is null) return BeginPass(command, resolved.DangerousGetHandle(), load, clear);
        var target = new SDL.GPUColorTargetInfo
        {
            Texture = multisample.DangerousGetHandle(),
            ResolveTexture = resolved.DangerousGetHandle(),
            LoadOp = load,
            StoreOp = SDL.GPUStoreOp.ResolveAndStore,
            ClearColor = new() { R = clear.R, G = clear.G, B = clear.B, A = clear.A }
        };
        var pass = SDL.BeginGPURenderPass(command, new ReadOnlySpan<SDL.GPUColorTargetInfo>(&target, 1), 1, 0);
        if (pass == 0) throw Failure("begin multisample canvas render pass");
        return pass;
    }
    private static void WriteSeedVertices(Span<CanvasVertex> vertices, Vector2i size)
    {
        var a = new CanvasVertex(Vector2.Zero, Colors.White, Vector2.Zero);
        var b = new CanvasVertex(new(size.X, 0), Colors.White, new(1, 0));
        var c = new CanvasVertex(new(size.X, size.Y), Colors.White, Vector2.One);
        var d = new CanvasVertex(new(0, size.Y), Colors.White, new(0, 1));
        vertices[0] = a; vertices[1] = b; vertices[2] = c; vertices[3] = a; vertices[4] = c; vertices[5] = d;
    }
    private void SeedBackBuffer(nint command, CanvasRenderTarget output, int first, int vertexOffset)
    {
        var pass = BeginPass(command, output.MultisampleBackBuffer!.DangerousGetHandle(), SDL.GPULoadOp.DontCare, default);
        try
        {
            SDL.BindGPUGraphicsPipeline(pass, _seedPipelines[output.HDR ? 1 : 0, (int)output.MSAA]!.DangerousGetHandle());
            var dimensions = new Vector4(output.Size.X, output.Size.Y, 0, 0);
            PushFrame(command, dimensions, Colors.White);
            var binding = new SDL.GPUBufferBinding { Buffer = _vertexBuffer!.DangerousGetHandle(), Offset = (uint)vertexOffset };
            SDL.BindGPUVertexBuffers(pass, 0, new ReadOnlySpan<SDL.GPUBufferBinding>(&binding, 1), 1);
            var sampler = new SDL.GPUTextureSamplerBinding { Texture = output.BackBuffer!.DangerousGetHandle(), Sampler = Sampler(TextureFilter.Nearest, TextureRepeat.Disabled) };
            SDL.BindGPUFragmentSamplers(pass, 0, new ReadOnlySpan<SDL.GPUTextureSamplerBinding>(&sampler, 1), 1);
            SDL.DrawGPUPrimitives(pass, 6, 1, (uint)first, 0);
        }
        finally { SDL.EndGPURenderPass(pass); }
    }
}
