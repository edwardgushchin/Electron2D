using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;
internal static class RenderingDevicePassTests
{
    private static RID Image(RenderingDevice device, bool input, bool sampling)
    {
        using var format = new RDTextureFormat
        {
            Width = 16,
            Height = 16,
            Format = DataFormat.R8G8B8A8UNORM,
            UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | (input ? TextureUsageBits.InputAttachment : 0) | (sampling ? TextureUsageBits.Sampling : 0)
        };
        using var view = new RDTextureView(); return device.TextureCreate(format, view);
    }
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        foreach (var language in new[] { "GLSL", "HLSL" }) foreach (var sampled in new[] { false, true })
            {
                var source = Image(device, true, sampled); var destination = Image(device, false, true);
                using var first = new RDFramebufferPass { ColorAttachments = [0] }; using var second = new RDFramebufferPass { ColorAttachments = [1], InputAttachments = [0] };
                var framebuffer = device.FramebufferCreateMultipass([source, destination], [first, second]); var format = device.FramebufferGetFormat(framebuffer);
                using var module = RenderingDeviceGraphicsTests.Load("DevicePassInput" + language); RenderingDeviceGraphicsTests.Export(module, "DevicePassInput" + language); var shader = device.ShaderCreateFromSPIRV(module);
                using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
                var pipeline = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 1);
                using var uniform = new RDUniform { Binding = 7, UniformType = UniformType.InputAttachment }; uniform.AddID(source); var set = device.UniformSetCreate([uniform], shader, 6);
                var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Red, Colors.Black]);
                device.DrawListEnableScissor(list, new(0, 0, 1, 1));
                Check(device.DrawListSwitchToNextPass() == list, "Pass transitions retain the active list identity.");
                device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 6); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
                var result = device.TextureGetData(destination, 0);
                for (var i = 0; i < result.Length; i += 4) Check(result[i] == 0 && result[i + 1] == 0 && result[i + 2] == 255 && result[i + 3] == 255, "Native " + language + " input attachment pixel " + i + " sampled=" + sampled + " value=" + string.Join(',', result.AsSpan(i, 4).ToArray()));
                Console.WriteLine(language + " input attachment native pixels passed, sampling=" + sampled);
            }
        foreach (var language in new[] { "GLSL", "HLSL" }) Multisample(device, language);
        IntegerInputs(device);
        Preserve(device);
        Validation(device);
    }

    private static void Multisample(RenderingDevice device, string language)
    {
        using var description = new RDTextureFormat { Width = 16, Height = 16, Format = DataFormat.R8G8B8A8UNORM, Samples = TextureSamples.Samples4, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.InputAttachment };
        using var view = new RDTextureView(); var source = device.TextureCreate(description, view);
        description.Samples = TextureSamples.Samples1; description.IsResolveBuffer = true; description.UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom; var resolved = device.TextureCreate(description, view); var output = Image(device, false, true);
        using var first = new RDFramebufferPass { ColorAttachments = [0], ResolveAttachments = [1] }; using var second = new RDFramebufferPass { ColorAttachments = [2], InputAttachments = [0], PreserveAttachments = [1] };
        var framebuffer = device.FramebufferCreateMultipass([source, resolved, output], [first, second]); var format = device.FramebufferGetFormat(framebuffer);
        Check(device.FramebufferFormatGetTextureSamples(format) == TextureSamples.Samples4 && device.FramebufferFormatGetTextureSamples(format, 1) == TextureSamples.Samples1, "Each pass reports its own exact raster samples.");
        using var producerModule = RenderingDeviceGraphicsTests.Load("DevicePassProducer"); using var readerModule = RenderingDeviceGraphicsTests.Load("DevicePassMS" + language);
        RenderingDeviceGraphicsTests.Export(readerModule, "DevicePassMS" + language); var producerShader = device.ShaderCreateFromSPIRV(producerModule); var readerShader = device.ShaderCreateFromSPIRV(readerModule);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var msaa = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4 };
        var producer = device.RenderPipelineCreate(producerShader, format, -1, RenderPrimitive.Triangles, raster, blend, multisampleState: msaa); var reader = device.RenderPipelineCreate(readerShader, format, -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 1);
        using var uniform = new RDUniform { Binding = 7, UniformType = UniformType.InputAttachment }; uniform.AddID(source); var set = device.UniformSetCreate([uniform], readerShader, 6);
        var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Black, Colors.Black, Colors.Black]); device.DrawListBindRenderPipeline(list, producer); device.DrawListDraw(list, false, 1, 3);
        device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, reader); device.DrawListBindUniformSet(list, set, 6); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        var oracle = device.TextureGetData(resolved, 0); var result = device.TextureGetData(output, 0); var edges = 0;
        for (var i = 0; i < result.Length; i += 4)
        {
            Check(Math.Abs(result[i] - oracle[i + 2]) <= 1 && Math.Abs(result[i + 1] - oracle[i + 1]) <= 1 && Math.Abs(result[i + 2] - oracle[i]) <= 1 && result[i + 3] == oracle[i + 3], "MS input samples agree with an independent native resolve at " + i);
            if (oracle[i] > 0 && oracle[i] < 255) edges++;
        }
        Check(edges > 0, "The multisample oracle includes partially covered pixels."); Console.WriteLine(language + " 2DMS input loads match native resolves, edge pixels=" + edges);
    }
    private static void IntegerInputs(RenderingDevice device)
    {
        using var description = new RDTextureFormat { Width = 16, Height = 16, Format = DataFormat.R32SINT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.InputAttachment };
        using var view = new RDTextureView(); var signed = device.TextureCreate(description, view); description.Format = DataFormat.R32UINT; var unsigned = device.TextureCreate(description, view); var output = Image(device, false, true);
        using var first = new RDFramebufferPass { ColorAttachments = [0, 1] }; using var last = new RDFramebufferPass { ColorAttachments = [2], InputAttachments = [RDFramebufferPass.AttachmentUnused, 0, 1] };
        var framebuffer = device.FramebufferCreateMultipass([signed, unsigned, output], [first, last]); var format = device.FramebufferGetFormat(framebuffer);
        using var module = RenderingDeviceGraphicsTests.Load("DevicePassInteger"); RenderingDeviceGraphicsTests.Export(module, "DevicePassInteger"); var shader = device.ShaderCreateFromSPIRV(module);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 1);
        using var a = new RDUniform { Binding = 7, UniformType = UniformType.InputAttachment }; a.AddID(signed); using var b = new RDUniform { Binding = 2, UniformType = UniformType.InputAttachment }; b.AddID(unsigned);
        var signedSet = device.UniformSetCreate([a], shader, 6); var unsignedSet = device.UniformSetCreate([b], shader, 3);
        var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [new(BitConverter.Int32BitsToSingle(-37), 0, 0, 0), new(BitConverter.UInt32BitsToSingle(4000000000), 0, 0, 0), Colors.Black]);
        device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, signedSet, 6); device.DrawListBindUniformSet(list, unsignedSet, 3); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        var pixels = device.TextureGetData(output, 0); for (var i = 0; i < pixels.Length; i += 4) Check(pixels[i] == 255 && pixels[i + 1] == 0 && pixels[i + 2] == 255 && pixels[i + 3] == 255, "Sparse signed/unsigned input slots retain their exact scalar bits and independent logical sets.");
        last.InputAttachments = [-1, 1, 0]; var wrong = device.FramebufferCreateMultipass([signed, unsigned, output], [first, last]); Reject<ArgumentException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(wrong), -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 1));
        last.InputAttachments = [-1, 0]; var missing = device.FramebufferCreateMultipass([signed, unsigned, output], [first, last]); Reject<ArgumentException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(missing), -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 1));
        Console.WriteLine("Signed/unsigned exact-bit inputs, sparse input indices, independent descriptor sets and scalar/index mismatches passed.");
    }
    private static void Preserve(RenderingDevice device)
    {
        var a = Image(device, true, true); var b = Image(device, false, true); var c = Image(device, false, true);
        using var first = new RDFramebufferPass { ColorAttachments = [0] }; using var middle = new RDFramebufferPass { ColorAttachments = [1], PreserveAttachments = [0, 0] }; using var last = new RDFramebufferPass { ColorAttachments = [2], InputAttachments = [0] };
        var framebuffer = device.FramebufferCreateMultipass([a, b, c], [first, middle, last]); var format = device.FramebufferGetFormat(framebuffer); using var module = RenderingDeviceGraphicsTests.Load("DevicePassInputGLSL"); var shader = device.ShaderCreateFromSPIRV(module);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 2); using var uniform = new RDUniform { Binding = 7, UniformType = UniformType.InputAttachment }; uniform.AddID(a); var set = device.UniformSetCreate([uniform], shader, 6);
        var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Red, Colors.Green, Colors.Black]); device.DrawListSwitchToNextPass(); device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 6); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        var pixels = device.TextureGetData(c, 0); for (var i = 0; i < pixels.Length; i += 4) Check(pixels[i] == 0 && pixels[i + 1] == 0 && pixels[i + 2] == 255 && pixels[i + 3] == 255, "A preserved attachment survives an inactive intermediate pass.");
        Console.WriteLine("Preserve attachment passes through an inactive middle pass.");
    }

    private static void Validation(RenderingDevice device)
    {
        var source = Image(device, true, true); var destination = Image(device, false, true); var alternate = Image(device, true, true);
        using var first = new RDFramebufferPass { ColorAttachments = [0] }; using var last = new RDFramebufferPass { ColorAttachments = [1], InputAttachments = [0] };
        var input = last.InputAttachments; input[0] = 1; Check(last.InputAttachments[0] == 0, "Input descriptions copy reads."); var preserve = new[] { 0 }; last.PreserveAttachments = preserve; preserve[0] = 1; Check(last.PreserveAttachments[0] == 0, "Preservation descriptions copy writes."); last.PreserveAttachments = [];
        using var sourceFormat = new RDAttachmentFormat { Format = DataFormat.R8G8B8A8UNORM, UsageFlags = TextureUsageBits.ColorAttachment | TextureUsageBits.InputAttachment | TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom };
        using var targetFormat = new RDAttachmentFormat { Format = DataFormat.R8G8B8A8UNORM, UsageFlags = TextureUsageBits.ColorAttachment | TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom };
        var format = device.FramebufferFormatCreateMultipass([sourceFormat, targetFormat], [first, last]); var framebuffer = device.FramebufferCreateMultipass([source, destination], [first, last], format);
        first.ColorAttachments = [1]; last.InputAttachments = [1]; Check(device.FramebufferIsValid(framebuffer), "Created framebuffer snapshots survive description mutation.");
        Reject<ArgumentException>(() => device.FramebufferCreateMultipass([source, destination], [first, last], format)); first.ColorAttachments = [0]; last.InputAttachments = [0];
        Reject<ArgumentException>(() => device.FramebufferCreateMultipass([source, source], [first, last]));
        last.PreserveAttachments = [-1]; Reject<ArgumentOutOfRangeException>(() => device.FramebufferCreateMultipass([source, destination], [first, last])); last.PreserveAttachments = [];
        last.InputAttachments = [0, 0]; Reject<ArgumentException>(() => device.FramebufferCreateMultipass([source, destination], [first, last])); last.InputAttachments = [1]; Reject<ArgumentException>(() => device.FramebufferCreateMultipass([source, destination], [first, last])); last.InputAttachments = [0];
        Reject<ArgumentException>(() => device.FramebufferCreateMultipass([destination, alternate], [first, last]));
        Reject<ArgumentOutOfRangeException>(() => device.FramebufferFormatGetTextureSamples(format, 2));
        using var malformed = RenderingDeviceGraphicsTests.Load("DevicePassInputGLSL");
        var code = malformed.BytecodeFragment; var words = MemoryMarshal.Cast<byte, uint>(code.AsSpan()); var removed = false;
        for (var offset = 5; offset < words.Length; offset += (int)(words[offset] >> 16))
            if ((words[offset] & 65535) == 71 && (words[offset] >> 16) == 4 && words[offset + 2] == 43) { words[offset + 2] = 33; removed = true; break; }
        Check(removed, "The invalid-module fixture removes its input attachment index."); malformed.BytecodeFragment = code; Reject<ArgumentException>(() => device.ShaderCreateFromSPIRV(malformed));
        using var msModule = RenderingDeviceGraphicsTests.Load("DevicePassMSGLSL"); var msShader = device.ShaderCreateFromSPIRV(msModule);
        using var module = RenderingDeviceGraphicsTests.Load("DevicePassInputGLSL"); var shader = device.ShaderCreateFromSPIRV(module); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        Reject<ArgumentException>(() => device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend));
        Reject<ArgumentException>(() => device.RenderPipelineCreate(msShader, format, -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 1));
        var pipeline = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 1);
        using var uniform = new RDUniform { Binding = 7, UniformType = UniformType.InputAttachment }; uniform.AddID(alternate); var wrong = device.UniformSetCreate([uniform], shader, 6); uniform.ClearIDs(); uniform.AddID(source); var correct = device.UniformSetCreate([uniform], shader, 6);
        Reject<InvalidOperationException>(() => device.DrawListSwitchToNextPass());
        Reject<ArgumentException>(() => device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Red]));
        var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Red, Colors.Black]); Reject<ArgumentException>(() => device.DrawListBindRenderPipeline(list, pipeline)); device.DrawListSwitchToNextPass();
        Reject<InvalidOperationException>(() => device.DrawListSwitchToNextPass()); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, wrong, 6); Reject<InvalidOperationException>(() => device.DrawListDraw(list, false, 1, 3));
        device.DrawListBindUniformSet(list, correct, 6); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        list = device.DrawListBegin(framebuffer); device.DrawListEnd(); Reject<ArgumentException>(() => device.DrawListBindRenderPipeline(list, pipeline));
        Task.Run(() => Reject<InvalidOperationException>(() => device.DrawListSwitchToNextPass())).GetAwaiter().GetResult();
        var colors = new[] { Colors.Red, Colors.Black }; var bytes = new byte[16 * 16 * 4];
        void Frame()
        {
            var id = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, colors); device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(id, pipeline); device.DrawListBindUniformSet(id, correct, 6); device.DrawListDraw(id, false, 1, 3); device.DrawListEnd(); device.TextureGetData(destination, 0, bytes);
        }
        for (var i = 0; i < 32; i++) Frame(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Frame(); var allocated = GC.GetAllocatedBytesForCurrentThread() - before;
        Check(allocated == 0, "Prepared pass/input-copy/draw/span-readback cycles allocate " + allocated + " managed owner bytes.");
        using var inputOnlyFormat = new RDAttachmentFormat { Format = DataFormat.R8G8B8A8UNORM, UsageFlags = TextureUsageBits.InputAttachment | TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom };
        var validatedFormat = device.FramebufferFormatCreateMultipass([inputOnlyFormat, targetFormat], [last, last]); var validated = device.FramebufferCreateMultipass([source, destination], [last, last], validatedFormat);
        device.DrawListBegin(validated, DrawFlags.ClearColorAll, [Colors.Green]); device.DrawListEnd(); var validatedPixels = device.TextureGetData(destination, 0);
        Check(validatedPixels[0] == 0 && validatedPixels[1] == 255 && validatedPixels[2] == 0, "Additional image usage does not alter copied framebuffer clear-slot order.");
        var images = new RID[18]; var passes = new RDFramebufferPass[images.Length];
        try
        {
            for (var i = 0; i < images.Length; i++) { images[i] = Image(device, false, true); passes[i] = new() { ColorAttachments = [i] }; }
            var many = device.FramebufferCreateMultipass(images, passes); device.DrawListBegin(many); device.DrawListEnd();
            Check(device.FramebufferFormatGetTextureSamples(device.FramebufferGetFormat(many), 17) == TextureSamples.Samples1, "Global framebuffer images are not limited by a single pass's target count.");
            var repeated = Enumerable.Repeat(passes[0], 257).ToArray(); var longSequence = device.FramebufferCreateMultipass([images[0]], repeated);
            Check(device.FramebufferFormatGetTextureSamples(device.FramebufferGetFormat(longSequence), 256) == TextureSamples.Samples1, "Copied pass sequences do not impose an unrelated byte-sized index limit.");
        }
        finally { foreach (var pass in passes) pass?.Dispose(); }
        device.FreeRID(source); Check(!device.FramebufferIsValid(framebuffer) && !device.UniformSetIsValid(correct), "Retiring an input image invalidates all borrowed framebuffer and set identities.");
        Console.WriteLine("Pass snapshots, invalid layouts/states/bindings, owner/lifetime and 32 prepared input-copy/draw/readback frames=0 managed owner bytes passed.");
    }
    private sealed class Patch(Texture texture) : Entity { protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 16, 16), false); }
    internal static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var texture = new TextureRD(); var root = new Window { Size = new(32, 32) }; var frames = 0;
        root.Ready += _ =>
        {
            var device = RenderingServer.GetRenderingDevice()!; var source = Image(device, true, true); var output = Image(device, false, true);
            using var first = new RDFramebufferPass { ColorAttachments = [0] }; using var last = new RDFramebufferPass { ColorAttachments = [1], InputAttachments = [0] }; var framebuffer = device.FramebufferCreateMultipass([source, output], [first, last]);
            using var module = RenderingDeviceGraphicsTests.Load("DevicePassInputGLSL"); var shader = device.ShaderCreateFromSPIRV(module); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
            var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: 1); using var uniform = new RDUniform { Binding = 7, UniformType = UniformType.InputAttachment }; uniform.AddID(source); var set = device.UniformSetCreate([uniform], shader, 6);
            var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Red, Colors.Black]); device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 6); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); texture.TextureRDRID = output; root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured;
        };
        void Captured()
        {
            if (++frames < 3) return;
            using var screenshot = root.GetTexture().GetImage()!; var pixel = screenshot.GetPixel(8, 8); Check(pixel.R < .01f && pixel.G < .01f && pixel.B > .99f, "Ordered input-pass output reaches retained canvas pixels without a CPU transfer."); RenderingServer.FramePostDraw -= Captured; root.Tree!.Quit();
        }
        Engine.Run(root);
        Console.WriteLine("Multipass input output reached retained canvas pixels without a CPU transfer.");
    }

}
