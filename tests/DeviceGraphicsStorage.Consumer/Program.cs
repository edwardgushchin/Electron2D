using System.Runtime.InteropServices;
using Electron2D;
using static Electron2D.RenderingDevice;

try { GraphicsStorage.Run(args.Length == 0 ? "graphics" : args[0]); return 0; }
catch (Exception error) { Console.Error.WriteLine(error); return 1; }

internal static partial class GraphicsStorage
{
    internal static void Run(string mode)
    {
        if (mode.StartsWith("texel", StringComparison.Ordinal)) { Texels(mode); return; }
        if (mode == "canvas") { Canvas(); Canvas(); return; }
        using var device = RenderingServer.CreateLocalRenderingDevice(); Console.WriteLine("Graphics storage device: " + device.GetDeviceName());
        if (mode.StartsWith("unavailable", StringComparison.Ordinal)) { Unavailable(device, mode); return; }
        foreach (var language in new[] { "glsl", "hlsl" }) { if (mode == "edges") Edges(device, language); else if (mode == "aliases") { Execute(device, language, bufferKind: 1); Execute(device, language, bufferKind: 2); } else if (mode == "precision") { if (language == "glsl") Precision(device); } else Execute(device, language, mode == "source"); }
    }
    private static RDShaderSPIRV Code(string name, string language)
    {
        byte[] Read(string stage) => File.ReadAllBytes(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name + "." + stage + "." + language + ".spv"));
        return name is "chain" or "texel-chain" ? new() { BytecodeCompute = Read("comp") } : new() { BytecodeVertex = Read("vert"), BytecodeFragment = Read("frag") };
    }
    private static RID Shader(RenderingDevice device, string name, string language, bool authoring)
    {
        if (!authoring) { using var code = Code(name, language); return device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(code)); }
        string Read(string stage) => File.ReadAllText(System.IO.Path.Combine(AppContext.BaseDirectory, "Shaders", name + "." + stage + "." + language));
        using var source = new RDShaderSource { Language = language == "glsl" ? ShaderLanguage.GLSL : ShaderLanguage.HLSL, SourceVertex = name is "chain" or "texel-chain" ? "" : Read("vert"), SourceFragment = name is "chain" or "texel-chain" ? "" : Read("frag"), SourceCompute = name is "chain" or "texel-chain" ? Read("comp") : "" };
        using var code2 = device.ShaderCompileSPIRVFromSource(source); Check(code2.CompileErrorVertex == "" && code2.CompileErrorFragment == "" && code2.CompileErrorCompute == "", code2.CompileErrorVertex + code2.CompileErrorFragment + code2.CompileErrorCompute); return device.ShaderCreateFromSPIRV(code2);
    }
    private static RID Set(RenderingDevice device, RID shader, RID buffer)
    { using var uniform = new RDUniform { Binding = 0, UniformType = UniformType.StorageBuffer }; uniform.AddID(buffer); return device.UniformSetCreate([uniform], shader, 0); }
    private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    private static void Reject<T>(Action action) where T : Exception { try { action(); } catch (T) { return; } throw new InvalidOperationException("Expected " + typeof(T).Name); }
    private static RID Execute(RenderingDevice device, string language, bool authoring = false, int bufferKind = 0)
    {
        var writer = Shader(device, "write", language, authoring); var reader = Shader(device, "read", language, authoring); var compute = Shader(device, "chain", language, authoring);
        using var format = new RDTextureFormat { Width = 4, Height = 4, Format = DataFormat.R32G32B32A32SFLOAT, UsageBits = TextureUsageBits.ColorAttachment | TextureUsageBits.CanCopyFrom | TextureUsageBits.Sampling }; using var view = new RDTextureView(); var image = device.TextureCreate(format, view); var target = device.FramebufferCreate([image]);
        using var raster = new RDPipelineRasterizationState(); using var attachment = new RDPipelineColorBlendStateAttachment(); using var blend = new RDPipelineColorBlendState { Attachments = [attachment] }; using var factor = new RDPipelineSpecializationConstant<uint> { ConstantID = 0, Value = 1 };
        var write = device.RenderPipelineCreate(writer, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend, specializationConstants: [factor]); var read = device.RenderPipelineCreate(reader, device.FramebufferGetFormat(target), -1, RenderPrimitive.Triangles, raster, blend); var kernel = device.ComputePipelineCreate(compute);
        var buffer = bufferKind == 1 ? device.VertexBufferCreate(48, creationBits: BufferCreationBits.AsStorage) : bufferKind == 2 ? device.IndexBufferCreate(12, IndexBufferFormat.UInt32, creationBits: BufferCreationBits.AsStorage) : device.StorageBufferCreate(48, usage: StorageBufferUsage.DispatchIndirect); var writeSet = Set(device, writer, buffer); var readSet = Set(device, reader, buffer); var computeSet = Set(device, compute, buffer); uint[] push = [1]; uint[] result = new uint[12]; var pixels = new float[64];
        void Draw()
        {
            device.BufferClear(buffer, 0, 48); var list = device.DrawListBegin(target, DrawFlags.ClearColorAll, [Colors.Black]); device.DrawListBindRenderPipeline(list, write); device.DrawListBindUniformSet(list, writeSet, 0);
            push[0] = 1; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4); device.DrawListEnableScissor(list, new(0, 0, 2, 4)); device.DrawListDraw(list, false, 1, 3);
            push[0] = 2; device.DrawListSetPushConstant(list, MemoryMarshal.AsBytes(push.AsSpan()), 4); device.DrawListEnableScissor(list, new(2, 0, 2, 4)); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.TextureGetData(image, 0, MemoryMarshal.AsBytes(pixels.AsSpan()));
        }
        Draw(); for (var i = 0; i < 16; i++) Check(pixels[i * 4] == (i % 4 < 2 ? 1 : 0) && pixels[i * 4 + 1] == (i % 4 < 2 ? 0 : 1), "Restart preserves prior color pixels and copied push/scissor state.");
        void Chain()
        {
            Draw(); var list = device.ComputeListBegin(); device.ComputeListBindComputePipeline(list, kernel); device.ComputeListBindUniformSet(list, computeSet, 0); device.ComputeListDispatch(list, 1, 1, 1); device.ComputeListEnd();
            list = device.DrawListBegin(target); device.DrawListBindRenderPipeline(list, read); device.DrawListBindUniformSet(list, readSet, 0); device.DrawListDraw(list, false, 1, 3); device.DrawListEnd(); device.BufferGetData(buffer, MemoryMarshal.AsBytes(result.AsSpan())); device.TextureGetData(image, 0, MemoryMarshal.AsBytes(pixels.AsSpan()));
        }
        Chain(); Check(result[0] == 6 && result[1] == 24 && result[2] == 30, "Vertex/fragment atomic writes are ordered before compute and CPU readback."); Check(Math.Abs(pixels[0] - .6f) < .0001 && Math.Abs(pixels[1] - .24f) < .0001 && Math.Abs(pixels[2] - .3f) < .0001, "Compute result returns to readonly graphics.");
        for (var i = 0; i < 16; i++) Chain(); var before = GC.GetAllocatedBytesForCurrentThread(); for (var i = 0; i < 32; i++) Chain(); Check(GC.GetAllocatedBytesForCurrentThread() == before, "Prepared storage-writing draw/compute/readback cycles allocate zero owner bytes.");
        device.FreeRID(writeSet); device.FreeRID(readSet); device.FreeRID(computeSet); device.FreeRID(write); device.FreeRID(read); device.FreeRID(kernel); device.FreeRID(writer); device.FreeRID(reader); device.FreeRID(compute); device.FreeRID(buffer); device.FreeRID(target);
        Console.WriteLine("PASS " + language + ": native vertex/fragment stores and atomics, shared descriptor, typed specialization, pass restart, compute/readback, borrowed lifetime and 32 zero-owner-byte cycles."); return image;
    }
    private static void Unavailable(RenderingDevice device, string mode)
    {
        foreach (var language in new[] { "glsl", "hlsl" })
        {
            using var both = Code("write", language); using var read = Code("read", language);
            using var vertex = new RDShaderSPIRV { BytecodeVertex = both.BytecodeVertex, BytecodeFragment = read.BytecodeFragment };
            using var fragment = new RDShaderSPIRV { BytecodeVertex = read.BytecodeVertex, BytecodeFragment = both.BytecodeFragment };
            foreach (var item in new[] { (vertex, mode != "unavailable-fragment"), (fragment, mode != "unavailable-vertex"), (both, true) })
            {
                if (item.Item2) { Reject<NotSupportedException>(() => device.ShaderCreateFromSPIRV(item.Item1)); Reject<NotSupportedException>(() => device.ShaderCompileBinaryFromSPIRV(item.Item1)); }
                else device.FreeRID(device.ShaderCreateFromBytecode(device.ShaderCompileBinaryFromSPIRV(item.Item1)));
            }
            device.FreeRID(device.ShaderCreateFromSPIRV(read));
        }
        Console.WriteLine("PASS independent stage feature refusal before shader/binary publication: " + mode);
    }
    private sealed class Patch(Texture texture) : Entity { protected override void OnDraw() => DrawTextureRect(texture, new(0, 0, 16, 16), false); }
    private static void Canvas()
    {
        ProjectSettings.Set(ProjectSettings.RenderingMethod, "gpu"); ProjectSettings.Set(ProjectSettings.RenderingFallback, false); using var root = new Window { Size = new(16, 16) }; using var texture = new TextureRD(); var frames = 0;
        root.Ready += _ => { texture.TextureRDRID = Execute(RenderingServer.GetRenderingDevice()!, "glsl"); root.AddChild(new Patch(texture)); RenderingServer.FramePostDraw += Captured; };
        void Captured()
        {
            if (++frames < 3) return; RenderingServer.FramePostDraw -= Captured; using var image = root.GetTexture().GetImage()!; var pixel = image.GetPixel(8, 8); Check(Math.Abs(pixel.R - .6f) < .01f && Math.Abs(pixel.G - .24f) < .01f && Math.Abs(pixel.B - .3f) < .01f, "Graphics storage reaches retained canvas pixels."); root.Tree!.Quit();
        }
        Engine.Run(root); Console.WriteLine("PASS graphics storage reaches retained canvas pixels.");
    }
}
