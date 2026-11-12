using SDL3;

namespace Electron2D;

internal sealed unsafe partial class GPUCanvasBackend
{
    private RenderHandle? _smaaEdgePipeline, _smaaWeightPipeline;
    private RenderHandle? _smaaArea, _smaaSearch;
    private readonly RenderHandle?[] _smaaBlendPipelines = new RenderHandle?[2];

    private static class SMAAShaders
    {
        internal static readonly byte[] Edges = BuiltInShaders.Read("CanvasSMAAEdges.frag.spv");
        internal static readonly byte[] Weights = BuiltInShaders.Read("CanvasSMAAWeights.frag.spv");
        internal static readonly byte[] Blend = BuiltInShaders.Read("CanvasSMAABlend.frag.spv");
    }

    private void PrepareSMAA(CanvasRenderTarget output)
    {
        var buffers = output.PostProcess!;
        const SDL.GPUTextureUsageFlags intermediate = SDL.GPUTextureUsageFlags.ColorTarget | SDL.GPUTextureUsageFlags.Sampler;
        buffers.SMAAEdges ??= CreateSMAATexture(output.Size, SDL.GPUTextureFormat.R8G8Unorm, intermediate);
        buffers.SMAAWeights ??= CreateSMAATexture(output.Size, SDL.GPUTextureFormat.R8G8B8A8Unorm, intermediate);
        _smaaEdgePipeline ??= CreatePostProcessPipeline(SMAAShaders.Edges, SDL.GPUTextureFormat.R8G8Unorm);
        _smaaWeightPipeline ??= CreatePostProcessPipeline(SMAAShaders.Weights, HDR: false);
        _smaaBlendPipelines[output.HDR ? 1 : 0] ??= CreatePostProcessPipeline(SMAAShaders.Blend, output.HDR);
        _smaaArea ??= UploadSMAALookup("Area.bin", new(160, 560), SDL.GPUTextureFormat.R8G8Unorm, 2);
        _smaaSearch ??= UploadSMAALookup("Search.bin", new(66, 33), SDL.GPUTextureFormat.R8Unorm, 1);
        _ = Sampler(TextureFilter.Nearest, TextureRepeat.Disabled);
    }

    private void DrawSMAA(nint command, CanvasRenderTarget output, nint source, nint destination, nint linear, in Vector4 metrics)
    {
        var buffers = output.PostProcess!;
        var edges = buffers.SMAAEdges!.DangerousGetHandle();
        var weights = buffers.SMAAWeights!.DangerousGetHandle();
        var nearest = Sampler(TextureFilter.Nearest, TextureRepeat.Disabled);
        var input = new SDL.GPUTextureSamplerBinding { Texture = source, Sampler = nearest };
        DrawPostProcessPass(command, edges, output.Size, _smaaEdgePipeline!, new(in input), in metrics);

        Span<SDL.GPUTextureSamplerBinding> bindings = stackalloc SDL.GPUTextureSamplerBinding[3];
        bindings[0] = new() { Texture = edges, Sampler = linear };
        bindings[1] = new() { Texture = _smaaArea!.DangerousGetHandle(), Sampler = linear };
        bindings[2] = new() { Texture = _smaaSearch!.DangerousGetHandle(), Sampler = nearest };
        DrawPostProcessPass(command, weights, output.Size, _smaaWeightPipeline!, bindings, in metrics);

        bindings[0] = new() { Texture = source, Sampler = linear };
        bindings[1] = new() { Texture = weights, Sampler = nearest };
        DrawPostProcessPass(command, destination, output.Size, _smaaBlendPipelines[output.HDR ? 1 : 0]!, bindings[..2], in metrics);
    }

    private RenderHandle CreateSMAATexture(Vector2i size, SDL.GPUTextureFormat format, SDL.GPUTextureUsageFlags usage)
    {
        const SDL.GPUTextureType type = SDL.GPUTextureType.TextureType2D;
        if (!SDL.GPUTextureSupportsFormat(Device, format, type, usage))
            throw new NotSupportedException($"SMAA requires {format} textures with {usage} usage.");
        var info = new SDL.GPUTextureCreateInfo
        {
            Type = type,
            Format = format,
            Width = (uint)size.X,
            Height = (uint)size.Y,
            NumLevels = 1,
            LayerCountOrDepth = 1,
            SampleCount = SDL.GPUSampleCount.SampleCount1,
            Usage = usage
        };
        return new(SDL.CreateGPUTexture(Device, in info), h => SDL.ReleaseGPUTexture(Device, h), _device);
    }

    private RenderHandle UploadSMAALookup(string name, Vector2i size, SDL.GPUTextureFormat format, int bytesPerPixel)
    {
        var bytes = checked(size.X * size.Y * bytesPerPixel);
        using var data = typeof(GPUCanvasBackend).Assembly.GetManifestResourceStream("Electron2D.SMAA." + name);
        if (data is null || data.Length != bytes) throw new InvalidOperationException("Invalid SMAA lookup: " + name);
        var info = new SDL.GPUTransferBufferCreateInfo { Usage = SDL.GPUTransferBufferUsage.Upload, Size = (uint)bytes };
        using var transfer = new RenderHandle(SDL.CreateGPUTransferBuffer(Device, in info), h => SDL.ReleaseGPUTransferBuffer(Device, h), _device);
        var mapped = SDL.MapGPUTransferBuffer(Device, transfer.DangerousGetHandle(), false);
        if (mapped == 0) throw Failure("map SMAA lookup data");
        try { data.ReadExactly(new Span<byte>((void*)mapped, bytes)); }
        finally { SDL.UnmapGPUTransferBuffer(Device, transfer.DangerousGetHandle()); }

        var texture = CreateSMAATexture(size, format, SDL.GPUTextureUsageFlags.Sampler);
        var command = SDL.AcquireGPUCommandBuffer(Device);
        try
        {
            if (command == 0) throw Failure("acquire SMAA lookup upload commands");
            var copy = SDL.BeginGPUCopyPass(command);
            if (copy == 0) throw Failure("begin SMAA lookup upload");
            var source = new SDL.GPUTextureTransferInfo { TransferBuffer = transfer.DangerousGetHandle() };
            var destination = new SDL.GPUTextureRegion { Texture = texture.DangerousGetHandle(), W = (uint)size.X, H = (uint)size.Y, D = 1 };
            SDL.UploadToGPUTexture(copy, in source, in destination, false);
            SDL.EndGPUCopyPass(copy);
            var submitted = command;
            command = 0;
            Check(SDL.SubmitGPUCommandBuffer(submitted), "submit SMAA lookup upload");
            return texture;
        }
        catch { texture.Dispose(); throw; }
        finally { if (command != 0) SDL.CancelGPUCommandBuffer(command); }
    }

    private void ReleaseSMAA()
    {
        _smaaArea?.Dispose();
        _smaaSearch?.Dispose();
        _smaaEdgePipeline?.Dispose();
        _smaaWeightPipeline?.Dispose();
        foreach (var pipeline in _smaaBlendPipelines) pipeline?.Dispose();
    }
}
