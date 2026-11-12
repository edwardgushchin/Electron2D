using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private readonly RenderHandle?[] _FXAAPipelines = new RenderHandle?[2];
    private static readonly byte[] FXAAShader = BuiltInShaders.Read("CanvasFXAA.frag.spv");

    private void PrepareFXAA(CanvasRenderTarget output, bool copyComposited)
    {
        _FXAAPipelines[output.HDR ? 1 : 0] ??= CreatePostProcessPipeline(FXAAShader, output.HDR);
        _ = Sampler(TextureFilter.Linear, TextureRepeat.Disabled);
        if (copyComposited) output.PostProcess!.FXAAInput ??= CreateTarget(output.Size, default, HDR: output.HDR);
    }

    private void DrawFXAA(nint command, CanvasRenderTarget output, bool copyComposited)
    {
        var buffers = output.PostProcess!;
        var size = output.Size;
        var destination = output.Next.DangerousGetHandle();
        var source = buffers.Raw.DangerousGetHandle();
        if (copyComposited)
        {
            source = buffers.FXAAInput!.DangerousGetHandle();
            Blit(command, destination, source, new(0, 0, size.X, size.Y));
        }
        var binding = new SDL.GPUTextureSamplerBinding { Texture = source, Sampler = Sampler(TextureFilter.Linear, TextureRepeat.Disabled) };
        var parameters = new Vector4(1f / size.X, 1f / size.Y, output.HDR ? 1f : 0f, 0f);
        DrawPostProcessPass(command, destination, size, _FXAAPipelines[output.HDR ? 1 : 0]!, new(in binding), in parameters);
    }
}
