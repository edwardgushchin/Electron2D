using System.Runtime.InteropServices;
using Electron2D;
using static TextureArrayTests;
using static Electron2D.RenderingDevice;

internal static class RenderingDeviceMultisampleTests
{
    private static RID Image(RenderingDevice device, TextureSamples samples, bool resolve = false, bool depth = false, uint size = 32)
    {
        using var description = new RDTextureFormat { Width = size, Height = size, Format = depth ? DataFormat.D16UNORM : DataFormat.R8G8B8A8UNORM, Samples = samples, IsResolveBuffer = resolve, UsageBits = depth ? TextureUsageBits.DepthStencilAttachment : TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo | (samples == TextureSamples.Samples1 ? TextureUsageBits.Sampling : 0) };
        using var view = new RDTextureView(); return device.TextureCreate(description, view);
    }
    internal static void Run()
    {
        using (var device = RenderingServer.CreateLocalRenderingDevice())
        {
            foreach (var language in new[] { "GLSL", "HLSL" }) Exercise(device, language);
            MultipleTargets(device); Stencil(device);
            Console.WriteLine("MSAA native pipelines, automatic/explicit resolve, exact counts, depth, coverage and zero allocation passed on " + device.GetDeviceName());
        }
        Canvas();
    }
    private static void Exercise(RenderingDevice device, string language)
    {
        using var module = RenderingDeviceGraphicsTests.Load("DeviceMSAA" + language); RenderingDeviceGraphicsTests.Export(module, "DeviceMSAA" + language); var shader = device.ShaderCreateFromSPIRV(module);
        using var passDescription = new RDFramebufferPass { ColorAttachments = [2], DepthAttachment = 1, ResolveAttachments = [0] };
        var destination = Image(device, TextureSamples.Samples1, true); var depth = Image(device, TextureSamples.Samples4, depth: true); var source = Image(device, TextureSamples.Samples4);
        var framebuffer = device.FramebufferCreateMultipass([destination, depth, source], [passDescription]); var layout = device.FramebufferGetFormat(framebuffer);
        Check(device.FramebufferFormatGetTextureSamples(layout) == TextureSamples.Samples4, "Resolve slots do not change the exact raster sample count.");
        using var actual = device.TextureGetFormat(source); Check(actual.Samples == TextureSamples.Samples4 && !actual.IsResolveBuffer, "Allocation retains requested samples without native fallback."); actual.Samples = TextureSamples.Samples1;
        using var resolvedInfo = device.TextureGetFormat(destination); Check(resolvedInfo.IsResolveBuffer && resolvedInfo.Samples == TextureSamples.Samples1, "Resolve role is copied and queryable.");
        using var a = new RDAttachmentFormat { Format = DataFormat.R8G8B8A8UNORM, Samples = TextureSamples.Samples1, UsageFlags = TextureUsageBits.ColorAttachment };
        using var z = new RDAttachmentFormat { Format = DataFormat.D16UNORM, Samples = TextureSamples.Samples4, UsageFlags = TextureUsageBits.DepthStencilAttachment };
        using var m = new RDAttachmentFormat { Format = DataFormat.R8G8B8A8UNORM, Samples = TextureSamples.Samples4, UsageFlags = TextureUsageBits.ColorAttachment };
        var explicitLayout = device.FramebufferFormatCreateMultipass([a, z, m], [passDescription]); var validated = device.FramebufferCreate([destination, depth, source], explicitLayout); Check(device.FramebufferGetFormat(validated) == explicitLayout, "Framebuffer validates copied explicit render/resolve roles.");
        passDescription.ColorAttachments = []; var indices = new[] { 2 }; passDescription.ColorAttachments = indices; indices[0] = 0; Check(passDescription.ColorAttachments[0] == 2, "Pass descriptions copy supplied arrays."); var read = passDescription.ColorAttachments; read[0] = 0; Check(passDescription.ColorAttachments[0] == 2, "Pass description reads return independent arrays.");
        Reject<ArgumentOutOfRangeException>(() => device.FramebufferCreateMultipass([destination], [passDescription, passDescription]));
        Reject<ArgumentException>(() => device.FramebufferCreate([source, destination]));
        using var duplicate = new RDFramebufferPass { ColorAttachments = [0], ResolveAttachments = [0] }; Reject<ArgumentException>(() => device.FramebufferCreateMultipass([source], [duplicate]));
        using var invalidIndex = new RDFramebufferPass { ColorAttachments = [3] }; Reject<ArgumentOutOfRangeException>(() => device.FramebufferCreateMultipass([source], [invalidIndex]));
        Reject<ArgumentException>(() => device.FramebufferCreateMultipass([Image(device, TextureSamples.Samples1), depth, source], [passDescription]));
        Reject<ArgumentException>(() => device.FramebufferCreateMultipass([destination, Image(device, TextureSamples.Samples1, depth: true), source], [passDescription]));
        Reject<ArgumentException>(() => device.FramebufferCreateMultipass([destination, depth, Image(device, TextureSamples.Samples4, size: 16)], [passDescription]));
        using var invalidFormat = new RDTextureFormat { Format = DataFormat.R8G8B8A8UNORM, Samples = TextureSamples.Samples16, UsageBits = TextureUsageBits.ColorAttachment }; using var view = new RDTextureView(); Reject<NotSupportedException>(() => device.TextureCreate(invalidFormat, view)); invalidFormat.Samples = TextureSamples.Max; Reject<ArgumentOutOfRangeException>(() => device.TextureCreate(invalidFormat, view));
        invalidFormat.Samples = TextureSamples.Samples4; invalidFormat.UsageBits |= TextureUsageBits.CPURead; Reject<NotSupportedException>(() => device.TextureCreate(invalidFormat, view));
        invalidFormat.UsageBits = TextureUsageBits.ColorAttachment; invalidFormat.MIPMaps = 2; invalidFormat.Width = invalidFormat.Height = 4; Reject<NotSupportedException>(() => device.TextureCreate(invalidFormat, view)); invalidFormat.MIPMaps = 1; invalidFormat.IsResolveBuffer = true; Reject<ArgumentException>(() => device.TextureCreate(invalidFormat, view));
        using var invalidDepth = new RDTextureFormat { Format = DataFormat.D16UNORM, Samples = TextureSamples.Samples4, UsageBits = TextureUsageBits.DepthStencilAttachment | TextureUsageBits.CanCopyFrom }; Reject<NotSupportedException>(() => device.TextureCreate(invalidDepth, view));
        using var raster = new RDPipelineRasterizationState(); using var blendAttachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [blendAttachment] };
        using var depthState = new RDPipelineDepthStencilState { EnableDepthTest = true, EnableDepthWrite = true, DepthCompareOperator = CompareOperator.Less };
        using var multi = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4 };
        var pipeline = device.RenderPipelineCreate(shader, layout, -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: depthState, multisampleState: multi);
        multi.SampleCount = TextureSamples.Samples1; Reject<ArgumentException>(() => device.RenderPipelineCreate(shader, layout, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: multi)); multi.SampleCount = TextureSamples.Samples4; multi.EnableAlphaToCoverage = true;
        var coverage = device.RenderPipelineCreate(shader, layout, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: multi); multi.EnableAlphaToCoverage = false;
        var plain = device.RenderPipelineCreate(shader, layout, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: multi);
        var uniform = device.UniformBufferCreate(32); using var descriptor = new RDUniform { Binding = 7, UniformType = UniformType.UniformBuffer }; descriptor.AddID(uniform); var set = device.UniformSetCreate([descriptor], shader, 5);
        var parameters = new float[8]; var output = new byte[32 * 32 * 4];
        void Parameters(Color tint, float d = .25f) { parameters[0] = tint.R; parameters[1] = tint.G; parameters[2] = tint.B; parameters[3] = tint.A; parameters[4] = d; device.BufferUpdate(uniform, 0, 32, MemoryMarshal.AsBytes(parameters.AsSpan())); }
        void Draw(RID selected, bool clear = true)
        {
            var list = device.DrawListBegin(framebuffer, clear ? DrawFlags.ClearAll : 0, clear ? [Colors.Black, Colors.Black] : default);
            device.DrawListBindRenderPipeline(list, selected); device.DrawListBindUniformSet(list, set, 5); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        }
        void Read() => device.TextureGetData(destination, 0, output.AsSpan());
        int Red(int x = 8, int y = 16) => output[(y * 32 + x) * 4];
        Parameters(Colors.Red); Draw(pipeline); Read(); Check(Red() == 255, "Native multisample drawing and automatic resolve write interior pixels.");
        Check(Enumerable.Range(0, 32 * 32).Any(i => output[i * 4] is > 1 and < 254), language + " MSAA produces fractional edge coverage, not a single-sample fallback.");
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_MSAA_OUTPUT"); if (directory is not null) { Directory.CreateDirectory(directory); using var image = Electron2D.Image.CreateFromData(32, 32, false, Electron2D.Image.Format.Rgba8, output); image.SavePNG(System.IO.Path.Combine(directory, language + "-edges.png")); }
        Parameters(Colors.Blue, .75f); Draw(pipeline, false); Read(); Check(Red() == 255, "Stored multisample depth rejects a farther draw across pass boundaries.");
        Parameters(new(1, 0, 0, .5f)); Draw(coverage); Read(); Check(Red() is >= 63 and <= 192, "Alpha-to-coverage affects resolved opaque interior samples.");
        Draw(plain); Read(); Check(Red() == 255, "Copied disabled alpha-to-coverage keeps opaque color coverage.");
        var sourceOnly = device.FramebufferCreate([source]); var sourcePipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(sourceOnly), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: multi);
        Parameters(Colors.Red); var list = device.DrawListBegin(sourceOnly, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, sourcePipeline); device.DrawListBindUniformSet(list, set, 5); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        device.TextureResolveMultisample(source, destination); Read(); Check(Red() == 255, "Standalone resolve reads stored source samples and writes a single-sample image.");
        device.TextureClear(source, Colors.Blue, 0, 1, 0, 1); device.TextureResolveMultisample(source, destination); Read(); Check(output[2] == 255 && output[0] == 0, "GPU multisample clear initializes every source sample before resolve.");
        Reject<NotSupportedException>(() => device.TextureGetData(source, 0)); Reject<NotSupportedException>(() => device.TextureCopy(source, destination, Vector3.Zero, Vector3.Zero, new(1, 1, 1), 0, 0, 0, 0));
        Reject<ArgumentException>(() => device.TextureResolveMultisample(destination, destination)); Reject<ArgumentException>(() => device.TextureResolveMultisample(source, Image(device, TextureSamples.Samples1, true, size: 16)));
        var single = Image(device, TextureSamples.Samples1); var singleTarget = device.FramebufferCreate([single]); var singlePipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(singleTarget), -1, RenderPrimitive.Triangles, raster, blend);
        Parameters(Colors.Red); list = device.DrawListBegin(singleTarget, DrawFlags.ClearColorAll, [Colors.Black]); Reject<ArgumentException>(() => device.DrawListBindRenderPipeline(list, pipeline)); device.DrawListBindRenderPipeline(list, singlePipeline); device.DrawListBindUniformSet(list, set, 5); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); var singleData = device.TextureGetData(single, 0); Check(!Enumerable.Range(0, 32 * 32).Any(i => singleData[i * 4] is > 1 and < 254), "Single-sample control has binary coverage on the same geometry.");
        if (language == "GLSL")
        {
            using var depthOnlyModule = RenderingDeviceGraphicsTests.Load("DeviceDepthOnly"); var depthOnlyShader = device.ShaderCreateFromSPIRV(depthOnlyModule); using var noColors = new RDPipelineColorBlendState();
            using var depthOnlyPass = new RDFramebufferPass { DepthAttachment = 0 }; var depthOnlyTarget = device.FramebufferCreateMultipass([depth], [depthOnlyPass]);
            var depthOnlyPipeline = device.RenderPipelineCreate(depthOnlyShader, device.FramebufferGetFormat(depthOnlyTarget), -1, RenderPrimitive.Triangles, raster, noColors, depthStencilState: depthState, multisampleState: multi); var depthOnlySet = device.UniformSetCreate([descriptor], depthOnlyShader, 5);
            Parameters(Colors.White, .25f); list = device.DrawListBegin(depthOnlyTarget, DrawFlags.ClearDepth); device.DrawListBindRenderPipeline(list, depthOnlyPipeline); device.DrawListBindUniformSet(list, depthOnlySet, 5); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            Parameters(Colors.Red, .75f); list = device.DrawListBegin(framebuffer, DrawFlags.ClearColorAll, [Colors.Black, Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 5); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); Read(); Check(Red() == 0, "Depth-only multisample pass stores depth consumed by a later color/resolve pass.");
        }
        void Warm() { Parameters(Colors.Red); var warm = device.DrawListBegin(framebuffer, DrawFlags.ClearDepth); device.DrawListBindRenderPipeline(warm, pipeline); device.DrawListBindUniformSet(warm, set, 5); device.DrawListDraw(warm, false, 1, 3); device.DrawListEnd(); device.TextureResolveMultisample(source, destination); Read(); }
        GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true); for (var i = 0; i < 16; i++) Warm(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Warm(); var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0, "Prepared multisample draw/automatic resolve/explicit resolve/readback allocate zero bytes: " + allocated); Console.WriteLine(language + " MSAA: 32 prepared cycles, " + allocated + " managed bytes.");
        foreach (var samples in new[] { TextureSamples.Samples2, TextureSamples.Samples8 })
        {
            RID image;
            try { image = Image(device, samples); }
            catch (NotSupportedException) { Console.WriteLine(language + ": exact " + samples + " unavailable and rejected."); continue; }
            using var sampleInfo = device.TextureGetFormat(image); Check(sampleInfo.Samples == samples, "Queried native allocation keeps the requested sample count.");
            var target = device.FramebufferCreate([image]); Check(device.FramebufferFormatGetTextureSamples(device.FramebufferGetFormat(target)) == samples, "Other supported sample counts propagate to layouts.");
            device.TextureClear(image, Colors.Red, 0, 1, 0, 1); device.TextureResolveMultisample(image, destination); Read(); Check(output[0] == 255 && output[2] == 0, "Other exact native sample counts resolve all cleared samples.");
            device.FreeRID(image);
        }
        using var unusedResolve = new RDFramebufferPass { ColorAttachments = [0], ResolveAttachments = [RDFramebufferPass.AttachmentUnused] };
        var withoutResolve = device.FramebufferCreateMultipass([source], [unusedResolve]); Check(device.FramebufferFormatGetTextureSamples(device.FramebufferGetFormat(withoutResolve)) == TextureSamples.Samples4, "AttachmentUnused preserves a stored render-only color slot.");
        using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState);
        using var sampledOutput = new RDUniform { Binding = 13, UniformType = UniformType.SamplerWithTexture }; sampledOutput.AddID(sampler); sampledOutput.AddID(destination);
        using var feedbackModule = RenderingDeviceGraphicsTests.Load("DeviceMSAAFeedback"); var feedbackShader = device.ShaderCreateFromSPIRV(feedbackModule); var feedbackSet = device.UniformSetCreate([sampledOutput], feedbackShader, 2); var feedbackUniform = device.UniformSetCreate([descriptor], feedbackShader, 5);
        var feedbackPipeline = device.RenderPipelineCreate(feedbackShader, layout, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: multi); list = device.DrawListBegin(framebuffer); device.DrawListBindRenderPipeline(list, feedbackPipeline); device.DrawListBindUniformSet(list, feedbackSet, 2); device.DrawListBindUniformSet(list, feedbackUniform, 5); Reject<InvalidOperationException>(() => device.DrawListDraw(list, false, 1, 3)); device.DrawListEnd();
        device.FreeRID(destination); Check(!device.FramebufferIsValid(framebuffer), "Freeing borrowed resolve invalidates the framebuffer."); Reject<ArgumentException>(() => device.DrawListBegin(framebuffer));
    }
    private static void MultipleTargets(RenderingDevice device)
    {
        var a = Image(device, TextureSamples.Samples4); var b = Image(device, TextureSamples.Samples4); var x = Image(device, TextureSamples.Samples1, true); var y = Image(device, TextureSamples.Samples1, true);
        using var pass = new RDFramebufferPass { ColorAttachments = [3, 1], ResolveAttachments = [2, 0] }; var target = device.FramebufferCreateMultipass([y, b, x, a], [pass]);
        using var module = RenderingDeviceGraphicsTests.Load("DeviceMSAAMRT"); var shader = device.ShaderCreateFromSPIRV(module); using var raster = new RDPipelineRasterizationState(); using var first = new RDPipelineColorBlendStateAttachment(); using var second = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [first, second] }; using var multi = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4 };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: multi); var uniform = device.UniformBufferCreate(32); using var u = new RDUniform { Binding = 7, UniformType = UniformType.UniformBuffer }; u.AddID(uniform); var set = device.UniformSetCreate([u], shader, 5);
        var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [Colors.Black, Colors.Black, Colors.Black, Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 5); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); var red = device.TextureGetData(x, 0); var blue = device.TextureGetData(y, 0); Check(red[0] == 255 && red[2] == 0 && blue[0] == 0 && blue[2] == 255, "Multiple resolve slots preserve logical color order across arbitrary attachment indices.");
    }
    private static void Stencil(RenderingDevice device)
    {
        using var module = RenderingDeviceGraphicsTests.Load("DeviceMSAAGLSL"); var shader = device.ShaderCreateFromSPIRV(module); using var view = new RDTextureView(); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var multi = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4 };
        var source = Image(device, TextureSamples.Samples4); var destination = Image(device, TextureSamples.Samples1, true); var buffer = device.UniformBufferCreate(32); using var uniform = new RDUniform { Binding = 7, UniformType = UniformType.UniformBuffer }; uniform.AddID(buffer); var set = device.UniformSetCreate([uniform], shader, 5); float[] values = [1, 0, 0, 1, .25f, 0, 0, 0]; device.BufferUpdate(buffer, 0, 32, MemoryMarshal.AsBytes(values.AsSpan()));
        foreach (var format in new[] { DataFormat.D24UNORMS8UINT, DataFormat.D32SFLOATS8UINT })
        {
            using var description = new RDTextureFormat { Format = format, Width = 32, Height = 32, Samples = TextureSamples.Samples4, UsageBits = TextureUsageBits.DepthStencilAttachment }; RID depth;
            try { depth = device.TextureCreate(description, view); } catch (NotSupportedException) { Console.WriteLine("MSAA stencil format unavailable: " + format); continue; }
            using var pass = new RDFramebufferPass { ColorAttachments = [0], DepthAttachment = 1, ResolveAttachments = [2] }; var target = device.FramebufferCreateMultipass([source, depth, destination], [pass]);
            using var state = new RDPipelineDepthStencilState { EnableStencil = true, FrontOpReference = 7, BackOpReference = 7, FrontOpCompareMask = 255, BackOpCompareMask = 255, FrontOpWriteMask = 255, BackOpWriteMask = 255, FrontOpPass = StencilOperation.Replace, BackOpPass = StencilOperation.Replace, FrontOpFail = StencilOperation.Keep, BackOpFail = StencilOperation.Keep, FrontOpDepthFail = StencilOperation.Keep, BackOpDepthFail = StencilOperation.Keep };
            var writer = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state, multisampleState: multi);
            state.FrontOpCompare = state.BackOpCompare = CompareOperator.Equal; state.FrontOpPass = state.BackOpPass = StencilOperation.Keep; var reader = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state, multisampleState: multi);
            state.FrontOpReference = state.BackOpReference = 8; var mismatch = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, depthStencilState: state, multisampleState: multi);
            void Draw(RID selected, DrawFlags flags) { var list = device.DrawListBegin(target, flags, [Colors.Black, Colors.Black]); device.DrawListBindRenderPipeline(list, selected); device.DrawListBindUniformSet(list, set, 5); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); }
            Draw(writer, DrawFlags.ClearAll); Draw(reader, DrawFlags.ClearColorAll); var pixels = device.TextureGetData(destination, 0); Check(pixels[(16 * 32 + 8) * 4] == 255, "Stored multisample stencil accepts the matching reference for " + format);
            Draw(mismatch, DrawFlags.ClearColorAll); pixels = device.TextureGetData(destination, 0); Check(pixels[(16 * 32 + 8) * 4] == 0, "Stored multisample stencil rejects a different reference for " + format); Console.WriteLine("MSAA stored stencil passed: " + format);
        }
    }
    private sealed class Patch(Texture texture) : Entity { protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 32, 32), false); }
    private static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var texture = new TextureRD(); var root = new Window { Size = new(40, 40) }; var frames = 0;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice()!; RenderingServer.SetDefaultClearColor(Colors.Black); var source = Image(device, TextureSamples.Samples4); var output = Image(device, TextureSamples.Samples1, true); using var pass = new RDFramebufferPass { ColorAttachments = [0], ResolveAttachments = [1] }; var target = device.FramebufferCreateMultipass([source, output], [pass]);
            using var module = RenderingDeviceGraphicsTests.Load("DeviceMSAAGLSL"); var shader = device.ShaderCreateFromSPIRV(module); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var multi = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4 };
            var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, multisampleState: multi); float[] values = [1, 0, 0, 1, .25f, 0, 0, 0]; var buffer = device.UniformBufferCreate(32, MemoryMarshal.AsBytes(values.AsSpan())); using var u = new RDUniform { Binding = 7, UniformType = UniformType.UniformBuffer }; u.AddID(buffer); var set = device.UniformSetCreate([u], shader, 5);
            var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [Colors.Black, Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 5); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); texture.TextureRDRID = output; root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured;
        };
        void Captured() { if (++frames < 3) return; using var image = root.GetTexture().GetImage()!; Check(image.GetPixel(8, 16).R > .99f, "Resolved multisample output reaches the rendered retained canvas without CPU copying."); root.Tree!.Quit(); }
        Engine.Run(root);
    }
}
