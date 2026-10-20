using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static partial class RenderingDeviceMultiviewTests
{
    private static RID Image(RenderingDevice device, uint layers = 4, bool input = false, bool sampled = false, TextureSamples samples = TextureSamples.Samples1, bool resolve = false, bool depth = false)
    {
        using var format = new RDTextureFormat
        {
            Width = 8,
            Height = 8,
            ArrayLayers = layers,
            TextureType = layers == 1 ? TextureType.Texture : TextureType.TextureArray,
            Format = depth ? DataFormat.D32SFLOAT : DataFormat.R8G8B8A8UNORM,
            Samples = samples,
            IsResolveBuffer = resolve,
            UsageBits = (depth ? TextureUsageBits.DepthStencilAttachment : TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo) | (input ? TextureUsageBits.InputAttachment : 0) | (sampled ? TextureUsageBits.Sampling : 0)
        };
        using var view = new RDTextureView(); return device.TextureCreate(format, view);
    }
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        foreach (var language in new[] { "GLSL", "HLSL", "Block", "BlockWhole", "Images" }) Draw(device, language);
        Inputs(device); Multisample(device);
        Console.WriteLine("Multiview: per-view vertex/fragment indices, ordered pixels, push/scissor state, untouched extra layers, depth, input passes and lifetime checks passed on " + device.GetDeviceName());
    }
    private static void Draw(RenderingDevice device, string language)
    {
        var image = Image(device); var depth = Image(device, depth: true);
        device.TextureClear(image, Colors.Magenta, 0, 1, 0, 4);
        using var description = new RDAttachmentFormat();
        Check(description.Format == DataFormat.R8G8B8A8UNORM && description.Samples == TextureSamples.Samples1 && description.UsageFlags == 0, "Attachment descriptions retain all three typed defaults.");
        description.UsageFlags = TextureUsageBits.ColorAttachment;
        using var depthDescription = new RDAttachmentFormat { Format = DataFormat.D32SFLOAT, UsageFlags = TextureUsageBits.DepthStencilAttachment };
        var format = device.FramebufferFormatCreate([description, depthDescription], viewCount: 3);
        description.Format = DataFormat.R32UINT; description.Samples = TextureSamples.Samples4;
        var framebuffer = device.FramebufferCreate([image, depth], format, viewCount: 3);
        Check(device.FramebufferFormatGetTextureSamples(format) == TextureSamples.Samples1, "Formats copy their descriptions before later edits.");
        Reject<ArgumentOutOfRangeException>(() => device.FramebufferFormatCreate([description], 0));
        Reject<ArgumentOutOfRangeException>(() => device.FramebufferCreate([image], viewCount: 257));
        Reject<ArgumentException>(() => device.FramebufferCreate([image], viewCount: 5));
        Reject<ArgumentException>(() => device.FramebufferCreate([image, depth], format, 1));
        using var module = RenderingDeviceGraphicsTests.Load("DeviceMultiview" + (language.StartsWith("Block", StringComparison.Ordinal) ? "GLSL" : language == "Images" ? "ImagesGLSL" : language));
        if (language.StartsWith("Block", StringComparison.Ordinal)) { using var input = typeof(RenderingDeviceMultiviewTests).Assembly.GetManifestResourceStream("TestShaders.DeviceMultiview" + language + ".vert.spv")!; using var bytes = new MemoryStream(); input.CopyTo(bytes); module.BytecodeVertex = bytes.ToArray(); }
        var lower = typeof(RenderingDevice).GetMethod("LowerViewIndex", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        for (uint view = 0; view < 3; view++)
        {
            using var native = new RDShaderSPIRV
            {
                BytecodeVertex = (byte[])lower.Invoke(null, [module.BytecodeVertex, view, ShaderStage.Vertex])!,
                BytecodeFragment = (byte[])lower.Invoke(null, [module.BytecodeFragment, view, ShaderStage.Fragment])!
            };
            RenderingDeviceGraphicsTests.Export(native, "Multiview" + language + "." + view);
        }
        var original = module.BytecodeVertex; var shader = device.ShaderCreateFromSPIRV(module);
        RID sourceSet = default;
        if (language == "Images")
        {
            using var imageFormat = new RDTextureFormat { Width = 1, Height = 1, Format = DataFormat.R8G8B8A8UNORM, UsageBits = TextureUsageBits.Sampling | TextureUsageBits.Storage | TextureUsageBits.CanCopyTo };
            using var imageView = new RDTextureView(); var source = device.TextureCreate(imageFormat, imageView); device.TextureClear(source, Colors.White, 0, 1, 0, 1);
            using var imageUniform = new RDUniform { Binding = 1, UniformType = UniformType.Image }; imageUniform.AddID(source); sourceSet = device.UniformSetCreate([imageUniform], shader, 7);
        }
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment { EnableBlend = true, SrcColorBlendFactor = BlendFactor.ConstantColor, DstColorBlendFactor = BlendFactor.Zero, SrcAlphaBlendFactor = BlendFactor.One }; using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        using var depthState = new RDPipelineDepthStencilState { EnableDepthTest = true, EnableDepthWrite = true, DepthCompareOperator = CompareOperator.Less };
        var pipeline = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, dynamicStateFlags: PipelineDynamicStateFlags.BlendConstants, depthStencilState: depthState);
        Check(module.BytecodeVertex.SequenceEqual(original), "View lowering retains authored source bytes.");
        uint[] commands = [3, 1, 0, 0, 99, 99, 3, 1, 0, 0]; var indirect = device.StorageBufferCreate(40, MemoryMarshal.AsBytes(commands.AsSpan()), StorageBufferUsage.DispatchIndirect);
        var data = new byte[8 * 8 * 4]; float[] tint = [1, 1, 1, 1];
        void Render()
        {
            var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Black]);
            device.DrawListBindRenderPipeline(list, pipeline); if (sourceSet.IsValid()) device.DrawListBindUniformSet(list, sourceSet, 7); device.DrawListSetBlendConstants(list, new(.75f, .5f, .25f, 1));
            tint.AsSpan().Fill(1); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(tint.AsSpan()), 16);
            device.DrawListEnableScissor(list, new(0, 0, 4, 8)); device.DrawListDraw(list, false, 1, 3);
            tint.AsSpan().Fill(.5f); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(tint.AsSpan()), 16);
            device.DrawListEnableScissor(list, new(4, 0, 4, 8)); device.DrawListDrawIndirect(list, false, indirect, drawCount: 2, stride: 24);
            device.DrawListEnd();
            for (uint layer = 0; layer < 4; layer++)
            {
                device.TextureGetData(image, layer, data.AsSpan());
                for (var y = 0; y < 8; y++) for (var x = 0; x < 8; x++)
                    {
                        var at = (y * 8 + x) * 4;
                        for (var channel = 0; channel < 4; channel++)
                        {
                            var expected = layer == 3 ? channel is 0 or 2 or 3 ? 255 : 0 : channel == 3 ? x < 4 ? 255 : 128 : channel == layer ? (int)MathF.Round(255 * (channel == 0 ? .75f : channel == 1 ? .5f : .25f) * (x < 4 ? 1 : .5f)) : 0;
                            if (Math.Abs(data[at + channel] - expected) > 1) throw new InvalidOperationException($"{language} layer={layer} pixel=({x},{y}) channel={channel}: {data[at + channel]} != {expected}.");
                        }
                    }
            }
        }
        Render();
        for (var i = 0; i < 8; i++) Render();
        var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Render();
        Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared multiview active draws/readback allocate zero managed owner bytes.");
        var empty = device.DrawListBegin(framebuffer, DrawFlags.ClearColorAll, [new Color(0, 1, 0, 1)]); device.DrawListEnd();
        for (uint layer = 0; layer < 3; layer++) { device.TextureGetData(image, layer, data.AsSpan()); Check(data[1] == 255 && data[0] == 0, "Empty draw lists clear every view."); }
        device.FreeRID(image); Check(!device.FramebufferIsValid(framebuffer), "Attachment retirement invalidates every borrowed view.");
        Reject<ArgumentException>(() => device.DrawListBegin(framebuffer));
        device.FreeRID(shader); Check(!device.RenderPipelineIsValid(pipeline), "Shader retirement invalidates all view programs."); device.FreeRID(pipeline);
    }
    private static void Multisample(RenderingDevice device)
    {
        var source = Image(device, layers: 3, samples: TextureSamples.Samples4, input: true); var depth = Image(device, layers: 3, samples: TextureSamples.Samples4, depth: true);
        var resolved = Image(device, layers: 3, resolve: true); var output = Image(device, layers: 3); var standalone = Image(device, layers: 3, resolve: true);
        using var first = new RDFramebufferPass { ColorAttachments = [0], ResolveAttachments = [1], DepthAttachment = 2 };
        using var second = new RDFramebufferPass { ColorAttachments = [3], InputAttachments = [0], PreserveAttachments = [1] };
        using var colorDescription = new RDAttachmentFormat { Format = DataFormat.R8G8B8A8UNORM, Samples = TextureSamples.Samples4, UsageFlags = TextureUsageBits.ColorAttachment | TextureUsageBits.InputAttachment };
        using var resolveDescription = new RDAttachmentFormat { Format = DataFormat.R8G8B8A8UNORM, UsageFlags = TextureUsageBits.ColorAttachment };
        using var depthDescription = new RDAttachmentFormat { Format = DataFormat.D32SFLOAT, Samples = TextureSamples.Samples4, UsageFlags = TextureUsageBits.DepthStencilAttachment };
        var format = device.FramebufferFormatCreateMultipass([colorDescription, resolveDescription, depthDescription, resolveDescription], [first, second], viewCount: 3);
        colorDescription.Dispose(); resolveDescription.Dispose(); depthDescription.Dispose();
        var framebuffer = device.FramebufferCreateMultipass([source, resolved, depth, output], [first, second], format, viewCount: 3);
        using var producerModule = RenderingDeviceGraphicsTests.Load("DeviceMultiviewGLSL"); using var readerModule = RenderingDeviceGraphicsTests.Load("DevicePassMSHLSL");
        var producerShader = device.ShaderCreateFromSPIRV(producerModule); var readerShader = device.ShaderCreateFromSPIRV(readerModule);
        using var raster = new RDPipelineRasterizationState(); using var a = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [a] };
        using var msaa = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4 }; using var ds = new RDPipelineDepthStencilState { EnableDepthTest = true, EnableDepthWrite = true, DepthCompareOperator = CompareOperator.Less };
        var producer = device.RenderPipelineCreate(producerShader, format, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: msaa, depthStencilState: ds);
        var reader = device.RenderPipelineCreate(readerShader, format, -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 1);
        using var uniform = new RDUniform { Binding = 7, UniformType = UniformType.InputAttachment }; uniform.AddID(source); var set = device.UniformSetCreate([uniform], readerShader, 6);
        var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Black, Colors.Black, Colors.Black]);
        device.DrawListBindRenderPipeline(list, producer); float[] tint = [1, 1, 1, 1]; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(tint.AsSpan()), 16); device.DrawListDraw(list, false, 1, 3);
        device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, reader); device.DrawListBindUniformSet(list, set, 6); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        device.TextureResolveMultisample(source, standalone);
        for (uint layer = 0; layer < 3; layer++)
        {
            var expected = device.TextureGetData(resolved, layer); var pixels = device.TextureGetData(output, layer); var independent = device.TextureGetData(standalone, layer);
            Check(expected.SequenceEqual(independent), "Automatic and standalone layered MSAA resolves agree.");
            for (var at = 0; at < pixels.Length; at += 4) Check(pixels[at] == expected[at + 2] && pixels[at + 1] == expected[at + 1] && pixels[at + 2] == expected[at] && pixels[at + 3] == 255 && expected[at + layer] == 255, "Layered MSAA depth/input/resolve pixels preserve the view.");
        }
    }
    private static void Inputs(RenderingDevice device)
    {
        foreach (var language in new[] { "GLSL", "HLSL" }) foreach (var sampled in new[] { false, true })
            {
                var source = Image(device, input: true, sampled: sampled); var output = Image(device);
                using var first = new RDFramebufferPass { ColorAttachments = [0] }; using var second = new RDFramebufferPass { ColorAttachments = [1], InputAttachments = [0] };
                var framebuffer = device.FramebufferCreateMultipass([source, output], [first, second], viewCount: 3); var format = device.FramebufferGetFormat(framebuffer);
                using var module = RenderingDeviceGraphicsTests.Load("DevicePassInput" + language); var shader = device.ShaderCreateFromSPIRV(module);
                using var raster = new RDPipelineRasterizationState(); using var a = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [a] };
                var pipeline = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 1);
                using var uniform = new RDUniform { Binding = 7, UniformType = UniformType.InputAttachment }; uniform.AddID(source); var set = device.UniformSetCreate([uniform], shader, 6);
                using var producerModule = RenderingDeviceGraphicsTests.Load("DeviceMultiview" + language); var producerShader = device.ShaderCreateFromSPIRV(producerModule); var producer = device.RenderPipelineCreate(producerShader, format, -1, RenderPrimitive.Triangles, raster, blend);
                var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Red, Colors.Black]);
                device.DrawListBindRenderPipeline(list, producer); float[] tint = [1, 1, 1, 1]; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(tint.AsSpan()), 16); device.DrawListDraw(list, false, 1, 3);
                device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 6); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
                for (uint layer = 0; layer < 3; layer++) { var pixels = device.TextureGetData(output, layer); Check(pixels[(int)(2 - layer)] == 255 && pixels[(int)((3 - layer) % 3)] == 0, "A layered input pass reads its corresponding source layer."); }
            }
    }
}
