using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;
using static TextureArrayTests;

internal static class RenderingDeviceSparseTests
{
    private static void Reject<T>(Action action) where T : Exception
    { try { action(); } catch (T) { return; } throw new InvalidOperationException(typeof(T).Name + " expected."); }
    private static RID Image(RenderingDevice device, DataFormat format = DataFormat.R8G8B8A8UNORM, TextureSamples samples = TextureSamples.Samples1, bool resolve = false)
    {
        using var description = new RDTextureFormat { Width = 16, Height = 16, Format = format, Samples = samples, IsResolveBuffer = resolve, UsageBits = TextureUsageBits.ColorAttachment | (samples == TextureSamples.Samples1 ? TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom : TextureUsageBits.InputAttachment) };
        using var view = new RDTextureView(); return device.TextureCreate(description, view);
    }
    private static RDPipelineColorBlendState Blends(int count, RDPipelineColorBlendStateAttachment active) => new() { Attachments = Enumerable.Repeat(active, count).ToArray() };
    internal static void Run()
    {
        using var device = RenderingServer.CreateLocalRenderingDevice();
        foreach (var language in new[] { "GLSL", "HLSL" }) Mixed(device, language);
        Signed(device); Multisample(device); Validation(device);
        Console.WriteLine("Sparse native MRT output maps, scalar types, grouped decorations, specialization, MSAA/input/preservation, metadata and lifetime passed on " + device.GetDeviceName());
    }
    private static byte[] GroupLocations(byte[] code)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code).ToArray(); var groups = new Dictionary<uint, uint>(); var result = new List<uint>(words.AsSpan(0, 5).ToArray());
        for (var i = 5; i < words.Length; i += (int)(words[i] >> 16))
        {
            var size = (int)(words[i] >> 16);
            if ((words[i] & 65535) == 71 && size == 4 && words[i + 2] == 30)
            {
                var location = words[i + 3];
                if (!groups.TryGetValue(location, out var group)) { group = words[3]++; groups.Add(location, group); result.AddRange([(2u << 16) | 73, group, (4u << 16) | 71, group, 30, location]); }
                result.AddRange([(3u << 16) | 74, group, words[i + 1]]);
            }
            else result.AddRange(words.AsSpan(i, size));
        }
        result[3] = words[3]; return MemoryMarshal.AsBytes(CollectionsMarshal.AsSpan(result)).ToArray();
    }
    private static void ExportTranslated(RenderingDevice device, RID shader, RDShaderSPIRV module, string name)
    {
        var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_GRAPHICS_OUTPUT"); if (directory is null) return;
        const System.Reflection.BindingFlags flags = System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance;
        var entries = (System.Collections.IDictionary)typeof(RenderingDevice).GetField("_entries", flags)!.GetValue(device)!; var entry = entries[shader]!;
        var outputs = entry.GetType().GetField("Outputs", flags)!.GetValue(entry);
        var remap = typeof(RenderingDevice).GetMethod("RemapFragmentOutputs", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static)!;
        var translated = (byte[])remap.Invoke(null, [module.BytecodeFragment, new[] { 1, 6 }, outputs])!;
        File.WriteAllBytes(System.IO.Path.Combine(directory, "SparseNative" + name + ".frag.spv"), translated);
    }
    private static void Mixed(RenderingDevice device, string language)
    {
        foreach (var grouped in new[] { false, true })
        {
            using var module = RenderingDeviceGraphicsTests.Load("DeviceSparse" + language); var original = module.BytecodeFragment;
            if (grouped) module.BytecodeFragment = GroupLocations(original);
            var published = module.BytecodeFragment; RenderingDeviceGraphicsTests.Export(module, "Sparse" + language + grouped);
            var shader = device.ShaderCreateFromSPIRV(module); ExportTranslated(device, shader, module, language + grouped); var color = Image(device); var numbers = Image(device, DataFormat.R32G32B32A32UINT);
            using var pass = new RDFramebufferPass { ColorAttachments = [-1, 1, -1, -1, -1, -1, 0, -1] };
            using var integerFormat = new RDAttachmentFormat { Format = DataFormat.R32G32B32A32UINT, UsageFlags = TextureUsageBits.ColorAttachment | TextureUsageBits.Sampling | TextureUsageBits.CanCopyFrom };
            using var colorFormat = new RDAttachmentFormat { Format = DataFormat.R8G8B8A8UNORM, UsageFlags = integerFormat.UsageFlags };
            var format = device.FramebufferFormatCreateMultipass([integerFormat, colorFormat], [pass]); integerFormat.Format = DataFormat.R8G8B8A8UNORM;
            var framebuffer = grouped ? device.FramebufferCreate([numbers, color], format) : device.FramebufferCreateMultipass([numbers, color], [pass], format);
            pass.ColorAttachments = [0]; Check(device.FramebufferFormatGetTextureSamples(format) == TextureSamples.Samples1, "Sparse format retains copied indices and selects the first active sample count.");
            using var raster = new RDPipelineRasterizationState(); using var active = new RDPipelineColorBlendStateAttachment(); using var ignored = new RDPipelineColorBlendStateAttachment { EnableBlend = true, ColorBlendOp = (BlendOperation)999 };
            using var blend = Blends(8, ignored); var attachments = blend.Attachments; attachments[1] = active; attachments[6] = active; blend.Attachments = attachments;
            var pipeline = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend);
            using var scale = new RDPipelineSpecializationConstant<float> { ConstantID = 3, Value = .5f };
            var specialized = device.RenderPipelineCreate(shader, format, -1, RenderPrimitive.Triangles, raster, blend, specializationConstants: [scale]);
            var bytes = new byte[16 * 16 * 4]; var integers = new byte[16 * 16 * 16]; var clear = new[] { Colors.Black, Colors.Black };
            void Frame(RID selected)
            {
                var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, clear); device.DrawListBindRenderPipeline(list, selected); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
                device.TextureGetData(color, 0, bytes); device.TextureGetData(numbers, 0, integers);
            }
            Frame(pipeline);
            for (var i = 0; i < bytes.Length; i += 4) Check(Math.Abs(bytes[i] - 64) <= 1 && Math.Abs(bytes[i + 1] - 128) <= 1 && Math.Abs(bytes[i + 2] - 191) <= 1 && bytes[i + 3] == 255, "Logical float output 1 retains its varying and native color pixels.");
            var values = MemoryMarshal.Cast<byte, uint>(integers); for (var i = 0; i < values.Length; i++) Check(values[i] == 42u + (uint)(i % 4), "Logical uint output 6 retains exact native integer pixels.");
            Frame(specialized); Check(Math.Abs(bytes[0] - 32) <= 1 && Math.Abs(bytes[1] - 64) <= 1, "Specialization composes with sparse native output remapping.");
            Check(module.BytecodeFragment.SequenceEqual(published), "Pipeline output translation never changes public module export.");
            GC.Collect(GC.MaxGeneration, GCCollectionMode.Forced, blocking: true, compacting: true); GC.WaitForPendingFinalizers();
            for (var i = 0; i < 32; i++) Frame((i & 1) == 0 ? pipeline : specialized);
            var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Frame((i & 1) == 0 ? pipeline : specialized);
            var allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0, "Prepared changing sparse pipelines/draw/readback allocate " + allocated + " managed owner bytes.");
            for (var i = 0; i < 32; i++) Frame(pipeline);
            before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 64; i++) Frame(pipeline);
            allocated = GC.GetAllocatedBytesForCurrentThread() - before; Check(allocated == 0, "Prepared unchanged sparse pipelines/draw/readback allocate " + allocated + " managed owner bytes.");
            device.FreeRID(pipeline); Check(device.RenderPipelineIsValid(specialized), "Private remapped stages retire independently."); device.FreeRID(numbers); Check(!device.FramebufferIsValid(framebuffer), "Sparse framebuffer retains native source dependency lifetime.");
            device.FreeRID(framebuffer); device.FreeRID(color); device.FreeRID(specialized); device.FreeRID(shader);
            Console.WriteLine(language + " grouped=" + grouped + ": exact float/uint MRT, specialization, 64 active/64 unchanged zero-managed-byte frames passed.");
        }
    }
    private static byte[] Stage(string name, string stage)
    {
        using var input = typeof(RenderingDeviceSparseTests).Assembly.GetManifestResourceStream("Electron2D.Device." + name + "." + stage + ".spv")!;
        using var bytes = new MemoryStream(); input.CopyTo(bytes); return bytes.ToArray();
    }
    private static byte[] OutputLocation(byte[] code, uint location)
    {
        var words = MemoryMarshal.Cast<byte, uint>(code).ToArray(); var outputs = new HashSet<uint>();
        for (var i = 5; i < words.Length; i += (int)(words[i] >> 16)) if ((words[i] & 65535) == 59 && words[i] >> 16 >= 4 && words[i + 3] == 3) outputs.Add(words[i + 2]);
        var changed = 0;
        for (var i = 5; i < words.Length; i += (int)(words[i] >> 16)) if ((words[i] & 65535) == 71 && words[i] >> 16 == 4 && words[i + 2] == 30 && outputs.Contains(words[i + 1])) { words[i + 3] = location; changed++; }
        Check(changed == 1, "The single-output fixture has one translated output location."); return MemoryMarshal.AsBytes(words.AsSpan()).ToArray();
    }
    private static void Signed(RenderingDevice device)
    {
        using var module = RenderingDeviceGraphicsTests.Load("DeviceSparseGLSL"); module.BytecodeFragment = Stage("DeviceSparseSigned", "frag");
        var shader = device.ShaderCreateFromSPIRV(module); var color = Image(device); var numbers = Image(device, DataFormat.R32G32B32A32SINT);
        using var pass = new RDFramebufferPass { ColorAttachments = [-1, 0, -1, -1, -1, -1, 1] }; var framebuffer = device.FramebufferCreateMultipass([color, numbers], [pass]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = Blends(7, attachment);
        var wrong = Image(device, DataFormat.R32G32B32A32UINT); var wrongTarget = device.FramebufferCreateMultipass([color, wrong], [pass]);
        Reject<ArgumentException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(wrongTarget), -1, RenderPrimitive.Triangles, raster, blend)); device.FreeRID(wrongTarget); device.FreeRID(wrong);
        var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend);
        var list = device.DrawListBegin(framebuffer); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
        var values = MemoryMarshal.Cast<byte, int>(device.TextureGetData(numbers, 0)); int[] expected = [-7, 17, -31, 1]; for (var i = 0; i < values.Length; i++) Check(values[i] == expected[i % 4], "Sparse signed integer output retains negative native lanes.");
        device.FreeRID(pipeline); device.FreeRID(framebuffer); device.FreeRID(shader); device.FreeRID(numbers); device.FreeRID(color);
    }
    private static void Multisample(RenderingDevice device)
    {
        foreach (var language in new[] { "GLSL", "HLSL" })
        {
            var output = Image(device); var source = Image(device, samples: TextureSamples.Samples4); var resolved = Image(device, resolve: true);
            using var first = new RDFramebufferPass { ColorAttachments = [-1, -1, -1, -1, 1, -1], ResolveAttachments = [-1, -1, -1, -1, 2, -1] };
            using var last = new RDFramebufferPass { ColorAttachments = [-1, -1, 0, -1], InputAttachments = [1], PreserveAttachments = [2] };
            var framebuffer = device.FramebufferCreateMultipass([output, source, resolved], [first, last]); var format = device.FramebufferGetFormat(framebuffer);
            Check(device.FramebufferFormatGetTextureSamples(format) == TextureSamples.Samples4 && device.FramebufferFormatGetTextureSamples(format, 1) == TextureSamples.Samples1, "Unused leading slots do not determine samples.");
            using var producerModule = RenderingDeviceGraphicsTests.Load("DevicePassProducer"); producerModule.BytecodeFragment = OutputLocation(producerModule.BytecodeFragment, 4);
            using var readerModule = RenderingDeviceGraphicsTests.Load("DevicePassMS" + language); readerModule.BytecodeFragment = OutputLocation(readerModule.BytecodeFragment, 2);
            var producerShader = device.ShaderCreateFromSPIRV(producerModule); var readerShader = device.ShaderCreateFromSPIRV(readerModule);
            using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var producerBlend = Blends(6, attachment); using var readerBlend = Blends(4, attachment); using var msaa = new RDPipelineMultisampleState { SampleCount = TextureSamples.Samples4 };
            var producer = device.RenderPipelineCreate(producerShader, format, -1, RenderPrimitive.Triangles, raster, producerBlend, multisampleState: msaa);
            var reader = device.RenderPipelineCreate(readerShader, format, -1, RenderPrimitive.Triangles, raster, readerBlend, forRenderPass: 1);
            using var uniform = new RDUniform { Binding = 7, UniformType = UniformType.InputAttachment }; uniform.AddID(source); var set = device.UniformSetCreate([uniform], readerShader, 6);
            var list = device.DrawListBegin(framebuffer, DrawFlags.ClearAll, [Colors.Black, Colors.Black, Colors.Black]); device.DrawListBindRenderPipeline(list, producer); device.DrawListDraw(list, false, 1, 3);
            device.DrawListSwitchToNextPass(); device.DrawListBindRenderPipeline(list, reader); device.DrawListBindUniformSet(list, set, 6); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            var oracle = device.TextureGetData(resolved, 0); var actual = device.TextureGetData(output, 0); var edges = 0;
            for (var i = 0; i < actual.Length; i += 4)
            {
                Check(Math.Abs(actual[i] - oracle[i + 2]) <= 1 && Math.Abs(actual[i + 1] - oracle[i + 1]) <= 1 && Math.Abs(actual[i + 2] - oracle[i]) <= 1 && actual[i + 3] == oracle[i + 3], "Sparse MSAA input loads match independent native resolve bytes.");
                if (oracle[i] > 0 && oracle[i] < 255) edges++;
            }
            Check(edges > 0, "Sparse MSAA includes fractional edge pixels."); Console.WriteLine(language + " sparse output/resolve/input sequence agrees with native MSAA oracle; edge pixels=" + edges);
            device.FreeRID(set); device.FreeRID(reader); device.FreeRID(producer); device.FreeRID(readerShader); device.FreeRID(producerShader); device.FreeRID(framebuffer); device.FreeRID(output); device.FreeRID(resolved); device.FreeRID(source);
        }
    }
    private static void Validation(RenderingDevice device)
    {
        var a = Image(device); var b = Image(device); using var pass = new RDFramebufferPass();
        foreach (var invalid in new[] { new[] { -2, 0 }, new[] { -1, 2 }, new[] { 0, -1, 0 }, new[] { -1, -1 }, Enumerable.Repeat(-1, 9).ToArray() })
        { pass.ColorAttachments = invalid; Reject<ArgumentException>(() => device.FramebufferCreateMultipass([a], [pass])); }
        pass.ColorAttachments = [-1, 0]; pass.ResolveAttachments = [1, -1]; Reject<ArgumentException>(() => device.FramebufferCreateMultipass([a, b], [pass])); pass.ResolveAttachments = [];
        using var module = RenderingDeviceGraphicsTests.Load("DeviceProcedural"); var shader = device.ShaderCreateFromSPIRV(module);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = Blends(2, attachment);
        var dense = device.FramebufferCreate([a, b]); Reject<ArgumentException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(dense), -1, RenderPrimitive.Triangles, raster, blend));
        var sparse = device.FramebufferCreateMultipass([a], [pass]); Reject<ArgumentException>(() => device.RenderPipelineCreate(shader, device.FramebufferGetFormat(sparse), -1, RenderPrimitive.Triangles, raster, blend));
        module.BytecodeFragment = OutputLocation(module.BytecodeFragment, 1); var matching = device.ShaderCreateFromSPIRV(module); var pipeline = device.RenderPipelineCreate(matching, device.FramebufferGetFormat(sparse), -1, RenderPrimitive.Triangles, raster, blend);
        var list = device.DrawListBegin(dense); Reject<ArgumentException>(() => device.DrawListBindRenderPipeline(list, pipeline)); device.DrawListEnd();
        using var upperPass = new RDFramebufferPass { ColorAttachments = [-1, -1, -1, -1, -1, -1, -1, 0] };
        var upperTarget = device.FramebufferCreateMultipass([a], [upperPass]); module.BytecodeFragment = OutputLocation(module.BytecodeFragment, 7); var upperShader = device.ShaderCreateFromSPIRV(module);
        using var wideBlend = Blends(9, attachment); var upperPipeline = device.RenderPipelineCreate(upperShader, device.FramebufferGetFormat(upperTarget), -1, RenderPrimitive.Triangles, raster, wideBlend);
        list = device.DrawListBegin(upperTarget); try { device.DrawListBindRenderPipeline(list, upperPipeline); device.DrawListDraw(list, false, 1, 3); } finally { device.DrawListEnd(); }
        Check(device.TextureGetData(a, 0)[0] == 255, "The last legal output slot executes and extra blend entries are ignored.");
        device.FreeRID(upperPipeline); device.FreeRID(upperShader); device.FreeRID(upperTarget);
        var ownerRejected = Task.Run(() => { try { device.FramebufferGetFormat(sparse); return false; } catch (InvalidOperationException) { return true; } }).GetAwaiter().GetResult(); Check(ownerRejected, "Sparse framebuffer remains owner-thread accessed.");
        using var depthDescription = new RDTextureFormat { Width = 16, Height = 16, Format = DataFormat.D32SFLOAT, UsageBits = TextureUsageBits.DepthStencilAttachment };
        using var view = new RDTextureView(); var depth = device.TextureCreate(depthDescription, view); using var depthPass = new RDFramebufferPass { ColorAttachments = [-1, -1], DepthAttachment = 0 }; var depthTarget = device.FramebufferCreateMultipass([depth], [depthPass]);
        using var depthModule = RenderingDeviceGraphicsTests.Load("DeviceProcedural"); depthModule.BytecodeFragment = Stage("DeviceDepthOnly", "frag"); var depthShader = device.ShaderCreateFromSPIRV(depthModule); var depthPipeline = device.RenderPipelineCreate(depthShader, device.FramebufferGetFormat(depthTarget), -1, RenderPrimitive.Triangles, raster, blend);
        list = device.DrawListBegin(depthTarget, DrawFlags.ClearDepth); try { device.DrawListBindRenderPipeline(list, depthPipeline); device.DrawListDraw(list, false, 1, 3); } finally { device.DrawListEnd(); }
        device.FreeRID(depthPipeline); device.FreeRID(depthShader); device.FreeRID(depthTarget); device.FreeRID(depth); device.FreeRID(pipeline); device.FreeRID(matching); device.FreeRID(shader); device.FreeRID(sparse); device.FreeRID(dense); device.FreeRID(a); device.FreeRID(b);
    }
    private sealed class Patch(Texture texture) : Entity { protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 16, 16), false); }
    internal static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false);
        using var texture = new TextureRD(); var root = new Window { Size = new(32, 32) }; var frames = 0;
        root.Ready += _ =>
        {
            RenderingServer.SetDefaultClearColor(Colors.Black); var device = RenderingServer.GetRenderingDevice()!; var image = Image(device);
            using var pass = new RDFramebufferPass { ColorAttachments = [-1, -1, -1, 0, -1] }; var framebuffer = device.FramebufferCreateMultipass([image], [pass]);
            using var module = RenderingDeviceGraphicsTests.Load("DeviceProcedural"); module.BytecodeFragment = OutputLocation(module.BytecodeFragment, 3); var shader = device.ShaderCreateFromSPIRV(module);
            using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = Blends(5, attachment);
            var pipeline = device.RenderPipelineCreate(shader, device.FramebufferGetFormat(framebuffer), -1, RenderPrimitive.Triangles, raster, blend);
            var list = device.DrawListBegin(framebuffer, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, pipeline); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd();
            texture.TextureRDRID = image; root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured;
        };
        void Captured()
        {
            if (++frames < 3) return;
            using var screenshot = root.GetTexture().GetImage()!; var pixel = screenshot.GetPixel(8, 8);
            var directory = Environment.GetEnvironmentVariable("ELECTRON2D_DEVICE_GRAPHICS_OUTPUT"); if (directory is not null) screenshot.SavePNG(System.IO.Path.Combine(directory, "sparse-canvas.png"));
            Check(pixel.R is > .49f and < .51f && pixel.G < .01f && pixel.B < .01f, "Sparse device output reaches actual retained canvas pixels: " + pixel); RenderingServer.FramePostDraw -= Captured; root.Tree!.Quit();
        }
        Engine.Run(root); Check(root.IsDisposed && RenderingServer.GetRenderingDevice() is null, "Sparse renderer-owned resources retire with their session.");
    }
}
