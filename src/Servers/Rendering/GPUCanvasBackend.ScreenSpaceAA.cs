using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private readonly RenderHandle?[] _fxaaPipelines = new RenderHandle?[2];
    private static readonly byte[] FXAAShader = BuiltInShaders.Read("CanvasFXAA.frag.spv");

    private void PrepareScreenSpaceAA(CanvasRenderTarget output, ScreenSpaceAAMode mode, bool copyComposited)
    {
        if (mode == ScreenSpaceAAMode.FXAA)
            _fxaaPipelines[output.HDR ? 1 : 0] ??= CreatePostProcessPipeline(FXAAShader, output.HDR);
        else
            PrepareSMAA(output);

        _ = Sampler(TextureFilter.Linear, TextureRepeat.Disabled);
        if (copyComposited) output.PostProcess!.AAInput ??= CreateTarget(output.Size, default, HDR: output.HDR);
    }

    private void DrawScreenSpaceAA(nint command, CanvasRenderTarget output, ScreenSpaceAAMode mode, bool copyComposited)
    {
        var buffers = output.PostProcess!;
        var size = output.Size;
        var destination = output.Next.DangerousGetHandle();
        var source = buffers.Raw.DangerousGetHandle();
        if (copyComposited)
        {
            source = buffers.AAInput!.DangerousGetHandle();
            Blit(command, destination, source, new(0, 0, size.X, size.Y));
        }

        var linear = Sampler(TextureFilter.Linear, TextureRepeat.Disabled);
        var metrics = new Vector4(1f / size.X, 1f / size.Y, output.HDR ? 1f : 0f, 0f);
        if (mode == ScreenSpaceAAMode.FXAA)
        {
            var binding = new SDL.GPUTextureSamplerBinding { Texture = source, Sampler = linear };
            DrawPostProcessPass(command, destination, size, _fxaaPipelines[output.HDR ? 1 : 0]!, new(in binding), in metrics);
        }
        else DrawSMAA(command, output, source, destination, linear, in metrics);
    }
}
