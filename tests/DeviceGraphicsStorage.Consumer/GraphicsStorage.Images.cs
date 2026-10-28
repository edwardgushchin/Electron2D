using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

internal static partial class GraphicsStorage
{
    private static void Images(string mode)
    {
        if (mode == "images-canvas") { ImageCanvas(); ImageCanvas(); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Graphics image device: " + device.GetDeviceName());
        if (mode is "images-export" or "images-unavailable" or "images-no-vertex" or "images-no-fragment" or "images-no-stores" or "images-failure") { ImageProfiles(device, mode); return; }
        foreach (var language in new[] { "glsl", "hlsl" })
        {
            if (mode is "images-atomics" or "images-atomics-source") ImageAtomics(device, language, mode.EndsWith("-source", StringComparison.Ordinal));
            else if (mode is "images-mixed" or "images-mixed-source" or "images-inputs") device.FreeRID(ImageMixed(device, language, mode.EndsWith("-source", StringComparison.Ordinal), mode == "images-inputs"));
            else if (mode is "images-arrays" or "images-arrays-source") ImageArrays(device, language, mode.EndsWith("-source", StringComparison.Ordinal));
            else ImageWrites(device, language, mode == "images-source", mode == "images-views");
        }
    }
    private static void ImageWrites(RenderingDevice device, string language, bool source, bool views = false)
    {
        var shader = Shader(device, "image-write", language, source);
        using var firstFormat = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R32UINT, UsageBits = TextureUsageBits.Storage | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo };
        using var secondFormat = new RDTextureFormat { Width = 4, Height = 4, MIPMaps = 2, Format = DataFormat.R32UINT, UsageBits = TextureUsageBits.Storage | TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo };
        using var colorFormat = new RDTextureFormat { Width = 4, Height = 4, TextureType = views ? TextureType.TextureArray : TextureType.Texture, ArrayLayers = views ? 2u : 1u, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling };
        using var view = new RDTextureView(); uint[] initial = new uint[20]; initial.AsSpan(16).Fill(0xCAFEBABE);
        var first = device.TextureCreate(firstFormat, view); var second = device.TextureCreate(secondFormat, view, [MemoryMarshal.AsBytes(initial.AsSpan()).ToArray()]); var color = device.TextureCreate(colorFormat, view); var target = device.FramebufferCreate([color], viewCount: views ? 2 : 1);
        using var firstImage = new RDUniform { Binding = 0, UniformType = UniformType.Image }; firstImage.AddID(first); using var secondImage = new RDUniform { Binding = 1, UniformType = UniformType.Image }; secondImage.AddID(second);
        var set = device.UniformSetCreate([firstImage, secondImage], shader, 0);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        uint[] push = [7], a = new uint[16], b = new uint[20]; float[] pixels = new float[64]; Color[] clear = [Colors.Black];
        void Draw()
        {
            device.TextureClear(first, default, 0, 1, 0, 1); device.TextureClear(second, default, 0, 1, 0, 1);
            var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, clear); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            device.TextureGetData(first, 0, MemoryMarshal.AsBytes(a.AsSpan())); device.TextureGetData(second, 0, MemoryMarshal.AsBytes(b.AsSpan())); device.TextureGetData(color, 0, MemoryMarshal.AsBytes(pixels.AsSpan()));
        }
        Draw(); for (var i = 0; i < 16; i++) { Check(a[i] == (i < 3 ? 7u + (uint)i : 0), "Vertex stores change the authoritative image."); Check(b[i] == 107u + (uint)i, "Fragment stores change the authoritative sampled image."); }
        for (var i = 16; i < 20; i++) Check(b[i] == 0xCAFEBABE, "Storage writes address only the base mip while preserving higher mip data.");
        Check(pixels[0] == 1 && pixels[1] == .5f && pixels[2] == .25f, "Image-writing graphics retains ordinary color output.");
        if (views) { device.TextureGetData(color, 1, MemoryMarshal.AsBytes(pixels.AsSpan())); Check(pixels[0] == 1 && pixels[1] == .5f && pixels[2] == .25f, "Every view restores and recaptures authoritative image layouts."); }
        for (var i = 0; i < 16; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared graphics image writes/clear/readback allocate zero owner bytes.");
        foreach (var rid in new[] { pipeline, set, target, first, second, color, shader }) device.FreeRID(rid);
        Console.WriteLine("PASS " + language + ": original vertex/fragment image bytes, base-mip view, color output and zero prepared owner bytes.");
    }
    private static void ImageAtomics(RenderingDevice device, string language, bool source)
    {
        var shader = Shader(device, "image-atomic", language, source);
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R32UINT, UsageBits = TextureUsageBits.Storage | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo };
        using var view = new RDTextureView(); var images = new RID[4];
        for (var i = 0; i < images.Length; i++) { format.Format = i % 2 == 0 ? DataFormat.R32UINT : DataFormat.R32SINT; images[i] = device.TextureCreate(format, view); }
        using var colorFormat = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom };
        var color = device.TextureCreate(colorFormat, view); var target = device.FramebufferCreate([color]); var buffer = device.StorageBufferCreate(160);
        var uniforms = new RDUniform[5];
        for (var i = 0; i < uniforms.Length; i++) { uniforms[i] = new() { Binding = i, UniformType = i == 4 ? UniformType.StorageBuffer : UniformType.Image }; uniforms[i].AddID(i == 4 ? buffer : images[i]); }
        var set = device.UniformSetCreate(uniforms, shader, 0); foreach (var uniform in uniforms) uniform.Dispose();
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var factor = new RDPipelineSpecializationConstant<uint> { ConstantID = 0, Value = 2 };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, specializationConstants: [factor]);
        uint[] values = new uint[40]; int[][] pixels = [new int[16], new int[16], new int[16], new int[16]]; Color[] clear = [Colors.Black];
        void Draw()
        {
            foreach (var image in images) device.TextureClear(image, default, 0, 1, 0, 1); device.BufferClear(buffer, 0, 160);
            var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, clear); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            device.BufferGetData(buffer, MemoryMarshal.AsBytes(values.AsSpan())); for (var i = 0; i < images.Length; i++) device.TextureGetData(images[i], 0, MemoryMarshal.AsBytes(pixels[i].AsSpan()));
        }
        Draw();
        for (var i = 0; i < 20; i++) { Check(values[i * 2] == (i == 3 ? 0u : 686u), "Unsigned graphics image atomic return values reach shared storage buffers."); Check(unchecked((int)values[i * 2 + 1]) == (i == 3 ? 0 : -554), "Signed graphics image atomic return values and specialization remain exact."); }
        for (var stage = 0; stage < 2; stage++) for (var i = 0; i < 16; i++) { Check(pixels[stage * 2][i] == (stage == 0 && i >= 3 ? 0 : 101), "Unsigned graphics image atomic final bytes are authoritative."); Check(pixels[stage * 2 + 1][i] == (stage == 0 && i >= 3 ? 0 : -99), "Signed graphics image atomic final bytes are authoritative."); }
        for (var i = 0; i < 16; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared image-atomic and shared-buffer cycles allocate zero owner bytes.");
        foreach (var rid in images) device.FreeRID(rid); foreach (var rid in new[] { pipeline, set, target, color, buffer, shader }) device.FreeRID(rid);
        Console.WriteLine("PASS " + language + ": signed/unsigned graphics image atomic operations, returned values, loads, dimensions, shared buffers, typed specialization and zero owner bytes.");
    }
    private static RID ImageMixed(RenderingDevice device, string language, bool source, bool inputs = false)
    {
        var shader = Shader(device, "image-mixed", language, source); var chain = Shader(device, "image-chain", language, source);
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.Storage | TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo | (inputs ? TextureUsageBits.ColorAttachment | TextureUsageBits.InputAttachment : 0) };
        using var view = new RDTextureView(); float[] initial = new float[64]; for (var i = 0; i < 16; i++) { initial[i * 4] = .1f; initial[i * 4 + 1] = .2f; initial[i * 4 + 2] = .3f; initial[i * 4 + 3] = 1; }
        var destination = device.TextureCreate(format, view); var input = device.TextureCreate(format, view, [MemoryMarshal.AsBytes(initial.AsSpan()).ToArray()]);
        format.Format = DataFormat.R32UINT; uint[] markers = new uint[16]; markers.AsSpan().Fill(3); var marker = device.TextureCreate(format, view, [MemoryMarshal.AsBytes(markers.AsSpan()).ToArray()]);
        format.Format = DataFormat.R32G32B32A32SFLOAT; format.UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.Storage | TextureUsageBits.CanCopyFrom; var color = device.TextureCreate(format, view);
        using var firstPass = new RDFramebufferPass { ColorAttachments = [0] }; using var lastPass = new RDFramebufferPass { ColorAttachments = [1], InputAttachments = [0] }; var target = inputs ? device.FramebufferCreateMultipass([input, color], [firstPass, lastPass]) : device.FramebufferCreate([color]);
        using var samplerState = new RDSamplerState(); var sampler = device.SamplerCreate(samplerState); var buffer = device.StorageBufferCreate(64); float[] increment = [.05f, .1f, .15f, -1]; var parameters = device.UniformBufferCreate(16, MemoryMarshal.AsBytes(increment.AsSpan()));
        var uniforms = new RDUniform[6];
        for (var i = 0; i < 6; i++) uniforms[i] = new() { Binding = i, UniformType = i == 2 ? UniformType.SamplerWithTexture : i == 3 ? UniformType.StorageBuffer : i == 4 ? UniformType.UniformBuffer : UniformType.Image };
        uniforms[0].AddID(destination); uniforms[1].AddID(input); uniforms[2].AddID(sampler); uniforms[2].AddID(input); uniforms[3].AddID(buffer); uniforms[4].AddID(parameters); uniforms[5].AddID(marker);
        var set = device.UniformSetCreate(uniforms, shader, 0);
        uniforms[0].ClearIDs(); uniforms[0].AddID(color); var feedback = device.UniformSetCreate(uniforms, shader, 0); foreach (var uniform in uniforms) uniform.Dispose();
        using var image = new RDUniform { Binding = 0, UniformType = UniformType.Image }; image.AddID(destination); var computeSet = device.UniformSetCreate([image], chain, 0); var kernel = device.ComputePipelineCreate(chain);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, forRenderPass: inputs ? 1u : 0u);
        uint[] push = [9], results = new uint[16]; float[] pixels = new float[64]; Color[] clear = [Colors.Black];
        var invalid = device.DrawListBegin(target); if (inputs) device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(invalid, pipeline); device.DrawListBindUniformSet(invalid, feedback, 0); device.DrawListSetPushConstant(invalid, MemoryMarshal.AsBytes(push.AsSpan()), 4); Reject<InvalidOperationException>(() => device.DrawListDraw(invalid, false, 1, 3)); device.DrawListEnd();
        void Chain()
        {
            var list = device.DrawListBegin(target, inputs ? 0 : DrawFlags.ClearColorAll, clear); if (inputs) device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, kernel); device.ComputeListBindUniformSet(list, computeSet, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
            device.TextureGetData(destination, 0, MemoryMarshal.AsBytes(pixels.AsSpan())); device.BufferGetData(buffer, MemoryMarshal.AsBytes(results.AsSpan()));
        }
        Chain(); for (var i = 0; i < 16; i++) { Check(Math.Abs(pixels[i * 4] - .375f) < .00001f && Math.Abs(pixels[i * 4 + 1] - .5f) < .00001f && Math.Abs(pixels[i * 4 + 2] - .75f) < .00001f && pixels[i * 4 + 3] == 1, "Mixed original image loads, sampled aliases and uniforms reach a subsequent compute image pass."); Check(results[i] == 9u + (uint)i, "Mixed native graphics image descriptors retain storage buffers and push uniforms."); }
        for (var i = 0; i < 16; i++) Chain(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Chain(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared mixed image/compute/readback cycles allocate zero owner bytes.");
        device.TextureCopy(destination, input, default, default, new(4, 4, 1), 0, 0, 0, 0); device.TextureGetData(input, 0, MemoryMarshal.AsBytes(initial.AsSpan())); Check(initial.AsSpan().SequenceEqual(pixels), "Graphics image results reach subsequent raw GPU texture copies.");
        foreach (var rid in new[] { pipeline, set, feedback, computeSet, kernel, shader, chain, target, input, marker, color, sampler, buffer, parameters }) device.FreeRID(rid);
        Console.WriteLine("PASS " + language + ": mixed readonly/writable images, sampled alias in GENERAL, buffers, uniforms, push, feedback refusal, compute/copy/readback and zero owner bytes."); return destination;
    }
    private static void ImageCanvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var root = new Window { Size = new(16, 16) }; using var texture = new TextureRD(); var frames = 0;
        root.Ready += _ => { texture.TextureRDRID = ImageMixed(RenderingServer.GetRenderingDevice()!, "glsl", false); root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured; };
        void Captured()
        {
            if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured; using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(8, 8); Check(Math.Abs(pixel.R - .375f) < .01f && Math.Abs(pixel.G - .5f) < .01f && Math.Abs(pixel.B - .75f) < .01f, "Graphics storage-image writes reach retained canvas sampling."); root.Tree!.Quit();
        }
        Engine.Run(root); Console.WriteLine("PASS original writable graphics image reaches retained canvas pixels.");
    }
    private static void ImageArrays(RenderingDevice device, string language, bool source)
    {
        var shader = Shader(device, "image-array", language, source); var target = device.FramebufferCreateEmpty(new(4, 4));
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R32UINT, UsageBits = TextureUsageBits.Storage | TextureUsageBits.Sampling | TextureUsageBits.CanCopyTo | TextureUsageBits.CanCopyFrom }; using var view = new RDTextureView();
        RID[] images = new RID[4]; for (var i = 0; i < 4; i++) images[i] = device.TextureCreate(format, view);
        using var vertex = new RDUniform { Binding = 0, UniformType = UniformType.Image }; vertex.AddID(images[0]); vertex.AddID(images[1]); using var fragment = new RDUniform { Binding = 1, UniformType = UniformType.Image }; fragment.AddID(images[2]); fragment.AddID(images[3]); var set = device.UniformSetCreate([vertex, fragment], shader, 0);
        using var raster = new RDPipelineRasterizationState(); using var blend = new RDPipelineColorBlendState(); var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        uint[] commands = [3, 1, 0, 0, 3, 1, 0, 0]; var indirect = device.StorageBufferCreate(32, MemoryMarshal.AsBytes(commands.AsSpan()), StorageBufferUsage.DispatchIndirect); uint[] push = [0], result = new uint[16];
        void Draw()
        {
            foreach (var image in images) device.TextureClear(image, default, 0, 1, 0, 1);
            var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0);
            for (var selected = 0; selected < 4; selected++) { push[0] = selected == 3 ? uint.MaxValue : (uint)selected; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4); device.DrawListDraw(list, false, 1, 3); device.DrawListDrawIndirect(list, false, indirect, 0, 2); }
            device.DrawListEnd();
            for (var image = 0; image < 4; image++) { device.TextureGetData(images[image], 0, MemoryMarshal.AsBytes(result.AsSpan())); for (var i = 0; i < 16; i++) Check(result[i] == (image < 2 ? i < 3 ? 3u : 0u : 6u), "Dynamic image arrays retain exact neighbors, suppress invalid stores/atomics and order direct/indirect attachmentless draws."); }
        }
        Draw(); for (var i = 0; i < 16; i++) Draw(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Draw(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared image-array/indirect cycles allocate zero owner bytes.");
        foreach (var image in images) device.FreeRID(image); foreach (var rid in new[] { pipeline, set, shader, target, indirect }) device.FreeRID(rid);
        Console.WriteLine("PASS " + language + ": writable graphics image arrays, out-of-range atomic suppression, attachmentless draws, indirect ordering and zero owner bytes.");
    }
    private static void ImageProfiles(RenderingDevice device, string mode)
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_GRAPHICS_IMAGE_OUTPUT") ?? throw new InvalidOperationException("Supply the qualified image-binary directory."); Directory.CreateDirectory(directory);
        foreach (var language in new[] { "glsl", "hlsl" })
        {
            byte[] Read(string name) => File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name + "." + language + ".spv"));
            using var both = Code("image-write", language); using var atomic = Code("image-atomic", language);
            using var vertex = new RDShaderSPIRV { BytecodeVertex = both.BytecodeVertex, BytecodeFragment = Read("image-plain.frag") }; using var fragment = new RDShaderSPIRV { BytecodeVertex = Read("read.vert"), BytecodeFragment = both.BytecodeFragment };
            foreach (var item in new[] { ("vertex", vertex, mode != "images-no-fragment"), ("fragment", fragment, mode != "images-no-vertex"), ("both", both, true), ("atomic", atomic, true) })
            {
                var path = System.IO.Path.Combine(directory, item.Item1 + "." + language + ".e2dshader");
                if (mode == "images-export") { File.WriteAllBytes(path, device.ShaderCompileBinaryFromSPIRV(item.Item2)); continue; }
                if (mode == "images-failure" || !item.Item3) { device.FreeRID(device.ShaderCreateFromSPIRV(item.Item2)); continue; }
                Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(item.Item2)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(item.Item2));
                var placeholder = device.ShaderCreatePlaceholder(); Reject<NotSupportedException>(() => device.ShaderCreateFromBytecode(File.ReadAllBytes(path), placeholder));
                using var valid = Code("read", language); Check(device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(valid), placeholder) == placeholder, "A refused image binary preserves its uninitialized placeholder."); device.FreeRID(placeholder);
            }
            if (mode is "images-export" or "images-no-vertex") ImageSingleStage(device, fragment, false);
            if (mode is "images-export" or "images-no-fragment") ImageSingleStage(device, vertex, true);
            if (mode is "images-export" or "images-no-fragment") { using var onlyVertex = new RDShaderSPIRV { BytecodeVertex = vertex.BytecodeVertex }; ImageSingleStage(device, onlyVertex, true, true); }
            if (mode == "images-failure")
            {
                var shader = device.ShaderCreateFromSPIRV(both); using var format = new RDAttachmentFormat { Format = DataFormat.R32G32B32A32SFLOAT, UsageFlags = TextureUsageBits.ColorAttachment }; var targetFormat = device.FramebufferFormatCreate([format]); using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] };
                for (var i = 0; i < 3; i++) Reject<InvalidOperationException>(() => device.RenderPipelineCreate(shader, targetFormat, -1, RenderPrimitive.Triangles, raster, blend)); device.FreeRID(shader);
            }
        }
        if (mode is "images-unavailable" or "images-failure") device.FreeRID(Execute(device, "glsl"));
        Console.WriteLine("PASS " + mode + ": shader/binary/template/placeholder refusal before publication, independent stage execution and retained buffer pipeline.");
    }
    private static void ImageSingleStage(RenderingDevice device, RDShaderSPIRV code, bool vertex, bool discard = false)
    {
        var placeholder = device.ShaderCreatePlaceholder(); var shader = device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code), placeholder);
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R32UINT, UsageBits = TextureUsageBits.Storage | TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom | TextureUsageBits.CanCopyTo }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view);
        format.Format = DataFormat.R32G32B32A32SFLOAT; format.UsageBits = TextureUsageBits.ColorAttachment; var color = discard ? default : device.TextureCreate(format, view); var target = discard ? device.FramebufferCreateEmpty(new(4, 4)) : device.FramebufferCreate([color]);
        using var uniform = new RDUniform { Binding = vertex ? 0 : 1, UniformType = UniformType.Image }; uniform.AddID(image); var set = device.UniformSetCreate([uniform], shader, 0);
        using var raster = new RDPipelineRasterizationState { DiscardPrimitives = discard }; using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = discard ? [] : [attachment] }; var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend);
        uint[] push = [7], pixels = new uint[16]; device.TextureClear(image, default, 0, 1, 0, 1); var list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListBindUniformSet(list, set, 0); device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(image, 0, MemoryMarshal.AsBytes(pixels.AsSpan()));
        for (var i = 0; i < 16; i++) Check(pixels[i] == (vertex ? i < 3 ? 7u + (uint)i : 0 : 107u + (uint)i), "The remaining native image-writing stage executes independently.");
        foreach (var rid in new[] { pipeline, set, target, image, shader }) device.FreeRID(rid); if (!discard) device.FreeRID(color);
    }
}
