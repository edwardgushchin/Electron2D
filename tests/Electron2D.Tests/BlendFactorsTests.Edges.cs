using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class BlendFactorsTests
{
    private static void Edges(RenderingDevice device, ShaderLanguage language, TextureSamples samples)
    {
        const int logical = 3;
        var shader = Shader(device, language, true, logical, true);
        using var format = new RDTextureFormat { Width = 2, Height = 2, Format = DataFormat.R32G32B32A32SFLOAT, TextureType = TextureType.TextureArray, ArrayLayers = 2, Samples = samples, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom };
        using var view = new RDTextureView(); var image = device.TextureCreate(format, view); RID resolve = default;
        if (samples != TextureSamples.Samples1) { format.Samples = TextureSamples.Samples1; format.IsResolveBuffer = true; resolve = device.TextureCreate(format, view); }
        format.Format = DataFormat.D32SFLOATS8UINT; format.Samples = samples; format.IsResolveBuffer = false; format.UsageBits = TextureUsageBits.DepthStencilAttachment; var depth = device.TextureCreate(format, view);
        using var pass = new RDFramebufferPass { ColorAttachments = logical == 0 ? [0] : [-1, -1, -1, 0], DepthAttachment = 1, ResolveAttachments = samples == TextureSamples.Samples1 ? [] : logical == 0 ? [2] : [-1, -1, -1, 2] };
        var framebuffer = device.FramebufferCreateMultipass(samples == TextureSamples.Samples1 ? [image, depth] : [image, depth, resolve], [pass, pass], viewCount: 2);
        using var sourceFormat = new RDTextureFormat { Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Sampling }; using var sourceView = new RDTextureView { SwizzleR = TextureSwizzle.B, SwizzleB = TextureSwizzle.R };
        var source = device.TextureCreate(sourceFormat, sourceView, [MemoryMarshal.AsBytes(Source.AsSpan()).ToArray()]); using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState);
        using var uniform = new RDUniform { Binding = 0, UniformType = language == ShaderLanguage.HLSL ? UniformType.Texture : UniformType.SamplerWithTexture }; if (language != ShaderLanguage.HLSL) uniform.AddID(sampler); uniform.AddID(source);
        using var samplerUniform = new RDUniform { Binding = 1, UniformType = UniformType.Sampler }; samplerUniform.AddID(sampler); var set = device.UniformSetCreate(language == ShaderLanguage.HLSL ? [uniform, samplerUniform] : [uniform], shader, 0);
        using var attachment = new RDPipelineColorBlendStateAttachment { EnableBlend = true, SrcColorBlendFactor = BlendFactor.Src1Color, DstColorBlendFactor = BlendFactor.ConstantAlpha, SrcAlphaBlendFactor = BlendFactor.Src1Alpha, DstAlphaBlendFactor = BlendFactor.OneMinusConstantAlpha, WriteG = false };
        using var blend = new RDPipelineColorBlendState { Attachments = [attachment, attachment, attachment, attachment], BlendConstant = Color(Constant) }; using var raster = new RDPipelineRasterizationState();
        using var depthState = new RDPipelineDepthStencilState { EnableDepthTest = true, EnableDepthWrite = true, DepthCompareOperator = CompareOperator.LessOrEqual, EnableStencil = true, FrontOpPass = StencilOperation.Replace, BackOpPass = StencilOperation.Replace, FrontOpReference = 3, BackOpReference = 5, FrontOpWriteMask = 255, BackOpWriteMask = 255 };
        using var sampleState = new RDPipelineMultisampleState { SampleCount = samples, SampleMasks = samples == TextureSamples.Samples1 ? [1] : [15, 0, 0, 0] };
        var layout = device.FramebufferGetFormat(framebuffer);
        var first = device.RenderPipelineCreate(shader, layout, -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: depthState, multisampleState: sampleState);
        var second = device.RenderPipelineCreate(shader, layout, -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: depthState, multisampleState: sampleState, forRenderPass: 1);
        attachment.SrcColorBlendFactor = BlendFactor.Zero; attachment.WriteR = false; blend.BlendConstant = new(0, 0, 0, 0); sampleState.SampleMasks = [0]; depthState.FrontOpReference = 0;
        var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, samples == TextureSamples.Samples1 ? [Color(Destination)] : [Color(Destination), Color(Destination)]); device.DrawListBindRenderPipeline(list, first); device.DrawListBindUniformSet(list, set, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, second); device.DrawListBindUniformSet(list, set, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        float[] swizzled = [Source[2], Source[1], Source[0], Source[3]];
        for (uint layer = 0; layer < 2; layer++)
        {
            var bytes = device.TextureGetData(samples == TextureSamples.Samples1 ? image : resolve, layer); var pixels = MemoryMarshal.Cast<byte, float>(bytes);
            for (var i = 0; i < pixels.Length; i++)
            {
                var channel = i % 4; var expected = channel == 1 ? Destination[1] : channel == 3 ? Source[3] * Second[3] + (Source[3] * Second[3] + Destination[3] * (1 - Constant[3])) * (1 - Constant[3]) : swizzled[channel] * Second[channel] + (swizzled[channel] * Second[channel] + Destination[channel] * Constant[3]) * Constant[3];
                Check(Math.Abs(pixels[i] - expected) < .00002, $"Sparse secondary output, views, pass, component map, depth/stencil, mask and copied blend state: {language} {samples} layer {layer}, {channel}, {pixels[i]} != {expected}");
            }
        }
        device.FreeRID(shader); Check(!device.RenderPipelineIsValid(first), "Shader retirement invalidates a pipeline with copied native factors.");
        foreach (var rid in new[] { second, first, set, sampler, source, framebuffer, image, depth }) device.FreeRID(rid); if (resolve.IsValid()) device.FreeRID(resolve);
    }
    private static void Refusal(RenderingDevice device, string mode)
    {
        var ordinary = Shader(device, ShaderLanguage.GLSL, false);
        using var format = new RDTextureFormat { Width = 2, Height = 2, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var framebuffer = device.FramebufferCreate([image]);
        using var attachment = new RDPipelineColorBlendStateAttachment { EnableBlend = true, SrcColorBlendFactor = BlendFactor.One, SrcAlphaBlendFactor = BlendFactor.One }; using var blend = new RDPipelineColorBlendState { Attachments = [attachment], BlendConstant = Color(Constant) }; using var raster = new RDPipelineRasterizationState();
        var layout = device.FramebufferGetFormat(framebuffer); var pipeline = device.RenderPipelineCreate(ordinary, layout, -1, RenderPrimitive.Triangles, raster, blend);
        var list = device.DrawListBegin(framebuffer); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); var pixels = MemoryMarshal.Cast<byte, float>(device.TextureGetData(image, 0)); Check(pixels[0] == Source[0], "Ordinary shader/pipeline execution survives optional blend feature refusal.");
        attachment.SrcColorBlendFactor = BlendFactor.ConstantAlpha;
        if (mode == "bridge") Reject<NotSupportedException>(() => device.RenderPipelineCreate(ordinary, layout, -1, RenderPrimitive.Triangles, raster, blend));
        else { var constant = device.RenderPipelineCreate(ordinary, layout, -1, RenderPrimitive.Triangles, raster, blend); device.FreeRID(constant); }
        Reject<NotSupportedException>(() => Shader(device, ShaderLanguage.GLSL, true));
        attachment.SrcColorBlendFactor = BlendFactor.One; var retry = device.RenderPipelineCreate(ordinary, layout, -1, RenderPrimitive.Triangles, raster, blend); Check(device.RenderPipelineIsValid(retry), "Ordinary pipeline creation remains retryable.");
        Console.WriteLine($"Blend refusal {mode} PASS {_checks}; ordinary native rendering preserved");
    }
}
