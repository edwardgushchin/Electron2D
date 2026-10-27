using Electron2D;
using System.Buffers.Binary;

internal static class DeviceResourceCacheChecks
{
    private sealed class Patch(Texture texture) : Entity
    {
        protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 64, 64), false);
    }
    internal static void Run(string profile)
    {
        Reject<InvalidOperationException>(() => UniformSetCacheRD.GetCache(default, 0, []));
        Reject<InvalidOperationException>(() => FramebufferCacheRD.GetCacheMultipass([], [], 1));
        ProjectSettings.Set(ProjectSettings.RenderingMethod, profile); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        if (profile == "compatibility")
        {
            using var window = new Window { Size = new(64, 64) };
            window.Ready += readyNode => { Reject<InvalidOperationException>(() => UniformSetCacheRD.GetCache(default, 0, [])); Reject<InvalidOperationException>(() => FramebufferCacheRD.GetCacheMultipass([], [], 1)); window.Tree!.Quit(); };
            Engine.Run(window); Console.WriteLine("Resource caches: absent/Compatibility GPU service guards passed."); return;
        }
        using var texture = new TextureRD(); using var root = new Window { Size = new(80, 80), Borderless = true };
        root.AddChild(new Patch(texture));
        var viewport = new SubViewport { Size = new(8, 8) }; viewport.AddChild(new ColorRect { Size = new(8, 8), Color = Colors.Blue });
        var preview = new Patch(viewport.GetTexture()) { Position = new(64, 0) }; root.AddChild(viewport); root.AddChild(preview);
        RID borrowedFramebuffer = default;

        RenderingDevice device = null!; RID oldShader = default, framebuffer = default, image = default, set = default, shader = default;
        RDUniform[] bindings = []; RDFramebufferPass[] passes = []; RID[] images = [];
        var frames = 0; long before = 0; long frameBytes = -1;
        root.Ready += readyNode =>
        {
            device = RenderingServer.GetRenderingDevice()!;
            using var compute = new RDShaderSPIRV { BytecodeCompute = Read("CacheWrite.comp") };
            shader = oldShader = device.ShaderCreateFromSPIRV(compute);
            var buffer = device.StorageBufferCreate(4); var otherBuffer = device.StorageBufferCreate(4);
            byte[] input = new byte[16]; BinaryPrimitives.WriteUInt32LittleEndian(input, 41); var uniform = device.UniformBufferCreate(16, input);
            bindings = [Binding(0, RenderingDevice.UniformType.StorageBuffer, buffer), Binding(1, RenderingDevice.UniformType.UniformBuffer, uniform)];
            set = UniformSetCacheRD.GetCache(shader, 0, bindings);
            using (var a = Binding(0, RenderingDevice.UniformType.StorageBuffer, buffer))
            using (var b = Binding(1, RenderingDevice.UniformType.UniformBuffer, uniform))
                Check(set == UniformSetCacheRD.GetCache(shader, 0, [a, b]), "Binding contents, not descriptor identity, select a cached set.");
            bindings[0].ClearIDs(); bindings[0].AddID(otherBuffer); var otherSet = UniformSetCacheRD.GetCache(shader, 0, bindings);
            Check(otherSet != set && device.UniformSetIsValid(set), "Edits create independent copied keys.");
            bindings[0].ClearIDs(); bindings[0].AddID(buffer); Check(UniformSetCacheRD.GetCache(shader, 0, bindings) == set, "Restoring descriptor contents reuses the original.");
            Reject<ArgumentException>(() => UniformSetCacheRD.GetCache(buffer, 0, bindings));
            Reject<ArgumentException>(() => UniformSetCacheRD.GetCache(shader, 0, [bindings[0], bindings[0]]));
            Reject<ArgumentNullException>(() => UniformSetCacheRD.GetCache(shader, 0, [null!]));
            Reject<InvalidOperationException>(() => Task.Run(() => UniformSetCacheRD.GetCache(shader, 0, bindings)).GetAwaiter().GetResult());
            device.FreeRID(set); Check(!device.UniformSetIsValid(set), "Explicit cached-set free."); set = UniformSetCacheRD.GetCache(shader, 0, bindings);
            var pipeline = device.ComputePipelineCreate(shader); var list = device.ComputeListBegin();
            Check(UniformSetCacheRD.GetCache(shader, 0, bindings) == set, "Uniform cache hits are allowed during compute recording.");
            Reject<InvalidOperationException>(() => FramebufferCacheRD.GetCacheMultipass([], [], 1));
            device.ComputeListBindComputePipeline(list, pipeline); device.ComputeListBindUniformSet(list, set, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
            Span<byte> result = stackalloc byte[4]; device.BufferGetData(buffer, result); Check(BinaryPrimitives.ReadUInt32LittleEndian(result) == 48, "A cached set executes real GPU writes.");
            device.FreeRID(otherBuffer); Check(!device.UniformSetIsValid(otherSet) && device.UniformSetIsValid(set), "Only dependent cached sets retire."); Reject<ArgumentException>(() => device.FreeRID(otherSet));
            image = Image(device); images = [image];
            passes = [new() { ColorAttachments = [0] }, new() { ColorAttachments = [0] }];
            framebuffer = FramebufferCacheRD.GetCacheMultipass(images, passes, 1);
            using (var a = new RDFramebufferPass { ColorAttachments = [0] })
            using (var b = new RDFramebufferPass { ColorAttachments = [0] })
                Check(framebuffer == FramebufferCacheRD.GetCacheMultipass([image], [a, b], 1), "Framebuffer keys copy pass values and attachment order.");
            passes[0].ColorAttachments = [-1, 0]; var sparse = FramebufferCacheRD.GetCacheMultipass(images, passes, 1);
            Check(sparse != framebuffer && device.FramebufferIsValid(framebuffer), "A pass edit does not rewrite old cached state."); passes[0].ColorAttachments = [0];
            Check(framebuffer == FramebufferCacheRD.GetCacheMultipass(images, passes, 1), "Restored pass contents reuse the original.");
            passes[0].DepthAttachment = 0; Reject<ArgumentException>(() => FramebufferCacheRD.GetCacheMultipass(images, passes, 1)); passes[0].DepthAttachment = -1;
            passes[0].InputAttachments = [999]; Reject<ArgumentException>(() => FramebufferCacheRD.GetCacheMultipass(images, passes, 1)); passes[0].InputAttachments = [];
            passes[0].PreserveAttachments = [999]; Reject<ArgumentException>(() => FramebufferCacheRD.GetCacheMultipass(images, passes, 1)); passes[0].PreserveAttachments = [];
            passes[0].ResolveAttachments = [999]; Reject<ArgumentException>(() => FramebufferCacheRD.GetCacheMultipass(images, passes, 1)); passes[0].ResolveAttachments = [];
            Reject<ArgumentOutOfRangeException>(() => FramebufferCacheRD.GetCacheMultipass(images, passes, 0));
            Reject<ArgumentOutOfRangeException>(() => FramebufferCacheRD.GetCacheMultipass(images, passes, 257));
            Reject<ArgumentOutOfRangeException>(() => FramebufferCacheRD.GetCacheMultipass([], [], 1));
            Reject<ArgumentException>(() => FramebufferCacheRD.GetCacheMultipass([buffer], [], 1));
            Reject<ArgumentException>(() => FramebufferCacheRD.GetCacheMultipass([image, image], [], 1));
            Reject<ArgumentNullException>(() => FramebufferCacheRD.GetCacheMultipass(images, [null!], 1));
            Reject<InvalidOperationException>(() => Task.Run(() => FramebufferCacheRD.GetCacheMultipass(images, passes, 1)).GetAwaiter().GetResult());
            using (var local = RenderingServer.CreateLocalRenderingDevice())
            {
                var foreign = Image(local); Reject<ArgumentException>(() => FramebufferCacheRD.GetCacheMultipass([foreign], [], 1));
                var foreignShader = local.ShaderCreateFromSPIRV(compute); Reject<ArgumentException>(() => UniformSetCacheRD.GetCache(foreignShader, 0, bindings));
            }
            var stableFormat = device.FramebufferGetFormat(framebuffer);
            for (var i = 0; i < 64; i++) { device.FreeRID(framebuffer); framebuffer = FramebufferCacheRD.GetCacheMultipass(images, passes, 1); Check(device.FramebufferGetFormat(framebuffer) == stableFormat, "Cache recreation does not grow equivalent framebuffer formats."); }
            device.FreeRID(framebuffer); var replacement = FramebufferCacheRD.GetCacheMultipass(images, passes, 1); Check(replacement != framebuffer, "Explicit cached-framebuffer free evicts its key."); framebuffer = replacement;
            using var graphics = new RDShaderSPIRV { BytecodeVertex = Read("CacheDraw.vert"), BytecodeFragment = Read("CacheDraw.frag") }; var drawShader = device.ShaderCreateFromSPIRV(graphics);
            using var raster = new RDPipelineRasterizationState(); using var blendTarget = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [blendTarget] };
            var format = device.FramebufferGetFormat(framebuffer);
            var first = device.RenderPipelineCreate(drawShader, format, -1, RenderingDevice.RenderPrimitive.Triangles, raster, blend);
            var second = device.RenderPipelineCreate(drawShader, format, -1, RenderingDevice.RenderPrimitive.Triangles, raster, blend, forRenderPass: 1);
            list = device.DrawListBegin(framebuffer, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Blue]);
            Reject<InvalidOperationException>(() => FramebufferCacheRD.GetCacheMultipass(images, passes, 1));
            device.DrawListBindRenderPipeline(list, first); device.DrawListDraw(list, false, 1, 3); list = device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, second); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            var pixels = device.TextureGetData(image, 0); Check(pixels[0] == 255 && pixels[1] == 0 && pixels[2] == 0 && pixels[3] == 255, "Cached multipass framebuffer executes actual graphics.");
            using (var nullablePass = new RDFramebufferPass { ColorAttachments = [1] })
            {
                var nullable = FramebufferCacheRD.GetCacheMultipass([default, image], [nullablePass], 1);
                Check(nullable == FramebufferCacheRD.GetCacheMultipass([default, image], [nullablePass], 1), "Empty unused texture slots belong to copied keys.");
                var nullablePipeline = device.RenderPipelineCreate(drawShader, device.FramebufferGetFormat(nullable), -1, RenderingDevice.RenderPrimitive.Triangles, raster, blend);
                var nullableList = device.DrawListBegin(nullable, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Blue]);
                device.DrawListBindRenderPipeline(nullableList, nullablePipeline); device.DrawListDraw(nullableList, false, 1, 3); device.DrawListEnd();
                Check(device.TextureGetData(image, 0)[0] == 255, "Unused empty attachments execute native drawing.");
                nullablePass.ColorAttachments = [0]; Reject<ArgumentException>(() => FramebufferCacheRD.GetCacheMultipass([default, image], [nullablePass], 1));
                nullablePass.ColorAttachments = [1]; nullablePass.PreserveAttachments = [0]; Reject<ArgumentException>(() => FramebufferCacheRD.GetCacheMultipass([default, image], [nullablePass], 1));
            }
            Layers(device); Resolve(device); BorrowedBuffer(device, shader, uniform, buffer);
            texture.TextureRDRID = image;
            for (var i = 0; i < 64; i++) { Check(UniformSetCacheRD.GetCache(shader, 0, bindings) == set, "Warm uniform reuse."); Check(FramebufferCacheRD.GetCacheMultipass(images, passes, 1) == framebuffer, "Warm framebuffer reuse."); }
            var start = GC.GetAllocatedBytesForCurrentThread();
            for (var i = 0; i < 512; i++) { _ = UniformSetCacheRD.GetCache(shader, 0, bindings); _ = FramebufferCacheRD.GetCacheMultipass(images, passes, 1); }
            Check(GC.GetAllocatedBytesForCurrentThread() == start, "512 prepared cache pairs allocate zero managed bytes.");
        };
        RenderingServerFrameHooks(root);
        Engine.Run(root);
        Check(device.IsDisposed && frames == 80 && frameBytes == 0, "Retained device and caches retire after zero-owner-byte frames.");
        foreach (var binding in bindings) binding.Dispose(); foreach (var pass in passes) pass.Dispose();
        using var reopened = new Window { Size = new(32, 32), Borderless = true };
        reopened.Ready += readyNode => { Reject<ArgumentException>(() => UniformSetCacheRD.GetCache(oldShader, 0, [])); Reject<ArgumentException>(() => FramebufferCacheRD.GetCacheMultipass([image], [], 1)); reopened.Tree!.Quit(); };
        Engine.Run(reopened);
        Console.WriteLine("Resource caches: compute/graphics/MSAA/multiview, copied keys, eviction, owner/session guards, 512 zero-byte cache pairs and 64 zero-owner-byte native frames passed.");
        void RenderingServerFrameHooks(Window window)
        {
            root.Ready += readyNode => RenderingServer.FramePostDraw += () =>
            {
                if (frames == 0)
                {
                    using var snapshot = window.GetTexture().GetImage()!; var pixel = snapshot.GetPixel(24, 24); Check(pixel.R > .99f && pixel.G < .01f && pixel.B < .01f, "Cached framebuffer feeds a real native canvas.");
                    var borrowed = RenderingServer.TextureGetRDTexture(viewport.GetTexture().GetRID());
                    borrowedFramebuffer = FramebufferCacheRD.GetCacheMultipass([borrowed], [], 1);
                    if (Environment.GetEnvironmentVariable("E2D_CACHE_OUTPUT") is { } path) { Directory.CreateDirectory(path); snapshot.SavePNG(System.IO.Path.Combine(path, "cached-framebuffer.png")); }
                }
                if (frames == 1) { preview.Dispose(); viewport.Dispose(); }
                if (frames == 3)
                {
                    Span<byte> sync = stackalloc byte[4]; device.BufferGetData(bindings[0].GetIDs()[0], sync);
                    Check(!device.FramebufferIsValid(borrowedFramebuffer), "Renderer target retirement invalidates a cached framebuffer.");
                    Reject<ArgumentException>(() => device.FreeRID(borrowedFramebuffer));
                }
                _ = UniformSetCacheRD.GetCache(shader, 0, bindings); _ = FramebufferCacheRD.GetCacheMultipass(images, passes, 1);
                if (++frames == 16) before = GC.GetAllocatedBytesForCurrentThread();
                if (frames == 80)
                {
                    frameBytes = GC.GetAllocatedBytesForCurrentThread() - before;
                    device.FreeRID(shader); Check(!device.UniformSetIsValid(set), "Shader retirement evicts cached sets."); Reject<ArgumentException>(() => device.FreeRID(set));
                    device.FreeRID(image); Check(!device.FramebufferIsValid(framebuffer), "Attachment retirement evicts cached framebuffers."); Reject<ArgumentException>(() => device.FreeRID(framebuffer)); root.Tree!.Quit();
                }
            };
        }
    }
    private static void BorrowedBuffer(RenderingDevice device, RID shader, RID uniform, RID syncBuffer)
    {
        using var instances = new MultiMesh { InstanceCount = 1 };
        var borrowed = RenderingServer.MultiMeshGetBufferRDRID(instances.GetRID());
        using var a = Binding(0, RenderingDevice.UniformType.StorageBuffer, borrowed); using var b = Binding(1, RenderingDevice.UniformType.UniformBuffer, uniform);
        var cached = UniformSetCacheRD.GetCache(shader, 0, [a, b]);
        instances.InstanceCount = 2; _ = RenderingServer.MultiMeshGetBufferRDRID(instances.GetRID());
        Span<byte> sync = stackalloc byte[4]; device.BufferGetData(syncBuffer, sync);
        Check(!device.UniformSetIsValid(cached), "Resource-owned instance-buffer replacement retires cached sets."); Reject<ArgumentException>(() => device.FreeRID(cached));
    }
    private static void Layers(RenderingDevice device)
    {
        var image = Image(device, 3); RID[] textures = [default, image];
        var framebuffer = FramebufferCacheRD.GetCacheMultipass(textures, [], 2); Check(framebuffer == FramebufferCacheRD.GetCacheMultipass(textures, [], 2) && framebuffer != FramebufferCacheRD.GetCacheMultipass(textures, [], 1), "View count belongs to the key.");
        device.DrawListBegin(framebuffer, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Green]); device.DrawListEnd();
        for (uint layer = 0; layer < 3; layer++) { var pixels = device.TextureGetData(image, layer); Check(layer < 2 ? pixels[1] == 255 : pixels.All(v => v == 0), "Ordered cached framebuffer views preserve untouched layers."); }
        device.FreeRID(image); Reject<ArgumentException>(() => device.FreeRID(framebuffer));
    }
    private static void Resolve(RenderingDevice device)
    {
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = RenderingDevice.TextureUsageBits.ColorAttachment, Samples = RenderingDevice.TextureSamples.Samples4 };
        using var view = new RDTextureView(); var source = device.TextureCreate(format, view); format.Samples = RenderingDevice.TextureSamples.Samples1; format.IsResolveBuffer = true; format.UsageBits |= RenderingDevice.TextureUsageBits.CanCopyFrom; var output = device.TextureCreate(format, view);
        using var pass = new RDFramebufferPass { ColorAttachments = [1], ResolveAttachments = [2] }; var target = FramebufferCacheRD.GetCacheMultipass([default, source, output], [pass], 1);
        device.DrawListBegin(target, RenderingDevice.DrawFlags.ClearColorAll, [Colors.Blue, Colors.Blue]); device.DrawListEnd(); var pixels = device.TextureGetData(output, 0); Check(pixels[2] == 255 && pixels[0] == 0, "Cached MSAA pass performs its native resolve.");
        device.FreeRID(source); Reject<ArgumentException>(() => device.FreeRID(target)); device.FreeRID(output);
    }
    private static RID Image(RenderingDevice device, uint layers = 1)
    {
        using var format = new RDTextureFormat { Width = 8, Height = 8, ArrayLayers = layers, TextureType = layers == 1 ? RenderingDevice.TextureType.Texture : RenderingDevice.TextureType.TextureArray, Format = RenderingDevice.DataFormat.R8G8B8A8UNORM, UsageBits = RenderingDevice.TextureUsageBits.ColorAttachment | RenderingDevice.TextureUsageBits.CanCopyFrom | RenderingDevice.TextureUsageBits.Sampling | RenderingDevice.TextureUsageBits.CanCopyTo };
        using var view = new RDTextureView(); return device.TextureCreate(format, view);
    }
    private static RDUniform Binding(int slot, RenderingDevice.UniformType type, RID resource) { var binding = new RDUniform { Binding = slot, UniformType = type }; binding.AddID(resource); return binding; }
    private static byte[] Read(string name) { using var stream = typeof(DeviceResourceCacheChecks).Assembly.GetManifestResourceStream("Electron2D.Device." + name + ".spv")!; using var output = new MemoryStream(); stream.CopyTo(output); return output.ToArray(); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
}
